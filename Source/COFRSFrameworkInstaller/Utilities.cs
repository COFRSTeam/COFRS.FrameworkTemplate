using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VSLangProj;

namespace COFRS.Template
{
	public static class Utilities
	{
        public static string LoadBaseFolder(string folder)
        {
			var files = Directory.GetFiles(folder, "*.csproj");

			if (files.Length > 0)
				return folder;

			foreach ( var childfolder in Directory.GetDirectories(folder))
            {
				if (!string.IsNullOrWhiteSpace(LoadBaseFolder(childfolder)))
					return childfolder;
            }

			return string.Empty;
        }

		private static string GetLocalFileName(string fileName, string rootFolder)
		{
			var files = Directory.GetFiles(rootFolder);

			foreach (var file in files)
			{
				if (file.ToLower().Contains(fileName))
					return file;
			}

			var childFolders = Directory.GetDirectories(rootFolder);

			foreach (var childFolder in childFolders)
			{
				var theFile = GetLocalFileName(fileName, childFolder);

				if (!string.IsNullOrWhiteSpace(theFile))
					return theFile;
			}


			return string.Empty;
		}

		public static List<ClassMember> LoadEntityClassMembers(string entityFileName, DBServerType serverType, List<DBColumn> columns)
		{
			List<ClassMember> members = new List<ClassMember>();
			string tableName = string.Empty;

			var entityContent = File.ReadAllText(entityFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			var columnName = string.Empty;

			foreach (var line in entityContent)
			{
				var entityName = string.Empty;

				//	Is this a table annotation?
				if (line.Trim().StartsWith("[table", StringComparison.OrdinalIgnoreCase))
				{
					var matcht = Regex.Match(line, "[Table([ \\t]*\\\"(?<tableName>[a-zA-Z0-9_]+)\\\"");

					//	If this was a table annotation, we now know the tablename the entity class was based upon
					if (matcht.Success)
						tableName = matcht.Groups["tableName"].Value;
				}

				//	Is this a member annotation?
				else if (line.Trim().StartsWith("[member", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "ColumnName[ \\t]*\\=[ \\t]*\\\"(?<columnName>[a-zA-Z0-9_]+)\\\"");

					//	If the entity specified a different column name than the member name, remember it.
					if (match.Success)
						columnName = match.Groups["columnName"].Value;
					else
						columnName = string.Empty;
				}

				//	Is this a member?
				else if (line.Trim().StartsWith("public", StringComparison.OrdinalIgnoreCase))
				{
					//	The following will recoginze these types of data types:
					//	
					//	Simple data types:  int, long, string, Guid, Datetime, etc.
					//	Typed data types:  List<T>, IEnumerable<int>
					//	Embedded Typed Data types: IEnumerable<ValueTuple<string, int>>

					var whitespace = "[ \\t]*";
					var space = "[ \\t]+";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";
					var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
					var match2 = Regex.Match(line, pattern);

					if (match2.Success)
					{
						//	Okay, we got a column. Get the member name can call it the entityName.
						entityName = match2.Groups["columnname"].Value;

						//	But if the previous annotation specified a column name, then use that instead
						var matchName = string.IsNullOrWhiteSpace(columnName) ? entityName : columnName;

						var column = columns.FirstOrDefault(c => string.Equals(c.ColumnName, matchName, StringComparison.OrdinalIgnoreCase));

						if (column != null)
						{
							column.EntityName = entityName;

							if (column.IsPrimaryKey)
							{
								var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "Href", StringComparison.OrdinalIgnoreCase));

								if (member == null)
								{
									member = new ClassMember()
									{
										ResourceMemberName = "Href",
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ColumnName = column.ColumnName,
									DataType = column.DataType,
									dbDataType = column.dbDataType,
									ForeignTableName = column.ForeignTableName,
									IsComputed = column.IsComputed,
									IsForeignKey = column.IsForeignKey,
									IsIdentity = column.IsIdentity,
									IsIndexed = column.IsIndexed,
									IsNullable = column.IsNullable,
									IsPrimaryKey = column.IsPrimaryKey,
									Length = column.Length
								};

								SetFixed(serverType, column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
							else if (column.IsForeignKey)
							{
								string shortColumnName;

								if (string.Equals(column.ForeignTableName, tableName, StringComparison.OrdinalIgnoreCase))
								{
									shortColumnName = column.ColumnName;
									if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
										shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
								}
								else
									shortColumnName = column.ForeignTableName;

								var normalizer = new NameNormalizer(shortColumnName);
								var domainName = normalizer.SingleForm;

								var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

								if (member == null)
								{
									member = new ClassMember()
									{
										ResourceMemberName = domainName,
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ColumnName = column.ColumnName,
									DataType = column.DataType,
									dbDataType = column.dbDataType,
									ForeignTableName = column.ForeignTableName,
									IsComputed = column.IsComputed,
									IsForeignKey = column.IsForeignKey,
									IsIdentity = column.IsIdentity,
									IsIndexed = column.IsIndexed,
									IsNullable = column.IsNullable,
									IsPrimaryKey = column.IsPrimaryKey,
									Length = column.Length
								};

								SetFixed(serverType, column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
							else
							{
								var normalizer = new NameNormalizer(column.EntityName);
								var domainName = normalizer.PluralForm;

								if (string.Equals(column.EntityName, normalizer.SingleForm, StringComparison.OrdinalIgnoreCase))
									domainName = normalizer.SingleForm;

								var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

								if (member == null)
								{
									ClassMember potentialMember = null;

									potentialMember = members.FirstOrDefault(m => domainName.Length > m.ResourceMemberName.Length ? string.Equals(m.ResourceMemberName, domainName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase) : false);

									if (potentialMember != null)
									{
										var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, domainName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

										if (childMember != null)
											member = childMember;
									}
								}

								if (member == null)
								{
									member = new ClassMember()
									{
										ResourceMemberName = domainName,
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ColumnName = column.ColumnName,
									DataType = column.DataType,
									dbDataType = column.dbDataType,
									ForeignTableName = column.ForeignTableName,
									IsComputed = column.IsComputed,
									IsForeignKey = column.IsForeignKey,
									IsIdentity = column.IsIdentity,
									IsIndexed = column.IsIndexed,
									IsNullable = column.IsNullable,
									IsPrimaryKey = column.IsPrimaryKey,
									Length = column.Length
								};

								SetFixed(serverType, column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
						}
					}
				}
			}

			return members;
		}

		public static List<ClassMember> LoadClassColumns(DBServerType serverType, string resourceFileName, string entityFileName, List<DBColumn> columns)
		{
			List<ClassMember> members = new List<ClassMember>();
			string tableName = string.Empty;

			var resourceContent = File.ReadAllText(resourceFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in resourceContent)
			{
				if (line.Trim().StartsWith("public", StringComparison.OrdinalIgnoreCase))
				{
					//	The following will recoginze these types of data types:
					//	
					//	Simple data types:  int, long, string, Guid, Datetime, etc.
					//	Typed data types:  List<T>, IEnumerable<int>
					//	Embedded Typed Data types: IEnumerable<ValueTuple<string, int>>

					var whitespace = "[ \\t]*";
					var space = "[ \\t]+";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";
					var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
					var match = Regex.Match(line, pattern);

					if (match.Success)
					{
						var memberName = match.Groups["columnname"].Value;
						var dataType = match.Groups["datatype"].Value;

						var member = new ClassMember()
						{
							ResourceMemberName = memberName,
							ResourceMemberType = dataType,
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						LoadChildMembers(Path.GetDirectoryName(resourceFileName), member);

						members.Add(member);
					}
				}
			}

			var entityContent = File.ReadAllText(entityFileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
			var columnName = string.Empty;

			foreach (var line in entityContent)
			{
				var entityName = string.Empty;

				if (line.Trim().StartsWith("[table", StringComparison.OrdinalIgnoreCase))
				{
					var matcht = Regex.Match(line, "[Table([ \\t]*\\\"(?<tableName>[a-zA-Z0-9_]+)\\\"");

					if (matcht.Success)
						tableName = matcht.Groups["tableName"].Value;
				}

				else if (line.Trim().StartsWith("[member", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "ColumnName[ \\t]*\\=[ \\t]*\\\"(?<columnName>[a-zA-Z0-9_]+)\\\"");

					if (match.Success)
						columnName = match.Groups["columnName"].Value;
					else
						columnName = string.Empty;
				}
				else if (line.Trim().StartsWith("public", StringComparison.OrdinalIgnoreCase))
				{
					var whitespace = "[ \\t]*";
					var space = "[ \\t]+";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";
					var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
					var match2 = Regex.Match(line, pattern);

					if (match2.Success)
					{
						entityName = match2.Groups["columnname"].Value;
						var matchName = string.IsNullOrWhiteSpace(columnName) ? entityName : columnName;
						var column = columns.FirstOrDefault(c => string.Equals(c.ColumnName, matchName, StringComparison.OrdinalIgnoreCase));

						if (column != null)
						{
							column.EntityName = entityName;

							if (column.IsPrimaryKey)
							{
								var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, "href", StringComparison.OrdinalIgnoreCase));

								if (member == null)
								{
									member = new ClassMember()
									{
										ResourceMemberName = string.Empty,
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ColumnName = column.ColumnName,
									DataType = column.DataType,
									dbDataType = column.dbDataType,
									ForeignTableName = column.ForeignTableName,
									IsComputed = column.IsComputed,
									IsForeignKey = column.IsForeignKey,
									IsIdentity = column.IsIdentity,
									IsIndexed = column.IsIndexed,
									IsNullable = column.IsNullable,
									IsPrimaryKey = column.IsPrimaryKey,
									Length = column.Length
								};

								SetFixed(serverType, column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
							else if (column.IsForeignKey)
							{
								string shortColumnName;

								if (string.Equals(column.ForeignTableName, tableName, StringComparison.OrdinalIgnoreCase))
								{
									shortColumnName = column.ColumnName;
									if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
										shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
								}
								else
									shortColumnName = column.ForeignTableName;

								var normalizer = new NameNormalizer(shortColumnName);
								var domainName = normalizer.SingleForm;

								var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, domainName, StringComparison.OrdinalIgnoreCase));

								if (member == null)
								{
									member = new ClassMember()
									{
										ResourceMemberName = string.Empty,
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ColumnName = column.ColumnName,
									DataType = column.DataType,
									dbDataType = column.dbDataType,
									ForeignTableName = column.ForeignTableName,
									IsComputed = column.IsComputed,
									IsForeignKey = column.IsForeignKey,
									IsIdentity = column.IsIdentity,
									IsIndexed = column.IsIndexed,
									IsNullable = column.IsNullable,
									IsPrimaryKey = column.IsPrimaryKey,
									Length = column.Length
								};

								SetFixed(serverType, column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
							else
							{
								var member = members.FirstOrDefault(m => string.Equals(m.ResourceMemberName, column.EntityName, StringComparison.OrdinalIgnoreCase));

								if (member == null)
								{
									var potentialMember = members.FirstOrDefault(m => column.EntityName.Length > m.ResourceMemberName.Length ? string.Equals(m.ResourceMemberName, column.EntityName.Substring(0, m.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase) : false);

									if (potentialMember != null)
									{
										var childMember = potentialMember.ChildMembers.FirstOrDefault(c => string.Equals(c.ResourceMemberName, column.EntityName.Substring(potentialMember.ResourceMemberName.Length), StringComparison.OrdinalIgnoreCase));

										if (childMember != null)
											member = childMember;
									}
								}

								if (member == null)
								{
									member = new ClassMember()
									{
										ResourceMemberName = string.Empty,
										ResourceMemberType = string.Empty,
										EntityNames = new List<DBColumn>(),
										ChildMembers = new List<ClassMember>()
									};

									members.Add(member);
								}

								var entityColumn = new DBColumn()
								{
									EntityName = column.EntityName,
									EntityType = match2.Groups["datatype"].Value,
									ColumnName = column.ColumnName,
									DataType = column.DataType,
									dbDataType = column.dbDataType,
									ForeignTableName = column.ForeignTableName,
									IsComputed = column.IsComputed,
									IsForeignKey = column.IsForeignKey,
									IsIdentity = column.IsIdentity,
									IsIndexed = column.IsIndexed,
									IsNullable = column.IsNullable,
									IsPrimaryKey = column.IsPrimaryKey,
									Length = column.Length
								};

								SetFixed(serverType, column, entityColumn);
								member.EntityNames.Add(entityColumn);
							}
						}
					}
				}
			}

			return members;
		}

		private static void SetFixed(DBServerType serverType, DBColumn column, DBColumn entityColumn)
		{
			if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NVarChar)
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Binary) ||
					  (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Binary))
			{
				entityColumn.IsFixed = true;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Char) ||
					  (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Char))
			{
				entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NChar)
			{
				entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.String)
			{
				if (column.Length > 1)
					entityColumn.IsFixed = true;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Image)
			{
				entityColumn.IsFixed = false;
				entityColumn.Length = -1;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.NText)
			{
				entityColumn.IsFixed = true;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Text) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Text) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.Text) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.MediumText) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.LongText) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.TinyText))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarBinary) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Bytea) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarBinary))
			{
				entityColumn.IsFixed = false;
			}
			else if ((serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.VarChar) ||
					 (serverType == DBServerType.POSTGRESQL && (NpgsqlDbType)column.DataType == NpgsqlDbType.Varchar) ||
					 (serverType == DBServerType.MYSQL && (MySqlDbType)column.DataType == MySqlDbType.VarChar))
			{
				entityColumn.IsFixed = false;
			}
			else if (serverType == DBServerType.SQLSERVER && (SqlDbType)column.DataType == SqlDbType.Timestamp)
			{
				entityColumn.IsFixed = true;
			}
		}

