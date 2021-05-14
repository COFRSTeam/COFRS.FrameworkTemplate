using COFRS.Template.Common.Models;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    public static class SolutionUtil
    {
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
