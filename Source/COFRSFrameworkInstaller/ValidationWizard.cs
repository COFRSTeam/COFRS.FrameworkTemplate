using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace COFRSFrameworkInstaller
{
	public class ValidationWizard : IWizard
	{
		private bool Proceed = false;
		private string SolutionFolder { get; set; }
		private ResourceClassFile Orchestrator;
		private ResourceClassFile ExampleClass;
		private ResourceClassFile CollectionExampleClass;
		private ResourceClassFile ValidatorClass;

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
		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
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

				SolutionFolder = replacementsDictionary["$solutiondirectory$"];

				var form = new UserInputValidation()
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					InstallType = 3
				};

				if (form.ShowDialog() == DialogResult.OK)
				{

					Orchestrator = null;
					ExampleClass = null;
					CollectionExampleClass = null;
					ValidatorClass = null;

					var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;
					var profileClassFile = (ProfileClassFile)form._profileModelList.SelectedItem;

					Utilities.LoadClassList(SolutionFolder, resourceClassFile.ClassName, ref Orchestrator, ref ValidatorClass, ref ExampleClass, ref CollectionExampleClass);

					var model = EmitModel(entityClassFile, resourceClassFile, profileClassFile, form.DatabaseColumns, replacementsDictionary);

					replacementsDictionary.Add("$orchestrationnamespace$", Orchestrator.ClassNamespace);
					replacementsDictionary.Add("$model$", model);
					replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
					replacementsDictionary.Add("$resourcenamespace$", resourceClassFile.ClassNamespace);

					Proceed = UpdateServices(resourceClassFile, replacementsDictionary);
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

		private string EmitModel(EntityClassFile entityClassFile, ResourceClassFile resourceClassFile, ProfileClassFile profileClassFile, List<DBColumn> columns, Dictionary<string, string> replacementsDictionary)
		{
			var results = new StringBuilder();

			var resourceMembers = Utilities.ExtractMembers(resourceClassFile);

			//	IValidator interface
			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\tInterface for the {resourceClassFile.ClassName} Validator");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic interface I{replacementsDictionary["$safeitemname$"]} : IValidator<{resourceClassFile.ClassName}>");
			results.AppendLine("\t{");
			results.AppendLine("\t}");
			results.AppendLine();

			//	Validator Class with constructor
			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine($"\tpublic class {replacementsDictionary["$safeitemname$"]} : Validator<{resourceClassFile.ClassName}>, I{replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t{");
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {replacementsDictionary["$safeitemname$"]}() : base()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//	Validator Class with constructor with user
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes the {replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {replacementsDictionary["$safeitemname$"]}(ClaimsPrincipal user) : base(user)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for GET
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidation for Queries");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the query</param>");
			results.AppendLine("\t\t///\t<param name=\"parameters\">Optional parameters that can be used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForGetAsync(RqlNode node, object[] parameters)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t//\tUn-comment out the line below if this table is large, and you want to prevent users from requesting a full table scan");
			results.AppendLine("\t\t\t//\tRequireIndexedQuery(node, \"The query is too broad. Please specify a more refined query that will produce fewer records.\");");
			results.AppendLine();
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PUT and POST
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidations common to adding and updating items");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"item\">The candidate item being added or updated</param>");
			results.AppendLine("\t\t///\t<param name=\"parameters\">Optional parameters that can be used by custom validators</param>");
			results.AppendLine($"\t\tpublic async Task ValidateForAddAndUpdateAsync({resourceClassFile.ClassName} item, object[] parameters)");
			results.AppendLine("\t\t{");

			results.AppendLine();
			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to");
			results.AppendLine("\t\t\t//\t       adding or updating an item.");
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PUT
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine("\t\t///\tValidation for updating existing items");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"item\">The candidate item being updated</param>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the update</param>");
			results.AppendLine("\t\t///\t<param name=\"parameters\">Optional parameters that can be used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForUpdateAsync({resourceClassFile.ClassName} item, RqlNode node, object[] parameters)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item, parameters).ConfigureAwait(false);");
			results.AppendLine();
			results.AppendLine("\t\t\t//\tTo do: add any specific validations pertaining to updating an item.");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for POST
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidation for adding new items");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"item\">The candidate item being added</param>");
			results.AppendLine("\t\t///\t<param name=\"parameters\">Optional parameters that can be used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForAddAsync({resourceClassFile.ClassName} item, object[] parameters)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tawait ValidateForAddAndUpdateAsync(item, parameters).ConfigureAwait(false);");
			results.AppendLine();
			results.AppendLine("\t\t\t//\tTo do: add any specific validations pertaining to adding an item.");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for PATCH
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine("\t\t///\tValidates a set of patch commands on an item");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"patchCommands\">The set of patch commands to validate</param>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the update</param>");
			results.AppendLine("\t\t///\t<param name=\"parameters\">Optional parameters that can be used by custom validators</param>");
			results.AppendLine("\t\tpublic override async Task ValidateForPatchAsync(IEnumerable<PatchCommand> patchCommands, RqlNode node, object[] parameters)");
			results.AppendLine("\t\t{");

			results.AppendLine("\t\t\tforeach (var command in patchCommands)");
			results.AppendLine("\t\t\t{");
			results.AppendLine("\t\t\t\tif (string.Equals(command.Op, \"replace\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");
			bool firstTime = true;


			foreach ( var member in resourceMembers )
            {
				if (firstTime)
				{
					firstTime = false;
					results.AppendLine($"\t\t\t\t\tif ( string.Equals(command.Path, \"{member.Name}\", StringComparison.Ordinal))");
				}
				else
                {
					results.AppendLine($"\t\t\t\t\telse if ( string.Equals(command.Path, \"{member.Name}\", StringComparison.Ordinal))");
				}

				if (string.Equals(member.Name, "href", StringComparison.OrdinalIgnoreCase))
				{
					results.AppendLine($"\t\t\t\t\t{{");
					results.AppendLine($"\t\t\t\t\t\tRequire(false, \"Invalid operation. Cannot alter {member.Name}\");");
					results.AppendLine($"\t\t\t\t\t}}");
				}
				else
				{
					results.AppendLine($"\t\t\t\t\t{{");
					results.AppendLine($"\t\t\t\t\t\tif ( command.Value != null )");
					results.AppendLine($"\t\t\t\t\t\t{{");

					if (string.Equals(member.DataType, "string", StringComparison.OrdinalIgnoreCase))
						results.AppendLine($"\t\t\t\t\t\t\tRequire(command.Value.GetType() == typeof(string), \"{member.Name} value must be a string.\");");

					else if (string.Equals(member.DataType, "char", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "char?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(command.Value.ToString().Length == 1, \"{member.Name} value must be convertable to a char.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToChar(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a char.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "byte", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "byte?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(byte.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a byte.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToByte(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a byte.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "sbyte", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "sbyte?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(sbyte.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to an sbyte.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToSByte(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to an sbyte.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "short", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "short?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(short.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a short.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToInt16(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a short.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "ushort", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "ushort?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(ushort.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a ushort.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToUInt16(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a ushort.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "int", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "int?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(int.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to an int.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToInt32(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to an int.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "uint", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "uint?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(uint.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to an uint.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToUInt32(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to an uint.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "long", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "long?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(long.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a long.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToInt64(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a long.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "ulong", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "ulong?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(ulong.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a ulong.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToUInt64(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a ulong.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "bool", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "bool?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(bool.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a boolean.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToBoolean(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a boolean.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "double", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "double?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(double.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a double.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToDouble(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a double.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "float", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "float?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(float.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a float.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToSingle(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a float.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "decimal", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "decimal?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(decimal.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a decimal.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = Convert.ToDecimal(command.Value);");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a decimal.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "Guid", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "Guid?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(Guid.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a Guid.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = (Guid) command.Value;");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a Guid.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "Uri", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "Uri?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(Uri.TryCreate(command.Value.ToString(), UriKind.RelativeOrAbsolute, out _), \"{member.Name} value must be convertable to a Uri.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = (Uri) command.Value;");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a Uri.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "DateTime", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "DateTime?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(DateTime.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a DateTime.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = (DateTime) command.Value;");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a DateTime.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "DateTimeOFfset", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "DateTimeOffset?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(DateTime.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a DateTimeOffset.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = (DateTimeOffset) command.Value;");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a DateTimeOFfset.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}
					else if (string.Equals(member.DataType, "TimeSpan", StringComparison.OrdinalIgnoreCase) || string.Equals(member.DataType, "TimeSpan?", StringComparison.OrdinalIgnoreCase))
					{
						results.AppendLine($"\t\t\t\t\t\t\tif ( command.Value.GetType() == typeof(string))");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\tRequire(TimeSpan.TryParse(command.Value.ToString(), out _), \"{member.Name} value must be convertable to a TimeSpan.\");");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\telse");
						results.AppendLine($"\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\ttry");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\t_ = (TimeSpan) command.Value;");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t\tcatch ( Exception )");
						results.AppendLine($"\t\t\t\t\t\t\t\t{{");
						results.AppendLine($"\t\t\t\t\t\t\t\t\tRequire(false, \"{member.Name} value must be convertable to a TimeSpan.\");");
						results.AppendLine($"\t\t\t\t\t\t\t\t}}");
						results.AppendLine($"\t\t\t\t\t\t\t}}");
					}

					results.AppendLine($"\t\t\t\t\t\t}}");
					results.AppendLine($"\t\t\t\t\t}}");
				}
			}

			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t\telse if (string.Equals(command.Op, \"add\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t\telse if (string.Equals(command.Op, \"delete\", StringComparison.OrdinalIgnoreCase))");
			results.AppendLine("\t\t\t\t{");

			results.AppendLine("\t\t\t\t}");
			results.AppendLine("\t\t\t}");
			results.AppendLine();

			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to patching an item.");
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine();

			//------------------------------------------------------------------------------------------
			//	Validation for DELETE
			//------------------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tValidation for deleting an item");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"node\">The <see cref=\"RqlNode\"/> that constricts the delete</param>");
			results.AppendLine("\t\t///\t<param name=\"parameters\">Optional parameters that can be used by custom validators</param>");
			results.AppendLine($"\t\tpublic override async Task ValidateForDeleteAsync(RqlNode node, object[] parameters)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\t//\tTo do: Replace the line below with code to perform any specific validations pertaining to deleting an item.");
			results.AppendLine("\t\t\tawait Task.CompletedTask.ConfigureAwait(false);");
			results.AppendLine("\t\t}");
			results.AppendLine("\t}");

			return results.ToString();
		}

		private bool UpdateServices(ResourceClassFile resourceClassFile, Dictionary<string, string> replacementsDictionary)
		{
			var servicesFile = FindServices(replacementsDictionary["$solutiondirectory$"]);

			if (!string.IsNullOrWhiteSpace(servicesFile))
			{
				var serviceFolder = Path.GetDirectoryName(servicesFile);
				var tempFile = Path.Combine(serviceFolder, "Services.old.cs");

				try
				{
					File.Delete(tempFile);
					File.Move(servicesFile, tempFile);

					using (var stream = new FileStream(tempFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
					{
						using (var reader = new StreamReader(stream))
						{
							using (var outStream = new FileStream(servicesFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
							{
								using (var writer = new StreamWriter(outStream))
								{
									var state = 1;
									bool hasDomainNamespace = false;
									bool hasValidationNamespace = false;
									bool hasEntityNamespace = false;
									bool validatorRegistered = false;

									while (!reader.EndOfStream)
									{
										var line = reader.ReadLine();

										if (state == 1)
										{
											if (line.ToLower().Contains(replacementsDictionary["$resourcenamespace$"].ToLower()))
											{
												hasDomainNamespace = true;
											}

											if (line.ToLower().Contains(replacementsDictionary["$rootnamespace$"].ToLower()))
											{
												hasValidationNamespace = true;
											}

											if (line.ToLower().Contains(replacementsDictionary["$entitynamespace$"].ToLower()))
											{
												hasEntityNamespace = true;
											}

											if (string.IsNullOrWhiteSpace(line))
											{
												if (!hasDomainNamespace)
												{
													writer.WriteLine($"using {replacementsDictionary["$resourcenamespace$"]};");
												}

												if (!hasValidationNamespace)
												{
													writer.WriteLine($"using {replacementsDictionary["$rootnamespace$"]};");
												}

												if (!hasEntityNamespace)
												{
													writer.WriteLine($"using {replacementsDictionary["$entitynamespace$"]};");
												}

												state = 2;
											}

										}
										else if (state == 2)
										{
											if (line.ToLower().Contains("public static iapioptions configureservices"))
											{
												state = 3;
											}
										}
										else if (state == 3)
										{
											if (line.Contains("{"))
												state++;
										}
										else if (state == 4)
										{
											if (line.ToLower().Contains(($"services.AddTransientWithParameters<I{replacementsDictionary["$safeitemname$"]}, {replacementsDictionary["$safeitemname$"]}>()").ToLower()))
												validatorRegistered = true;

											state += line.CountOf('{') - line.CountOf('}');

											if (line.Contains("services.InitializeFactories();"))
												state--;

											if (state == 3)
											{
												if (!validatorRegistered)
												{
													writer.WriteLine($"\t\t\tservices.AddTransientWithParameters<I{replacementsDictionary["$safeitemname$"]}, {replacementsDictionary["$safeitemname$"]}>();");
												}
												state = 1000000;
											}
										}
										else
										{
											state += line.CountOf('{') - line.CountOf('}');
										}

										writer.WriteLine(line);
									}
								}
							}
						}
					}

					File.Delete(tempFile);
				}
				catch (Exception error)
				{
					MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					File.Delete(servicesFile);
					File.Move(tempFile, servicesFile);
					return false;
				}
			}

			return true;
		}

		private string FindServices(string folder)
		{
			string filePath = Path.Combine(folder, "ServicesConfig.cs");

			if (File.Exists(filePath))
				return filePath;

			foreach (var childFolder in Directory.GetDirectories(folder))
			{
				filePath = FindServices(childFolder);

				if (!string.IsNullOrWhiteSpace(filePath))
					return filePath;
			}

			return string.Empty;
		}


	}
}
