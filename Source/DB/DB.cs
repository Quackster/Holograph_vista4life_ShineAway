using MySqlConnector;
using Holo.Data;

namespace Holo;

/// <summary>
/// Legacy static DB class - delegates to the new Database class and repositories.
/// This class maintains backward compatibility during the migration.
/// WARNING: Uses raw SQL queries - use repositories for new code.
/// </summary>
public static class DB
{
    private static Database? _database;

    #region Database connection management
    /// <summary>
    /// Opens connection to the MySQL database with the supplied parameters.
    /// </summary>
    public static bool openConnection(string dbHost, int dbPort, string dbName, string dbUsername, string dbPassword)
    {
        _database = Database.Instance;
        return _database.OpenConnection(dbHost, dbPort, dbName, dbUsername, dbPassword);
    }

    /// <summary>
    /// Closes connection with the MySQL database.
    /// </summary>
    public static void closeConnection()
    {
        _database?.CloseConnection();
    }
    #endregion

    #region Database data manipulation
    /// <summary>
    /// Executes a SQL statement on the database.
    /// </summary>
    public static void runQuery(string Query)
    {
        try
        {
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query, conn);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{Query}'");
        }
    }
    #endregion

    #region Database data retrieval
    /// <summary>
    /// Performs a SQL query and returns the first selected field as string.
    /// </summary>
    public static string runRead(string Query)
    {
        try
        {
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query + " LIMIT 1", conn);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{Query}'");
            return "";
        }
    }

    /// <summary>
    /// Performs a SQL query and returns the first selected field as integer.
    /// </summary>
    public static int runRead(string Query, object? Tick)
    {
        try
        {
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query + " LIMIT 1", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{Query}'");
            return 0;
        }
    }

    /// <summary>
    /// Performs a SQL query and returns all vertical matching fields as a String array.
    /// </summary>
    public static string[] runReadColumn(string Query, int maxResults)
    {
        if (maxResults > 0)
            Query += " LIMIT " + maxResults;

        try
        {
            var columnBuilder = new List<string>();
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query, conn);
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
            Out.WriteError($"Error '{ex.Message}' at '{Query}'");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Performs a SQL query and returns all vertical matching fields as an Integer array.
    /// </summary>
    public static int[] runReadColumn(string Query, int maxResults, object? Tick)
    {
        if (maxResults > 0)
            Query += " LIMIT " + maxResults;

        try
        {
            var columnBuilder = new List<int>();
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query, conn);
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
            Out.WriteError($"Error '{ex.Message}' at '{Query}'");
            return Array.Empty<int>();
        }
    }

    /// <summary>
    /// Performs a SQL query and returns the first row as a String array.
    /// </summary>
    public static string[] runReadRow(string Query)
    {
        try
        {
            var rowBuilder = new List<string>();
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query + " LIMIT 1", conn);
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
            Out.WriteError($"Error '{ex.Message}' at '{Query}'");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Performs a SQL query and returns the first row as an Integer array.
    /// </summary>
    public static int[] runReadRow(string Query, object? Tick)
    {
        try
        {
            var rowBuilder = new List<int>();
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query + " LIMIT 1", conn);
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
            Out.WriteError($"Error '{ex.Message}' at '{Query}'");
            return Array.Empty<int>();
        }
    }

    /// <summary>
    /// Performs a SQL query and returns the result as a string. No error reported on failure.
    /// </summary>
    public static string runReadUnsafe(string Query)
    {
        try
        {
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query + " LIMIT 1", conn);
            var result = cmd.ExecuteScalar();
            return result?.ToString() ?? "";
        }
        catch { return ""; }
    }

    /// <summary>
    /// Performs a SQL query and returns the result as an integer. No error reported on failure.
    /// </summary>
    public static int runReadUnsafe(string Query, object? Tick)
    {
        try
        {
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query + " LIMIT 1", conn);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        catch { return 0; }
    }
    #endregion

    #region Data availability checks
    /// <summary>
    /// Checks if any rows match the query.
    /// </summary>
    public static bool checkExists(string Query)
    {
        try
        {
            using var conn = _database!.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand(Query + " LIMIT 1", conn);
            using var reader = cmd.ExecuteReader();
            return reader.HasRows;
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error '{ex.Message}' at '{Query}'");
            return false;
        }
    }
    #endregion

    #region Misc
    /// <summary>
    /// Returns a stripslashed copy of the input string.
    /// </summary>
    public static string Stripslash(string Query)
    {
        try { return Query.Replace(@"\", "\\").Replace("'", @"\'"); }
        catch { return ""; }
    }
    #endregion
}