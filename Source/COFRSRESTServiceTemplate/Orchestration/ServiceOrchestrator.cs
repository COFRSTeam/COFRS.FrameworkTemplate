using COFRS;
using Microsoft.Extensions.Logging;
using System;
using System.Security.Claims;

namespace $safeprojectname$.Orchestration
{
	///	<summary>
	///	The $safeprojectname$Orchestrator
	///	</summary>
	///	<typeparam name="T">The type of repository used by the orchestration layer.</typeparam>
	public class ServiceOrchestrator<T> : BaseOrchestrator<T>, IServiceOrchestrator
	{
		///	<summary>
		///	Initiates the $safeprojectname$Orchestrator
		///	</summary>
		public ServiceOrchestrator() 
		{
		}
	}
}
