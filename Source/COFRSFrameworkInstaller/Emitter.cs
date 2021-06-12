using COFRS.Template.Common.Models;
using COFRS.Template.Common.ServiceUtilities;
using MySql.Data.MySqlClient;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace COFRS.Template
{
    public class Emitter
	{
		public string EmitController(EntityClassFile entityClass, ResourceClassFile resourceClass, string moniker, string controllerClassName, string ValidatorInterface, string policy)
		{
			var results = new StringBuilder();
			var nn = new NameNormalizer(resourceClass.ClassName);
			var pkcolumns = resourceClass.Members.Where(c => c.EntityNames.Count > 0 && c.EntityNames[0].IsPrimaryKey);

			// --------------------------------------------------------------------------------
			//	Class
			// --------------------------------------------------------------------------------

			results.AppendLine("\t///\t<summary>");
			results.AppendLine($"\t///\t{resourceClass.ClassName} Controller");
			results.AppendLine("\t///\t</summary>");
			results.AppendLine("\t[ApiVersion(\"1.0\")]");
			results.AppendLine($"\tpublic class {controllerClassName} : COFRSController");
			results.AppendLine("\t{");
			results.AppendLine("\t\t///\t<value>");
			results.AppendLine("\t\t///\tA generic interface for logging where the category name is derrived from");
			results.AppendLine($"\t\t///\tthe specified <see cref=\"{controllerClassName}\"/> type name.");
			results.AppendLine("\t\t///\t</value>");
			results.AppendLine($"\t\tprivate readonly ILogger<{controllerClassName}> Logger;");
			results.AppendLine();
			results.AppendLine("\t\t///\t<value>");
			results.AppendLine("\t\t///\tThe validator used to validate any requested actions");
			results.AppendLine("\t\t///\t</value>");
			results.AppendLine($"\t\tprotected {ValidatorInterface} Validator {{ get; set; }}");
			results.AppendLine();
			results.AppendLine("\t\t///\t<value>");
			results.AppendLine("\t\t///\tThe orchestration layer");
			results.AppendLine("\t\t///\t</value>");
			results.AppendLine("\t\tprotected IServiceOrchestrator Orchestrator { get; set; }");
			results.AppendLine();

			// --------------------------------------------------------------------------------
			//	Constructor
			// --------------------------------------------------------------------------------

			results.AppendLine("\t\t///\t<summary>");
			results.AppendLine($"\t\t///\tInstantiates a {controllerClassName}");
			results.AppendLine("\t\t///\t</summary>");
			results.AppendLine("\t\t///\t<param name=\"logger\">A generic interface for logging where the category name is derrived from");
			results.AppendLine($"\t\t///\tthe specified <see cref=\"{controllerClassName}\"/> type name. The logger is activated from dependency injection.</param>");
			results.AppendLine("\t\t///\t<param name=\"orchestrator\">The <see cref=\"IServiceOrchestrator\"/> interface for the Orchestration layer. </param>");
			results.AppendLine($"\t\t///\t<param name=\"validator\">The <see cref=\"{ValidatorInterface}\"/> used to validate requested actions.</param>");
			results.AppendLine($"\t\tpublic {controllerClassName}(ILogger<{controllerClassName}> logger, IServiceOrchestrator orchestrator, {ValidatorInterface} validator)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger = logger;");
			results.AppendLine("\t\t\tValidator = validator;");
			results.AppendLine("\t\t\tOrchestrator = orchestrator;");
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
			results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.OK, Type = typeof(RqlCollection<{nn.SingleForm}>))]");
			if (!string.IsNullOrWhiteSpace(policy))
				results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IHttpActionResult> Get{nn.PluralForm}Async()");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.RequestUri.AbsolutePath}\");");
			results.AppendLine("\t\t\tvar node = RqlNode.Parse(Request.RequestUri.Query);");
			results.AppendLine();

			results.AppendLine("\t\t\tawait Validator.ValidateForGetAsync(node);");
			results.AppendLine($"\t\t\tvar collection = await Orchestrator.GetCollectionAsync<{resourceClass.ClassName}>(Request.RequestUri.Query, node);");
			results.AppendLine($"\t\t\treturn Ok(collection);");
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
				EmitEndpointExamples(entityClass.ServerType, resourceClass.ClassName, results, pkcolumns);
				results.AppendLine("\t\t///\t<remarks>This call supports RQL. Use the RQL select clause to limit the members returned.</remarks>");
				results.AppendLine($"\t\t///\t<response code=\"200\">Returns the specified {nn.SingleForm}.</response>");
				results.AppendLine($"\t\t///\t<response code=\"404\">Not Found - returned when the speicifed {nn.SingleForm} does not exist in the datastore.</response>");
				results.AppendLine("\t\t[HttpGet]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]"); 

				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.OK, Type = typeof({resourceClass.ClassName}))]");

				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NotFound)]");
				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine("\t\t[SupportRQL]");

				EmitEndpoint(entityClass.ServerType, resourceClass.ClassName, "Get", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method} {Request.RequestUri.AbsolutePath}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href=uri:/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.RequestUri.Query));");

				results.AppendLine("\t\t\tawait Validator.ValidateForGetAsync(node);");
				results.AppendLine($"\t\t\tvar item = await Orchestrator.GetSingleAsync<{resourceClass.ClassName}>(node);");
				results.AppendLine();
				results.AppendLine("\t\t\tif (item == null)");
				results.AppendLine("\t\t\t\treturn NotFound();");
				results.AppendLine();
				results.AppendLine("\t\t\treturn Ok(item);");

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

			results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.Created, Type = typeof({resourceClass.ClassName}))]");

			results.AppendLine($"\t\t[SwaggerResponseHeader(HttpStatusCode.Created, \"Location\", \"string\", \"Returns Href of new {resourceClass.ClassName}\")]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine($"\t\tpublic async Task<IHttpActionResult> Add{resourceClass.ClassName}Async([FromBody] {resourceClass.ClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.RequestUri.AbsolutePath}\");");
			results.AppendLine();

			results.AppendLine("\t\t\tawait Validator.ValidateForAddAsync(item);");
			results.AppendLine($"\t\t\t\titem = await Orchestrator.AddAsync(item);");
			results.AppendLine($"\t\t\t\treturn Created(item.Href.AbsoluteUri, item);");

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

			results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NoContent)]");
			results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NotFound)]");
			results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
			results.AppendLine("\t\t[SupportRQL]");
			results.AppendLine($"\t\tpublic async Task<IHttpActionResult> Update{resourceClass.ClassName}Async([FromBody] {resourceClass.ClassName} item)");
			results.AppendLine("\t\t{");
			results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.RequestUri.AbsolutePath}\");");
			results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href=uri:{{item.Href}}\")");
			results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.RequestUri.Query));");
			results.AppendLine();

			results.AppendLine("\t\t\tawait Validator.ValidateForUpdateAsync(item, node);");
			results.AppendLine($"\t\t\tawait Orchestrator.UpdateAsync(item, node);");
			results.AppendLine($"\t\t\treturn NoContent();");
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
				EmitEndpointExamples(entityClass.ServerType, resourceClass.ClassName, results, pkcolumns);
				results.AppendLine("\t\t///\t<param name=\"commands\">The list of patch commands</param>");
				results.AppendLine($"\t\t///\t<remarks>Update a {nn.SingleForm} in the datastore.</remarks>");
				results.AppendLine("\t\t[HttpPatch]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NoContent)]");
				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NotFound)]");
				results.AppendLine($"\t\t[Consumes(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				results.AppendLine($"\t\t[Produces(\"application/vnd.{moniker}.v1+json\", \"application/json\", \"text/json\")]");
				EmitEndpoint(entityClass.ServerType, resourceClass.ClassName, "Patch", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.RequestUri.AbsolutePath}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href=uri:/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.RequestUri.Query));");

				results.AppendLine("\t\t\tawait Validator.ValidateForPatchAsync(commands, node);");
				results.AppendLine($"\t\t\tawait Orchestrator.PatchAsync<{resourceClass.ClassName}>(commands, node);");
				results.AppendLine($"\t\t\treturn NoContent();");
				results.AppendLine("\t\t}");
				results.AppendLine();

				// --------------------------------------------------------------------------------
				//	DELETE Endpoint
				// --------------------------------------------------------------------------------

				results.AppendLine("\t\t///\t<summary>");
				results.AppendLine($"\t\t///\tDelete a {nn.SingleForm}");
				results.AppendLine("\t\t///\t</summary>");
				EmitEndpointExamples(entityClass.ServerType, resourceClass.ClassName, results, pkcolumns);
				results.AppendLine($"\t\t///\t<remarks>Deletes a {nn.SingleForm} in the datastore.</remarks>");
				results.AppendLine("\t\t[HttpDelete]");
				results.AppendLine("\t\t[MapToApiVersion(\"1.0\")]");
				EmitRoute(results, nn.PluralCamelCase, pkcolumns);

				if (!string.IsNullOrWhiteSpace(policy))
					results.AppendLine($"\t\t[Authorize(\"{policy}\")]");

				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NoContent)]");
				results.AppendLine($"\t\t[SwaggerResponse(HttpStatusCode.NotFound)]");

				EmitEndpoint(entityClass.ServerType, resourceClass.ClassName, "Delete", results, pkcolumns);

				results.AppendLine("\t\t{");
				results.AppendLine("\t\t\tLogger.LogTrace($\"{Request.Method}	{Request.RequestUri.AbsolutePath}\");");
				results.AppendLine();
				results.AppendLine($"\t\t\tvar node = RqlNode.Parse($\"Href=uri:/{BuildRoute(nn.PluralCamelCase, pkcolumns)}\")");
				results.AppendLine($"\t\t\t\t\t\t\t  .Merge(RqlNode.Parse(Request.RequestUri.Query));");

				results.AppendLine("\t\t\tawait Validator.ValidateForDeleteAsync(node);");
				results.AppendLine($"\t\t\tawait Orchestrator.DeleteAsync<{resourceClass.ClassName}>(node);");
				results.AppendLine($"\t\t\treturn NoContent();");

				results.AppendLine("\t\t}");
				results.AppendLine("\t}");
			}

			return results.ToString();
		}

		private void EmitEndpointExamples(DBServerType serverType, string resourceClassName, StringBuilder results, IEnumerable<ClassMember> pkcolumns)
		{
			bool first = true;

			foreach (var resourceMember in pkcolumns)
			{
				foreach (var column in resourceMember.EntityNames)
				{
					if (first)
						first = false;
					else
						results.Append(", ");

					string exampleValue = "example";

					if (serverType == DBServerType.POSTGRESQL)
						exampleValue = DBHelper.GetPostgresqlExampleValue(column);
					else if (serverType == DBServerType.MYSQL)
						exampleValue = DBHelper.GetMySqlExampleValue(column);
					else if (serverType == DBServerType.SQLSERVER)
						exampleValue = DBHelper.GetSqlServerExampleValue(column);

					results.AppendLine($"\t\t///\t<param name=\"{column.EntityName}\" example=\"{exampleValue}\">The {column.EntityName} of the {resourceClassName}.</param>");
				}
			}
		}

		private void EmitEndpoint(DBServerType serverType, string resourceClassName, string action, StringBuilder results, IEnumerable<ClassMember> pkcolumns)
		{
			results.Append($"\t\tpublic async Task<IHttpActionResult> {action}{resourceClassName}Async(");
			bool first = true;

			foreach (var domainColumn in pkcolumns)
			{
				foreach (var column in domainColumn.EntityNames)
				{
					if (first)
						first = false;
					else
						results.Append(", ");

					string dataType = "Unrecognized";

					if (serverType == DBServerType.POSTGRESQL)
						dataType = DBHelper.GetNonNullablePostgresqlDataType(column);
					else if (serverType == DBServerType.MYSQL)
						dataType = DBHelper.GetNonNullableMySqlDataType(column);
					else if (serverType == DBServerType.SQLSERVER)
						dataType = DBHelper.GetNonNullableSqlServerDataType(column);

					results.Append($"{dataType} {column.EntityName}");
				}
			}

			if (string.Equals(action, "patch", StringComparison.OrdinalIgnoreCase))
				results.AppendLine(", [FromBody] IEnumerable<PatchCommand> commands)");
			else
				results.AppendLine(")");
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
	}
}
