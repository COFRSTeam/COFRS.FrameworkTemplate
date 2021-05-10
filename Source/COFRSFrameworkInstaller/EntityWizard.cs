using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace COFRSFrameworkInstaller
{
	public class EntityWizard : IWizard
	{
		private bool Proceed = false;

		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
		}

		public void RunFinished()
		{
		}

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			try
			{
				DTE2 _appObject = Package.GetGlobalService(typeof(DTE)) as DTE2;

				var entityModelsFolder = Utilities.FindEntityModelsFolder(_appObject.Solution);
				var solutionDirectory = replacementsDictionary["$solutiondirectory$"];
				var rootNamespace = replacementsDictionary["$rootnamespace$"];

				var namespaceParts = rootNamespace.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

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

				var form = new UserInputEntity
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					RootNamespace = rootNamespace,
					replacementsDictionary = replacementsDictionary,
					EntityModelsFolder = entityModelsFolder
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					Proceed = true;
					var connectionString = form.ConnectionString;
					Utilities.ReplaceConnectionString(_appObject.Solution, connectionString);
					var className = replacementsDictionary["$safeitemname$"];
					replacementsDictionary["$entityClass$"] = className;

					var emitter = new Emitter();

					var etype = DBHelper.GetElementType(form.DatabaseTable.Schema, form.DatabaseTable.Table, connectionString);
					string entityModel = string.Empty;

					if (etype == ElementType.Enum)
					{
						entityModel = emitter.EmitEnum(form.DatabaseTable.Schema, form.DatabaseTable.Table, replacementsDictionary["$safeitemname$"], connectionString);
						replacementsDictionary["$npgsqltypes$"] = "true";

						var entityclassFile = new EntityDetailClassFile()
						{
							ClassName = replacementsDictionary["$safeitemname$"],
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							ClassNameSpace = rootNamespace,
							ElementType = ElementType.Enum
						};

						Utilities.RegisterComposite(_appObject.Solution, entityclassFile);
					}
					else if (etype == ElementType.Composite)
					{
						var undefinedElements = new List<EntityDetailClassFile>();
						entityModel = emitter.EmitComposite(form.DatabaseTable.Schema, form.DatabaseTable.Table, replacementsDictionary["$safeitemname$"], connectionString, replacementsDictionary, form._entityClassList, undefinedElements);
						replacementsDictionary["$npgsqltypes$"] = "true";

						var classFile = new EntityDetailClassFile()
						{
							ClassName = replacementsDictionary["$safeitemname$"],
							SchemaName = form.DatabaseTable.Schema,
							TableName = form.DatabaseTable.Table,
							ClassNameSpace = rootNamespace,
							ElementType = ElementType.Composite
						};

						Utilities.RegisterComposite(_appObject.Solution, classFile);
					}
					else
					{
						entityModel = emitter.EmitEntityModel(form.DatabaseTable, replacementsDictionary["$safeitemname$"], form.DatabaseColumns, replacementsDictionary, connectionString);
					}

					replacementsDictionary.Add("$model$", entityModel);
				}
				else
					Proceed = false;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				Proceed = false;
			}
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
