using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for room vote-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class RoomVoteDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Checks if a user has already voted on a room.
        /// </summary>
        public bool HasUserVoted(int userId, int roomId)
        {
            string query = "SELECT userid FROM room_votes WHERE userid = @userId AND roomid = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@roomId", roomId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets the sum of all votes for a room.
        /// </summary>
        public int GetRoomVoteSum(int roomId)
        {
            string query = "SELECT SUM(vote) FROM room_votes WHERE roomid = @roomId";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Creates a new vote for a room by a user.
        /// </summary>
        public bool CreateRoomVote(int userId, int roomId, int vote)
        {
            string query = "INSERT INTO room_votes (userid, roomid, vote) VALUES (@userId, @roomId, @vote)";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@vote", vote)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes all votes for a room (used when deleting a room).
        /// </summary>
        public bool DeleteRoomVotes(int roomId)
        {
            string query = "DELETE FROM room_votes WHERE roomid = @roomId";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }
    }
}
