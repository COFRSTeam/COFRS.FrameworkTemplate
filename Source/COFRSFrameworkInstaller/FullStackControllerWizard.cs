using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using VSLangProj;

namespace COFRSFrameworkInstaller
{
	public class FullStackControllerWizard : IWizard
	{
		private bool Proceed = false;
		private string SolutionFolder { get; set; }

		// This method is called before opening any item that
		// has the OpenInEditor attribute.
		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		// This method is only called for item templates,
		// not for project templates.
		public void ProjectItemFinishedGenerating(ProjectItem
			projectItem)
		{
		}

		// This method is called after the project is created.
		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject,
			Dictionary<string, string> replacementsDictionary,
			WizardRunKind runKind, object[] customParams)
		{

			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;
				var parent = new WindowClass((IntPtr) _appObject.ActiveWindow.HWnd);

				var progressDialog = new ProgressDialog("Loading classes and preparing project...");
				progressDialog.Show(parent);
				progressDialog.Focus();

				HandleMessages();

				_appObject.StatusBar.Animate(true, vsStatusAnimation.vsStatusAnimationGeneral);
				_appObject.StatusBar.Text = "Preparing project...";


				var entityModelsFolder = Utilities.FindEntityModelsFolder(_appObject.Solution);
				var solutionDirectory = replacementsDictionary["$solutiondirectory$"];
				var rootNamespace = replacementsDictionary["$rootnamespace$"];
				var computedRootNamespace = Utilities.GetRootNamespace(solutionDirectory);

				var classList = new List<EntityDetailClassFile>();

				HandleMessages();


				foreach (Project project in _appObject.Solution.Projects)
				{
					if (project.Kind == PrjKind.prjKindCSharpProject)
                    {
						classList.AddRange(ScanProject(project.ProjectItems));
                    }
					HandleMessages();
				}

				if (!string.Equals(rootNamespace, computedRootNamespace, StringComparison.OrdinalIgnoreCase))
				{
					MessageBox.Show("The COFRS Controller Full Stack should be placed at the project root. It will add the appropriate components in the appropriate folders.", "Placement Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					Proceed = false;
					return;
				}

				var namespaceParts = rootNamespace.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				var candidateName = replacementsDictionary["$safeitemname$"];

				if (candidateName.EndsWith("Controller", StringComparison.OrdinalIgnoreCase))
					candidateName = candidateName.Substring(0, candidateName.Length - 10);

				var resourceName = new NameNormalizer(candidateName);
				var filePath = solutionDirectory;

				for (int i = 0; i < namespaceParts.Length; i++)
				{
					if (i == 0)
					{
						var candidate = Path.Combine(filePath, namespaceParts[i]);

						if (Directory.Exists(candidate))
							filePath = candidate;
					}
					else
						filePath = Path.Combine(filePath, namespaceParts[i]);
				}

				if (!Directory.Exists(filePath))
					Directory.CreateDirectory(filePath);

				HandleMessages();

				replacementsDictionary["$entitynamespace$"] = $"{rootNamespace}.Models.EntityModels";
				replacementsDictionary["$resourcenamespace$"] = $"{rootNamespace}.Models.ResourceModels";
				replacementsDictionary["$orchestrationnamespace$"] = $"{rootNamespace}.Orchestration";
				replacementsDictionary["$validatornamespace$"] = $"{rootNamespace}.Validation";
				replacementsDictionary["$validationnamespace$"] = $"{rootNamespace}.Validation";
				replacementsDictionary["$singleexamplenamespace$"] = $"{rootNamespace}.Models.SwaggerExamples";

				SolutionFolder = replacementsDictionary["$solutiondirectory$"];

				var form = new UserInputFullStack
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					SingularResourceName = resourceName.SingleForm,
					PluralResourceName = resourceName.PluralForm,
					RootNamespace = rootNamespace,
					ReplacementsDictionary = replacementsDictionary,
					ClassList = classList,
					EntityModelsFolder = entityModelsFolder
				};

				HandleMessages();

				progressDialog.Close();

				if (form.ShowDialog() == DialogResult.OK)
				{
					progressDialog = new ProgressDialog("Building classes...");
					progressDialog.Show(parent);
					progressDialog.Focus();

					HandleMessages();
					//	Replace the ConnectionString
					var connectionString = form.ConnectionString;
					Utilities.ReplaceConnectionString(_appObject.Solution, connectionString);
					HandleMessages();

					var entityClassName = $"E{form.SingularResourceName}";
					var resourceClassName = form.SingularResourceName;
					var mappingClassName = $"{form.PluralResourceName}Profile";
					var exampleClassName = $"{form.PluralResourceName}Example";
					var exampleCollectionClassName = $"Collection{form.PluralResourceName}Example";
					var validationClassName = $"{form.PluralResourceName}Validator";
					var controllerClassName = $"{form.PluralResourceName}Controller";

					replacementsDictionary["$entityClass$"] = entityClassName;
					replacementsDictionary["$resourceClass$"] = resourceClassName;
					replacementsDictionary["$swaggerClass$"] = exampleClassName;
					replacementsDictionary["$swaggerCollectionClass$"] = exampleCollectionClassName;
					replacementsDictionary["$mapClass$"] = mappingClassName;
					replacementsDictionary["$validatorClass$"] = validationClassName;
					replacementsDictionary["$controllerClass$"] = controllerClassName;

					var moniker = Utilities.LoadMoniker(_appObject.Solution);
					var policy = form.Policy;
					HandleMessages();

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

					List<EntityDetailClassFile> composits = form.UndefinedClassList;

					var emitter = new Emitter();
					emitter.GenerateComposites(composits, connectionString, replacementsDictionary, form.ClassList);
					HandleMessages();

					foreach ( var composite in composits)
                    {
						var pj = (VSProject)_appObject.Solution.Projects.Item(1).Object;
						pj.Project.ProjectItems.AddFromFile(composite.FileName);

						Utilities.RegisterComposite(_appObject.Solution, composite);
					}

					//	Emit Entity Model
					var entityModel = emitter.EmitEntityModel(form.DatabaseTable, entityClassName, form.DatabaseColumns, replacementsDictionary, connectionString);
					replacementsDictionary.Add("$entityModel$", entityModel);
					HandleMessages();

					List<ClassMember> classMembers = LoadClassMembers(form.DatabaseTable, form.DatabaseColumns, connectionString);

					var ClassList = Utilities.LoadDetailEntityClassList(SolutionFolder, connectionString);
					HandleMessages();

					//	Emit Resource Model
					var resourceModel = emitter.EmitResourceModel(classMembers, resourceClassName, entityClassName, form.DatabaseTable, form.DatabaseColumns, replacementsDictionary, connectionString);
					replacementsDictionary.Add("$resourceModel$", resourceModel);
					HandleMessages();

					//	Emit Mapping Model
					var mappingModel = emitter.EmitMappingModel(classMembers, resourceClassName, entityClassName, mappingClassName, form.DatabaseColumns, replacementsDictionary);
					replacementsDictionary.Add("$mappingModel$", mappingModel);
					HandleMessages();

					//	Emit Example Model
					var exampleModel = emitter.EmitExampleModel(
											form.DatabaseTable.Schema,
											connectionString,
											classMembers,
											entityClassName,
											resourceClassName,
											exampleClassName,
											form.DatabaseColumns, form.Examples, replacementsDictionary,
											ClassList);
					replacementsDictionary.Add("$exampleModel$", exampleModel);
					HandleMessages();


					var exampleCollectionModel = emitter.EmitExampleCollectionModel(
						form.DatabaseTable.Schema,
						connectionString,
						classMembers,
						entityClassName,
						resourceClassName,
						exampleCollectionClassName,
						form.DatabaseColumns, form.Examples, replacementsDictionary,
						ClassList);
					replacementsDictionary.Add("$exampleCollectionModel$", exampleCollectionModel);
					HandleMessages();

					//	Emit Validation Model
					var validationModel = emitter.EmitValidationModel(resourceClassName, validationClassName);
					replacementsDictionary.Add("$validationModel$", validationModel);
					HandleMessages();

					//	Register the validation model

					Utilities.RegisterValidationModel(_appObject.Solution, 
						                              validationClassName,
													  replacementsDictionary["$validatornamespace$"]);

					//	Emit Controller
					var controllerModel = emitter.EmitController(classMembers,
								   true,
								   moniker,
								   resourceClassName,
								   controllerClassName,
								   validationClassName,
								   exampleClassName,
								   exampleCollectionClassName,
								   policy);
					replacementsDictionary.Add("$controllerModel$", controllerModel);
					HandleMessages();

					progressDialog.Close();

					Proceed = true;
				}
				else
					Proceed = false;

				_appObject.StatusBar.Animate(false, vsStatusAnimation.vsStatusAnimationGeneral);
				_appObject.StatusBar.Text = "Ready";
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				Proceed = false;
			}
		}

        private List<EntityDetailClassFile> ScanProject(ProjectItems projectItems)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
			var results = new List<EntityDetailClassFile>();

            foreach (ProjectItem item in projectItems)
            {
                if (item.FileCodeModel != null)
                {
                    int buildAction = 0;

                    foreach (Property property in item.Properties)
                    {
                        if (property.Name == "BuildAction")
                            buildAction = Convert.ToInt32(property.Value);
                    }

                    if (item.Name.Contains(".cs") && buildAction == 1)
                    {
						var wasOpen = item.IsOpen[Constants.vsViewKindAny];

						if (!wasOpen)
							item.Open(Constants.vsViewKindCode);

						var doc = item.Document;
						var sel = (TextSelection)doc.Selection;

						var anchorPoint = sel.AnchorPoint;
						var activePoint = sel.ActivePoint;

						bool isComposite = false;
						bool isEnum = false;
						bool isEntity = false;

						sel.StartOfDocument();
						isComposite = sel.FindText("[PgComposite");

						if (!isComposite)
						{
							sel.StartOfDocument();
							isEnum = sel.FindText("[PgEnum");

							if (!isEnum)
							{
								sel.StartOfDocument();
								isEntity = sel.FindText("[Table");
							}
						}

						if (!wasOpen)
							doc.Close();
						else
                        {
							sel.MoveToPoint(anchorPoint);
							sel.SwapAnchor();
							sel.MoveToPoint(activePoint);
                        }

						if (isComposite || isEnum || isEntity)
						{
							var entity = Utilities.LoadEntityClass(item.FileNames[0]); 

							results.Add(entity);
						}
                    }
                }
				else
                {
					results.AddRange(ScanProject(item.ProjectItems));
                }
            }

			return results;
        }

        // This method is only called for item templates,
        // not for project templates.
        public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}

		private List<ClassMember> LoadClassMembers(DBTable table, List<DBColumn> columns, string connectionString)
		{
			var members = new List<ClassMember>();

			var member = new ClassMember()
			{
				ResourceMemberName = "Href",
				ResourceMemberType = string.Empty,
				EntityNames = new List<DBColumn>(),
				ChildMembers = new List<ClassMember>()
			};

			foreach (var column in columns)
			{
				if (column.IsPrimaryKey)
					member.EntityNames.Add(column);
			}

			members.Add(member);

			foreach (var column in columns)
			{
				if (string.Equals(column.ColumnName, "abstract", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "as", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "base", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "bool", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "break", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "byte", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "case", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "catch", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "char", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "checked", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "class", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "const", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "continue", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "decimal", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "default", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "delegate", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "do", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "double", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "else", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "enum", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "event", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "explicit", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "extern", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "false", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "finally", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "fixed", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "float", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "for", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "foreach", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "goto", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "if", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "implicit", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "in", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "int", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "interface", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "internal", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "is", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "lock", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "long", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "namespace", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "new", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "null", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "object", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "operator", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "out", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "override", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "params", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "private", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "protected", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "public", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "readonly", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "ref", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "return", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "sbyte", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "sealed", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "short", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "sizeof", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "stackalloc", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "static", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "string", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "struct", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "switch", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "this", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "throw", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "true", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "try", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "typeof", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "uint", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "ulong", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "unchecked", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "unsafe", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "ushort", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "using", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "virtual", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "void", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "volatile", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "while", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "add", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "alias", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "ascending", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "async", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "await", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "by", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "descending", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "dynamic", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "equals", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "from", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "get", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "global", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "group", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "into", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "join", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "let", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "nameof", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "on", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "orderby", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "partial", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "remove", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "select", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "set", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "unmanaged", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "value", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "var", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "when", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "where", StringComparison.OrdinalIgnoreCase) ||
					 string.Equals(column.ColumnName, "yield", StringComparison.OrdinalIgnoreCase))
				{
					column.EntityName = $"{column.ColumnName}_Value";
				}
				else
				{
					column.EntityName = column.ColumnName;
				}

				if (column.ServerType == DBServerType.POSTGRESQL)
					column.EntityType = DBHelper.GetPostgresDataType(table.Schema, column, connectionString, SolutionFolder);
				else if (column.ServerType == DBServerType.MYSQL)
					column.EntityType = DBHelper.GetMySqlDataType(column);
				else if (column.ServerType == DBServerType.SQLSERVER)
					column.EntityType = DBHelper.GetSQLServerDataType(column);

				if (!column.IsPrimaryKey)
				{
					if (!column.IsForeignKey)
					{
						var childMember = new ClassMember()
						{
							ResourceMemberName = column.ColumnName,
							ResourceMemberType = string.Empty,
							EntityNames = new List<DBColumn>() { column },
							ChildMembers = new List<ClassMember>()
						};

						members.Add(childMember);
					}
					else
					{
						string shortColumnName;

						if (string.Equals(column.ForeignTableName, table.Table, StringComparison.OrdinalIgnoreCase))
						{
							shortColumnName = column.ColumnName;
							if (column.ColumnName.EndsWith("ID", StringComparison.OrdinalIgnoreCase))
								shortColumnName = column.ColumnName.Substring(0, column.ColumnName.Length - 2);
						}
						else
							shortColumnName = column.ForeignTableName;

						var normalizer = new NameNormalizer(shortColumnName);

						var childMember = new ClassMember()
						{
							ResourceMemberName = normalizer.SingleForm,
							ResourceMemberType = string.Empty,
							EntityNames = new List<DBColumn>() { column },
							ChildMembers = new List<ClassMember>()
						};

						members.Add(childMember);
					}
				}
			}

			return members;
		}

		private void HandleMessages()
        {
			WinNative.NativeMessage msg;

			while ( WinNative.PeekMessage(out msg, IntPtr.Zero, 0, (uint) 0xFFFFFFFF, 1) != 0)
            {
				WinNative.SendMessage(msg.handle, msg.msg, msg.wParam, msg.lParam);
            }
        }
	}
}
