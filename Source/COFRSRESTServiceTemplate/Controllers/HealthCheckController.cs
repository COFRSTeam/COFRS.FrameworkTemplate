using COFRS;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Http;
using Swashbuckle.Examples;
using Swashbuckle.Swagger.Annotations;
using System.Net;
using System.Web.Http;
using System.Web.Routing;
using $safeprojectname$.Models.ResourceModels;
using $safeprojectname$.Models.SwaggerExamples;

namespace $safeprojectname$.Controllers
{
	///	<summary>
	///	Heartbeat controller
	///	</summary>
	[ApiVersion("1.0")]
	public class HealthCheckController : COFRSController
	{
		private readonly ILogger<HealthCheckController> Logger;

		/// <summary>
		/// Constructor
		/// </summary>
		public HealthCheckController(ILogger<HealthCheckController> logger)
		{
			Logger = logger;
		}

		///	<summary>
		///	Returns a heartbeat message
		///	</summary>
		///	<remarks>This method is used to supply "I am alive" messages to monitoring systems.</remarks>
		[HttpGet]
		[MapToApiVersion("1.0")]
		[Route("health_check")]
		[SwaggerResponse(HttpStatusCode.OK, Type = typeof(Heartbeat))]
		[SwaggerResponseExample(HttpStatusCode.OK, typeof(HeartbeatExample))]
		public IHttpActionResult Get()
		{
			Logger.LogInformation($"{Request.Method} {Request.RequestUri.PathAndQuery}");
			return Ok(new HealthCheck() { Message = "$safeprojectname$ is running" });
		}
	}
}
