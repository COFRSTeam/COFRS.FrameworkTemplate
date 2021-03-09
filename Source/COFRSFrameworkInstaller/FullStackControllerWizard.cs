﻿using EnvDTE;
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
			try
			{
				var solutionDirectory = replacementsDictionary["$solutiondirectory$"];
				var rootNamespace = replacementsDictionary["$rootnamespace$"];
				var computedRootNamespace = Utilities.GetRootNamespace(solutionDirectory);

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
					PluralResourceName = resourceName.PluralForm
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					var connectionString = form.ConnectionString;
					ReplaceConnectionString(connectionString, replacementsDictionary);

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

                    var moniker = LoadMoniker(SolutionFolder);
                    var policy = LoadPolicy(SolutionFolder);

                    replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
                    replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
                    replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");

                    var emitter = new Emitter();
                    var entityModel = emitter.EmitEntityModel(form.DatabaseTable, entityClassName, form.DatabaseColumns, replacementsDictionary);
                    replacementsDictionary.Add("$entityModel$", entityModel);

                    List<ClassMember> classMembers = LoadClassMembers(form.DatabaseTable, form.DatabaseColumns);

                    var resourceModel = emitter.EmitResourceModel(classMembers, resourceClassName, entityClassName, form.DatabaseTable, form.DatabaseColumns, replacementsDictionary);
                    replacementsDictionary.Add("$resourceModel$", resourceModel);

                    var mappingModel = emitter.EmitMappingModel(classMembers, resourceClassName, entityClassName, mappingClassName, form.DatabaseColumns, replacementsDictionary);
                    replacementsDictionary.Add("$mappingModel$", mappingModel);

                    var exampleModel = emitter.EmitExampleModel(replacementsDictionary["$targetframeworkversion$"],
                                            classMembers,
                                            entityClassName,
                                            resourceClassName,
                                            exampleClassName,
                                            form.DatabaseColumns, form.Examples, replacementsDictionary);
                    replacementsDictionary.Add("$exampleModel$", exampleModel);

                    var exampleCollectionModel = emitter.EmitExampleCollectionModel(replacementsDictionary["$targetframeworkversion$"],
                        classMembers,
                        entityClassName,
                        resourceClassName,
                        exampleCollectionClassName,
                        form.DatabaseColumns, form.Examples, replacementsDictionary);
                    replacementsDictionary.Add("$exampleCollectionModel$", exampleCollectionModel);

                    var validationModel = emitter.EmitValidationModel(entityClassName, resourceClassName, validationClassName);
                    replacementsDictionary.Add("$validationModel$", validationModel);

                    Proceed = emitter.UpdateServices(solutionDirectory, validationClassName,
                                    replacementsDictionary["$entitynamespace$"], replacementsDictionary["$resourcenamespace$"],
                                    replacementsDictionary["$validatornamespace$"]);


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


                    Proceed = true;
				}
				else
					Proceed = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
				Proceed = false;
			}
		}

		// This method is only called for item templates,
		// not for project templates.
		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}

		private List<ClassMember> LoadClassMembers(DBTable table, List<DBColumn> columns)
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
				column.EntityName = column.ColumnName;
				column.EntityType = column.dbDataType;

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
		private string LoadPolicy(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "appSettings.json"))
				{
					using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						using (var reader = new StreamReader(stream))
						{
							while (!reader.EndOfStream)
							{
								var line = reader.ReadLine();

								var match = Regex.Match(line, "[ \t]*\\\"Policy\\\"\\:[ \t]\\\"(?<policy>[^\\\"]+)\\\"");
								if (match.Success)
									return match.Groups["policy"].Value;
							}
						}
					}

					return string.Empty;
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					string policy = LoadPolicy(subfolder);

					if (!string.IsNullOrWhiteSpace(policy))
						return policy;
				}

				return string.Empty;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
		}

		private string LoadMoniker(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "appSettings.json"))
				{
					using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
					{
						using (var reader = new StreamReader(stream))
						{
							while (!reader.EndOfStream)
							{
								var line = reader.ReadLine();

								var match = Regex.Match(line, "[ \t]*\\\"CompanyName\\\"\\:[ \t]\\\"(?<moniker>[^\\\"]+)\\\"");
								if (match.Success)
									return match.Groups["moniker"].Value;
							}
						}
					}

					return string.Empty;
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					string moniker = LoadMoniker(subfolder);

					if (!string.IsNullOrWhiteSpace(moniker))
						return moniker;
				}

				return string.Empty;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return string.Empty;
			}
		}

		private void ReplaceConnectionString(string connectionString, Dictionary<string, string> replacementsDictionary)
		{
			//	The first thing we need to do, is we need to load the appSettings.local.json file
			var fileName = GetLocalFileName(replacementsDictionary["$solutiondirectory$"]);
			string content;

			using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				using (var reader = new StreamReader(stream))
				{
					content = reader.ReadToEnd();
				}
			}

			var appSettings = JObject.Parse(content);
			var connectionStrings = appSettings.Value<JObject>("ConnectionStrings");

			if (string.Equals(connectionStrings.Value<string>("DefaultConnection"), "Server=developmentdb;Database=master;Trusted_Connection=True;", StringComparison.OrdinalIgnoreCase))
			{
				connectionStrings["DefaultConnection"] = connectionString;

				using (var stream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
				{
					using (var writer = new StreamWriter(stream))
					{
						writer.Write(appSettings.ToString());
						writer.Flush();
					}
				}
			}
		}

		private string GetLocalFileName(string rootFolder)
		{
			var files = Directory.GetFiles(rootFolder);

			foreach (var file in files)
			{
				if (file.ToLower().Contains("appsettings.local.json"))
					return file;
			}

			var childFolders = Directory.GetDirectories(rootFolder);

			foreach (var childFolder in childFolders)
			{
				var theFile = GetLocalFileName(childFolder);

				if (!string.IsNullOrWhiteSpace(theFile))
					return theFile;
			}


			return string.Empty;
		}
	}
}