		private static void LoadChildMembers(string folder, ClassMember member)
		{
			string memberProperName = string.Empty;

			if (member.ResourceMemberType.Contains("<"))
				return;

			if (member.ResourceMemberType.Contains(">"))
				return;

			if (member.ResourceMemberType.EndsWith("?"))
				memberProperName = member.ResourceMemberType.Substring(0, member.ResourceMemberType.Length - 1);
			else
				memberProperName = member.ResourceMemberType;

			var fileName = FindFile(folder, memberProperName + ".cs");

			if (!string.IsNullOrWhiteSpace(fileName))
			{
				var childContent = File.ReadAllText(fileName).Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in childContent)
				{
					var whitespace = "[ \\t]*";
					var space = "[ \\t]+";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";
					var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
					var match = Regex.Match(line, pattern);
					if (match.Success)
					{
						var memberName = match.Groups["columnname"].Value;
						var dataType = match.Groups["datatype"].Value;

						var childMember = new ClassMember()
						{
							ResourceMemberName = memberName,
							ResourceMemberType = dataType,
							EntityNames = new List<DBColumn>(),
							ChildMembers = new List<ClassMember>()
						};

						LoadChildMembers(folder, childMember);

						member.ChildMembers.Add(childMember);
					}
				}
			}
		}

		private static string FindFile(string folder, string fileName)
		{
			var fullPath = Path.Combine(folder, fileName);

			if (File.Exists(fullPath))
				return fullPath;

			foreach (var subfolder in Directory.GetDirectories(folder))
			{
				var childName = FindFile(subfolder, fileName);

				if (!string.IsNullOrWhiteSpace(childName))
					return childName;
			}

			return string.Empty;
		}

		public static void LoadClassList(string SolutionFolder, string resourceClass,
											ref ResourceClassFile Orchestrator,
											ref ResourceClassFile ValidatorClass,
											ref ResourceClassFile ExampleClass,
											ref ResourceClassFile CollectionExampleClass)
		{
			try
			{
				foreach (var file in Directory.GetFiles(SolutionFolder, "*.cs"))
				{
					LoadDomainClass(file, resourceClass, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
				}

				foreach (var folder in Directory.GetDirectories(SolutionFolder))
				{
					LoadDomainList(folder, resourceClass, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private static void LoadDomainList(string folder, string DomainClassName, ref ResourceClassFile Orchestrator,
											ref ResourceClassFile ValidatorClass,
											ref ResourceClassFile ExampleClass,
											ref ResourceClassFile CollectionExampleClass)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "*.cs"))
				{
					LoadDomainClass(file, DomainClassName, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					LoadDomainList(subfolder, DomainClassName, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}


		private static void LoadDomainClass(string file, string resourceClassName, ref ResourceClassFile Orchestrator,
											ref ResourceClassFile ValidatorClass,
											ref ResourceClassFile ExampleClass,
											ref ResourceClassFile CollectionExampleClass)
		{
			try
			{
				var data = File.ReadAllText(file).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); ;
				var className = string.Empty;
				var baseClassName = string.Empty;
				var namespaceName = string.Empty;

				foreach (var line in data)
				{
					var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)([ \t]*\\:[ \t]*(?<baseClass>[A-Za-z][A-Za-z0-9_\\<\\>]*))*");

					if (match.Success)
					{
						className = match.Groups["className"].Value;
						baseClassName = match.Groups["baseClass"].Value;
					}

					match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)*)");

					if (match.Success)
						namespaceName = match.Groups["namespaceName"].Value;

					if (!string.IsNullOrWhiteSpace(className) &&
						!string.IsNullOrWhiteSpace(namespaceName))
					{
						var classfile = new ResourceClassFile
						{
							ClassName = $"{className}",
							FileName = file,
							EntityClass = string.Empty,
							ClassNamespace = namespaceName
						};

						if (string.Equals(classfile.ClassName, "ServiceOrchestrator", StringComparison.OrdinalIgnoreCase))
							Orchestrator = classfile;

						if (!string.IsNullOrWhiteSpace(baseClassName) &&
							string.Equals(baseClassName, $"Validator<{resourceClassName}>", StringComparison.OrdinalIgnoreCase))
							ValidatorClass = classfile;

						if (!string.IsNullOrWhiteSpace(baseClassName) &&
							string.Equals(baseClassName, $"IExamplesProvider<{resourceClassName}>", StringComparison.OrdinalIgnoreCase))
							ExampleClass = classfile;

						if (!string.IsNullOrWhiteSpace(baseClassName) &&
							string.Equals(baseClassName, $"IExamplesProvider<RqlCollection<{resourceClassName}>>", StringComparison.OrdinalIgnoreCase))
							CollectionExampleClass = classfile;
					}

					className = string.Empty;
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		public static List<ResourceClassFile> LoadResourceClassList(string folder)
		{
			var theList = new List<ResourceClassFile>();

			foreach (var file in Directory.GetFiles(folder, "*.cs"))
			{
				var classFile = LoadResourceClass(file);

				if (classFile != null)
					theList.Add(classFile);
			}

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				theList.AddRange(LoadResourceClassList(childFolder));
			}

			return theList;
		}

		public static ResourceClassFile LoadResourceClass(string file)
		{
			var content = File.ReadAllText(file);
			var namespaceName = string.Empty;
			var entityClass = string.Empty;

			if (content.Contains("[Entity"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[Entity"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[Entity[ \t]*\\([ \t]*typeof\\((?<entityClass>[A-Za-z][A-Za-z0-9_]*[ \t]*)\\)[ \t]*\\)[ \t]*\\]");

						if (match.Success)
						{
							entityClass = match.Groups["entityClass"].Value;
						}
					}
					else if (line.Contains("class"))
					{
						var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new ResourceClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								EntityClass = entityClass,
								ClassNamespace = namespaceName
							};

							return classfile;
						}
					}
				}
			}

			return null;
		}

		public static List<EntityDetailClassFile> LoadEntityClassList(string folder)
		{
			var theList = new List<EntityDetailClassFile>();

			foreach (var file in Directory.GetFiles(folder, "*.cs"))
			{
				var classFile = LoadEntityClass(file);

				if (classFile != null)
					theList.Add(classFile);
			}

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				theList.AddRange(LoadEntityClassList(childFolder));
			}

			return theList;
		}

		public static List<EntityDetailClassFile> LoadDetailEntityClassList(string folder, string connectionString)
		{
			var theList = new List<EntityDetailClassFile>();

			foreach (var file in Directory.GetFiles(folder, "*.cs"))
			{
				var classFile = LoadDetailEntityClass(file, connectionString);

				if (classFile != null)
					theList.Add(classFile);
			}

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				theList.AddRange(LoadDetailEntityClassList(childFolder, connectionString));
			}

			return theList;
		}

		public static EntityDetailClassFile LoadEntityClass(string file)
		{
			var content = File.ReadAllText(file);
			var namespaceName = string.Empty;
			var schemaName = string.Empty;
			var tableName = string.Empty;

			if (content.Contains("[Table"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[Table"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[Table[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("class"))
					{
						var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Table
							};

							return classfile;
						}
					}
				}
			}
			else if (content.Contains("[PgEnum"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[PgEnum"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[PgEnum[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("enum"))
					{
						var match = Regex.Match(line, "enum[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Enum
							};

							return classfile;
						}
					}
				}
			}
			else if (content.Contains("[PgComposite"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[PgComposite"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[PgComposite[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("class"))
					{
						var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Composite
							};

							return classfile;
						}
					}
				}
			}

			return null;
		}


		private static EntityDetailClassFile LoadDetailEntityClass(string file, string ConnectionString)
		{
			var content = File.ReadAllText(file);
			var namespaceName = string.Empty;
			var schemaName = string.Empty;
			var tableName = string.Empty;

			if (content.Contains("[Table"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[Table"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[Table[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("class"))
					{
						var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Table,
							};

							LoadColumns(ConnectionString, classfile);

							return classfile;
						}
					}
				}
			}
			else if (content.Contains("[PgEnum"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[PgEnum"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[PgEnum[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("enum"))
					{
						var match = Regex.Match(line, "enum[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Enum
							};

							LoadEnumColumns(classfile);

							return classfile;
						}
					}
				}
			}
			else if (content.Contains("[PgComposite"))
			{
				var data = content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

				foreach (var line in data)
				{
					if (line.Contains("namespace"))
					{
						var match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

						if (match.Success)
						{
							namespaceName = match.Groups["namespaceName"].Value;
						}
					}
					else if (line.Contains("[PgComposite"))
					{
						// 	[Table("Products", Schema = "dbo")]
						var match = Regex.Match(line, "\\[PgComposite[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						if (match.Success)
						{
							tableName = match.Groups["tableName"].Value;
							schemaName = match.Groups["schemaName"].Value;
						}
					}
					else if (line.Contains("class"))
					{
						var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

						if (match.Success)
						{
							var classfile = new EntityDetailClassFile
							{
								ClassName = match.Groups["className"].Value,
								FileName = file,
								TableName = tableName,
								SchemaName = schemaName,
								ClassNameSpace = namespaceName,
								ElementType = ElementType.Composite,
							};

							LoadColumns(ConnectionString, classfile);

							return classfile;
						}
					}
				}
			}

			return null;
		}

		private static void LoadEnumColumns(EntityDetailClassFile classFile)
		{
			var contents = File.ReadAllText(classFile.FileName);
			classFile.Columns = new List<DBColumn>();
			string entityName = string.Empty;
			bool foundPgName = false;

			var lines = contents.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				if (line.Trim().StartsWith("[PgName", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "\\[PgName[ \\t]*\\([ \\t]*\\\"(?<columnName>[a-zA-Z_][a-zA-Z0-9_]*)\\\"\\)\\]");

					//	If the entity specified a different column name than the member name, remember it.
					if (match.Success)
					{
						entityName = match.Groups["columnName"].Value;
						foundPgName = true;
					}
				}
				else if (foundPgName)
				{
					var match = Regex.Match(line, "[ \\t]*(?<classname>[a-zA-Z_][a-zA-Z0-9_]*)");

					if (match.Success)
					{
						var dbColumn = new DBColumn()
						{
							ColumnName = match.Groups["classname"].Value,
							EntityName = entityName
						};

						classFile.Columns.Add(dbColumn);

						foundPgName = false;
					}
				}
			}
		}

		private static void LoadColumns(string connectionString, EntityDetailClassFile classFile)
		{
			if (File.Exists(classFile.FileName))
			{
				var contents = File.ReadAllText(classFile.FileName);
				classFile.Columns = new List<DBColumn>();

				var lines = contents.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				var tableName = classFile.TableName;
				var schema = classFile.SchemaName;
				var entityName = string.Empty;

				foreach (var line in lines)
				{
					//	Is this a member annotation?
					if (line.Trim().StartsWith("[PgName", StringComparison.OrdinalIgnoreCase))
					{
						var match = Regex.Match(line, "\\[PgName[ \\t]*\\([ \\t]*\\\"(?<columnName>[a-zA-Z_][a-zA-Z0-9_]*)\\\"\\)\\]");

						//	If the entity specified a different column name than the member name, remember it.
						if (match.Success)
							entityName = match.Groups["columnName"].Value;
					}

					//	Is this a member?
					else if (line.Trim().StartsWith("public", StringComparison.OrdinalIgnoreCase))
					{
						//	The following will recoginze these types of data types:
						//	
						//	Simple data types:  int, long, string, Guid, Datetime, etc.
						//	Typed data types:  List<T>, IEnumerable<int>
						//	Embedded Typed Data types: IEnumerable<ValueTuple<string, int>>

						var whitespace = "[ \\t]*";
						var space = "[ \\t]+";
						var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
						var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
						var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
						var typedecl = $"{variableName}(({singletype})|({multitype}))*";
						var pattern = $"{whitespace}public{space}(?<datatype>{typedecl})[ \\t]+(?<columnname>{variableName})[ \\t]+{{{whitespace}get{whitespace}\\;{whitespace}set{whitespace}\\;{whitespace}\\}}";
						var match2 = Regex.Match(line, pattern);

						if (match2.Success)
						{
							//	Okay, we got a column. Get the member name can call it the entityName.
							var className = match2.Groups["columnname"].Value;

							if (string.IsNullOrWhiteSpace(entityName))
								entityName = className;

							var entityColumn = new DBColumn()
							{
								ColumnName = className,
								EntityName = entityName,
								EntityType = match2.Groups["datatype"].Value
							};

							classFile.Columns.Add(entityColumn);

							entityName = string.Empty;
						}
					}
				}
			}
			else
            {
				classFile.Columns = new List<DBColumn>();
            }

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();

				var query = @"
select a.attname as columnname,
	   t.typname as datatype,
	   case when t.typname = 'varchar' then a.atttypmod-4
	        when t.typname = 'bpchar' then a.atttypmod-4
			when t.typname = '_varchar' then a.atttypmod-4
			when t.typname = '_bpchar' then a.atttypmod-4
	        when a.atttypmod > -1 then a.atttypmod
	        else a.attlen end as max_len,
	   not a.attnotnull as is_nullable,

	   case when ( a.attgenerated = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_computed,

	   case when ( a.attidentity = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_identity,

	   case when (select indrelid from pg_index as px where px.indisprimary = true and px.indrelid = c.oid and a.attnum = ANY(px.indkey)) = c.oid then true else false end as is_primary,
	   case when (select indrelid from pg_index as ix where ix.indrelid = c.oid and a.attnum = ANY(ix.indkey)) = c.oid then true else false end as is_indexed,
	   case when (select conrelid from pg_constraint as cx where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) = c.oid then true else false end as is_foreignkey,
       (  select cc.relname from pg_constraint as cx inner join pg_class as cc on cc.oid = cx.confrelid where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) as foeigntablename
  from pg_class as c
  inner join pg_namespace as ns on ns.oid = c.relnamespace
  inner join pg_attribute as a on a.attrelid = c.oid and not a.attisdropped and attnum > 0
  inner join pg_type as t on t.oid = a.atttypid
  left outer join pg_attrdef as ad on ad.adrelid = a.attrelid and ad.adnum = a.attnum 
  where ns.nspname = @schema
    and c.relname = @tablename
 order by a.attnum
";

				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@schema", classFile.SchemaName);
					command.Parameters.AddWithValue("@tablename", classFile.TableName);
					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							NpgsqlDbType dataType = NpgsqlDbType.Unknown;

							try
							{
								dataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1));
							}
							catch (InvalidCastException)
							{
							}

							var dbColumn = classFile.Columns.FirstOrDefault(c => string.Equals(c.EntityName, reader.GetString(0), StringComparison.OrdinalIgnoreCase));
							
							if ( dbColumn == null )
                            {
								dbColumn = new DBColumn();
								dbColumn.EntityName = reader.GetString(0);
								dbColumn.ColumnName = StandardUtils.NormalizeClassName(reader.GetString(0));
								classFile.Columns.Add(dbColumn);
							}
							
							dbColumn.DataType = dataType;
							dbColumn.dbDataType = reader.GetString(1);
							dbColumn.Length = Convert.ToInt64(reader.GetValue(2));
							dbColumn.IsNullable = Convert.ToBoolean(reader.GetValue(3));
							dbColumn.IsComputed = Convert.ToBoolean(reader.GetValue(4));
							dbColumn.IsIdentity = Convert.ToBoolean(reader.GetValue(5));
							dbColumn.IsPrimaryKey = Convert.ToBoolean(reader.GetValue(6));
							dbColumn.IsIndexed = Convert.ToBoolean(reader.GetValue(7));
							dbColumn.IsForeignKey = Convert.ToBoolean(reader.GetValue(8));
							dbColumn.ForeignTableName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9);
						}
					}
				}
			}
		}
	}
}
