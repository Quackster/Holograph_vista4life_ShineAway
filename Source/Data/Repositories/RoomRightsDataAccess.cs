using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for room rights, bans, and votes related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class RoomRightsDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Checks if a user has rights in a room.
        /// </summary>
        public bool UserHasRights(int roomId, int userId)
        {
            string query = "SELECT userid FROM room_rights WHERE roomid = @roomId AND userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@userId", userId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Grants room rights to a user.
        /// </summary>
        public bool GrantRoomRights(int roomId, int userId)
        {
            string query = "INSERT INTO room_rights(roomid, userid) VALUES (@roomId, @userId)";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Revokes room rights from a user.
        /// </summary>
        public bool RevokeRoomRights(int roomId, int userId)
        {
            string query = "DELETE FROM room_rights WHERE roomid = @roomId AND userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Checks if a user is banned from a room.
        /// </summary>
        public bool UserIsBannedFromRoom(int roomId, int userId)
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
        public DateTime? GetRoomBanExpiration(int roomId, int userId)
        {
            string query = "SELECT ban_expire FROM room_bans WHERE roomid = @roomId AND userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@userId", userId)
            };
            string expireDateStr = ExecuteScalarString(query, parameters);
            
            if (string.IsNullOrEmpty(expireDateStr))
                return null;

            if (DateTime.TryParse(expireDateStr, out DateTime expireDate))
                return expireDate;
            
            return null;
        }

        /// <summary>
        /// Deletes an expired room ban.
        /// </summary>
        public bool DeleteExpiredRoomBan(int roomId, int userId)
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
        /// Checks if a user has voted in a room.
        /// </summary>
        public bool UserHasVotedInRoom(int roomId, int userId)
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
        /// Gets the total vote amount for a room.
        /// </summary>
        public int GetRoomTotalVotes(int roomId)
        {
            string query = "SELECT SUM(vote) FROM room_votes WHERE roomid = @roomId";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            int total = ExecuteScalarInt(query, parameters);
            return total < 0 ? 0 : total;
        }

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
        /// Gets favorite room information.
        /// </summary>
        public FavoriteRoomInfo GetFavoriteRoomInfo(int roomId)
        {
            string query = "SELECT name, owner, state, showname, visitors_now, visitors_max, description FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new FavoriteRoomInfo
            {
                RoomId = roomId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Owner = row.ContainsKey("owner") ? row["owner"] : string.Empty,
                State = row.ContainsKey("state") ? int.Parse(row["state"]) : 0,
                ShowName = row.ContainsKey("showname") ? int.Parse(row["showname"]) : 0,
                VisitorsNow = row.ContainsKey("visitors_now") ? int.Parse(row["visitors_now"]) : 0,
                VisitorsMax = row.ContainsKey("visitors_max") ? int.Parse(row["visitors_max"]) : 0,
                Description = row.ContainsKey("description") ? row["description"] : string.Empty
            };
        }

        /// <summary>
        /// Removes a room from user's favorites.
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

        /// <summary>
        /// Gets the category ID for a room.
        /// </summary>
        public int GetRoomCategoryId(int roomId)
        {
            string query = "SELECT category FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets room CCTs (custom colors/textures).
        /// </summary>
        public string GetRoomCCTs(int roomId)
        {
            string query = "SELECT ccts FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Checks if a room is already in user's favorites.
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
        /// Adds a room to user's favorites.
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
    }

    /// <summary>
    /// Represents favorite room information.
    /// </summary>
    public class FavoriteRoomInfo
    {
        public int RoomId { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public int State { get; set; }
        public int ShowName { get; set; }
        public int VisitorsNow { get; set; }
        public int VisitorsMax { get; set; }
        public string Description { get; set; }
    }
}
