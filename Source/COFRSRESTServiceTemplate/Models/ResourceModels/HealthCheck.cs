﻿namespace $safeprojectname$.Models.ResourceModels
{
	///	<summary>
	///	A message used to verify that the service is running.
	///	</summary>
	public class HealthCheck
	{
		///	<summary>
		///	The "I am alive" message.
		///	</summary>
		public string Message { get; set; }
	}
}