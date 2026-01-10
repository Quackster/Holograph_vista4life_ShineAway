using System;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for system-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class SystemDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the count of users in the database.
        /// </summary>
        public int GetUserCount()
        {
            string query = "SELECT COUNT(*) FROM users";
            return ExecuteScalarInt(query);
        }

        /// <summary>
        /// Gets the count of rooms in the database.
        /// </summary>
        public int GetRoomCount()
        {
            string query = "SELECT COUNT(*) FROM rooms";
            return ExecuteScalarInt(query);
        }

        /// <summary>
        /// Gets the count of furniture items in the database.
        /// </summary>
        public int GetFurnitureCount()
        {
            string query = "SELECT COUNT(*) FROM furniture";
            return ExecuteScalarInt(query);
        }

        /// <summary>
        /// Resets system dynamic values.
        /// </summary>
        public bool ResetSystemDynamics()
        {
            string query = @"
                UPDATE system 
                SET onlinecount = '0', 
                    onlinecount_peak = '0', 
                    connections_accepted = '0', 
                    activerooms = '0'";
            
            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Nullifies all user SSO tickets.
        /// </summary>
        public bool NullifyAllUserTickets()
        {
            string query = "UPDATE users SET ticket_sso = NULL";
            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Resets visitor counts for all rooms.
        /// </summary>
        public bool ResetRoomVisitorCounts()
        {
            string query = "UPDATE rooms SET visitors_now = '0'";
            return ExecuteNonQuery(query);
        }

        /// <summary>
        /// Updates system statistics.
        /// </summary>
        public bool UpdateSystemStats(int onlineCount, int peakOnlineCount, int activeRooms, int peakRoomCount, int acceptedConnections)
        {
            string query = @"
                UPDATE system 
                SET onlinecount = @onlineCount, 
                    onlinecount_peak = @peakOnlineCount, 
                    activerooms = @activeRooms, 
                    activerooms_peak = @peakRoomCount, 
                    connections_accepted = @acceptedConnections";
            
            var parameters = new[]
            {
                new MySqlParameter("@onlineCount", onlineCount),
                new MySqlParameter("@peakOnlineCount", peakOnlineCount),
                new MySqlParameter("@activeRooms", activeRooms),
                new MySqlParameter("@peakRoomCount", peakRoomCount),
                new MySqlParameter("@acceptedConnections", acceptedConnections)
            };
            
            return ExecuteNonQuery(query, parameters);
        }
    }
}
