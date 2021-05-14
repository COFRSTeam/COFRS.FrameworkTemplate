using COFRS.Template.Common.Forms;
using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace COFRS.Template.Common.Wizards
{
	public class ResourceWizard : IWizard
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

			var form = new UserInputResource()
			{
				SolutionFolder = replacementsDictionary["$solutiondirectory$"]
			};

			if (form.ShowDialog() == DialogResult.OK)
			{
				var connectionString = form.ConnectionString;
				var entityClassFile = (EntityDetailClassFile)form._entityClassList.SelectedItem;
				var entityClassMembers = Utilities.LoadEntityClassMembers(entityClassFile.FileName, form.ServerType, form.DatabaseColumns);

				var standardEmitter = new StandardEmitter();
				var model = standardEmitter.EmitResourceModel(form.ServerType, entityClassMembers, replacementsDictionary["$safeitemname$"], entityClassFile.ClassName, form.DatabaseTable, form.DatabaseColumns, replacementsDictionary, connectionString);

				replacementsDictionary.Add("$model$", model);
				replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
				Proceed = true;
			}
			else
				Proceed = false;
		}

		public bool ShouldAddProjectItem(string filePath)
		{
			return Proceed;
		}
	}
}
