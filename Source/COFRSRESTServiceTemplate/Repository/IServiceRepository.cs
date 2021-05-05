using COFRS;
$if$ ($databasetech$ == sqlserver)using COFRS.SqlServer;
$endif$$if$ ($databasetech$ == mysql)using COFRS.MySql;
$endif$$if$ ($databasetech$ == postgresql)using COFRS.Postgresql;
$endif$
namespace $safeprojectname$.Repository
{
	///	<summary>
	///	The IServiceRepository 
	///	</summary>
	$if$ ($databasetech$ == sqlserver)public interface IServiceRepository : ISqlServerRepository
	$endif$$if$ ($databasetech$ == mysql)public interface IServiceRepository : IMySqlRepository
	$endif$$if$ ($databasetech$ == postgresql)public interface IServiceRepository : IPostgresqlRepository
	$endif${
	}
}
