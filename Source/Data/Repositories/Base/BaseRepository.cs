using MySqlConnector;

namespace Holo.Data.Repositories.Base;

/// <summary>
/// Base class for all repositories providing parameterized query execution methods.
/// All SQL queries use parameters to prevent SQL injection attacks.
/// </summary>
public abstract class BaseRepository
{
    protected readonly Database _db;

    protected BaseRepository()
    {
        _db = Database.Instance;
    }

    /// <summary>
    /// Creates a MySqlParameter with the given name and value.
    /// </summary>
    protected static MySqlParameter Param(string name, object? value)
    {
        return new MySqlParameter(name, value ?? DBNull.Value);
    }

    /// <summary>
    /// Executes a non-query SQL statement (INSERT, UPDATE, DELETE).
    /// </summary>
    protected void Execute(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{sql}'");
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the first column of the first row as a string.
    /// </summary>
    protected string? ReadScalar(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql + " LIMIT 1", conn);
            cmd.Parameters.AddRange(parameters);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{sql}'");
            return "";
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the first column of the first row as an integer.
    /// </summary>
    protected int ReadScalarInt(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql + " LIMIT 1", conn);
            cmd.Parameters.AddRange(parameters);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{sql}'");
            return 0;
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the first column of the first row as a string.
    /// Does not report errors on failure.
    /// </summary>
    protected string ReadScalarUnsafe(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql + " LIMIT 1", conn);
            cmd.Parameters.AddRange(parameters);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the first column of the first row as an integer.
    /// Does not report errors on failure.
    /// </summary>
    protected int ReadScalarIntUnsafe(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql + " LIMIT 1", conn);
            cmd.Parameters.AddRange(parameters);
            var result = cmd.ExecuteScalar();
            return Convert.ToInt32(result);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the first row as a string array.
    /// </summary>
    protected string[] ReadRow(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            var rowBuilder = new List<string>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql + " LIMIT 1", conn);
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    try { rowBuilder.Add(reader[i].ToString() ?? ""); }
                    catch { rowBuilder.Add(""); }
                }
            }

            return rowBuilder.ToArray();
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{sql}'");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the first row as an integer array.
    /// </summary>
    protected int[] ReadRowInt(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            var rowBuilder = new List<int>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql + " LIMIT 1", conn);
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    try { rowBuilder.Add(reader.GetInt32(i)); }
                    catch { rowBuilder.Add(0); }
                }
            }

            return rowBuilder.ToArray();
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{sql}'");
            return Array.Empty<int>();
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the first column of all rows as a string array.
    /// </summary>
    protected string[] ReadColumn(string sql, int maxResults, params MySqlParameter[] parameters)
    {
        string query = maxResults > 0 ? sql + " LIMIT " + maxResults : sql;

        try
        {
            var columnBuilder = new List<string>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                try { columnBuilder.Add(reader[0].ToString() ?? ""); }
                catch { columnBuilder.Add(""); }
            }

            return columnBuilder.ToArray();
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{query}'");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the first column of all rows as an integer array.
    /// </summary>
    protected int[] ReadColumnInt(string sql, int maxResults, params MySqlParameter[] parameters)
    {
        string query = maxResults > 0 ? sql + " LIMIT " + maxResults : sql;

        try
        {
            var columnBuilder = new List<int>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                try { columnBuilder.Add(reader.GetInt32(0)); }
                catch { columnBuilder.Add(0); }
            }

            return columnBuilder.ToArray();
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{query}'");
            return Array.Empty<int>();
        }
    }

    /// <summary>
    /// Checks if any rows match the given query.
    /// </summary>
    protected bool Exists(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql + " LIMIT 1", conn);
            cmd.Parameters.AddRange(parameters);
            using var reader = cmd.ExecuteReader();
            return reader.HasRows;
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{sql}'");
            return false;
        }
    }

    /// <summary>
    /// Executes a SQL query and returns the last inserted ID.
    /// </summary>
    protected long ExecuteAndGetLastInsertId(string sql, params MySqlParameter[] parameters)
    {
        try
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddRange(parameters);
            cmd.ExecuteNonQuery();
            return cmd.LastInsertedId;
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{sql}'");
            return 0;
        }
    }
}
