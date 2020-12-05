﻿using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace COFRSFrameworkInstaller
{
	public partial class UserInputGeneral : Form
	{
		#region variables
		private ServerConfig _serverConfig;
		public int InstallType { get; set; }
		public string SolutionFolder { get; set; }
		private bool Populating = false;
		public DBTable DatabaseTable { get; set; }
		public List<DBColumn> DatabaseColumns { get; set; }
		public JObject Examples { get; set; }
		#endregion

		#region Utility functions
		public UserInputGeneral()
		{
			InitializeComponent();
		}

		private void OnLoad(object sender, EventArgs e)
		{
			DatabaseColumns = new List<DBColumn>();
			_portNumber.Location = new Point(103, 60);

			if (InstallType == 1)
			{
				_InstructionsLabel.Text = "Select the database and table that contains the resource/entity combination you wish to translate between. This will select the Entity and Resource models in the dropdowns provided if they exist. Both entity and resource models must exist to generate the AutoMapper Profile. Then press OK to generate the AutoMapper Profile class.";
				_titleLabel.Text = "COFRS AutoMapper Profile Generator";
			}
			else if (InstallType == 4)
			{
				_InstructionsLabel.Text = "Select the database and table that contains the resource/entity combination you wish to create examples for. This will select the Entity and Resource models in the dropdowns provided if they exist. Both entity and resource models must exist to generate the example classes. Then press OK to generate the example classes.";
				_titleLabel.Text = "COFRS Examples Class Generator";
			}
			else if (InstallType == 5)
			{
				_InstructionsLabel.Text = "Select the database and table that contains the resource/entity combination you wish to create a controller for. This will select the Entity and Resource models in the dropdowns provided if they exist. Both entity and resource models must exist to generate a controller. Then press OK to generate the controller.";
				_titleLabel.Text = "COFRS Controller Class Generator";
			}

			ReadServerList();
			LoadClassList();

			if (_entityModelList.Items.Count == 0)
			{
				MessageBox.Show("No entity models were found in the project. Please create a corresponding entity model before attempting to create the class.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				DialogResult = DialogResult.Cancel;
				Close();
			}

			if (_resourceModelList.Items.Count == 0)
			{
				MessageBox.Show("No resource models were found in the project. Please create a corresponding resource model before attempting to create the class.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				DialogResult = DialogResult.Cancel;
				Close();
			}

			OnServerChanged(this, new EventArgs());
		}
		#endregion

		#region User Interactions
		private void OnServerTypeChanged(object sender, EventArgs e)
		{
			try
			{
				if (_serverTypeList.SelectedIndex == 0 || _serverTypeList.SelectedIndex == 1)
				{
					_authenticationList.Enabled = false;
					_authenticationList.Hide();
					_authenticationLabel.Text = "Port Number";
					_portNumber.Show();
					_portNumber.Enabled = true;
					_userName.Enabled = true;
					_userNameLabel.Enabled = true;
					_password.Enabled = true;
					_passwordLabel.Enabled = true;
					_rememberPassword.Enabled = true;
				}
				else
				{
					_authenticationList.Enabled = true;
					_authenticationList.Show();
					_authenticationLabel.Text = "Authentication";
					_portNumber.Hide();
					_portNumber.Enabled = false;
				}

				if (!Populating)
				{
					PopulateServers();
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnServerChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;

					if (server != null)
					{
						if (server.DBType == DBServerType.SQLSERVER)
						{
							_authenticationLabel.Enabled = true;
							_authenticationLabel.Text = "Authentication";
							_authenticationList.Enabled = true;
							_authenticationList.Show();

							_authenticationList.SelectedIndex = (server.DBAuth == DBAuthentication.SQLSERVERAUTH) ? 0 : 1;

							if (server.DBAuth == DBAuthentication.SQLSERVERAUTH)
							{
								_userNameLabel.Enabled = true;
								_userName.Enabled = true;
								_userName.Text = server.Username;

								_passwordLabel.Enabled = true;
								_password.Enabled = true;
								_password.Text = (server.RememberPassword) ? server.Password : string.Empty;

								_rememberPassword.Enabled = true;
								_rememberPassword.Checked = server.RememberPassword;
							}
							else
							{
								_userNameLabel.Enabled = false;
								_userName.Enabled = false;
								_userName.Text = string.Empty;

								_passwordLabel.Enabled = false;
								_password.Enabled = false;
								_password.Text = string.Empty;

								_rememberPassword.Enabled = false;
								_rememberPassword.Checked = false;
							}
						}
						else if (server.DBType == DBServerType.POSTGRESQL)
						{
							_authenticationLabel.Enabled = true;
							_authenticationLabel.Text = "Port Number";
							_authenticationList.Enabled = false;
							_authenticationList.Hide();

							_portNumber.Enabled = true;
							_portNumber.Value = server.PortNumber;

							_userNameLabel.Enabled = true;
							_userName.Enabled = true;
							_userName.Text = server.Username;

							_passwordLabel.Enabled = true;
							_password.Enabled = true;
							_password.Text = (server.RememberPassword) ? server.Password : string.Empty;

							_rememberPassword.Enabled = true;
							_rememberPassword.Checked = server.RememberPassword;
						}
						else if (server.DBType == DBServerType.MYSQL)
						{
							_authenticationLabel.Enabled = true;
							_authenticationLabel.Text = "Port Number";
							_authenticationList.Enabled = false;
							_authenticationList.Hide();

							_portNumber.Enabled = true;
							_portNumber.Value = server.PortNumber;

							_userNameLabel.Enabled = true;
							_userName.Enabled = true;
							_userName.Text = server.Username;

							_passwordLabel.Enabled = true;
							_password.Enabled = true;
							_password.Text = (server.RememberPassword) ? server.Password : string.Empty;

							_rememberPassword.Enabled = true;
							_rememberPassword.Checked = server.RememberPassword;
						}

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnSelectedDatabaseChanged(object sender, EventArgs e)
		{
			try
			{
				var server = (DBServer)_serverList.SelectedItem;
				var db = (string)_dbList.SelectedItem;

				if (server.DBType == DBServerType.POSTGRESQL)
				{
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={_password.Text};";
					_tableList.Items.Clear();

					using (var connection = new NpgsqlConnection(connectionString))
					{
						connection.Open();

						var query = @"
SELECT schemaname, tablename
  FROM pg_catalog.pg_tables
 WHERE schemaname != 'pg_catalog' AND schemaname != 'information_schema';
";

						using (var command = new NpgsqlCommand(query, connection))
						{
							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var dbTable = new DBTable
									{
										Schema = reader.GetString(0),
										Table = reader.GetString(1)
									};

									_tableList.Items.Add(dbTable);
								}
							}
						}
					}
				}
				else if (server.DBType == DBServerType.MYSQL)
				{
					string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};UID={server.Username};PWD={_password.Text};";
					_tableList.Items.Clear();

					using (var connection = new MySqlConnection(connectionString))
					{
						connection.Open();

						var query = @"

SELECT TABLE_SCHEMA, TABLE_NAME FROM information_schema.tables 
 where table_type = 'BASE TABLE'
   and TABLE_SCHEMA = @databaseName;
";

						using (var command = new MySqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@databaseName", db);

							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var dbTable = new DBTable
									{
										Schema = reader.GetString(0),
										Table = reader.GetString(1)
									};

									_tableList.Items.Add(dbTable);
								}
							}
						}
					}
				}
				else
				{
					string connectionString;

					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
						connectionString = $"Server ={server.ServerName};Database={db};Trusted_Connection=True;";
					else
						connectionString = $"Server={server.ServerName};Database={db};uid={server.Username};pwd={_password.Text};";

					_tableList.Items.Clear();

					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();

						var query = @"
select s.name, t.name
  from sys.tables as t with(nolock)
 inner join sys.schemas as s with(nolock) on s.schema_id = t.schema_id
  order by s.name, t.name";

						using (var command = new SqlCommand(query, connection))
						{
							using (var reader = command.ExecuteReader())
							{
								while (reader.Read())
								{
									var dbTable = new DBTable
									{
										Schema = reader.GetString(0),
										Table = reader.GetString(1)
									};
									_tableList.Items.Add(dbTable);
								}
							}
						}
					}
				}

				_tableList.SelectedIndex = -1;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnSelectedTableChanged(object sender, EventArgs e)
		{
			try
            {
                var server = (DBServer)_serverList.SelectedItem;
                if (server == null)
                    return;

                var db = (string)_dbList.SelectedItem;
                if (string.IsNullOrWhiteSpace(db))
                    return;

                var table = (DBTable)_tableList.SelectedItem;
                if (table == null)
                    return;

                Populating = true;
				bool foundit = false;

                for (int i = 0; i < _entityModelList.Items.Count; i++)
                {
                    var entity = (EntityClassFile)_entityModelList.Items[i];

                    if (entity.TableName == table.Table)
                    {
                        _entityModelList.SelectedIndex = i;

                        for (int j = 0; j < _resourceModelList.Items.Count; j++)
                        {
                            var resource = (ResourceClassFile)_resourceModelList.Items[j];

                            if (string.Equals(resource.EntityClass, entity.ClassName, StringComparison.OrdinalIgnoreCase))
                            {
                                _resourceModelList.SelectedIndex = j;
								PopulateDatabaseColumns(server, db, table);
								foundit = true;
								break;
                            }
                        }
						break;
                    }
                }

				if (!foundit)
				{
					_entityModelList.SelectedIndex = -1;
					_resourceModelList.SelectedIndex = -1;
					_tableList.SelectedIndex = -1;

					if (InstallType == 1)
					{
						MessageBox.Show("No matching entity/resource class found. You will not be able to create a mapping model without a matching entity and resource models.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					else if (InstallType == 4)
					{
						MessageBox.Show("No matching entity/resource class found. You will not be able to create a example model without a matching entity and resource models.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
					else if (InstallType == 5)
					{
						MessageBox.Show("No matching entity/resource class found. You will not be able to create a Controller without a matching entity and resource models.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
				}

                Populating = false;
            }
            catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

        private void PopulateDatabaseColumns(DBServer server, string db, DBTable table)
        {
            DatabaseColumns.Clear();
            if (server.DBType == DBServerType.POSTGRESQL)
            {
                string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={_password.Text};";

                using (var connection = new NpgsqlConnection(connectionString))
                {
                    connection.Open();

                    var query = @"
select a.attname as columnname,
	   t.typname as datatype,
	   case when t.typname = 'varchar' then a.atttypmod-4
	        when t.typname = 'bpchar' then a.atttypmod-4
			when t.typname = '_varchar' then a.atttypmod-4
			when t.typname = '_bpchar' then a.atttypmod-4
	        when a.atttypmod > -1 then a.atttypmod
	        else a.attlen end as max_len,
	   not a.attnotnull as is_nullable,

	   case when ( a.attgenerated = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_computed,

	   case when ( a.attidentity = 'a' ) or  ( pg_get_expr(ad.adbin, ad.adrelid) = 'nextval('''
                 || (pg_get_serial_sequence (a.attrelid::regclass::text, a.attname))::regclass
                 || '''::regclass)')
	        then true else false end as is_identity,

	   case when (select indrelid from pg_index as px where px.indisprimary = true and px.indrelid = c.oid and a.attnum = ANY(px.indkey)) = c.oid then true else false end as is_primary,
	   case when (select indrelid from pg_index as ix where ix.indrelid = c.oid and a.attnum = ANY(ix.indkey)) = c.oid then true else false end as is_indexed,
	   case when (select conrelid from pg_constraint as cx where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) = c.oid then true else false end as is_foreignkey,
       (  select cc.relname from pg_constraint as cx inner join pg_class as cc on cc.oid = cx.confrelid where cx.conrelid = c.oid and cx.contype = 'f' and a.attnum = ANY(cx.conkey)) as foeigntablename
  from pg_class as c
  inner join pg_namespace as ns on ns.oid = c.relnamespace
  inner join pg_attribute as a on a.attrelid = c.oid and not a.attisdropped and attnum > 0
  inner join pg_type as t on t.oid = a.atttypid
  left outer join pg_attrdef as ad on ad.adrelid = a.attrelid and ad.adnum = a.attnum 
  where ns.nspname = @schema
    and c.relname = @tablename
 order by a.attnum
";

                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@schema", table.Schema);
                        command.Parameters.AddWithValue("@tablename", table.Table);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var dbColumn = new DBColumn
                                {
                                    ColumnName = reader.GetString(0),
                                    DataType = DBHelper.ConvertPostgresqlDataType(reader.GetString(1)),
                                    dbDataType = reader.GetString(1),
                                    Length = Convert.ToInt64(reader.GetValue(2)),
                                    IsNullable = Convert.ToBoolean(reader.GetValue(3)),
                                    IsComputed = Convert.ToBoolean(reader.GetValue(4)),
                                    IsIdentity = Convert.ToBoolean(reader.GetValue(5)),
                                    IsPrimaryKey = Convert.ToBoolean(reader.GetValue(6)),
                                    IsIndexed = Convert.ToBoolean(reader.GetValue(7)),
                                    IsForeignKey = Convert.ToBoolean(reader.GetValue(8)),
                                    ForeignTableName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                    ServerType = DBServerType.POSTGRESQL
                                };

                                DatabaseColumns.Add(dbColumn);

                            }
                        }
                    }
                }
            }
            else if (server.DBType == DBServerType.MYSQL)
            {
                string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};UID={server.Username};PWD={_password.Text};";

                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    var query = @"
SELECT c.COLUMN_NAME as 'columnName',
       c.COLUMN_TYPE as 'datatype',
       case when c.CHARACTER_MAXIMUM_LENGTH is null then -1 else c.CHARACTER_MAXIMUM_LENGTH end as 'max_len',
       case when c.GENERATION_EXPRESSION != '' then 1 else 0 end as 'is_computed',
       case when c.EXTRA = 'auto_increment' then 1 else 0 end as 'is_identity',
       case when c.COLUMN_KEY = 'PRI' then 1 else 0 end as 'is_primary',
       case when c.COLUMN_KEY != '' then 1 else 0 end as 'is_indexed',
       case when c.IS_NULLABLE = 'no' then 0 else 1 end as 'is_nullable',
       case when cu.REFERENCED_TABLE_NAME is not null then 1 else 0 end as 'is_foreignkey',
       cu.REFERENCED_TABLE_NAME as 'foreigntablename'
  FROM `INFORMATION_SCHEMA`.`COLUMNS` as c
left outer join information_schema.KEY_COLUMN_USAGE as cu on cu.CONSTRAINT_SCHEMA = c.TABLE_SCHEMA
                                                         and cu.TABLE_NAME = c.TABLE_NAME
														 and cu.COLUMN_NAME = c.COLUMN_NAME
                                                         and cu.REFERENCED_TABLE_NAME is not null
 WHERE c.TABLE_SCHEMA=@schema
  AND c.TABLE_NAME=@tablename
ORDER BY c.ORDINAL_POSITION;
";
                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@schema", db);
                        command.Parameters.AddWithValue("@tablename", table.Table);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var x = reader.GetValue(8);

                                var dbColumn = new DBColumn
                                {
                                    ColumnName = reader.GetString(0),
                                    DataType = DBHelper.ConvertMySqlDataType(reader.GetString(1)),
                                    dbDataType = reader.GetString(1),
                                    Length = Convert.ToInt64(reader.GetValue(2)),
                                    IsComputed = Convert.ToBoolean(reader.GetValue(3)),
                                    IsIdentity = Convert.ToBoolean(reader.GetValue(4)),
                                    IsPrimaryKey = Convert.ToBoolean(reader.GetValue(5)),
                                    IsIndexed = Convert.ToBoolean(reader.GetValue(6)),
                                    IsNullable = Convert.ToBoolean(reader.GetValue(7)),
                                    IsForeignKey = Convert.ToBoolean(reader.GetValue(8)),
                                    ForeignTableName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                    ServerType = DBServerType.MYSQL
                                };

                                DatabaseColumns.Add(dbColumn);

                            }
                        }
                    }
                }
            }
            else
            {
                string connectionString;

                if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
                    connectionString = $"Server={server.ServerName};Database={db};Trusted_Connection=True;";
                else
                    connectionString = $"Server={server.ServerName};Database={db};uid={server.Username};pwd={_password.Text};";

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    var query = @"
select c.name as column_name, 
       x.name as datatype, 
	   case when x.name = 'nchar' then c.max_length / 2
	        when x.name = 'nvarchar' then c.max_length / 2
			when x.name = 'text' then -1
			when x.name = 'ntext' then -1
			else c.max_length 
			end as max_length,
	   c.is_nullable, 
	   c.is_computed, 
	   c.is_identity,
	   case when ( select i.is_primary_key from sys.indexes as i inner join sys.index_columns as ic on ic.object_id = i.object_id and ic.index_id = i.index_id and i.is_primary_key = 1 where i.object_id = t.object_id and ic.column_id = c.column_id ) is not null  
	        then 1 
			else 0
			end as is_primary_key,
       case when ( select count(*) from sys.index_columns as ix where ix.object_id = c.object_id and ix.column_id = c.column_id ) > 0 then 1 else 0 end as is_indexed,
	   case when ( select count(*) from sys.foreign_key_columns as f where f.parent_object_id = c.object_id and f.parent_column_id = c.column_id ) > 0 then 1 else 0 end as is_foreignkey,
	   ( select t.name from sys.foreign_key_columns as f inner join sys.tables as t on t.object_id = f.referenced_object_id where f.parent_object_id = c.object_id and f.parent_column_id = c.column_id ) as foreigntablename
  from sys.columns as c
 inner join sys.tables as t on t.object_id = c.object_id
 inner join sys.schemas as s on s.schema_id = t.schema_id
 inner join sys.types as x on x.system_type_id = c.system_type_id and x.user_type_id = c.user_type_id
 where t.name = @tablename
   and s.name = @schema
   and x.name != 'sysname'
 order by t.name, c.column_id
";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@schema", table.Schema);
                        command.Parameters.AddWithValue("@tablename", table.Table);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var dbColumn = new DBColumn
                                {
                                    ColumnName = reader.GetString(0),
                                    dbDataType = reader.GetString(1),
                                    DataType = DBHelper.ConvertSqlServerDataType(reader.GetString(1)),
                                    Length = Convert.ToInt64(reader.GetValue(2)),
                                    IsNullable = Convert.ToBoolean(reader.GetValue(3)),
                                    IsComputed = Convert.ToBoolean(reader.GetValue(4)),
                                    IsIdentity = Convert.ToBoolean(reader.GetValue(5)),
                                    IsPrimaryKey = Convert.ToBoolean(reader.GetValue(6)),
                                    IsIndexed = Convert.ToBoolean(reader.GetValue(7)),
                                    IsForeignKey = Convert.ToBoolean(reader.GetValue(8)),
                                    ForeignTableName = reader.IsDBNull(9) ? string.Empty : reader.GetString(9),
                                    ServerType = DBServerType.SQLSERVER
                                };

                                if (string.Equals(dbColumn.dbDataType, "geometry", StringComparison.OrdinalIgnoreCase))
                                {
                                    _tableList.SelectedIndex = -1;
                                    throw new Exception("COFRS .NET Core does not support the SQL Server geometry data type.");
                                }

                                if (string.Equals(dbColumn.dbDataType, "geography", StringComparison.OrdinalIgnoreCase))
                                {
                                    _tableList.SelectedIndex = -1;
                                    throw new Exception("COFRS .NET Core does not support the SQL Server geography data type.");
                                }

                                DatabaseColumns.Add(dbColumn);
                            }
                        }
                    }
                }
            }
        }

        private void OnUserNameChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;

					if (server != null)
					{
						server.Username = _userName.Text;

						var otherServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnPasswordChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;

					if (server != null)
					{
						if (server.RememberPassword)
							server.Password = _password.Text;
						else
							server.Password = string.Empty;

						var otherServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnSavePasswordChanged(object sender, EventArgs e)
		{
			try
			{
				if (!Populating)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					var server = (DBServer)_serverList.SelectedItem;

					if (server != null)
					{
						server.RememberPassword = _rememberPassword.Checked;

						if (!server.RememberPassword)
							server.Password = string.Empty;
						else
							server.Password = _password.Text;

						var savedServer = _serverConfig.Servers.FirstOrDefault(s => string.Equals(s.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase));

						Save();

						if (TestConnection(server))
							PopulateDatabases();
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnAuthenticationChanged(object sender, EventArgs e)
		{
			if (!Populating)
			{
				_dbList.Items.Clear();
				_tableList.Items.Clear();
				var server = (DBServer)_serverList.SelectedItem;

				if (server != null)
				{
					server.DBAuth = _authenticationList.SelectedIndex == 0 ? DBAuthentication.SQLSERVERAUTH : DBAuthentication.WINDOWSAUTH;

					if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					{
						_userName.Text = string.Empty;
						_userName.Enabled = false;
						_userNameLabel.Enabled = false;

						_password.Text = string.Empty;
						_password.Enabled = false;
						_passwordLabel.Enabled = false;

						_rememberPassword.Checked = false;
						_rememberPassword.Enabled = false;
					}
					else
					{
						_userName.Enabled = true;
						_userNameLabel.Enabled = true;

						_password.Enabled = true;
						_passwordLabel.Enabled = true;

						_rememberPassword.Checked = server.RememberPassword;
						_rememberPassword.Enabled = true;
					}

					Save();

					if (TestConnection(server))
						PopulateDatabases();
				}
			}
		}

		private void OnAddServer(object sender, EventArgs e)
		{
			try
			{
				var dialog = new AddConnection
				{
					LastServerUsed = (DBServer)_serverList.SelectedItem
				};

				if (dialog.ShowDialog() == DialogResult.OK)
				{
					_dbList.Items.Clear();
					_tableList.Items.Clear();
					_serverConfig.Servers.Add(dialog.Server);
					Save();

					switch (dialog.Server.DBType)
					{
						case DBServerType.MYSQL: _serverTypeList.SelectedIndex = 0; break;
						case DBServerType.POSTGRESQL: _serverTypeList.SelectedIndex = 1; break;
						case DBServerType.SQLSERVER: _serverTypeList.SelectedIndex = 2; break;
					}

					OnServerTypeChanged(this, new EventArgs());

					for (int index = 0; index < _serverList.Items.Count; index++)
					{
						if (string.Equals((_serverList.Items[index] as DBServer).ServerName, dialog.Server.ServerName, StringComparison.OrdinalIgnoreCase))
						{
							_serverList.SelectedIndex = index;
							break;
						}
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnRemoveServer(object sender, EventArgs e)
		{
			try
			{
				var deprecatedServer = (DBServer)_serverList.SelectedItem;
				var newList = new List<DBServer>();

				foreach (var server in _serverConfig.Servers)
				{
					if (!string.Equals(server.ServerName, deprecatedServer.ServerName, StringComparison.OrdinalIgnoreCase))
					{
						newList.Add(server);
					}
				}

				_serverConfig.Servers = newList;

				if (_serverConfig.LastServerUsed >= _serverConfig.Servers.Count())
				{
					_serverConfig.LastServerUsed = 0;
				}

				Save();

				OnServerTypeChanged(this, new EventArgs());
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnResourceModelChanged(object sender, EventArgs e)
		{
			try
			{
				if (_resourceModelList.SelectedIndex != -1 && !Populating)
				{
					var resourceClassFile = (ResourceClassFile)_resourceModelList.SelectedItem;
					var foundIt = false;

					for (int index = 0; index < _entityModelList.Items.Count; index++)
					{
						var entityClassFile = (EntityClassFile)_entityModelList.Items[index];

						if (string.Equals(resourceClassFile.EntityClass, entityClassFile.ClassName, StringComparison.OrdinalIgnoreCase))
						{
							foundIt = true;
							_entityModelList.SelectedIndex = index;
							break;
						}
					}

					if (!foundIt)
					{
						MessageBox.Show("No corresponding entity class for this model was found. Please select another model, or create the entity model for this model.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						_tableList.SelectedIndex = -1;
						_entityModelList.SelectedIndex = -1;
						_resourceModelList.SelectedIndex = -1;
						return;
					}

					var entity = _entityModelList.SelectedItem as EntityClassFile;
					foundIt = false;

					for (int index = 0; index < _tableList.Items.Count; index++)
					{
						var table = (DBTable)_tableList.Items[index];

						if (string.Equals(table.Table, entity.TableName, StringComparison.OrdinalIgnoreCase))
						{
							foundIt = true;
							_tableList.SelectedIndex = index;
							break;
						}
					}

					if (!foundIt)
					{
						MessageBox.Show("No corresponding table for this entity was found in the selected database. Please select the server and database that contains this table.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						_tableList.SelectedIndex = -1;
						_entityModelList.SelectedIndex = -1;
						_resourceModelList.SelectedIndex = -1;
						return;
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnEntityModelChanged(object sender, EventArgs e)
		{
			try
			{
				if (_entityModelList.SelectedIndex != -1 && !Populating)
				{
					var classFile = (EntityClassFile)_entityModelList.SelectedItem;
					var foundIt = false;

					for (int index = 0; index < _tableList.Items.Count; index++)
					{
						var table = (DBTable)_tableList.Items[index];

						if (string.Equals(table.Table, classFile.TableName, StringComparison.OrdinalIgnoreCase))
						{
							foundIt = true;
							_tableList.SelectedIndex = index;
							break;
						}
					}

					if (!foundIt)
					{
						MessageBox.Show("No corresponding table for this entity was found in the selected database. Please select the server and database that contains this table.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						_tableList.SelectedIndex = -1;
						_entityModelList.SelectedIndex = -1;
						_resourceModelList.SelectedIndex = -1;
						return;
					}

					foundIt = false;

					for (int index = 0; index < _resourceModelList.Items.Count; index++)
					{
						var resourceClassFile = (ResourceClassFile)_resourceModelList.Items[index];

						if (string.Equals(resourceClassFile.EntityClass, classFile.ClassName, StringComparison.OrdinalIgnoreCase))
						{
							foundIt = true;
							_resourceModelList.SelectedIndex = index;
							break;
						}
					}

					if (!foundIt)
					{
						MessageBox.Show("No corresponding resource class for this entity was found. Please select another entity, or create the resource model for this entity.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						_tableList.SelectedIndex = -1;
						_entityModelList.SelectedIndex = -1;
						_resourceModelList.SelectedIndex = -1;
						return;
					}
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OnOK(object sender, EventArgs e)
		{
			Save();
			DatabaseTable = (DBTable)_tableList.SelectedItem;

			if (_entityModelList.SelectedIndex == -1)
			{
				MessageBox.Show("You must select an entity model in order to create this item.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (_resourceModelList.SelectedIndex == -1)
			{
				MessageBox.Show("You must select a resource model in order to create this item.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (InstallType == 4)
				Examples = ConstructExample();

			DialogResult = DialogResult.OK;
			Close();
		}
		#endregion

		#region Helper Functions
		private void LoadClassList()
		{
			try
			{
				_entityModelList.Items.Clear();
				_resourceModelList.Items.Clear();

				foreach (var file in Directory.GetFiles(SolutionFolder, "*.cs"))
				{
					LoadEntityClass(file);
					LoadResourceClass(file);
				}

				foreach (var folder in Directory.GetDirectories(SolutionFolder))
				{
					LoadEntityList(folder);
					LoadResourceList(folder);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LoadEntityClass(string file)
		{
			var data = File.ReadAllText(file).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			var className = string.Empty;
			var namespaceName = string.Empty;
			var schemaName = string.Empty;
			var tableName = string.Empty;

			foreach (var line in data)
			{
				var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

				if (match.Success)
					className = match.Groups["className"].Value;

				match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)*)");

				if (match.Success)
					namespaceName = match.Groups["namespaceName"].Value;

				// 	[Table("Products", Schema = "dbo")]
				match = Regex.Match(line, "\\[[ \t]*Table[ \t]*\\([ \t]*\"(?<tableName>[A-Za-z][A-Za-z0-9_]*)\"([ \t]*\\,[ \t]*Schema[ \t]*=[ \t]*\"(?<schemaName>[A-Za-z][A-Za-z0-9_]*)\"){0,1}\\)\\]");

				if (match.Success)
				{
					tableName = match.Groups["tableName"].Value;
					schemaName = match.Groups["schemaName"].Value;
				}
			}

			if (!string.IsNullOrWhiteSpace(tableName) &&
				 !string.IsNullOrWhiteSpace(className) &&
				 !string.IsNullOrWhiteSpace(namespaceName))
			{
				var classfile = new EntityClassFile
				{
					ClassName = $"{className}",
					FileName = file,
					TableName = tableName,
					SchemaName = schemaName,
					ClassNameSpace = namespaceName
				};

				_entityModelList.Items.Add(classfile);
			}
		}

		private void LoadResourceClass(string file)
		{
			try
			{
				var data = File.ReadAllText(file).Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries); ;
				var className = string.Empty;
				var namespaceName = string.Empty;
				var entityName = string.Empty;

				foreach (var line in data)
				{
					var match = Regex.Match(line, "class[ \t]+(?<className>[A-Za-z][A-Za-z0-9_]*)");

					if (match.Success)
						className = match.Groups["className"].Value;

					match = Regex.Match(line, "namespace[ \t]+(?<namespaceName>[A-Za-z][A-Za-z0-9_]*(\\.[A-Za-z][A-Za-z0-9_]*)*)");

					if (match.Success)
						namespaceName = match.Groups["namespaceName"].Value;

					match = Regex.Match(line, "\\[[ \t]*Entity[ \t]*\\([ \t]*typeof\\([ \t]*(?<entityClass>[A-Za-z][A-Za-z0-9_]*)[ \t]*\\)");

					if (match.Success)
						entityName = match.Groups["entityClass"].Value;
				}

				if (!string.IsNullOrWhiteSpace(entityName) &&
					 !string.IsNullOrWhiteSpace(className) &&
					 !string.IsNullOrWhiteSpace(namespaceName))
				{
					var classfile = new ResourceClassFile
					{
						ClassName = $"{className}",
						FileName = file,
						EntityClass = entityName,
						ClassNamespace = namespaceName
					};

					_resourceModelList.Items.Add(classfile);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LoadEntityList(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "*.cs"))
				{
					LoadEntityClass(file);
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					LoadEntityList(subfolder);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void LoadResourceList(string folder)
		{
			try
			{
				foreach (var file in Directory.GetFiles(folder, "*.cs"))
				{
					LoadResourceClass(file);
				}

				foreach (var subfolder in Directory.GetDirectories(folder))
				{
					LoadResourceList(subfolder);
				}
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Reads the list of SQL Servers from the server configuration list
		/// </summary>
		private void PopulateServers()
		{
			DBServerType serverType;

			switch (_serverTypeList.SelectedIndex)
			{
				case 0: serverType = DBServerType.MYSQL; break;
				case 1: serverType = DBServerType.POSTGRESQL; break;
				case 2: serverType = DBServerType.SQLSERVER; break;
				default: serverType = DBServerType.SQLSERVER; break;
			}

			var serverList = _serverConfig.Servers.Where(s => s.DBType == serverType);

			_serverList.Items.Clear();
			_dbList.Items.Clear();
			_tableList.Items.Clear();

			if (serverList.Count() == 0)
			{
				_serverList.Enabled = false;
				_serverList.SelectedIndex = -1;

				if (serverType == DBServerType.SQLSERVER)
				{
					_authenticationList.SelectedIndex = -1;
					_authenticationList.Enabled = false;
					_authenticationList.Show();

					_portNumber.Enabled = false;
					_portNumber.Hide();
				}

				else if (serverType == DBServerType.POSTGRESQL)
				{
					_portNumber.Enabled = false;
					_portNumber.Value = 5432;
					_portNumber.Show();

					_authenticationList.Enabled = false;
					_authenticationList.Hide();
				}

				else if (serverType == DBServerType.MYSQL)
				{
					_portNumber.Enabled = false;
					_portNumber.Value = 3306;
					_portNumber.Show();

					_authenticationList.Enabled = false;
					_authenticationList.Hide();
				}

				_userName.Enabled = false;
				_userName.Text = string.Empty;

				_password.Enabled = false;
				_password.Text = string.Empty;

				_rememberPassword.Enabled = false;
				_rememberPassword.Checked = false;
			}
			else
			{
				_serverList.Enabled = true;

				if (serverType == DBServerType.POSTGRESQL)
				{
					_portNumber.Enabled = true;
					_portNumber.Show();
					_authenticationList.Enabled = false;
					_authenticationList.Hide();
				}

				else if (serverType == DBServerType.MYSQL)
				{
					_portNumber.Enabled = true;
					_portNumber.Show();
					_authenticationList.Enabled = false;
					_authenticationList.Hide();
				}

				else if (serverType == DBServerType.SQLSERVER)
				{
					_portNumber.Enabled = false;
					_portNumber.Hide();
					_authenticationList.Enabled = true;
					_authenticationList.Show();
				}

				foreach (var server in serverList)
				{
					_serverList.Items.Add(server);
				}

				if (_serverList.Items.Count > 0)
					_serverList.SelectedIndex = 0;
			}
		}

		private void Save()
		{
			int index = 0;
			var server = (DBServer)_serverList.SelectedItem;

			if (server != null)
			{
				foreach (var dbServer in _serverConfig.Servers)
				{
					if (string.Equals(dbServer.ServerName, server.ServerName, StringComparison.OrdinalIgnoreCase) &&
						dbServer.DBType == server.DBType)
					{
						_serverConfig.LastServerUsed = index;
						break;
					}

					index++;
				}
			}

			var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var dataFolder = Path.Combine(baseFolder, "COFRS");

			if (!Directory.Exists(dataFolder))
				Directory.CreateDirectory(dataFolder);

			var filePath = Path.Combine(dataFolder, "Servers");
			File.Delete(filePath);

			using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				using (var streamWriter = new StreamWriter(stream))
				{
					using (var writer = new JsonTextWriter(streamWriter))
					{
						var serializer = new JsonSerializer();
						serializer.Serialize(writer, _serverConfig);
					}
				}
			}
		}

		private void PopulateDatabases()
		{
			var server = (DBServer)_serverList.SelectedItem;

			switch (server.DBType)
			{
				case DBServerType.MYSQL:
					PopulateMySql(server);
					break;

				case DBServerType.POSTGRESQL:
					PopulatePostgresql(server);
					break;

				case DBServerType.SQLSERVER:
					PopulateSqlServer(server);
					break;

				default:
					MessageBox.Show("Unrecognized Database Type\r\nPlease select a database that is either MySQL, Postgresql, or SQL Server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					break;
			}
		}

		private void PopulateSqlServer(DBServer server)
		{
			string connectionString;

			if (server.DBAuth == DBAuthentication.SQLSERVERAUTH && string.IsNullOrWhiteSpace(_password.Text))
				return;

			if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
				connectionString = $"Server={server.ServerName};Database=master;Trusted_Connection=True;";
			else
				connectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={_password.Text};";

			_dbList.Items.Clear();
			_tableList.Items.Clear();

			try
			{
				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();

					var query = @"
select name
  from sys.databases with(nolock)
 where name not in ( 'master', 'model', 'msdb', 'tempdb' )
 order by name";

					using (var command = new SqlCommand(query, connection))
					{
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								_dbList.Items.Add(reader.GetString(0));
							}
						}
					}
				}

				if (_dbList.Items.Count > 0)
					_dbList.SelectedIndex = 0;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void PopulateMySql(DBServer server)
		{
			if (string.IsNullOrWhiteSpace(_password.Text))
				return;

			string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={_password.Text};";

			_dbList.Items.Clear();
			_tableList.Items.Clear();

			try
			{
				using (var connection = new MySqlConnection(connectionString))
				{
					connection.Open();

					var query = @"
select SCHEMA_NAME from information_schema.SCHEMATA
 where SCHEMA_NAME not in ( 'information_schema', 'performance_schema', 'sys', 'mysql');";

					using (var command = new MySqlCommand(query, connection))
					{
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								_dbList.Items.Add(reader.GetString(0));
							}
						}
					}
				}

				if (_dbList.Items.Count > 0)
					_dbList.SelectedIndex = 0;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void PopulatePostgresql(DBServer server)
		{
			if (string.IsNullOrWhiteSpace(_password.Text))
				return;

			string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={_password.Text};";

			_dbList.Items.Clear();
			_tableList.Items.Clear();

			try
			{
				using (var connection = new NpgsqlConnection(connectionString))
				{
					connection.Open();

					var query = @"
SELECT datname 
  FROM pg_database
 WHERE datistemplate = false
   AND datname != 'postgres'
 ORDER BY datname";

					using (var command = new NpgsqlCommand(query, connection))
					{
						using (var reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								_dbList.Items.Add(reader.GetString(0));
							}
						}
					}
				}

				if (_dbList.Items.Count > 0)
					_dbList.SelectedIndex = 0;
			}
			catch (Exception error)
			{
				MessageBox.Show(error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>
		/// Tests to see if the server credentials are sufficient to establish a connection
		/// to the server
		/// </summary>
		/// <param name="server">The Database Server we are trying to connect to.</param>
		/// <returns></returns>
		private bool TestConnection(DBServer server)
		{
			_tableList.Items.Clear();
			_dbList.Items.Clear();

			if (server.DBType == DBServerType.POSTGRESQL)
			{
				string connectionString;
				connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=postgres;User ID={server.Username};Password={_password.Text};";

				//	Attempt to connect to the database.
				try
				{
					using (var connection = new NpgsqlConnection(connectionString))
					{
						connection.Open();
					}
				}
				catch (Exception)
				{
					//	We did not succeed. We do not have sufficient information to 
					//	establish the connection.
					return false;
				}
			}
			else if (server.DBType == DBServerType.MYSQL)
			{
				string connectionString;
				connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database=mysql;UID={server.Username};PWD={_password.Text};";

				//	Attempt to connect to the database.
				try
				{
					using (var connection = new MySqlConnection(connectionString))
					{
						connection.Open();
					}
				}
				catch (Exception)
				{
					//	We did not succeed. We do not have sufficient information to 
					//	establish the connection.
					return false;
				}
			}
			else
			{
				//	Construct the connection string
				string connectionString;

				if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					connectionString = $"Server={server.ServerName};Database=master;Trusted_Connection=True;";
				else
					connectionString = $"Server={server.ServerName};Database=master;uid={server.Username};pwd={_password.Text};";


				//	Attempt to connect to the database.
				try
				{
					using (var connection = new SqlConnection(connectionString))
					{
						connection.Open();
					}
				}
				catch (Exception)
				{
					//	We did not succeed. We do not have sufficient information to 
					//	establish the connection.
					return false;
				}
			}

			//	If we got here, it worked. We were able to establish and close
			//	the connection.
			return true;
		}

		/// <summary>
		/// Reads the list of SQL Servers from the server configuration list
		/// </summary>
		private void ReadServerList()
		{
			//	Indicate that we are merely populating windows at this point. There are certain
			//	actions that occur during the loading of windows that mimic user interaction.
			//	There is no user interaction at this point, so there are certain actions we 
			//	do not want to run while populating.
			Populating = true;

			//	Get the location of the server configuration on disk
			var baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var dataFolder = Path.Combine(baseFolder, "COFRS");

			if (!Directory.Exists(dataFolder))
				Directory.CreateDirectory(dataFolder);

			var filePath = Path.Combine(dataFolder, "Servers");

			//	Read the ServerConfig into memory. If one does not exist
			//	create an empty one.
			using (var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				using (var streamReader = new StreamReader(stream))
				{
					using (var reader = new JsonTextReader(streamReader))
					{
						var serializer = new JsonSerializer();

						_serverConfig = serializer.Deserialize<ServerConfig>(reader);

						if (_serverConfig == null)
							_serverConfig = new ServerConfig();
					}
				}
			}

			//	If there are any servers in the list, we need to populate
			//	the windows controls.
			if (_serverConfig.Servers.Count() > 0)
			{
				//	When we populate the windows controls, ensure that the last server that
				//	the user used is in the visible list, and make sure it is the one
				//	selected.
				var dbServer = _serverConfig.Servers.ToList()[_serverConfig.LastServerUsed];
				DBServerType selectedType = dbServer.DBType;

				switch (dbServer.DBType)
				{
					case DBServerType.MYSQL: _serverTypeList.SelectedIndex = 0; break;
					case DBServerType.POSTGRESQL: _serverTypeList.SelectedIndex = 1; break;
					case DBServerType.SQLSERVER: _serverTypeList.SelectedIndex = 2; break;
				}

				var serverList = _serverConfig.Servers.Where(s => s.DBType == selectedType);
				int index = 0;
				int selectedIndex = -1;

				foreach (var server in serverList)
				{
					_serverList.Items.Add(server);

					if (string.Equals(server.ServerName, dbServer.ServerName, StringComparison.OrdinalIgnoreCase))
						selectedIndex = index;

					index++;
				}

				if (_serverList.Items.Count > 0)
				{
					_serverList.SelectedIndex = selectedIndex;

					if (dbServer.DBType == DBServerType.SQLSERVER)
					{
						_authenticationList.SelectedIndex = dbServer.DBAuth == DBAuthentication.WINDOWSAUTH ? 0 : 1;

						if (_authenticationList.SelectedIndex == 0)
						{
							_userName.Text = string.Empty;
							_userName.Enabled = false;

							_password.Text = string.Empty;
							_password.Enabled = false;

							_rememberPassword.Checked = false;
							_rememberPassword.Enabled = false;
						}
						else
						{
							_userName.Text = dbServer.Username;
							_userName.Enabled = true;

							_rememberPassword.Checked = dbServer.RememberPassword;
							_rememberPassword.Enabled = true;

							if (dbServer.RememberPassword)
							{
								_password.Text = dbServer.Password;
								_password.Enabled = true;
							}
							else
							{
								_password.Text = string.Empty;
								_password.Enabled = true;
							}
						}
					}
					else if (dbServer.DBType == DBServerType.POSTGRESQL)
					{
						_portNumber.Value = dbServer.PortNumber;
						_userName.Text = dbServer.Username;
						_userName.Enabled = true;

						_rememberPassword.Checked = dbServer.RememberPassword;
						_rememberPassword.Enabled = true;
						_password.Enabled = true;

						if (dbServer.RememberPassword)
						{
							_password.Text = dbServer.Password;
						}
						else
						{
							_password.Text = string.Empty;
						}
					}
					else if (dbServer.DBType == DBServerType.MYSQL)
					{
						_portNumber.Value = dbServer.PortNumber;
						_userName.Text = dbServer.Username;
						_userName.Enabled = true;

						_rememberPassword.Checked = dbServer.RememberPassword;
						_rememberPassword.Enabled = true;
						_password.Enabled = true;

						if (dbServer.RememberPassword)
						{
							_password.Text = dbServer.Password;
						}
						else
						{
							_password.Text = string.Empty;
						}
					}

					PopulateDatabases();
				}
			}
			else
			{
				//	There were no servers in the list, make sure everything is empty
				_serverTypeList.SelectedIndex = 1;

				_authenticationList.Enabled = false;
				_authenticationList.SelectedIndex = -1;

				_serverList.Enabled = false;
				_serverList.Items.Clear();

				_userName.Enabled = false;
				_userName.Text = string.Empty;

				_password.Enabled = false;
				_password.Text = string.Empty;

				_rememberPassword.Enabled = false;
				_rememberPassword.Checked = false;
			}

			//	We're done. Turn off the populating flag.
			Populating = false;
		}

		private JObject ConstructExample()
		{
			var server = (DBServer)_serverList.SelectedItem;
			var table = (DBTable)_tableList.SelectedItem;
			var db = (string)_dbList.SelectedItem;
			var values = new JObject();

			//	-----------------------------------------------------------------
			//	Read data from MySQL
			//	-----------------------------------------------------------------
			if (server.DBType == DBServerType.MYSQL)
			{
				string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};uid={server.Username};pwd={_password.Text};";

				using (var connection = new MySqlConnection(connectionString))
				{
					connection.Open();

					var query = GenerateMySQLQuery(table);

					using (var command = new MySqlCommand(query, connection))
					{
						using (var reader = command.ExecuteReader())
						{
							if (reader.Read())
							{
								foreach (var column in DatabaseColumns)
								{
									var columnName = column.ColumnName;

									if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
									{
										values.Add(columnName, null);
									}
									else
									{
										var value = reader.GetValue(reader.GetOrdinal(columnName));
										values.Add(columnName, JToken.FromObject(value));
									}
								}

								return values;
							}
						}

						return GetMySqlValues(values);
					}
				}
			}

			//	-----------------------------------------------------------------
			//	Read data from Postgresql
			//	-----------------------------------------------------------------
			else if (server.DBType == DBServerType.POSTGRESQL)
			{
				string connectionString = $"Server={server.ServerName};Port={server.PortNumber};Database={db};User ID={server.Username};Password={_password.Text};";

				using (var connection = new NpgsqlConnection(connectionString))
				{
					connection.Open();

					var query = GeneratePostgresqlQuery(table);

					using (var command = new NpgsqlCommand(query, connection))
					{
						using (var reader = command.ExecuteReader())
						{
							if (reader.Read())
							{
								foreach (var column in DatabaseColumns)
								{
									var columnName = column.ColumnName;

									if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
									{
										values.Add(columnName, null);
									}
									else
									{
										var value = reader.GetValue(reader.GetOrdinal(columnName));
										values.Add(columnName, JToken.FromObject(value));
									}
								}

								return values;
							}
						}
					}

					return GetPostgresqlValues(values);
				}
			}

			//	-----------------------------------------------------------------
			//	Read data from SQL Server
			//	-----------------------------------------------------------------
			else
			{
				string connectionString;

				if (server.DBAuth == DBAuthentication.WINDOWSAUTH)
					connectionString = $"Server={server.ServerName};Database={(string)_dbList.SelectedItem};Trusted_Connection=True;";
				else
					connectionString = $"Server={server.ServerName};Database={(string)_dbList.SelectedItem};uid={server.Username};pwd={_password.Text};";

				using (var connection = new SqlConnection(connectionString))
				{
					connection.Open();

					var query = GenerateSQLServerQuery(table);

					using (var command = new SqlCommand(query, connection))
					{
						using (var reader = command.ExecuteReader())
						{
							if (reader.Read())
							{
								foreach (var column in DatabaseColumns)
								{
									var columnName = column.ColumnName;

									if (reader.IsDBNull(reader.GetOrdinal(column.ColumnName)))
									{
										values.Add(columnName, null);
									}
									else
									{
										var value = reader.GetValue(reader.GetOrdinal(columnName));
										values.Add(columnName, JToken.FromObject(value));
									}
								}
								return values;
							}
						}
					}

					return GetSqlServerValues(values);
				}
			}
		}

		private JObject GetSqlServerValues(JObject values)
		{
			foreach (var column in DatabaseColumns)
			{
				var columnName = column.ColumnName;

				switch ((SqlDbType)column.DataType)
				{
					#region tinyint, smallint, int, bigint
					case SqlDbType.TinyInt:
						values.Add(columnName, JToken.FromObject((byte)1));
						break;

					case SqlDbType.SmallInt:
						values.Add(columnName, JToken.FromObject((short)1));
						break;

					case SqlDbType.Int:
						values.Add(columnName, JToken.FromObject((int)1));
						break;

					case SqlDbType.BigInt:
						values.Add(columnName, JToken.FromObject((long)1));
						break;
					#endregion

					#region varchar, nvarchar, text, ntext
					case SqlDbType.VarChar:
					case SqlDbType.NVarChar:
						{
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							values.Add(columnName, JToken.FromObject(answer));
						}
						break;

					case SqlDbType.Text:
					case SqlDbType.NText:
						values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
						break;
					#endregion

					#region binary, varbinary
					case SqlDbType.Binary:
						{
							if (column.Length == 1)
							{
								values.Add(columnName, JToken.FromObject((byte)32));
							}
							else if (column.Length == -1)
							{
								values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
							}
							else
							{
								var answer = new byte[column.Length];
								for (int i = 0; i < column.Length; i++)
								{
									var byteValue = (byte)(i & 0x00FF);
									answer[i] = byteValue;
								}

								values.Add(columnName, JToken.FromObject(answer));
							}
						}
						break;

					case SqlDbType.VarBinary:
						values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;
					#endregion

					#region bit
					case SqlDbType.Bit:
						values.Add(columnName, JToken.FromObject(true));
						break;
					#endregion

					#region char, nchar
					case SqlDbType.NChar:
					case SqlDbType.Char:
						{
							if (column.Length == 1)
							{
								values.Add(columnName, JToken.FromObject('A'));
							}
							else if (column.Length == -1)
							{
								values.Add(columnName, JToken.FromObject("The brown cow jumped over the moon.The dog barked at the cow, and the bull chased the dog."));
							}
							else
							{
								const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
								var chars = new char[column.Length];
								for (int i = 0; i < chars.Length; i++)
								{
									int j = i % alphabet.Length;
									chars[i] = alphabet[j];
								}

								values.Add(columnName, JToken.FromObject(new string(chars)));
							}
						}
						break;
					#endregion

					#region image
					case SqlDbType.Image:
						{
							var image = (Image)new Bitmap(100, 100);
							var imageConverter = new ImageConverter();

							var bytes = imageConverter.ConvertTo(image, typeof(byte[]));
							values.Add(columnName, JToken.FromObject(bytes));
						}
						break;
					#endregion

					case SqlDbType.Date: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
					case SqlDbType.DateTime: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
					case SqlDbType.DateTime2: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;
					case SqlDbType.DateTimeOffset: values.Add(columnName, JToken.FromObject(DateTimeOffset.Now)); break;
					case SqlDbType.Decimal: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;
					case SqlDbType.Float: values.Add(columnName, JToken.FromObject(Single.Parse("123.45"))); break;

					case SqlDbType.Money: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;
					case SqlDbType.Real: values.Add(columnName, JToken.FromObject(Double.Parse("123.45"))); break;
					case SqlDbType.SmallDateTime: values.Add(columnName, JToken.FromObject(DateTime.Now)); break;


					case SqlDbType.SmallMoney: values.Add(columnName, JToken.FromObject(Decimal.Parse("123.45"))); break;

					case SqlDbType.Time: values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(3))); break;
					case SqlDbType.Timestamp: values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 })); break;
					case SqlDbType.UniqueIdentifier: values.Add(columnName, JToken.FromObject(Guid.NewGuid())); break;

					default:
						values.Add(columnName, JToken.FromObject("Unrecognized"));
						break;
				}
			}

			return values;
		}

		private JObject GetMySqlValues(JObject values)
		{
			foreach (var column in DatabaseColumns)
			{
				var columnName = column.ColumnName;

				switch ((MySqlDbType)column.DataType)
				{
					#region tinyint, smallint, int, bigint
					case MySqlDbType.Byte:
						values.Add(columnName, JToken.FromObject((sbyte)1));
						break;

					case MySqlDbType.UByte:
						values.Add(columnName, JToken.FromObject((byte)1));
						break;

					case MySqlDbType.Int16:
						values.Add(columnName, JToken.FromObject((short)1));
						break;

					case MySqlDbType.UInt16:
						values.Add(columnName, JToken.FromObject((ushort)1));
						break;

					case MySqlDbType.Int24:
						values.Add(columnName, JToken.FromObject((int)1));
						break;

					case MySqlDbType.UInt24:
						values.Add(columnName, JToken.FromObject((uint)1));
						break;

					case MySqlDbType.Int32:
						values.Add(columnName, JToken.FromObject((int)1));
						break;

					case MySqlDbType.UInt32:
						values.Add(columnName, JToken.FromObject((uint)1));
						break;

					case MySqlDbType.Int64:
						values.Add(columnName, JToken.FromObject((long)1));
						break;

					case MySqlDbType.UInt64:
						values.Add(columnName, JToken.FromObject((ulong)1));
						break;
					#endregion

					#region decimal, double, float
					case MySqlDbType.Decimal:
						values.Add(columnName, JToken.FromObject((decimal)1.24m));
						break;
					case MySqlDbType.Double:
						values.Add(columnName, JToken.FromObject((double)1.24));
						break;
					case MySqlDbType.Float:
						values.Add(columnName, JToken.FromObject((float)1.24f));
						break;
					#endregion

					#region varchar, nvarchar, text, ntext
					case MySqlDbType.VarChar:
					case MySqlDbType.VarString:
						{
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							values.Add(columnName, JToken.FromObject(answer));
						}
						break;

					case MySqlDbType.Text:
					case MySqlDbType.TinyText:
					case MySqlDbType.MediumText:
					case MySqlDbType.LongText:
						values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
						break;

					case MySqlDbType.String:
						if (column.Length == 1)
							values.Add(columnName, JToken.FromObject('A'));
						else
						{
							const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
							var chars = new char[column.Length];
							for (int i = 0; i < chars.Length; i++)
							{
								int j = i % alphabet.Length;
								chars[i] = alphabet[j];
							}

							values.Add(columnName, JToken.FromObject(new string(chars)));
						}
						break;

					#endregion

					#region binary, varbinary
					case MySqlDbType.Binary:
						{
							if (column.Length == 1)
							{
								values.Add(columnName, JToken.FromObject((byte)32));
							}
							else if (column.Length == -1)
							{
								values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
							}
							else
							{
								var answer = new byte[column.Length];
								for (int i = 0; i < column.Length; i++)
								{
									var byteValue = (byte)(i & 0x00FF);
									answer[i] = byteValue;
								}

								values.Add(columnName, JToken.FromObject(answer));
							}
						}
						break;

					case MySqlDbType.VarBinary:
					case MySqlDbType.TinyBlob:
					case MySqlDbType.Blob:
					case MySqlDbType.MediumBlob:
					case MySqlDbType.LongBlob:
						values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;
					#endregion

					#region bit
					case MySqlDbType.Bit:
						if (column.Length == 1)
							values.Add(columnName, JToken.FromObject(true));
						else
							values.Add(columnName, JToken.FromObject((ulong)1));
						break;
					#endregion

					#region enum, set
					case MySqlDbType.Enum:
						{
							var theValues = column.dbDataType.Split(new char[] { '(', ')', ',', '\'' }, StringSplitOptions.RemoveEmptyEntries);
							values.Add(columnName, JToken.FromObject(theValues[1]));
						}
						break;

					case MySqlDbType.Set:
						{
							var theValues = column.dbDataType.Split(new char[] { '(', ')', ',', '\'' }, StringSplitOptions.RemoveEmptyEntries);
							values.Add(columnName, JToken.FromObject(theValues[1]));
						}
						break;
					#endregion

					#region Datetime, timestamp, time, date, year
					case MySqlDbType.DateTime:
					case MySqlDbType.Date:
					case MySqlDbType.Timestamp:
						values.Add(columnName, JToken.FromObject(DateTime.Now));
						break;

					case MySqlDbType.Time:
						values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(2)));
						break;

					case MySqlDbType.Year:
						values.Add(columnName, JToken.FromObject(2020));
						break;
					#endregion

					default:
						values.Add(columnName, JToken.FromObject("Unrecognized"));
						break;
				}
			}

			return values;
		}

		private JObject GetPostgresqlValues(JObject values)
		{
			foreach (var column in DatabaseColumns)
			{
				var columnName = column.ColumnName;

				switch ((NpgsqlDbType)column.DataType)
				{
					#region smallint, int, bigint
					case NpgsqlDbType.Smallint:
						values.Add(columnName, JToken.FromObject((short)1));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Smallint:
						values.Add(columnName, JToken.FromObject(new short[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;

					case NpgsqlDbType.Integer:
						values.Add(columnName, JToken.FromObject((int)1));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Integer:
						values.Add(columnName, JToken.FromObject(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;

					case NpgsqlDbType.Bigint:
						values.Add(columnName, JToken.FromObject((long)1));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Bigint:
						values.Add(columnName, JToken.FromObject(new long[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;
					#endregion

					#region real, double, numeric
					case NpgsqlDbType.Real:
						values.Add(columnName, JToken.FromObject((float)1.3f));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Real:
						values.Add(columnName, JToken.FromObject(new float[] { 1.23f, 2.45f, 3.67f, 4.89f, 5.01f, 6.23f, 7.45f, 8.67f, 9.89f }));
						break;

					case NpgsqlDbType.Double:
						values.Add(columnName, JToken.FromObject((double)1.3f));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Double:
						values.Add(columnName, JToken.FromObject(new double[] { 1.23f, 2.45f, 3.67f, 4.89f, 5.01f, 6.23f, 7.45f, 8.67f, 9.89f }));
						break;

					case NpgsqlDbType.Numeric:
					case NpgsqlDbType.Money:
						values.Add(columnName, JToken.FromObject((decimal)1.3f));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Numeric:
					case NpgsqlDbType.Array | NpgsqlDbType.Money:
						values.Add(columnName, JToken.FromObject(new decimal[] { 1.23m, 2.45m, 3.67m, 4.89m, 5.01m, 6.23m, 7.45m, 8.67m, 9.89m }));
						break;
					#endregion


					#region Guid
					case NpgsqlDbType.Uuid:
						values.Add(columnName, JToken.FromObject(Guid.NewGuid()));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Uuid:
						values.Add(columnName, JToken.FromObject(new Guid[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }));
						break;
					#endregion

					#region json
					case NpgsqlDbType.Json:
						{
							var answer = "{ \"Name\": \"John\" }";
							values.Add(columnName, JToken.FromObject(answer));
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Json:
						{
							var theList = new List<string>();

							var answer1 = "{ \"Name\": \"John\" }";
							theList.Add(answer1);

							var answer2 = "{ \"Name\": \"Jane\" }";
							theList.Add(answer2);

							var answer3 = "{ \"Name\": \"Bill\" }";
							theList.Add(answer3);

							values.Add(columnName, JToken.FromObject(theList.ToArray()));
						}
						break;

					#endregion

					#region varchar, text
					case NpgsqlDbType.Varchar:
						{
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							values.Add(columnName, JToken.FromObject(answer));
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Varchar:
						{
							var array = new JArray();
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							answer = "The cow mooed at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							answer = "The cat watched the dog and the cow.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							values.Add(columnName, array);
						}
						break;

					case NpgsqlDbType.Text:
						values.Add(columnName, JToken.FromObject("The dog barked at the moon"));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Text:
						{
							var array = new JArray
							{
								new JValue("The dog barked at the moon"),
								new JValue("The cow mooed at the moon"),
								new JValue("The cat watched the dog and the cow.")
							};
							values.Add(columnName, array);
						}
						break;
					#endregion

					#region bytea
					case NpgsqlDbType.Bytea:
						values.Add(columnName, JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Bytea:
						{
							var array = new JArray
							{
								JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }),
								JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }),
								JToken.FromObject(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 })
							};
							values.Add(columnName, array);
						}
						break;
					#endregion

					#region bit, varbit
					case NpgsqlDbType.Bit:
						{
							if (column.Length == 1)
								values.Add(columnName, JToken.FromObject(true));
							else
							{
								var str = new StringBuilder();
								for (int i = 0; i < column.Length; i++)
									str.Append("1");

								values.Add(columnName, JToken.FromObject(str.ToString()));
							}
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Bit:
						{
							if (column.Length == 1)
							{
								var array = new JArray
								{
									JToken.FromObject(true),
									JToken.FromObject(true),
									JToken.FromObject(false),
									JToken.FromObject(true)
								};
								values.Add(columnName, array);
							}
							else
							{
								var array = new JArray();

								for (int i = 0; i < 3; i++)
								{
									var str = new StringBuilder();
									for (int j = 0; j < column.Length; j++)
										str.Append("1");
									array.Add(JToken.FromObject(str.ToString()));
								}

								values.Add(columnName, array);
							}
						}
						break;

					case NpgsqlDbType.Varbit:
						{
							var str = new StringBuilder();
							for (int i = 0; i < column.Length && i < 10; i++)
								str.Append("1");

							values.Add(columnName, JToken.FromObject(str.ToString()));
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Varbit:
						{
							if (column.Length == 1)
							{
								var array = new JArray
								{
									JToken.FromObject(true),
									JToken.FromObject(true),
									JToken.FromObject(false),
									JToken.FromObject(true)
								};
								values.Add(columnName, array);
							}
							else
							{
								var array = new JArray();

								for (int i = 0; i < 3; i++)
								{
									var str = new StringBuilder();
									for (int j = 0; j < column.Length && j < 10; j++)
										str.Append("1");
									array.Add(JToken.FromObject(str.ToString()));
								}

								values.Add(columnName, array);
							}
						}
						break;
					#endregion

					#region char
					case NpgsqlDbType.Char:
						{
							if (string.Equals(column.dbDataType, "_char", StringComparison.OrdinalIgnoreCase))
							{
								values.Add(columnName, JToken.FromObject("The brown cow jumped over the moon.The dog barked at the cow, and the bull chased the dog."));
							}
							else if (string.Equals(column.dbDataType, "char", StringComparison.OrdinalIgnoreCase))
							{
								values.Add(columnName, JToken.FromObject('A'));
							}
							else
							{
								const string alphabet = "The brown cow jumped over the moon. The dog barked at the cow, and the bull chased the dog.";
								var chars = new char[column.Length];
								for (int i = 0; i < chars.Length; i++)
								{
									int j = i % alphabet.Length;
									chars[i] = alphabet[j];
								}

								values.Add(columnName, JToken.FromObject(new string(chars)));
							}
						}
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Char:
						{
							var array = new JArray();
							var answer = "The dog barked at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							answer = "The cow mooed at the moon.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							answer = "The cat watched the dog and the cow.";

							if (column.Length > -1)
								if (column.Length < answer.Length)
									answer = answer.Substring(0, (int)column.Length);

							array.Add(new JValue(answer));

							values.Add(columnName, array);
						}
						break;
					#endregion

					#region Boolean
					case NpgsqlDbType.Boolean:
						values.Add(columnName, JToken.FromObject((bool)true));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Boolean:
						values.Add(columnName, JToken.FromObject(new bool[] { true, true, false }));
						break;
					#endregion

					#region DateTime
					case NpgsqlDbType.Date:
						values.Add(columnName, JToken.FromObject(DateTime.Now));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Date:
						values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
						break;

					case NpgsqlDbType.Timestamp:
						values.Add(columnName, JToken.FromObject(DateTime.Now));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Timestamp:
						values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
						break;

					case NpgsqlDbType.Time:
						values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(10)));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Time:
						values.Add(columnName, JToken.FromObject(new TimeSpan[] { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2) }));
						break;

					case NpgsqlDbType.Interval:
						values.Add(columnName, JToken.FromObject(TimeSpan.FromMinutes(15)));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.Interval:
						values.Add(columnName, JToken.FromObject(new TimeSpan[] { TimeSpan.FromSeconds(20), TimeSpan.FromMinutes(30), TimeSpan.FromHours(2) }));
						break;

					case NpgsqlDbType.TimeTz:
						values.Add(columnName, JToken.FromObject(DateTimeOffset.Now));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.TimeTz:
						values.Add(columnName, JToken.FromObject(new DateTimeOffset[] { DateTimeOffset.Now, DateTimeOffset.Now.AddDays(1), DateTimeOffset.Now.AddDays(2) }));
						break;

					case NpgsqlDbType.TimestampTz:
						values.Add(columnName, JToken.FromObject(DateTime.Now));
						break;

					case NpgsqlDbType.Array | NpgsqlDbType.TimestampTz:
						values.Add(columnName, JToken.FromObject(new DateTime[] { DateTime.Now, DateTime.Now.AddDays(1), DateTime.Now.AddDays(2) }));
						break;
					#endregion

					default:
						values.Add(columnName, JToken.FromObject("Unrecognized"));
						break;
				}
			}

			return values;
		}

		private string GenerateSQLServerQuery(DBTable table)
		{
			var query = new StringBuilder("SELECT TOP 1");

			bool firstColumn = true;

			foreach (var column in DatabaseColumns)
			{
				if (firstColumn)
					firstColumn = false;
				else
					query.Append(", ");

				if (string.Equals(column.dbDataType, "hierarchyid", StringComparison.OrdinalIgnoreCase))
				{
					query.Append($"REPLACE(CAST({column.ColumnName} AS nvarchar({column.Length})), '/', '-' ) as [{column.ColumnName}]");
				}
				else
				{
					query.Append($"[{column.ColumnName}]");
				}
			}

			query.Append($" FROM [{table.Schema}].[{table.Table}] WITH(NOLOCK)");
			return query.ToString();
		}

		private string GeneratePostgresqlQuery(DBTable table)
		{
			var query = new StringBuilder("SELECT ");

			bool firstColumn = true;

			foreach (var column in DatabaseColumns)
			{
				if (firstColumn)
					firstColumn = false;
				else
					query.Append(", ");

				query.Append($"\"{column.ColumnName}\"");
			}

			query.Append($" FROM \"{table.Schema}\".\"{table.Table}\" LIMIT 1");
			return query.ToString();
		}

		private string GenerateMySQLQuery(DBTable table)
		{
			var query = new StringBuilder("SELECT ");

			bool firstColumn = true;

			foreach (var column in DatabaseColumns)
			{
				if (firstColumn)
					firstColumn = false;
				else
					query.Append(", ");

				query.Append($"{column.ColumnName}");
			}

			query.Append($" FROM {table.Table} LIMIT 1;");
			return query.ToString();
		}
		#endregion
	}
}
