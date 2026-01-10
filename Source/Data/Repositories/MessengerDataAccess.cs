using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for messenger and friend-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class MessengerDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the next friend request ID for a user.
        /// </summary>
        public int GetNextFriendRequestId(int toUserId)
        {
            string query = "SELECT MAX(requestid) FROM messenger_friendrequests WHERE userid_to = @toUserId";
            var parameters = new[]
            {
                new MySqlParameter("@toUserId", toUserId)
            };
            int maxId = ExecuteScalarInt(query, parameters);
            return maxId + 1;
        }

        /// <summary>
        /// Creates a new friend request.
        /// </summary>
        public bool CreateFriendRequest(int toUserId, int fromUserId, int requestId)
        {
            string query = "INSERT INTO messenger_friendrequests(userid_to, userid_from, requestid) VALUES (@toUserId, @fromUserId, @requestId)";
            var parameters = new[]
            {
                new MySqlParameter("@toUserId", toUserId),
                new MySqlParameter("@fromUserId", fromUserId),
                new MySqlParameter("@requestId", requestId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets the user ID who sent a friend request.
        /// </summary>
        public int GetFriendRequestSenderId(int toUserId, int requestId)
        {
            string query = "SELECT userid_from FROM messenger_friendrequests WHERE userid_to = @toUserId AND requestid = @requestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@toUserId", toUserId),
                new MySqlParameter("@requestId", requestId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Creates a friendship between two users.
        /// </summary>
        public bool CreateFriendship(int userId, int friendId)
        {
            string query = "INSERT INTO messenger_friendships(userid, friendid) VALUES (@userId, @friendId)";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@friendId", friendId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes a friend request.
        /// </summary>
        public bool DeleteFriendRequest(int toUserId, int requestId)
        {
            string query = "DELETE FROM messenger_friendrequests WHERE userid_to = @toUserId AND requestid = @requestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@toUserId", toUserId),
                new MySqlParameter("@requestId", requestId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes a friendship between two users (bidirectional).
        /// </summary>
        public bool DeleteFriendship(int userId, int friendId)
        {
            string query = "DELETE FROM messenger_friendships WHERE (userid = @userId AND friendid = @friendId) OR (userid = @friendId AND friendid = @userId) LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@friendId", friendId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets all user IDs that sent friend requests to a user.
        /// </summary>
        public List<int> GetFriendRequestUserIds(int toUserId)
        {
            string query = @"
                SELECT userid_from 
                FROM messenger_friendrequests 
                WHERE userid_to = @toUserId 
                ORDER BY requestid ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@toUserId", toUserId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all request IDs for friend requests sent to a user.
        /// </summary>
        public List<int> GetFriendRequestIds(int toUserId)
        {
            string query = @"
                SELECT requestid 
                FROM messenger_friendrequests 
                WHERE userid_to = @toUserId 
                ORDER BY requestid ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@toUserId", toUserId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Checks if there are friend requests between two users.
        /// </summary>
        public bool HasFriendRequests(int userId1, int userId2)
        {
            string query = @"
                SELECT requestid 
                FROM messenger_friendrequests 
                WHERE (userid_to = @userId1 AND userid_from = @userId2) 
                    OR (userid_to = @userId2 AND userid_from = @userId1) 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@userId1", userId1),
                new MySqlParameter("@userId2", userId2)
            };
            
            return RecordExists(query, parameters);
        }
    }
}
