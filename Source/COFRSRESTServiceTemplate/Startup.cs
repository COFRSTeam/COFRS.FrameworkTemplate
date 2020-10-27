using COFRS;
$if$ ($securitymodel$ == OAuth)using COFRS.OAuth;
$endif$using $safeprojectname$.App_Start;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin;
using Microsoft.Web.Http;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.ExceptionHandling;
using System.Web.Mvc;
using System.Web.Routing;
using Newtonsoft.Json;
$if$ ($securitymodel$ == OAuth)using IdentityServer3.AccessTokenValidation;
$endif$using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

[assembly: OwinStartup(typeof($safeprojectname$.Startup))]

namespace $safeprojectname$
{
	/// <summary>
	/// Startup
	/// </summary>
	public partial class Startup
	{
		/// <summary>
		/// The configuration
		/// </summary>
		public static IConfiguration AppConfig { get; private set; }

		/// <summary>
		/// Instantiates the Startup class
		/// </summary>
		public Startup()
		{
			//	Get the service environment from the web.config file
			var environment = ConfigurationManager.AppSettings["ENVIRONMENT"];

			//	Setup appSettings to be read from the json configuration files
			AppConfig = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: true)
				.AddJsonFile($"appsettings.{environment}.json", optional: true)
				.Build();
		}

		/// <summary>
		/// Configuration
		/// </summary>
		/// <param name="app">The Microsoft Owin AppBuilder</param>
		public void Configuration(IAppBuilder app)
		{
			//	Create the httpConfiguration and the service collection for this service
			var httpConfiguration = new HttpConfiguration();

			//	configure JSON
			var defaultSettings = new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented,
				ContractResolver = new COFRSJsonContractResolver(),
				Converters = new List<JsonConverter>
					{
						new ApiJsonEnumConverter(),
						new ApiJsonByteArrayConverter()
					}
			};

			JsonConvert.DefaultSettings = () => { return defaultSettings; };
			httpConfiguration.Formatters.JsonFormatter.SerializerSettings = defaultSettings;
			
			//	setup filters and routing
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);

			httpConfiguration.UseRql();
			var services = new ServiceCollection();

			$if$ ( $securitymodel$ == OAuth )var authorityUrl = AppConfig["OAuth2:AuthorityURL"];
			var scopes = Scope.Load(AppConfig.GetSection("OAuth2:Scopes"));
			var policies = Policy.Load(AppConfig.GetSection("OAuth2:Policies"));

			$endif$//	Configure the internal services used by this service
			$if$ ($securitymodel$ == OAuth)var options = services.ConfigureServices(AppConfig, scopes);$else$var options = services.ConfigureServices(AppConfig);$endif$

			$if$ ($securitymodel$ == OAuth)var idsOptions = new IdentityServerBearerTokenAuthenticationOptions
			 {
				 Authority = authorityUrl,
				 ValidationMode = ValidationMode.ValidationEndpoint,
				 EnableValidationResultCache = true,
				 ValidationResultCacheDuration = TimeSpan.FromMinutes(30)
			 };

			app.ConfigureAuthentication(services, idsOptions, scopes, policies);
			
			$endif$//	Setup dependency injection
			var provider = services.BuildServiceProvider();
			DependencyResolver.SetResolver(new COFRSMvcDependencyResolver(provider));
			httpConfiguration.DependencyResolver = new COFRSApiDependencyResolver(provider);

			//  Setup the HTTP routing
			httpConfiguration.MapHttpAttributeRoutes();

			//	Configure CORS Origins
			var AllowedCorsOrigins = AppConfig["ApiSettings:AllowedCors"];
			var cors = new EnableCorsAttribute(AllowedCorsOrigins, "*", "*");
			httpConfiguration.EnableCors(cors);
			httpConfiguration.Services.Replace(typeof(IExceptionHandler), new COFRSExceptionHandler());

			//	Set up versioning
			var apiExplorer = httpConfiguration.UseVerioning(provider, new ApiVersion(1, 0));

			//	Configure Swagger
			$if$ ($securitymodel$ == OAuth)app.RegisterSwagger(AppConfig, options, httpConfiguration, apiExplorer, scopes);
			$else$app.RegisterSwagger(AppConfig, options, httpConfiguration, apiExplorer);$endif$

			// invoke the web API 
			app.UseWebApi(httpConfiguration);

			//	Automatically redirect to swagger upon load
			app.Run(context =>
			{
				context.Response.Redirect("swagger/ui/index");
				return Task.CompletedTask;
			});
		}
	}
}
