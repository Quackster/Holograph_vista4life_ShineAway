using MySqlConnector;

namespace Holo.Data;

/// <summary>
/// Provides connection pooling and management for MySQL database access using MySqlConnector.
/// Replaces the legacy ODBC-based DB class with modern async-capable connection pooling.
/// </summary>
public sealed class Database
{
    private static Database? _instance;
    private static readonly object _lock = new();
    private string _connectionString = "";

    public static Database Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new Database();
                }
            }
            return _instance;
        }
    }

    private Database() { }

    /// <summary>
    /// Opens connection pool to the MySQL database with the supplied parameters.
    /// </summary>
    /// <param name="host">The hostname/IP address where the database server is located.</param>
    /// <param name="port">The port the database server is running on.</param>
    /// <param name="database">The name of the database.</param>
    /// <param name="username">The username for authentication with the database.</param>
    /// <param name="password">The password for authentication with the database.</param>
    /// <returns>True if connection test succeeds, false otherwise.</returns>
    public bool OpenConnection(string host, int port, string database, string username, string password)
    {
        try
        {
            Out.WriteLine($"Connecting to {database} at {host}:{port} for user '{username}'");

            var builder = new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = (uint)port,
                Database = database,
                UserID = username,
                Password = password,
                Pooling = true,
                MinimumPoolSize = 5,
                MaximumPoolSize = 100,
                ConnectionLifeTime = 0,
                ConnectionIdleTimeout = 60,
                AllowUserVariables = true,
                DefaultCommandTimeout = 30
            };

            _connectionString = builder.ConnectionString;

            // Test connection
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            Out.WriteLine("Connection to database successful.");
            return true;
        }
        catch (Exception ex)
        {
            Out.WriteError($"Failed to connect! Error thrown was: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets a new connection from the connection pool.
    /// </summary>
    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }

    /// <summary>
    /// Closes the connection pool.
    /// </summary>
    public void CloseConnection()
    {
        Out.WriteLine("Closing database connection pool...");
        try
        {
            MySqlConnection.ClearAllPools();
            Out.WriteLine("Database connection pool closed.");
        }
        catch
        {
            Out.WriteLine("No active database connections.");
        }
    }
}
