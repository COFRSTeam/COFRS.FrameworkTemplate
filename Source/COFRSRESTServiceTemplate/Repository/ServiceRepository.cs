using COFRS;
$if$ ($databasetech$ == sqlserver)using COFRS.SqlServer;
$endif$$if$ ($databasetech$ == mysql)using COFRS.MySql;
$endif$$if$ ($databasetech$ == postgresql)using COFRS.Postgresql;
$endif$using Microsoft.Extensions.Logging;
using System;

namespace $safeprojectname$.Repository
{
	///	<summary>
	///	Used to access the database on behalf of the service
	///	</summary>
	$if$ ($databasetech$ == mysql )public class ServiceRepository : MySqlRepository, IServiceRepository
	$endif$$if$ ($databasetech$ == postgresql )public class ServiceRepository : PostgresqlRepository, IServiceRepository
	$endif$$if$ ($databasetech$ == sqlserver )public class ServiceRepository : SqlServerRepository, IServiceRepository
	$endif$
	{
		///	<summary>
		///	Instantiates a ServiceRepository object used to access the database on behalf of the service
		///	</summary>
		///	<param name="logger">A generic interface for logging where the category name is derrived from 
		///	the specified TCategoryName type name.</param>
		///	<param name="provider">Defines a mechanism for retrieving a service object; that is, an object 
		///	that provides custom support to other objects.</param>
		///	<param name="options">The runtime options for this repository</param>
		$if$ ($databasetech$ == mysql )public ServiceRepository(ILogger<MySqlRepository> logger, IServiceProvider provider, IRepositoryOptions options) : base(logger, provider, options)
		$endif$$if$ ($databasetech$ == postgresql )public ServiceRepository(ILogger<PostgresqlRepository> logger, IServiceProvider provider, IRepositoryOptions options) : base(logger, provider, options)
		$endif$$if$ ($databasetech$ == sqlserver )public ServiceRepository(ILogger<SqlServerRepository> logger, IServiceProvider provider, IRepositoryOptions options) : base(logger, provider, options)
		$endif${
		}
	}
}
