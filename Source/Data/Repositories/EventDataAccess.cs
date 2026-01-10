using System;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for event-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class EventDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Creates a new event.
        /// </summary>
        public bool CreateEvent(int userId, int roomId, int categoryId, string name, string description, string date)
        {
            string query = @"
                INSERT INTO events (
                    name, 
                    description, 
                    userid, 
                    roomid, 
                    category, 
                    date
                ) VALUES (
                    @name, 
                    @description, 
                    @userId, 
                    @roomId, 
                    @categoryId, 
                    @date
                )";
            
            var parameters = new[]
            {
                new MySqlParameter("@name", name ?? string.Empty),
                new MySqlParameter("@description", description ?? string.Empty),
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@categoryId", categoryId),
                new MySqlParameter("@date", date ?? string.Empty)
            };
            
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes an event by room ID.
        /// </summary>
        public bool DeleteEvent(int roomId)
        {
            string query = @"
                DELETE FROM events 
                WHERE roomid = @roomId";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates an event.
        /// </summary>
        public bool UpdateEvent(int roomId, int categoryId, string name, string description, string date)
        {
            string query = @"
                UPDATE events 
                SET name = @name, 
                    description = @description, 
                    category = @categoryId, 
                    date = @date 
                WHERE roomid = @roomId";
            
            var parameters = new[]
            {
                new MySqlParameter("@name", name ?? string.Empty),
                new MySqlParameter("@description", description ?? string.Empty),
                new MySqlParameter("@categoryId", categoryId),
                new MySqlParameter("@date", date ?? string.Empty),
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }
    }
}
