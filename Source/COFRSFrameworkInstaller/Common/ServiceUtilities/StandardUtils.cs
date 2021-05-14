using COFRS.Template.Common.Models;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VSLangProj;

namespace COFRS.Template.Common.ServiceUtilities
{
    public static class StandardUtils
    {
		public static string CorrectForReservedNames(string columnName)
		{
			if (string.Equals(columnName, "abstract", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "as", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "base", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "bool", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "break", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "byte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "case", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "catch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "char", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "checked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "class", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "const", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "continue", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "decimal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "default", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "delegate", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "do", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "double", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "else", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "enum", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "event", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "explicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "extern", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "false", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "finally", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "fixed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "float", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "for", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "foreach", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "goto", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "if", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "implicit", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "in", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "int", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "interface", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "internal", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "is", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "lock", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "long", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "namespace", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "new", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "null", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "object", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "operator", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "out", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "override", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "params", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "private", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "protected", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "public", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "readonly", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ref", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "return", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sbyte", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sealed", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "short", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "sizeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "stackalloc", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "static", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "string", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "struct", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "switch", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "this", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "throw", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "true", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "try", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "typeof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "uint", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ulong", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unchecked", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unsafe", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ushort", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "using", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "virtual", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "void", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "volatile", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "while", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "add", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "alias", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "ascending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "async", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "await", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "by", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "descending", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "dynamic", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "equals", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "from", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "get", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "global", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "group", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "into", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "join", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "let", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "nameof", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "on", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "orderby", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "partial", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "remove", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "select", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "set", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "unmanaged", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "var", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "when", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "where", StringComparison.OrdinalIgnoreCase) ||
				 string.Equals(columnName, "yield", StringComparison.OrdinalIgnoreCase))
			{
				return $"{columnName}_Value";
			}

			return columnName;
		}

		public static string NormalizeClassName(string className)
		{
			var normalizedName = new StringBuilder();
			var indexStart = 1;

			while (className.EndsWith("_") && className.Length > 1)
				className = className.Substring(0, className.Length - 1);

			while (className.StartsWith("_") && className.Length > 1)
				className = className.Substring(1);

			if (className == "_")
				return className;

			normalizedName.Append(className.Substring(0, 1).ToUpper());

			int index = className.IndexOf("_");

			while (index != -1)
			{
				//	0----*----1----*----2
				//	street_address_1

				normalizedName.Append(className.Substring(indexStart, index - indexStart));
				normalizedName.Append(className.Substring(index + 1, 1).ToUpper());
				indexStart = index + 2;

				if (indexStart >= className.Length)
					index = -1;
				else
					index = className.IndexOf("_", indexStart);
			}

			if (indexStart < className.Length)
				normalizedName.Append(className.Substring(indexStart));

			return normalizedName.ToString();
		}

		public static List<EntityDetailClassFile> GenerateDetailEntityClassList(List<EntityDetailClassFile> UndefinedClassList, List<EntityDetailClassFile> DefinedClassList, string baseFolder, string connectionString)
		{
			List<EntityDetailClassFile> resultList = new List<EntityDetailClassFile>();

			foreach (var classFile in UndefinedClassList)
			{
				var newClassFile = GenerateDetailEntityClass(classFile, connectionString);
				resultList.Add(newClassFile);

				if (newClassFile.ElementType != ElementType.Enum)
				{
					foreach (var column in newClassFile.Columns)
					{
						if ((NpgsqlDbType)column.DataType == NpgsqlDbType.Unknown)
						{
							if (DefinedClassList.FirstOrDefault(c => string.Equals(c.TableName, column.EntityName, StringComparison.OrdinalIgnoreCase)) == null)
							{
								var aList = new List<EntityDetailClassFile>();
								var bList = new List<EntityDetailClassFile>();

								var aClassFile = new EntityDetailClassFile()
								{
									ClassName = CorrectForReservedNames(NormalizeClassName(column.EntityName)),
									TableName = column.EntityName,
									SchemaName = classFile.SchemaName,
									FileName = Path.Combine(baseFolder, $"{CorrectForReservedNames(NormalizeClassName(column.EntityName))}.cs"),
									ClassNameSpace = classFile.ClassNameSpace,
									ElementType = DBHelper.GetElementType(classFile.SchemaName, column.EntityName, DefinedClassList, connectionString)
								};
								aList.Add(aClassFile);
								bList.AddRange(DefinedClassList);
								bList.AddRange(UndefinedClassList);

								resultList.AddRange(GenerateDetailEntityClassList(aList, bList, baseFolder, connectionString));
							}
						}
					}
				}
			}

			return resultList;
		}

