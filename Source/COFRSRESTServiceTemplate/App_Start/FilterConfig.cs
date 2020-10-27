using System.Web;
using System.Web.Mvc;

namespace $safeprojectname$.App_Start
{
	/// <summary>
	/// Filter Config
	/// </summary>
	public class FilterConfig
	{
		/// <summary>
		/// Register global filters for the service
		/// </summary>
		/// <param name="filters"></param>
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}
	}
}
