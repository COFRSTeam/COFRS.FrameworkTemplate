﻿using $safeprojectname$.Orchestration.DomainModels;
using Swashbuckle.Examples;

namespace $safeprojectname$.SwaggerExamples
{
	/// <summary>
	/// Example of Heartbeat object
	/// </summary>
	public class HeartbeatExample : IExamplesProvider
	{
		/// <summary>
		/// Gets an example of the heartbeat object
		/// </summary>
		/// <returns></returns>
		public object GetExamples()
		{
			return new Heartbeat()
			{
				Message = "$safeprojectname$ is running"
			};
		}
	}
}