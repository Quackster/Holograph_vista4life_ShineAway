using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for user ignore-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class IgnoreDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the list of user IDs that a user has ignored.
        /// </summary>
        public List<int> GetIgnoredUserIds(int userId)
        {
            string query = "SELECT targetid FROM user_ignores WHERE userid = @userId";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Adds a user to the ignore list.
        /// </summary>
        public bool AddIgnoredUser(int userId, int targetUserId)
        {
            string query = "INSERT INTO user_ignores(userid, targetid) VALUES (@userId, @targetUserId)";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@targetUserId", targetUserId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Removes a user from the ignore list.
        /// </summary>
        public bool RemoveIgnoredUser(int userId, int targetUserId)
        {
            string query = "DELETE FROM user_ignores WHERE userid = @userId AND targetid = @targetUserId";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@targetUserId", targetUserId)
            };
            return ExecuteNonQuery(query, parameters);
        }
    }
}
