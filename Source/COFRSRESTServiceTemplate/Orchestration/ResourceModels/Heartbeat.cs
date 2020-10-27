namespace $safeprojectname$.Orchestration.DomainModels
{
	///	<summary>
	///	A message used to verify that the service is running.
	///	</summary>
	public class Heartbeat
	{
		///	<summary>
		///	The "I am alive" message.
		///	</summary>
		public string Message { get; set; }
	}
}