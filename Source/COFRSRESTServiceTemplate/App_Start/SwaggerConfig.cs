using COFRS;
$if$ ($securitymodel$ == OAuth)using COFRS.OAuth;
$endif$using Microsoft.Extensions.Configuration;
using Microsoft.Web.Http.Description;
using Owin;
using Swashbuckle.Application;
using Swashbuckle.Examples;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;

namespace $safeprojectname$.App_Start
{
	/// <summary>
	/// Swagger Config
	/// </summary>
	public static class SwaggerConfig
{
		/// <summary>
		/// Configures Swagger for the service
		/// </summary>
		$if$ ($securitymodel$ == OAuth)public static IAppBuilder RegisterSwagger(this IAppBuilder app, IConfiguration config, IApiOptions options, HttpConfiguration httpConfiguration, VersionedApiExplorer apiExplorer, List<Scope> scopes)
		$else$public static IAppBuilder RegisterSwagger(this IAppBuilder app, IConfiguration config, IApiOptions options, HttpConfiguration httpConfiguration, VersionedApiExplorer apiExplorer)
		$endif${
			// Setup Swagger

			var rootUrl = config.GetSection("ApiSettings").GetValue<string>("RootUrl");
			$if$ ($securitymodel$ == OAuth)var authorityUrl = config["OAuth2:AuthorityURL"];

			var tokenEndPoint = AuthenticationServices.GetTokenEndpoint(authorityUrl).GetAwaiter().GetResult();
			$endif$
				// This is the call to our swashbuckle config that needs to be called 
				httpConfiguration
				.EnableSwagger("{apiVersion}/swagger",
				swagger =>
				{
					// build a swagger document and endpoint for each discovered API version
					swagger.MultipleApiVersions(
						(apiDescription, version) => apiDescription.GetGroupName() == version,
						info =>
						{
							foreach (var group in apiExplorer.ApiDescriptions)
							{
								var description = @"
<p style=""font-family:verdana; color:#6495ED;"">A detailed description of the service goes here.The description
should give the reader a good idea of what the service does, and should list any dependencies or
restrictions upon its use. The description is written in HTML, so don't be afraid to use formatting
constructs, such as <b>bold</b> and other HTML attributes to enhance the appearance of your description.</p>
<p style=""font-family:verdana; color:#6495ED;"">A professional Web Service uses detailed descriptions to enhance its usablity, and the descriptions
should be visually appealing as well.</p>
<p style=""font-family:verdana; color:#6495ED;"">Note, however, that the support for HTML tags in .NET Framework
is severly limited.</p>";

								if (group.IsDeprecated)
								{
									description += "This API version has been deprecated.";
								}

								info.Version(group.Name, $"$safeprojectname$ {group.ApiVersion}")
									.Contact(c => c.Name("Author").Email("author.email@provider.com"))
									.Description(description);
}
						});

					$if$ ($securitymodel$ == OAuth)var theScopes = new Dictionary<string, string>();
					foreach (var scope in scopes)
						theScopes.Add(scope.Name, scope.Description);

					swagger.OAuth2("oauth2")
						.Description("OAuth2 Client Credentials")
						.Flow("application")
						.TokenUrl(tokenEndPoint)
						.Scopes(s =>
						{
						foreach (var scope in scopes)
							s.Add(scope.Name, scope.Name);
					});

					swagger.OperationFilter<AssignOAuth2SecurityRequirements>();
					$endif$swagger.OperationFilter<SwaggerDefaultValues>();
					swagger.OperationFilter<ApiSwaggerFilter>();
					swagger.OperationFilter<ExamplesOperationFilter>();
					swagger.OperationFilter<DescriptionOperationFilter>();
					swagger.DescribeAllEnumsAsStrings(true);
					swagger.RootUrl((req) => rootUrl);
					swagger.IncludeXmlComments(GetXmlPath("$safeprojectname$.xml"));
					swagger.IncludeXmlComments(GetXmlPath("COFRS.Common.xml"));
					swagger.IncludeXmlComments(GetXmlPath("COFRS.xml"));
				})
				.EnableSwaggerUi(c =>
				{
					c.DocumentTitle("$safeprojectname$");
					c.EnableDiscoveryUrlSelector();
					c.DisableValidator();
				});

			return app;
		}

		///	<summary>
		///	Converts a file name to a fullly qualified file name
		///	</summary>
		///	<param name="filename"></param>
		///	<returns></returns>
		private static string GetXmlPath(string filename)
		{
			return Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Startup)).CodeBase), filename);
		}
	}
}
