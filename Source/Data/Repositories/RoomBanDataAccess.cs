using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for room ban-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class RoomBanDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Checks if a user is banned from a room.
        /// </summary>
        public bool IsUserBannedFromRoom(int userId, int roomId)
        {
            string query = "SELECT roomid FROM room_bans WHERE roomid = @roomId AND userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@userId", userId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets the ban expiration date for a user in a room.
        /// </summary>
        public string GetBanExpirationDate(int userId, int roomId)
        {
            string query = "SELECT ban_expire FROM room_bans WHERE roomid = @roomId AND userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Creates a new room ban for a user.
        /// </summary>
        public bool CreateRoomBan(int roomId, int userId, string banExpireDate)
        {
            string query = "INSERT INTO room_bans (roomid, userid, ban_expire) VALUES (@roomId, @userId, @banExpireDate)";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@banExpireDate", banExpireDate)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Removes a room ban for a user (when ban has expired).
        /// </summary>
        public bool RemoveRoomBan(int roomId, int userId)
        {
            string query = "DELETE FROM room_bans WHERE roomid = @roomId AND userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes all room bans for a room (used when deleting a room).
        /// </summary>
        public bool DeleteRoomBans(int roomId)
        {
            string query = "DELETE FROM room_bans WHERE roomid = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }
    }
}
