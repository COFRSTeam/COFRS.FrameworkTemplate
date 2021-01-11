using COFRS;
$if$ ($databasetech$ == sqlserver)using COFRS.SqlServer;
$else$$if$ ($databasetech$ == mysql)using COFRS.MySql;
$else$$if$ ($databasetech$ == postgresql)using COFRS.Postgresql;
$endif$
namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The IServiceRepository 
	///	</summary>
	$if$ ($databasetech$ == sqlserver)public interface IServiceRepository : ISqlServerRepository
	$else$$if$ ($databasetech$ == mysql)public interface IServiceRepository : IMySqlRepository
	$else$$if$ ($databasetech$ == postgresql)public interface IServiceRepository : IPostgresqlRepository
	$endif${
	}
}
