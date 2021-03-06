﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace Olive.Entities.Data
{
    public class SqlServerManager
    {
        readonly string ConnectionString;

        public SqlServerManager() => ConnectionString = Config.GetConnectionString("AppDatabase");

        public SqlServerManager(string connectionString) => ConnectionString = connectionString;

        /// <summary>
        /// Executes a specified SQL command.
        /// </summary>
        public void ExecuteSql(string sql)
        {
            var lines = new Regex(@"^\s*GO\s*$", RegexOptions.Multiline).Split(sql);

            using (var connection = new SqlConnection(ConnectionString))
            {
                try
                {
                    connection.Open();
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to open a DB connection.", ex);
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;

                    foreach (var line in lines.Trim())
                    {
                        cmd.CommandText = line;

                        try { cmd.ExecuteNonQuery(); }
                        catch (Exception ex) { throw EnrichError(ex, line); }
                    }
                }
            }
        }

        Exception EnrichError(Exception ex, string command) =>
            throw new Exception($"Could not execute SQL command: \r\n-----------------------\r\n{command.Trim()}\r\n-----------------------\r\n Because:\r\n\r\n{ex.Message}");

        public void DetachDatabase(string databaseName)
        {
            var script = @"
ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
ALTER DATABASE [{0}] SET MULTI_USER;
exec sp_detach_db '{0}'".FormatWith(databaseName);

            try
            {
                ExecuteSql(script);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Could not detach database '{databaseName}' becuase '{ex.Message}'", ex);
            }
        }

        public void DeleteDatabase(string databaseName)
        {
            var script = @"
IF EXISTS (SELECT name FROM master.dbo.sysdatabases WHERE name = N'{0}')
BEGIN
    ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    ALTER DATABASE [{0}] SET MULTI_USER;
    DROP DATABASE [{0}];
END".FormatWith(databaseName);

            try
            {
                ExecuteSql(script);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not drop database '" + databaseName + "'.", ex);
            }
        }

        public SqlServerManager CloneFor(string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString)
            {
                InitialCatalog = databaseName
            };

            return new SqlServerManager(builder.ToString());
        }

        public bool DatabaseExists(string databaseName)
        {
            var script = $"SELECT count(name) FROM master.dbo.sysdatabases WHERE name = N'{databaseName}'";

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = script;

                    try { return (int)cmd.ExecuteScalar() > 0; }
                    catch (Exception ex) { throw EnrichError(ex, script); }
                }
            }
        }
    }
}