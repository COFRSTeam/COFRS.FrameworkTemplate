﻿using EnvDTE;
using Microsoft.VisualStudio.TemplateWizard;
using MySql.Data.MySqlClient;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace COFRSFrameworkInstaller
{
	public class ControllerWizard : IWizard
	{
		private bool Proceed = false;
		private string SolutionFolder { get; set; }
		private ResourceClassFile Orchestrator { get; set; }
		private ResourceClassFile ExampleClass { get; set; }
		private ResourceClassFile ValidatorClass { get; set; }

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
				SolutionFolder = replacementsDictionary["$solutiondirectory$"];

				var form = new UserInputGeneral
				{
					SolutionFolder = replacementsDictionary["$solutiondirectory$"],
					InstallType = 5
				};

				if (form.ShowDialog() == DialogResult.OK)
				{
					bool hasValidator;
					var entityClassFile = (EntityClassFile)form._entityModelList.SelectedItem;
					var resourceClassFile = (ResourceClassFile)form._resourceModelList.SelectedItem;
					var moniker = LoadMoniker(SolutionFolder);

					Orchestrator = null;
					ExampleClass = null;
					ValidatorClass = null;

					LoadClassList(resourceClassFile.ClassName);
					var policy = LoadPolicy(SolutionFolder);

					replacementsDictionary.Add("$companymoniker$", string.IsNullOrWhiteSpace(moniker) ? "acme" : moniker);
					replacementsDictionary.Add("$securitymodel$", string.IsNullOrWhiteSpace(policy) ? "none" : "OAuth");
					replacementsDictionary.Add("$policy$", string.IsNullOrWhiteSpace(policy) ? "none" : "using");
					replacementsDictionary.Add("$entitynamespace$", entityClassFile.ClassNameSpace);
					replacementsDictionary.Add("$domainnamespace$", resourceClassFile.ClassNamespace);
					replacementsDictionary.Add("$orchestrationnamespace$", Orchestrator.ClassNamespace);

					if (ValidatorClass != null)
					{
						hasValidator = true;
						replacementsDictionary.Add("$validationnamespace$", ValidatorClass.ClassNamespace);
					}
					else
					{
						hasValidator = false;
						replacementsDictionary.Add("$validationnamespace$", "none");
					}

					if (ExampleClass != null)
						replacementsDictionary.Add("$singleexamplenamespace$", ExampleClass.ClassNamespace);
					else
						replacementsDictionary.Add("$singleexamplenamespace$", "none");

					var model = Emit(replacementsDictionary,
									 hasValidator,
									 resourceClassFile,
									 entityClassFile,
									 policy,
									 form.DatabaseColumns);

					replacementsDictionary.Add("$model$", model);
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

		public string Emit(Dictionary<string, string> replacementsDictionary, bool hasValidator, ResourceClassFile domain, EntityClassFile entity, string policy, List<DBColumn> Columns)
		{
			var results = new StringBuilder();
			var nn = new NameNormalizer(domain.ClassName);
			var columns = Utilities.LoadClassColumns(domain.FileName, entity.FileName, Columns)?.ToList();
			var moniker = replacementsDictionary["$companymoniker$"];

			var pkcolumns = new List<ClassMember>();

			foreach (var column in columns)
			{
				bool includeColumn = false;

				foreach (var entityMember in column.EntityNames)
				{
					includeColumn |= entityMember.IsPrimaryKey;
				}

				if (includeColumn)
					pkcolumns.Add(column);
			}

			// --------------------------------------------------------------------------------
			//	Class
			// --------------------------------------------------------------------------------

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{domain.ClassName} Controller");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine("\t[ApiVersion(\"1.0\")]");
			results.AppendLine($"\tpublic class {replacementsDictionary["$safeitemname$"]} : COFRSController");
			results.AppendLine("\t{");
			results.AppendLine($"\t\tprivate readonly ILogger<{replacementsDictionary["$safeitemname$"]}> Logger;");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	Constructor
			// --------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInitializes a {replacementsDictionary["$safeitemname$"]}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\tpublic {replacementsDictionary["$safeitemname$"]}(ILogger<{replacementsDictionary["$safeitemname$"]}> logger)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger = logger;");
			results.AppendLine("\t\t}");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	GET Collection Endpoint
			// --------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tReturns a collection of {nn.PluralForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<remarks>This call supports RQL. The call will only return up to a maximum of \"QueryLimit\" records, where the value of the query limit is predefined in the service and cannot be changed by the user.</remarks>");
			results.AppendLine($"\t\t///\t<response code=\"200\">Returns a collection of {nn.PluralForm}</response>");
			results.AppendLine("\t\t[HttpGet]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if (!string.IsNullOrWhiteSpace(policy))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RqlCollection<{domain.ClassName}>))]");

			if (ExampleClass != null)
				results.AppendLine($"\t\t[SwaggerResponseExample(HttpStatusCode.OK, typeof({domain.ClassName}CollectionExample))]");

			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IHttpActionResult> Get{nn.PluralForm}Async()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.RequestUri.AbsolutePath}\");");
			results.AppendLine("\t\t\tvar node = RqlNode.Parse(Request.RequestUri.Query);");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = ServiceContainer.RequestServices.Get<I{domain.ClassName}Validator>(User);");
				results.AppendLine("\t\t\tawait validator.ValidateForGetAsync(node).ConfigureAwait(false);");
				results.AppendLine();
			}

			results.AppendLine($"\t\t\tusing (var service = ServiceContainer.RequestServices.Get<IServiceOrchestrator>(User))");
			results.AppendLine("\t\t\t{");
			results.AppendLine($"\t\t\t\tvar collection = await service.GetCollectionAsync<{domain.ClassName}>(HttpContext.Current.Request, node).ConfigureAwait(false);");
			results.AppendLine($"\t\t\t\treturn Ok(collection);");
			results.AppendLine("\t\t\t}");
			results.AppendLine("\t\t}");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	GET Single Endpoint
			// --------------------------------------------------------------------------------

			if (pkcolumns.Count() > 0)
			{
				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tReturns a {nn.SingleForm}");
				results.AppendLine("\t\t///\t</summary>");
				results.AppendLine("\t\t///\t<remarks>This call supports RQL. </remarks>");
				results.AppendLine("\t\t[HttpGet]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.OK, Type = typeof({domain.ClassName}))]");

				if (ExampleClass != null)
					results.AppendLine($"\t\t[SwaggerResponseExample(HttpStatusCode.OK, typeof({domain.ClassName}Example))]");

				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine("\t\t[SupportRQL]");

				EmitEndpoint(domain, "Get", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.RequestUri.AbsolutePath}\");");
				results.AppendLine("\t\t\tvar translationOptions = ServiceContainer.RequestServices.GetService<ITranslationOptions>();");
				results.AppendLine($"\t\t\tvar url = new Uri(translationOptions.RootUrl, $\"{BuildRoute(nn.PluralCamelCase, pkcolumns)}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"href={{url.AbsoluteUri}}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.RequestUri.Query));");

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = ServiceContainer.RequestServices.Get<I{domain.ClassName}Validator>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForGetAsync(node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = ServiceContainer.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");
				results.AppendLine($"\t\t\t\tvar item = await service.GetSingleAsync<{domain.ClassName}>(node).ConfigureAwait(false);");
				results.AppendLine();
				results.AppendLine("\t\t\t\tif (item == null)");
				results.AppendLine("\t\t\t\t\treturn NotFound();");
				results.AppendLine();
				results.AppendLine("\t\t\t\treturn Ok(item);");
				results.AppendLine("\t\t\t}");

				results.AppendLine("\t\t}");
				results.AppendLine();
			}

			// --------------------------------------------------------------------------------
			//	POST Endpoint
			// --------------------------------------------------------------------------------
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tAdds a {nn.SingleForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<remarks>Add a {nn.SingleForm} to the datastore.</remarks>");
			results.AppendLine("\t\t[HttpPost]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if (!string.IsNullOrWhiteSpace(policy))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			if (ExampleClass != null)
				results.AppendLine($"\t\t[SwaggerRequestExample(typeof({domain.ClassName}), typeof({domain.ClassName}Example))]");

			results.AppendLine($"\t\t[SwaggerResponse((int)HttpStatusCode.Created, Type = typeof({domain.ClassName}))]");

			if (ExampleClass != null)
				results.AppendLine($"\t\t[SwaggerResponseExample(HttpStatusCode.Created, typeof({domain.ClassName}Example))]");

			results.AppendLine($"\t\t[SwaggerResponseHeader(HttpStatusCode.Created, \"Location\", \"string\", \"Returns Href of the new {domain.ClassName}\")]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\tpublic async Task<IHttpActionResult> Add{domain.ClassName}Async([FromBody] {domain.ClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.RequestUri.AbsolutePath}\");");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = ServiceContainer.RequestServices.Get<I{domain.ClassName}Validator>(User);");
				results.AppendLine("\t\t\tawait validator.ValidateForAddAsync(item).ConfigureAwait(false);");
				results.AppendLine();
			}

			results.AppendLine($"\t\t\tusing (var service = ServiceContainer.RequestServices.Get<IServiceOrchestrator>(User))");
			results.AppendLine("\t\t\t{");
			results.AppendLine($"\t\t\t\titem = await service.AddAsync(item).ConfigureAwait(false);");
			results.AppendLine($"\t\t\t\treturn Created(item.href.AbsoluteUri, item);");
			results.AppendLine("\t\t\t}");
			results.AppendLine("\t\t}");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	PUT Endpoint
			// --------------------------------------------------------------------------------
			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tUpdate a {nn.SingleForm}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
			results.AppendLine("\t\t[HttpPut]");
			results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
			results.AppendLine($"\t\t[Route(\"{nn.PluralCamelCase}\")]");

			if (!string.IsNullOrWhiteSpace(policy))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			if (ExampleClass != null)
				results.AppendLine($"\t\t[SwaggerRequestExample(typeof({domain.ClassName}), typeof({domain.ClassName}Example))]");

			results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NoContent)]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\tpublic async Task<IHttpActionResult> Update{domain.ClassName}Async([FromBody] {domain.ClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.RequestUri.AbsolutePath}\");");
			results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"href={{item.href.AbsoluteUri}}\")");
			results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.RequestUri.Query));");
			results.AppendLine();

			if (hasValidator)
			{
				results.AppendLine($"\t\t\tvar validator = ServiceContainer.RequestServices.Get<I{domain.ClassName}Validator>(User);");
				results.AppendLine("\t\t\tawait validator.ValidateForUpdateAsync(item, node).ConfigureAwait(false);");
				results.AppendLine();
			}

			results.AppendLine($"\t\t\tusing (var service = ServiceContainer.RequestServices.Get<IServiceOrchestrator>(User))");
			results.AppendLine("\t\t\t{");
			results.AppendLine($"\t\t\t\tawait service.UpdateAsync(item, node).ConfigureAwait(false);");
			results.AppendLine($"\t\t\t\treturn NoContent();");
			results.AppendLine("\t\t\t}");
			results.AppendLine("\t\t}");
			results.AppendLine();

			if (pkcolumns.Count() > 0)
			{
				// --------------------------------------------------------------------------------
				//	PATCH Endpoint
				// --------------------------------------------------------------------------------

				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tUpdate a {nn.SingleForm} using patch commands");
				results.AppendLine("\t\t///\t</summary>");
				results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
				results.AppendLine("\t\t[HttpPatch]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NoContent)]");

				results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				EmitEndpoint(domain, "Patch", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.RequestUri.AbsolutePath}\");");
				results.AppendLine();
				results.AppendLine("\t\t\tvar translationOptions = ServiceContainer.RequestServices.GetService<ITranslationOptions>();");
				results.AppendLine($"\t\t\tvar url = new Uri(translationOptions.RootUrl, $\"{BuildRoute(nn.PluralCamelCase, pkcolumns)}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"href={{url.AbsoluteUri}}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.RequestUri.Query));");
				results.AppendLine();

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = ServiceContainer.RequestServices.Get<I{domain.ClassName}Validator>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForPatchAsync(commands, node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = ServiceContainer.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");
				results.AppendLine($"\t\t\t\tawait service.PatchAsync<{domain.ClassName}>(commands, node).ConfigureAwait(false);");
				results.AppendLine($"\t\t\t\treturn NoContent();");
				results.AppendLine("\t\t\t}");
				results.AppendLine("\t\t}");
				results.AppendLine();

				// --------------------------------------------------------------------------------
				//	DELETE Endpoint
				// --------------------------------------------------------------------------------

				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tDelete a {nn.SingleForm}");
				results.AppendLine("\t\t///\t</summary>");
				results.AppendLine($"\t\t///\t<remarks>Delets a {nn.SingleForm} in the datastore.</remarks>");
				results.AppendLine("\t\t[HttpDelete]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NoContent)]");

				EmitEndpoint(domain, "Delete", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.RequestUri.AbsolutePath}\");");
				results.AppendLine("\t\t\tvar translationOptions = ServiceContainer.RequestServices.GetService<ITranslationOptions>();");
				results.AppendLine($"\t\t\tvar url = new Uri(translationOptions.RootUrl, $\"{BuildRoute(nn.PluralCamelCase, pkcolumns)}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"href={{url.AbsoluteUri}}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.RequestUri.Query));");
				results.AppendLine();

				if (hasValidator)
				{
					results.AppendLine($"\t\t\tvar validator = ServiceContainer.RequestServices.Get<I{domain.ClassName}Validator>(User);");
					results.AppendLine("\t\t\tawait validator.ValidateForDeleteAsync(node).ConfigureAwait(false);");
					results.AppendLine();
				}

				results.AppendLine($"\t\t\tusing (var service = ServiceContainer.RequestServices.Get<IServiceOrchestrator>(User))");
				results.AppendLine("\t\t\t{");

				results.AppendLine($"\t\t\t\tawait service.DeleteAsync<{domain.ClassName}>(node).ConfigureAwait(false);");

				results.AppendLine($"\t\t\t\treturn NoContent();");
				results.AppendLine("\t\t\t}");
				results.AppendLine("\t\t}");
				results.AppendLine("\t}");
			}

			return results.ToString();
		}

		private void EmitEndpoint(ResourceClassFile domain, string action, StringBuilder results, IEnumerable<ClassMember> pkcolumns)
		{
			results.Append($"\t\tpublic async Task<IHttpActionResult> {action}{domain.ClassName}Async(");
			bool first = true;

			foreach (var domainColumn in pkcolumns)
			{
				foreach (var column in domainColumn.EntityNames)
				{
					if (first)
						first = false;
					else
						results.Append(", ");
					var dataType = GetNonNullableDataType(column);
					results.Append($"{dataType} {column.EntityName}");
				}
			}

			if (string.Equals(action, "patch", StringComparison.OrdinalIgnoreCase))
				results.AppendLine(", [FromBody] PatchCommand[] commands)");
			else
				results.AppendLine(")");
		}
		private void EmitUrl(StringBuilder results, string routeName, IEnumerable<ClassMember> pkcolumns)
		{
			results.Append($"\t\t\tvar url = new Uri(options.RootUrl, $\"{routeName}/id");

			foreach (var domainColumn in pkcolumns)
			{
				foreach (var column in domainColumn.EntityNames)
				{
					results.Append($"/{{{column.EntityName}}}");
				}
			}

			results.Append("\");");
		}

		private static string BuildRoute(string routeName, IEnumerable<ClassMember> pkcolumns)
		{
			var route = new StringBuilder();

			route.Append(routeName);
			route.Append("/id");

			foreach (var domainColumn in pkcolumns)
			{
				foreach (var column in domainColumn.EntityNames)
				{
					route.Append($"/{{{column.EntityName}}}");
				}
			}

			return route.ToString();
		}

		private static void EmitRoute(StringBuilder results, string routeName, IEnumerable<ClassMember> pkcolumns)
		{
			results.Append($"\t\t[Route(\"{routeName}/id");

			foreach (var domainColumn in pkcolumns)
			{
				foreach (var column in domainColumn.EntityNames)
				{
					results.Append($"/{{{column.EntityName}}}");
				}
			}

			results.AppendLine("\")]");
		}

		private string GetNonNullableDataType(DBColumn column)
		{
			if (column.DataType.GetType() == typeof(NpgsqlDbType))
			{
				switch (column.DataType)
				{
					case NpgsqlDbType.Boolean:
						return "bool";

					case NpgsqlDbType.Integer:
						return "int";

					case NpgsqlDbType.Smallint:
						return "short";

					case NpgsqlDbType.Bigint:
						return "long";

					case NpgsqlDbType.Real:
						return "float";

					case NpgsqlDbType.Double:
						return "double";

					case NpgsqlDbType.Numeric:
					case NpgsqlDbType.Money:
						return "decimal";

					case NpgsqlDbType.Text:
					case NpgsqlDbType.Varchar:
					case NpgsqlDbType.Citext:
					case NpgsqlDbType.Json:
					case NpgsqlDbType.Jsonb:
					case NpgsqlDbType.Xml:
					case NpgsqlDbType.Name:
						return "string";

					case NpgsqlDbType.Char:
						if (column.Length == 1)
							return "char";
						else
							return "string";

					case NpgsqlDbType.Point:
						return "NpgsqlPoint";

					case NpgsqlDbType.LSeg:
						return "NpgsqlLSeg";

					case NpgsqlDbType.Path:
						return "NpgsqlPath";

					case NpgsqlDbType.Polygon:
						return "NpgsqlPolygon";

					case NpgsqlDbType.Line:
						return "NpgsqlLine";

					case NpgsqlDbType.Circle:
						return "NpgsqlCircle";

					case NpgsqlDbType.Box:
						return "NpgsqlBox";

					case NpgsqlDbType.Bit:
						if (column.Length > 1)
							return "BitArray";
						else
							return "bool";

					case NpgsqlDbType.Varbit:
						return "BitArray";

					case NpgsqlDbType.Hstore:
						return "Dictionary<string,string>";

					case NpgsqlDbType.Uuid:
						return "Guid";

					case NpgsqlDbType.Inet:
						return "IPAddress";

					case NpgsqlDbType.MacAddr:
						return "PhysicalAddress";

					case NpgsqlDbType.TsVector:
						return "NpgsqlTsVector";

					case NpgsqlDbType.TsQuery:
						return "NpgsqlTsQuery";

					case NpgsqlDbType.Date:
					case NpgsqlDbType.Timestamp:
					case NpgsqlDbType.TimestampTz:
						return "DateTime";

					case NpgsqlDbType.Interval:
					case NpgsqlDbType.Time:
						return "TimeSpan";

					case NpgsqlDbType.TimeTz:
						return "DateTimeOffset";

					case NpgsqlDbType.Bytea:
						return "byte[]";

					case NpgsqlDbType.Oid:
					case NpgsqlDbType.Xid:
					case NpgsqlDbType.Cid:
						return "uint";

					case NpgsqlDbType.Oidvector:
						return "uint[]";

					case NpgsqlDbType.Geometry:
						return "PostgisGeometry";
				}
			}
			else if (column.DataType.GetType() == typeof(MySqlDbType))
			{
				switch (column.DataType)
				{
					case MySqlDbType.Int16:
					case MySqlDbType.Year:
						return "short";

					case MySqlDbType.Int24:
					case MySqlDbType.Int32:
						return "int";

					case MySqlDbType.Int64:
						return "long";

					case MySqlDbType.Binary:
					case MySqlDbType.VarBinary:
					case MySqlDbType.Blob:
					case MySqlDbType.TinyBlob:
					case MySqlDbType.MediumBlob:
					case MySqlDbType.LongBlob:
						return "byte[]";

					case MySqlDbType.Byte:
						if ( column.Length > 1 )
							return "byte[]";
						else
							return "byte";

					case MySqlDbType.Decimal:
						return "decimal";

					case MySqlDbType.Float:
						return "single";

					case MySqlDbType.Double:
						return "double";

					case MySqlDbType.Timestamp:
					case MySqlDbType.Date:
					case MySqlDbType.DateTime:
						return "DateTime";

					case MySqlDbType.Time:
						return "TimeSpan";

					case MySqlDbType.VarChar:
					case MySqlDbType.VarString:
					case MySqlDbType.Text:
					case MySqlDbType.LongText:
					case MySqlDbType.TinyText:
					case MySqlDbType.MediumText:
					case MySqlDbType.String:
						return "string";

					case MySqlDbType.Bit:
						if (column.Length > 1)
							return "BitArray";
						else
							return "bool";

					case MySqlDbType.UInt16:
						return "ushort";

					case MySqlDbType.UInt24:
					case MySqlDbType.UInt32:
						return "uint";

					case MySqlDbType.UInt64:
						return "ulong";

					case MySqlDbType.Enum:
						return "Enum";

					case MySqlDbType.Guid:
						return "Guid";

					case MySqlDbType.JSON:
						return "string";
				}
			}
			else if (column.DataType.GetType() == typeof(SqlDbType))
			{
				switch (column.DataType)
				{
					case SqlDbType.Bit:
						return "bool";

					case SqlDbType.TinyInt:
						return "sbyte";

					case SqlDbType.SmallInt:
						return "short";

					case SqlDbType.Int:
						return "int";

					case SqlDbType.BigInt:
						return "long";

					case SqlDbType.Real:
						return "float";

					case SqlDbType.Float:
						return "double";

					case SqlDbType.Decimal:
					case SqlDbType.Money:
					case SqlDbType.SmallMoney:
						return "decimal";

					case SqlDbType.Date:
					case SqlDbType.DateTime:
					case SqlDbType.DateTime2:
					case SqlDbType.SmallDateTime:
						return "DateTime";

					case SqlDbType.DateTimeOffset:
						return "DateTimeOffset";

					case SqlDbType.NText:
					case SqlDbType.NVarChar:
					case SqlDbType.Text:
					case SqlDbType.VarChar:
						return "string";

					case SqlDbType.NChar:
					case SqlDbType.Char:
						if (column.Length == 1)
							return "char";
						else
							return "string";

					case SqlDbType.Binary:
					case SqlDbType.VarBinary:
					case SqlDbType.Image:
						return "byte[]";

					case SqlDbType.Time:
						return "TimeSpan";

					case SqlDbType.UniqueIdentifier:
						return "Guid";
				}
			}

			return "Unknown";
		}

		private void LoadClassList(string DomainClassName)
		{
			try
			{
				foreach (var file in Directory.GetFiles(SolutionFolder, "*.cs"))
				{
					LoadDomainClass(file, DomainClassName);
				}

				foreach (var folder in Directory.GetDirectories(SolutionFolder))
				{
					LoadDomainList(folder, DomainClassName);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void LoadDomainClass(string file, string domainClassName)
		{
			try
			{
				var data = File.ReadAllText(file).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); ;
				var className = string.Empty;
				var namespaceName = string.Empty;

				foreach (var line in data)
				{
					var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

					if (match.Success)
						className = match.Groups["className"].Value;

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

						if (string.Equals(classfile.ClassName, $"{domainClassName}Example", StringComparison.OrdinalIgnoreCase))
							ExampleClass = classfile;

						if (string.Equals(classfile.ClassName, $"{domainClassName}Validator", StringComparison.OrdinalIgnoreCase))
							ValidatorClass = classfile;
					}

					className = string.Empty;
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		private void LoadDomainList(string folder, string DomainClassName)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "*.cs"))
				{
					LoadDomainClass(file, DomainClassName);
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					LoadDomainList(subfolder, DomainClassName);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
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
	}
}
