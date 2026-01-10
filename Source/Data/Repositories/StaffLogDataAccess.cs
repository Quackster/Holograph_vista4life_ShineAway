using System;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for staff log-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class StaffLogDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Adds a staff log entry.
        /// </summary>
        public bool AddStaffLog(string action, int userId, int targetId, string message, string note, string timestamp)
        {
            string query = @"
                INSERT INTO system_stafflog (
                    action, 
                    userid, 
                    targetid, 
                    message, 
                    note, 
                    timestamp
                ) VALUES (
                    @action, 
                    @userId, 
                    @targetId, 
                    @message, 
                    @note, 
                    @timestamp
                )";
            
            var parameters = new[]
            {
                new MySqlParameter("@action", action ?? string.Empty),
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@targetId", targetId),
                new MySqlParameter("@message", message ?? string.Empty),
                new MySqlParameter("@note", note ?? string.Empty),
                new MySqlParameter("@timestamp", timestamp ?? string.Empty)
            };
            
            return ExecuteNonQuery(query, parameters);
        }
    }
}
