using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Holo;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Base class for all data access operations. Provides parameterized query execution.
    /// </summary>
    public abstract class BaseDataAccess
    {
        /// <summary>
        /// Executes a parameterized query that returns a single scalar value as a string.
        /// </summary>
        protected string ExecuteScalarString(string query, params MySqlParameter[] parameters)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        var result = command.ExecuteScalar();
                        return result?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error executing scalar query: {ex.Message} at '{query}'");
                return string.Empty;
            }
        }

        /// <summary>
        /// Executes a parameterized query that returns a single scalar value as an integer.
        /// </summary>
        protected int ExecuteScalarInt(string query, params MySqlParameter[] parameters)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        var result = command.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                            return 0;
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error executing scalar int query: {ex.Message} at '{query}'");
                return 0;
            }
        }

        /// <summary>
        /// Executes a parameterized query that returns a single row as a dictionary of column names to values.
        /// </summary>
        protected Dictionary<string, string> ExecuteSingleRow(string query, params MySqlParameter[] parameters)
        {
            var result = new Dictionary<string, string>();
            try
            {
                using (var connection = GetConnection())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    string value = reader.IsDBNull(i) ? string.Empty : reader[i].ToString();
                                    result[columnName] = value;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error executing single row query: {ex.Message} at '{query}'");
            }
            return result;
        }

        /// <summary>
        /// Executes a parameterized query that returns multiple rows as a list of dictionaries.
        /// </summary>
        protected List<Dictionary<string, string>> ExecuteMultipleRows(string query, int? maxResults = null, params MySqlParameter[] parameters)
        {
            var results = new List<Dictionary<string, string>>();
            try
            {
                if (maxResults.HasValue && maxResults.Value > 0)
                {
                    query += $" LIMIT {maxResults.Value}";
                }

                using (var connection = GetConnection())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var row = new Dictionary<string, string>();
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnName = reader.GetName(i);
                                    string value = reader.IsDBNull(i) ? string.Empty : reader[i].ToString();
                                    row[columnName] = value;
                                }
                                results.Add(row);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error executing multiple rows query: {ex.Message} at '{query}'");
            }
            return results;
        }

        /// <summary>
        /// Executes a parameterized query that returns a single column as a list of strings.
        /// </summary>
        protected List<string> ExecuteSingleColumnString(string query, int? maxResults = null, params MySqlParameter[] parameters)
        {
            var results = new List<string>();
            try
            {
                if (maxResults.HasValue && maxResults.Value > 0)
                {
                    query += $" LIMIT {maxResults.Value}";
                }

                using (var connection = GetConnection())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string value = reader.IsDBNull(0) ? string.Empty : reader[0].ToString();
                                results.Add(value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error executing single column string query: {ex.Message} at '{query}'");
            }
            return results;
        }

        /// <summary>
        /// Executes a parameterized query that returns a single column as a list of integers.
        /// </summary>
        protected List<int> ExecuteSingleColumnInt(string query, int? maxResults = null, params MySqlParameter[] parameters)
        {
            var results = new List<int>();
            try
            {
                if (maxResults.HasValue && maxResults.Value > 0)
                {
                    query += $" LIMIT {maxResults.Value}";
                }

                using (var connection = GetConnection())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int value = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                                results.Add(value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error executing single column int query: {ex.Message} at '{query}'");
            }
            return results;
        }

        /// <summary>
        /// Executes a parameterized non-query (INSERT, UPDATE, DELETE).
        /// </summary>
        protected bool ExecuteNonQuery(string query, params MySqlParameter[] parameters)
        {
            try
            {
                using (var connection = GetConnection())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        command.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error executing non-query: {ex.Message} at '{query}'");
                return false;
            }
        }

        /// <summary>
        /// Checks if a record exists based on the query.
        /// </summary>
        protected bool RecordExists(string query, params MySqlParameter[] parameters)
        {
            try
            {
                query += " LIMIT 1";
                using (var connection = GetConnection())
                {
                    using (var command = new MySqlCommand(query, connection))
                    {
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        using (var reader = command.ExecuteReader())
                        {
                            return reader.HasRows;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Out.WriteError($"Error checking record existence: {ex.Message} at '{query}'");
                return false;
            }
        }

        /// <summary>
        /// Gets a connection from the DB class.
        /// </summary>
        private MySqlConnection GetConnection()
        {
            return DB.GetConnection();
        }
    }
}
