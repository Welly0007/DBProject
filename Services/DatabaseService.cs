using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace TaskWorkerApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _schemaFilePath;

        public DatabaseService(string connectionString, string schemaFilePath)
        {
            _connectionString = connectionString;
            _schemaFilePath = schemaFilePath;
        }

        public void EnsureDatabaseInitialized()
        {
            if (!IsDatabaseInitialized())
            {
                InitializeDatabase();
            }
        }

        private bool IsDatabaseInitialized()
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    string checkQuery = "SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Workers'";
                    using (SqlCommand command = new SqlCommand(checkQuery, connection))
                    {
                        int tableCount = (int)command.ExecuteScalar();
                        return tableCount > 0;
                    }
                }
            }
            catch
            {
                return false; // If an error occurs (e.g., can't connect), assume not initialized
            }
        }

        private void InitializeDatabase()
        {
            string schemaSql = System.IO.File.ReadAllText(_schemaFilePath);
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(schemaSql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public DataTable ExecuteQuery(string query)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);
                return dataTable;
            }
        }

        public DataTable ExecuteQuery(string query, Action<SqlCommand> parameterize)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand(query, connection))
                {
                    parameterize(command);
                    using (var adapter = new SqlDataAdapter(command))
                    {
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        return dt;
                    }
                }
            }
        }

        public int ExecuteNonQuery(string query, Action<SqlCommand> parameterize)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    parameterize(command);
                    return command.ExecuteNonQuery();
                }
            }
        }

        public object ExecuteQueryScalar(string query, Action<SqlCommand> parameterize)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    parameterize(command);
                    return command.ExecuteScalar();
                }
            }
        }

        public void ResetDatabase()
        {
            // Read the SQL script from the file
            string sql = File.ReadAllText(_schemaFilePath);

            // Execute the script to reset the database
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                
                // Split script by GO statements if present
                string[] commands = sql.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string command in commands)
                {
                    if (!string.IsNullOrWhiteSpace(command))
                    {
                        using (SqlCommand cmd = new SqlCommand(command, connection))
                        {
                            try
                            {
                                cmd.ExecuteNonQuery();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error executing SQL: {ex.Message}", ex);
                            }
                        }
                    }
                }
            }
        }
    }
}
