using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for favorite room-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class FavoriteRoomDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets favorite room IDs for a user.
        /// </summary>
        public List<int> GetFavoriteRoomIds(int userId, int maxRooms)
        {
            string query = "SELECT roomid FROM users_favouriterooms WHERE userid = @userId ORDER BY roomid DESC LIMIT @maxRooms";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@maxRooms", maxRooms)
            };
            return ExecuteSingleColumnInt(query, maxRooms, parameters);
        }

        /// <summary>
        /// Gets the count of favorite rooms for a user.
        /// </summary>
        public int GetFavoriteRoomCount(int userId)
        {
            string query = "SELECT COUNT(userid) FROM users_favouriterooms WHERE userid = @userId";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Checks if a room is already in a user's favorites.
        /// </summary>
        public bool IsRoomInFavorites(int userId, int roomId)
        {
            string query = "SELECT userid FROM users_favouriterooms WHERE userid = @userId AND roomid = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@roomId", roomId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Adds a room to a user's favorites.
        /// </summary>
        public bool AddFavoriteRoom(int userId, int roomId)
        {
            string query = "INSERT INTO users_favouriterooms(userid, roomid) VALUES (@userId, @roomId)";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Removes a room from a user's favorites.
        /// </summary>
        public bool RemoveFavoriteRoom(int userId, int roomId)
        {
            string query = "DELETE FROM users_favouriterooms WHERE userid = @userId AND roomid = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }
    }
}
