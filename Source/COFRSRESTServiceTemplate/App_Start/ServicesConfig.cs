using AutoMapper;
using COFRS;
$if$ ($securitymodel$ == OAuth)using COFRS.OAuth;
$endif$using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using $safeprojectname$.Orchestration;
using $safeprojectname$.Repository;

namespace $safeprojectname$.App_Start
{
	/// <summary>
	/// A <see langword="static"/> class used to configure child services for this service.
	/// </summary>
	public static class ServicesConfig
	{
		private static TranslationOptions TranslationOptions { get; set; }
		private static RepositoryOptions RepositoryOptions { get; set; }
		private static ApiOptions ApiOptions { get; set; }
		private static ObjectPoolProvider Provider { get; set; }
		$if$ ($securitymodel$ == OAuth)private static Scopes Scopes { get; set; }
		/// An extention method used to configure services with Microsoft's dependency injection services.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> that this function extends.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> for the service.</param>
		/// <param name="scopes">The list of <see cref="Scope"/>s required by this service</param>
		/// <returns>An <see cref="IApiOptions"/> interface to the API options defined for the service.</returns>
		public static IApiOptions ConfigureServices(this IServiceCollection services, IConfiguration configuration, List<Scope> scopes)
		$else$/// <summary>
		/// An extention method used to configure services with Microsoft's dependency injection services.
		/// </summary>
		/// <param name="services">The <see cref="IServiceCollection"/> that this function extends.</param>
		/// <param name="configuration">The <see cref="IConfiguration"/> for the service.</param>
		/// <returns>An <see cref="IApiOptions"/> interface to the API options defined for the service.</returns>
		public static IApiOptions ConfigureServices(this IServiceCollection services, IConfiguration configuration)
		$endif${
			services.AddControllersAsServices(typeof(Startup).Assembly.GetExportedTypes()
				.Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition)
				.Where(t => typeof(IController).IsAssignableFrom(t) || t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)));

			// instantiate and configure logging. Using serilog here, to log to console and a text-file.
			var loggerConfig = new LoggerConfiguration().ReadFrom.Configuration(configuration);
			Log.Logger = loggerConfig.CreateLogger();

			services.AddSingleton(new LoggerFactory().AddSerilog(Log.Logger));
			services.AddLogging();

			Log.Logger.Information("Service is starting...");

			//	If you wish to use caching, uncomment out the next line, and replace the DefaultCacheProvider
			//	with a provider to the caching service of your choice. Also, change the -1 to the number of
			//	megabytes the cache should be limited to (-1 = no limit).

			//	services.AddSingleton<ICacheProvider>(new DefaultCacheProvider(-1));
		 
			ApiOptions = ApiOptions.Load(configuration);
			services.AddSingleton<IApiOptions>(ApiOptions);
			services.AddApiOptions(ApiOptions);

			Provider = new DefaultObjectPoolProvider();
			services.AddSingleton(Provider);

			TranslationOptions = new TranslationOptions(configuration.GetSection("ApiSettings").GetValue<string>("RootUrl"));
			services.AddSingleton<ITranslationOptions>(TranslationOptions);

			RepositoryOptions = new RepositoryOptions(configuration.GetConnectionString("DefaultConnection"),
													  configuration.GetSection("ApiSettings").GetValue<int>("QueryLimit"),
													  configuration.GetSection("ApiSettings").GetValue<TimeSpan>("Timeout"));
			services.AddSingleton<IRepositoryOptions>(RepositoryOptions);

			var myAssembly = Assembly.GetExecutingAssembly();
			AutoMapperFactory.MapperConfiguration = new MapperConfiguration(cfg => cfg.AddMaps(myAssembly));
			AutoMapperFactory.CreateMapper();

			services.AddTransient<IServiceRepository>(sp => new ServiceRepository(sp.GetService<ILogger<ServiceRepository>>(), sp, RepositoryOptions));
			services.AddTransientWithParameters<IServiceOrchestrator, ServiceOrchestrator<IServiceRepository>>();
	
			services.InitializeFactories();
			return ApiOptions;
		}
	}
}