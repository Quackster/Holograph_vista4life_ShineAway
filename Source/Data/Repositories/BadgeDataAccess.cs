using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for badge-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class BadgeDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets all badges for a user ordered by slot ID.
        /// </summary>
        public List<BadgeInfo> GetUserBadges(int userId)
        {
            string query = "SELECT badge, slotid FROM users_badges WHERE userid = @userId ORDER BY slotid ASC";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            var rows = ExecuteMultipleRows(query, null, parameters);
            
            var badges = new List<BadgeInfo>();
            foreach (var row in rows)
            {
                badges.Add(new BadgeInfo
                {
                    Badge = row.ContainsKey("badge") ? row["badge"] : string.Empty,
                    SlotId = row.ContainsKey("slotid") ? int.Parse(row["slotid"]) : 0
                });
            }
            return badges;
        }

        /// <summary>
        /// Gets badge names for a user ordered by slot ID.
        /// </summary>
        public List<string> GetUserBadgeNames(int userId)
        {
            string query = "SELECT badge FROM users_badges WHERE userid = @userId ORDER BY slotid ASC";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets badge slot IDs for a user ordered by slot ID.
        /// </summary>
        public List<int> GetUserBadgeSlotIds(int userId)
        {
            string query = "SELECT slotid FROM users_badges WHERE userid = @userId ORDER BY slotid ASC";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Resets all badge slots for a user to 0.
        /// </summary>
        public bool ResetUserBadgeSlots(int userId)
        {
            string query = "UPDATE users_badges SET slotid = '0' WHERE userid = @userId";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the slot ID for a specific badge of a user.
        /// </summary>
        public bool UpdateBadgeSlot(int userId, string badge, int slotId)
        {
            string query = "UPDATE users_badges SET slotid = @slotId WHERE userid = @userId AND badge = @badge LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@slotId", slotId),
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@badge", badge)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Checks if a user has a badge record entry.
        /// </summary>
        public bool UserBadgeRecordExists(int userId)
        {
            string query = "SELECT * FROM users_badgescur WHERE userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Creates a new badge record entry for a user.
        /// </summary>
        public bool CreateUserBadgeRecord(int userId)
        {
            string query = "INSERT INTO users_badgescur(userid, badge1, badge2, badge3, badge4, badge5) VALUES (@userId, '', '', '', '', '')";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets current badge information for a user.
        /// </summary>
        public UserCurrentBadges GetUserCurrentBadges(int userId)
        {
            string query = "SELECT badge1, badge2, badge3, badge4, badge5 FROM users_badgescur WHERE userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new UserCurrentBadges
            {
                UserId = userId,
                Badge1 = row.ContainsKey("badge1") ? row["badge1"] : string.Empty,
                Badge2 = row.ContainsKey("badge2") ? row["badge2"] : string.Empty,
                Badge3 = row.ContainsKey("badge3") ? row["badge3"] : string.Empty,
                Badge4 = row.ContainsKey("badge4") ? row["badge4"] : string.Empty,
                Badge5 = row.ContainsKey("badge5") ? row["badge5"] : string.Empty
            };
        }
    }

    /// <summary>
    /// Represents badge information.
    /// </summary>
    public class BadgeInfo
    {
        public string Badge { get; set; }
        public int SlotId { get; set; }
    }

    /// <summary>
    /// Represents current badges for a user.
    /// </summary>
    public class UserCurrentBadges
    {
        public int UserId { get; set; }
        public string Badge1 { get; set; }
        public string Badge2 { get; set; }
        public string Badge3 { get; set; }
        public string Badge4 { get; set; }
        public string Badge5 { get; set; }
    }
}