		private static EntityDetailClassFile GenerateDetailEntityClass(EntityDetailClassFile classFile, string connectionString)
		{
			classFile.ElementType = DBHelper.GetElementType(classFile.SchemaName, classFile.TableName, null, connectionString);

			if (classFile.ElementType == ElementType.Enum)
				GenerateEnumColumns(connectionString, classFile);
			else
				GenerateColumns(connectionString, classFile);

			return classFile;
		}

		private static void GenerateEnumColumns(string connectionString, EntityDetailClassFile classFile)
		{
			classFile.Columns = new List<DBColumn>();
			string query = @"
select e.enumlabel as enum_value
from pg_type t 
   join pg_enum e on t.oid = e.enumtypid  
   join pg_catalog.pg_namespace n ON n.oid = t.typnamespace
where t.typname = @dataType
  and n.nspname = @schema";

			using (var connection = new NpgsqlConnection(connectionString))
			{
				connection.Open();
				using (var command = new NpgsqlCommand(query, connection))
				{
					command.Parameters.AddWithValue("@dataType", classFile.TableName);
					command.Parameters.AddWithValue("@schema", classFile.SchemaName);

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							var element = reader.GetString(0);
							var elementName = StandardUtils.NormalizeClassName(element);

							var column = new DBColumn()
							{
								ColumnName = elementName,
								EntityName = element
							};

							classFile.Columns.Add(column);
						}
					}
				}
			}
		}

		private static void GenerateColumns(string connectionString, EntityDetailClassFile classFile)
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

							if (dbColumn == null)
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



		#region Solution functions
		/// <summary>
		/// Loads all the entity models in a solution
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static List<EntityDetailClassFile> LoadEntityDetailClassList(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var classList = new List<EntityDetailClassFile>();

			foreach (Project project in solution.Projects)
			{
				if (project.Kind == PrjKind.prjKindCSharpProject)
				{
					classList.AddRange(ScanProject(project.ProjectItems));
				}
			}

			return classList;
		}

		/// <summary>
		/// Loads all the entity models in a project
		/// </summary>
		/// <param name="projectItems"></param>
		/// <returns></returns>
		private static List<EntityDetailClassFile> ScanProject(ProjectItems projectItems)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var results = new List<EntityDetailClassFile>();

			foreach (ProjectItem projectItem in projectItems)
			{
				if (projectItem.FileCodeModel != null)
				{
					int buildAction = Convert.ToInt32(projectItem.Properties.Item("BuildAction").Value);

                    if (projectItem.Name.Contains(".cs") && buildAction == 1)
                        results.AddRange(LoadEntityFile(projectItem));
                }
				else
				{
					results.AddRange(ScanProject(projectItem.ProjectItems));
				}
			}

			return results;
		}

		/// <summary>
		/// Load all entity models in a file
		/// </summary>
		/// <param name="projectItem"></param>
		/// <returns></returns>
		private static List<EntityDetailClassFile> LoadEntityFile(ProjectItem projectItem)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new List<EntityDetailClassFile>();

			//	Open file and save context
			var wasOpen = projectItem.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				projectItem.Open(Constants.vsViewKindCode);

			var doc = projectItem.Document;
			var sel = (TextSelection)doc.Selection;

			var anchorPoint = sel.AnchorPoint;
			var activePoint = sel.ActivePoint;

			//	Get the list of namespaces in the file
			var namespaceList = LoadSnipit(sel, SnipitType.TYPE_NAMESPACE);
			var compositeList = LoadSnipit(sel, SnipitType.TYPE_COMPOSITE);
			var tableList = LoadSnipit(sel, SnipitType.TYPE_TABLE);
			var enumList = LoadSnipit(sel, SnipitType.TYPE_ENUM);

			foreach (var ns in namespaceList)
			{
				foreach (var cl in compositeList.Where(c => c.Start < ns.End))
				{
					var classEntity = ParseClass(projectItem, ns, cl, sel);

					if (classEntity != null)
						results.Add(classEntity);
				}

				foreach (var cl in tableList.Where(c => c.Start < ns.End))
				{
					var classEntity = ParseClass(projectItem, ns, cl, sel);

					if (classEntity != null)
						results.Add(classEntity);
				}

				foreach (var en in enumList.Where(e => e.Start < ns.End))
				{
					var enumEntity = ParseEnum(projectItem, ns, en, sel);

					if ( enumEntity != null )
						results.Add(enumEntity);
				}
			}

			return results;
		}

		public static EntityDetailClassFile ParseClass(ProjectItem projectItem, Snipit ns, Snipit cl, TextSelection sel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var entity = new EntityDetailClassFile();

			sel.GotoLine(cl.Start);
			sel.SelectLine();

			var match = Regex.Match(sel.Text, "\\[PgComposite[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");
			
			if (match.Success)
			{
				bool notFound = true;

				while (notFound)
				{
					sel.SelectLine();
					var match2 = Regex.Match(sel.Text, "class[ \t]+(?<classname>[_a-zA-Z][_a-zA-Z0-9]*)");

					if (match2.Success)
					{
						notFound = false;
						entity.ClassName = match2.Groups["classname"].Value;
					}
				}

				entity.FileName = projectItem.FileNames[0];
				entity.TableName = match.Groups["tableName"].Value;
				entity.SchemaName = match.Groups["schemaName"].Value;
				entity.ClassNameSpace = ns.Name;
				entity.ElementType = ElementType.Composite;
				entity.Columns = LoadColumns(DBServerType.POSTGRESQL, ns, cl, sel);
			}
			else 
			{
				match = Regex.Match(sel.Text, "\\[Table[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}([ \t]*\\,[ \t]*DBType[ \t]*=[ \t]*\"(?<dbtype>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

				if (match.Success)
				{
					bool notFound = true;

					while (notFound)
					{
						sel.SelectLine();
						var match2 = Regex.Match(sel.Text, "class[ \t]+(?<classname>[_a-zA-Z][_a-zA-Z0-9]*)");

						if ( match2.Success )
                        {
							notFound = false;
							entity.ClassName = match2.Groups["classname"].Value;
                        }
					}

					entity.FileName = projectItem.FileNames[0];
					entity.TableName = match.Groups["tableName"].Value;
					entity.SchemaName = match.Groups["schemaName"].Value;
					entity.ClassNameSpace = ns.Name;
					entity.ElementType = ElementType.Composite;
					entity.Columns = LoadColumns((DBServerType) Enum.Parse(typeof(DBServerType), match.Groups["dbtype"].Value), ns, cl, sel);
				}
				else 
				{
					return null;
				}
			}

			return entity;
        }

		public static EntityDetailClassFile ParseEnum(ProjectItem projectItem, Snipit ns, Snipit cl, TextSelection sel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var entity = new EntityDetailClassFile();

			sel.GotoLine(cl.Start);
			sel.SelectLine();

			var match = Regex.Match(sel.Text, "\\[PgEnum[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

			if (match.Success)
			{
				bool notFound = true;

				while (notFound)
				{
					sel.LineDown();
					sel.SelectLine();
					var match2 = Regex.Match(sel.Text, "enum[ \t]+(?<classname>[_a-zA-Z][_a-zA-Z0-9]*)");

					if (match2.Success)
					{
						notFound = false;
						entity.ClassName = match2.Groups["classname"].Value;
					}
				}

				entity.FileName = projectItem.FileNames[0];
				entity.TableName = match.Groups["tableName"].Value;
				entity.SchemaName = match.Groups["schemaName"].Value;
				entity.ClassNameSpace = ns.Name;
				entity.ElementType = ElementType.Enum;
			}
			else
				return null;


			return entity;
		}

        private static List<Snipit> LoadSnipit(TextSelection sel, SnipitType codeType)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var codeList = new List<Snipit>();
            sel.StartOfDocument();
            bool found = true;

			if (codeType == SnipitType.TYPE_NAMESPACE)
			{
				while (found)
				{
					if (sel.FindText("namespace"))
					{
						sel.SelectLine();
						var match = Regex.Match(sel.Text, $"namespace[ \t]+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)");

						var entry = new Snipit
						{
							Name = match.Groups["name"].Value,
							Start = sel.ActivePoint.Line - 1,
							End = int.MaxValue
						};

						if (codeList.Count > 0)
						{
							var prev = codeList[codeList.Count - 1];
							prev.End = sel.ActivePoint.Line - 1;
						}

						codeList.Add(entry);
					}
					else
						found = false;
				}
			}
			else if (codeType == SnipitType.TYPE_COMPOSITE)
			{
				while (found)
				{
					if (sel.FindText("[PgComposite"))
					{
						sel.SelectLine();
						var match = Regex.Match(sel.Text, "\\[PgComposite[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						var entry = new Snipit
						{
							Name = match.Groups["tableName"].Value,
							Start = sel.ActivePoint.Line - 1,
							End = int.MaxValue
						};

						if (codeList.Count > 0)
						{
							var prev = codeList[codeList.Count - 1];
							prev.End = sel.ActivePoint.Line - 1;
						}

						codeList.Add(entry);
					}
					else
						found = false;
				}
			}
			else if (codeType == SnipitType.TYPE_TABLE)
			{
				while (found)
				{
					if (sel.FindText("[Table"))
					{
						sel.SelectLine();
						var match = Regex.Match(sel.Text, "\\[Table[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}([ \t]*\\,[ \t]*DBType[ \t]*=[ \t]*\"(?<dbtype>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						var entry = new Snipit
						{
							Name = match.Groups["tableName"].Value,
							Start = sel.ActivePoint.Line - 1,
							End = int.MaxValue
						};

						if (codeList.Count > 0)
						{
							var prev = codeList[codeList.Count - 1];
							prev.End = sel.ActivePoint.Line - 1;
						}

						codeList.Add(entry);
					}
					else
						found = false;
				}
			}
			else if (codeType == SnipitType.TYPE_ENUM)
			{
				while (found)
				{
					if (sel.FindText("[PgEnum"))
					{
						sel.SelectLine();
						var match = Regex.Match(sel.Text, "\\[PgEnum[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

						var entry = new Snipit
						{
							Name = match.Groups["tableName"].Value,
							Start = sel.ActivePoint.Line - 1,
							End = int.MaxValue
						};

						if (codeList.Count > 0)
						{
							var prev = codeList[codeList.Count - 1];
							prev.End = sel.ActivePoint.Line - 1;
						}

						codeList.Add(entry);
					}
					else
						found = false;
				}
			}

			return codeList;
        }

        private static List<DBColumn> LoadColumns(DBServerType dbType, Snipit ns, Snipit cl, TextSelection sel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			sel.SelectAll();
			var lines = sel.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
			var columns = new List<DBColumn>();

			var entityName = string.Empty;
			var isPrimaryKey = false;
			var isAutoField = false;
			var isIdentity = false;
			var isIndexed = false;
			var isNullable = false;
			var isForeignKey = false;
			var isFixed = false;
			string nativeDataType = string.Empty;
			long dataLength = 0;
			int precision = 0;
			int scale = 0;

			int start = cl.Start;
			int end = cl.End == int.MaxValue ? lines.Count() - 1 : cl.End;
			
			for ( int index = start-1; index < end; index++)
			{
				var line = lines[index];

				//	Is this a member annotation?
				if (line.Trim().StartsWith("[PgName", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "\\[PgName[ \\t]*\\([ \\t]*\\\"(?<columnName>[a-zA-Z_][a-zA-Z0-9_]*)\\\"\\)\\]");

					//	If the entity specified a different column name than the member name, remember it.
					if (match.Success)
						entityName = match.Groups["columnName"].Value;
				}

				else if (line.Trim().StartsWith("[Member", StringComparison.OrdinalIgnoreCase))
				{
					var match = Regex.Match(line, "IsPrimaryKey[ \t]*=[ \t]*(?<boolValue>true|false)");

					if (match.Success)
						isPrimaryKey = bool.Parse(match.Groups["boolValue"].Value);

					match = Regex.Match(line, "AutoField[ \t]*=[ \t]*(?<boolValue>true|false)");

					if (match.Success)
						isAutoField = bool.Parse(match.Groups["boolValue"].Value);

					match = Regex.Match(line, "IsIdentity[ \t]*=[ \t]*(?<boolValue>true|false)");

					if (match.Success)
						isIdentity = bool.Parse(match.Groups["boolValue"].Value);

					match = Regex.Match(line, "IsIndexed[ \t]*=[ \t]*(?<boolValue>true|false)");

					if (match.Success)
						isIndexed = bool.Parse(match.Groups["boolValue"].Value);

					match = Regex.Match(line, "IsForeignKey[ \t]*=[ \t]*(?<boolValue>true|false)");

					if (match.Success)
						isForeignKey = bool.Parse(match.Groups["boolValue"].Value);

					match = Regex.Match(line, "IsNullable[ \t]*=[ \t]*(?<boolValue>true|false)");

					if (match.Success)
						isNullable = bool.Parse(match.Groups["boolValue"].Value);

					match = Regex.Match(line, "IsFixed[ \t]*=[ \t]*(?<boolValue>true|false)");

					if (match.Success)
						isFixed = bool.Parse(match.Groups["boolValue"].Value);

					var whitespace = "[ \\t]*";
					var variableName = "[a-zA-Z_][a-zA-Z0-9_]*[\\?]?(\\[\\])?";
					var singletype = $"\\<{whitespace}{variableName}({whitespace}\\,{whitespace}{variableName})*{whitespace}\\>";
					var multitype = $"<{whitespace}{variableName}{whitespace}{singletype}{whitespace}\\>";
					var typedecl = $"{variableName}(({singletype})|({multitype}))*";

					match = Regex.Match(line, $"NativeDataType[ \t]*=[ \t]*\"(?<nativeType>{typedecl})\"");

					if (match.Success)
						nativeDataType = match.Groups["nativeType"].Value;

					match = Regex.Match(line, $"Length[ \t]*=[ \t]*(?<length>[0-9]+)");

					if (match.Success)
						dataLength = Convert.ToInt64(match.Groups["length"].Value);

					match = Regex.Match(line, $"Precision[ \t]*=[ \t]*(?<precision>[0-9]+)");

					if (match.Success)
						precision = Convert.ToInt32(match.Groups["precision"].Value);

					match = Regex.Match(line, $"Scale[ \t]*=[ \t]*(?<scale>[0-9]+)");

					if (match.Success)
						scale = Convert.ToInt32(match.Groups["scale"].Value);

					match = Regex.Match(line, $"ColumnName[ \t]*=[ \t]*(?<entityName>[_a-zA-Z][_a-zA-Z0-9]*)");

					if (match.Success)
						entityName = match.Groups["entityName"].Value;
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
							EntityType = match2.Groups["datatype"].Value,
							IsIdentity = isIdentity,
							IsPrimaryKey = isPrimaryKey,
							IsComputed = isAutoField,
							IsIndexed = isIndexed,
							IsForeignKey = isForeignKey,
							IsNullable = isNullable,
							IsFixed = isFixed,
							dbDataType = nativeDataType,
							Length = dataLength,
							NumericPrecision = precision,
							NumericScale = scale
						};

						columns.Add(entityColumn);

						entityName = string.Empty;
						isPrimaryKey = false;
						isAutoField = false;
						isIdentity = false;
						isIndexed = false;
						isNullable = false;
						isForeignKey = false;
						isFixed = false;
						nativeDataType = string.Empty;
						dataLength = 0;
						precision = 0;
						scale = 0;
					}
				}
			}

			return columns;
		}

		public static List<string> LoadPolicies(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var results = new List<string>();
			var appSettings = FindProjectItem(solution, "appSettings.json");

			var wasOpen = appSettings.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				appSettings.Open(Constants.vsViewKindTextView);

			var doc = appSettings.Document;
			var sel = (TextSelection)doc.Selection;

			sel.SelectAll();

			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(sel.Text)))
			{
				using (var textReader = new StreamReader(stream))
				{
					using (var reader = new JsonTextReader(textReader))
					{
						var jsonConfig = JObject.Load(reader, new JsonLoadSettings { CommentHandling = CommentHandling.Ignore, LineInfoHandling = LineInfoHandling.Ignore });

						if (jsonConfig["OAuth2"] == null)
							return null;

						var oAuth2Settings = jsonConfig["OAuth2"].Value<JObject>();

						if (oAuth2Settings["Policies"] == null)
							return null;

						var policyArray = oAuth2Settings["Policies"].Value<JArray>();

						foreach (var policy in policyArray)
							results.Add(policy["Policy"].Value<string>());
					}
				}
			}

			if (!wasOpen)
				doc.Close();

			return results;
		}

		private static string ExtractEnumName(TextSelection sel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			sel.StartOfDocument();
			sel.FindText("class");
			sel.SelectLine();

			var match = Regex.Match(sel.Text, "enum[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

			if (match.Success)
				return match.Groups["className"].Value;

			return string.Empty;
		}

		private static string ExtractClassName(TextSelection sel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			sel.StartOfDocument();
			sel.FindText("class");
			sel.SelectLine();

			var match = Regex.Match(sel.Text, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

			if (match.Success)
				return match.Groups["className"].Value;

			return string.Empty;
		}

		private static string ExtractNamespace(TextSelection sel)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			sel.StartOfDocument();
			sel.FindText("namespace");
			sel.SelectLine();

			var match = Regex.Match(sel.Text, "namespace[ \t]+(?<namespaceName>[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*)");

			if (match.Success)
			{
				return match.Groups["namespaceName"].Value;
			}

			return string.Empty;
		}

		/// <summary>
		/// Checks to see if the candidate namespace is the root namespace of the startup project
		/// </summary>
		/// <param name="solution">The solution</param>
		/// <param name="candidateNamespace">The candidate namesapce</param>
		/// <returns><see langword="true"/> if the candidate namespace is the root namespace of the startup project; <see langword="false"/> otherwise</returns>
		public static bool IsRootNamespace(Solution solution, string candidateNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				try
				{
					var projectNamespace = project.Properties.Item("RootNamespace").Value.ToString();

					if (string.Equals(candidateNamespace, projectNamespace, StringComparison.OrdinalIgnoreCase))
						return true;
				}
				catch (ArgumentException) { }
			}

			return false;
		}

		/// <summary>
		/// Locates and returns the entity models folder for the project
		/// </summary>
		/// <param name="solution"></param>
		/// <returns></returns>
		public static ProjectFolder FindEntityModelsFolder(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var entityModelsFolder = FindProjectFolder(solution, "EntityModels");

			if (entityModelsFolder != null)
				return entityModelsFolder;

			var modelsFolder = FindProjectItem(solution, "Models");

			if (modelsFolder != null)
			{
				modelsFolder.ProjectItems.AddFolder("EntityModels");
				return FindProjectFolder(solution, "EntityModels");
			}

			Project project = solution.Projects.Item(0);

			modelsFolder = project.ProjectItems.AddFolder("Models");
			modelsFolder.ProjectItems.AddFolder("EntityModels");
			return FindProjectFolder(solution, "EntityModels");
		}

		public static ProjectItem FindProjectItem(Solution solution, string itemName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				foreach (ProjectItem projectItem in project.ProjectItems)
				{
					if (string.Equals(projectItem.Name, itemName, StringComparison.OrdinalIgnoreCase))
					{
						return projectItem;
					}

					var candidate = FindProjectItem(projectItem, itemName);

					if (candidate != null)
						return candidate;
				}
			}
			return null;
		}

		public static ProjectItem FindProjectItem(ProjectItem parent, string itemName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem projectItem in parent.ProjectItems)
			{
				if (string.Equals(projectItem.Name, itemName, StringComparison.OrdinalIgnoreCase))
				{
					return projectItem;
				}

				var candidate = FindProjectItem(projectItem, itemName);

				if (candidate != null)
					return candidate;
			}

			return null;
		}

		public static ProjectFolder FindProjectFolder(Solution solution, string folderName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (Project project in solution.Projects)
			{
				var projectNamespace = project.Properties.Item("RootNamespace").Value.ToString();
				var projectName = project.Name;

				foreach (ProjectItem projectItem in project.ProjectItems)
				{
					if (string.IsNullOrWhiteSpace(Path.GetExtension(projectItem.Name)))
					{
						var folderNamespace = $"{projectNamespace}.{projectItem.Name}";

						if (string.Equals(projectItem.Name, folderName, StringComparison.OrdinalIgnoreCase))
						{
							var folder = new ProjectFolder { ProjectName = projectName, Namespace = folderNamespace, Folder = projectItem.FileNames[0] };
							return folder;
						}

						var candidate = FindProjectFolder(folderNamespace, projectItem, folderName);

						if (candidate != null)
						{
							candidate.ProjectName = projectName;
							return candidate;
						}
					}
				}
			}
			return null;
		}

		private static ProjectFolder FindProjectFolder(string projectNamespace, ProjectItem projectItem, string folderName)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem child in projectItem.ProjectItems)
			{
				if (string.IsNullOrWhiteSpace(Path.GetExtension(projectItem.Name)))
				{
					var folderNamespace = $"{projectNamespace}.{child.Name}";

					if (string.Equals(child.Name, folderName, StringComparison.OrdinalIgnoreCase))
					{
						var folder = new ProjectFolder { Namespace = folderNamespace, Folder = child.FileNames[0] };
						return folder;
					}

					var candidate = FindProjectFolder(folderNamespace, child, folderName);

					if (candidate != null)
						return candidate;
				}
			}

			return null;
		}

		public static string LoadPolicy(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appSettings.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				settingsFile.Open(Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.SelectAll();

			var lines = sel.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				var match = Regex.Match(line, "[ \t]*\\\"Policy\\\"\\:[ \t]\\\"(?<policy>[^\\\"]+)\\\"");
				if (match.Success)
					return match.Groups["policy"].Value;
			}

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);

			return string.Empty;
		}

		public static string LoadMoniker(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appSettings.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				settingsFile.Open(Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;
			string moniker = string.Empty;

			sel.StartOfDocument();
			if (sel.FindText("CompanyName"))
			{
				sel.SelectLine();

				var match = Regex.Match(sel.Text, "[ \t]*\\\"CompanyName\\\"\\:[ \t]\\\"(?<moniker>[^\\\"]+)\\\"");

				if (match.Success)
					moniker = match.Groups["moniker"].Value;
			}

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);

			return moniker;
		}

		public static string GetConnectionString(Solution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			var connectionString = string.Empty;

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appsettings.local.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				settingsFile.Open(Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.SelectAll();
			var settings = JObject.Parse(sel.Text);
			var connectionStrings = settings["ConnectionStrings"].Value<JObject>();
			connectionString = connectionStrings["DefaultConnection"].Value<string>();

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);

			return connectionString;
		}

		public static void ReplaceConnectionString(Solution solution, string connectionString)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			//	The first thing we need to do, is we need to load the appSettings.local.json file
			ProjectItem settingsFile = GetProjectItem(solution, "appsettings.local.json");

			var wasOpen = settingsFile.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				settingsFile.Open(Constants.vsViewKindTextView);

			Document doc = settingsFile.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();
			if (sel.FindText("Server=developmentdb;Database=master;Trusted_Connection=True;"))
			{
				sel.SelectLine();
				sel.Text = $"\t\t\"DefaultConnection\": \"{connectionString}\"";
			}

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);
		}

		public static void RegisterValidationModel(Solution solution, string validationClass, string validationNamespace)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			ProjectItem serviceConfig = GetProjectItem(solution, "ServicesConfig.cs");

			var wasOpen = serviceConfig.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				serviceConfig.Open(Constants.vsViewKindCode);

			Document doc = serviceConfig.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();
			var hasValidationUsing = sel.FindText($"using {validationNamespace}");

			if (!hasValidationUsing)
			{
				sel.StartOfDocument();
				sel.FindText("namespace");
				sel.LineUp();
				sel.LineUp();
				sel.EndOfLine();

				sel.NewLine();
				sel.Insert($"using {validationNamespace};");
			}

			if (!sel.FindText($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();", (int)vsFindOptions.vsFindOptionsFromStart))
			{
				sel.StartOfDocument();
				sel.FindText("services.InitializeFactories();");
				sel.LineUp();
				sel.LineUp();

				sel.SelectLine();

				if (sel.Text.Contains("services.AddTransientWithParameters<IServiceOrchestrator"))
				{
					sel.EndOfLine();
					sel.NewLine();
					sel.Insert($"//\tRegister Validators");
					sel.NewLine();
					sel.Insert($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();");
					sel.NewLine();
				}
				else
				{
					sel.EndOfLine();
					sel.Insert($"services.AddTransientWithParameters<I{validationClass}, {validationClass}>();");
					sel.NewLine();
				}
			}

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);
		}

		public static void RegisterComposite(Solution solution, EntityDetailClassFile classFile)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			if (classFile.ElementType == ElementType.Undefined || classFile.ElementType == ElementType.Table)
				return;

			ProjectItem serviceConfig = GetProjectItem(solution, "ServicesConfig.cs");

			var wasOpen = serviceConfig.IsOpen[Constants.vsViewKindAny];

			if (!wasOpen)
				serviceConfig.Open(Constants.vsViewKindCode);

			Document doc = serviceConfig.Document;
			TextSelection sel = (TextSelection)doc.Selection;

			sel.StartOfDocument();
			var hasNpgsql = sel.FindText($"using Npgsql;");

			sel.StartOfDocument();
			var hasClassNamespace = sel.FindText($"using {classFile.ClassNameSpace};");

			if (!hasNpgsql || !hasClassNamespace)
			{
				sel.StartOfDocument();
				sel.FindText("namespace");

				sel.LineUp();
				sel.LineUp();
				sel.EndOfLine();

				if (!hasNpgsql)
				{
					sel.NewLine();
					sel.Insert($"using Npgsql;");
				}

				if (!hasClassNamespace)
				{
					sel.NewLine();
					sel.Insert($"using {classFile.ClassNameSpace};");
				}
			}

			if (!sel.FindText($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{classFile.ClassName}>(\"{classFile.TableName}\");", (int)vsFindOptions.vsFindOptionsFromStart))
			{
				sel.StartOfDocument();
				sel.FindText("var myAssembly = Assembly.GetExecutingAssembly();");
				sel.LineUp();
				sel.LineUp();

				sel.SelectLine();

				if (sel.Text.Contains("services.AddSingleton<IRepositoryOptions>(RepositoryOptions);"))
				{
					sel.EndOfLine();
					sel.NewLine();
					sel.Insert($"//\tRegister Postgresql Composits and Enums");
					sel.NewLine();
					if (classFile.ElementType == ElementType.Composite)
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{classFile.ClassName}>(\"{classFile.TableName}\");");
					else
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{classFile.ClassName}>(\"{classFile.TableName}\");");
					sel.NewLine();
				}
				else
				{
					sel.EndOfLine();
					if (classFile.ElementType == ElementType.Composite)
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapComposite<{classFile.ClassName}>(\"{classFile.TableName}\");");
					else
						sel.Insert($"NpgsqlConnection.GlobalTypeMapper.MapEnum<{classFile.ClassName}>(\"{classFile.TableName}\");");
					sel.NewLine();
				}
			}

			if (!wasOpen)
				doc.Close(vsSaveChanges.vsSaveChangesYes);
		}

		public static ProjectItem GetProjectItem(Solution solution, string name)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			var theItem = (ProjectItem)DBHelper._cache.Get($"ProjectItem_{name}");

			if (theItem == null)
			{
				foreach (Project project in solution.Projects)
				{
					theItem = GetProjectItem(project.ProjectItems, name);

					if (theItem != null)
					{
						DBHelper._cache.Set(new CacheItem($"ProjectItem_{name}", theItem), new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(5) });
						return theItem;
					}
				}
			}

			return theItem;
		}

		public static ProjectItem GetProjectItem(ProjectItems items, string name)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			foreach (ProjectItem projectItem in items)
			{
				if (string.Equals(projectItem.Name, name, StringComparison.OrdinalIgnoreCase))
					return projectItem;

				var theChildItem = GetProjectItem(projectItem.ProjectItems, name);

				if (theChildItem != null)
					return theChildItem;
			}

			return null;
		}
		#endregion
	}
}
