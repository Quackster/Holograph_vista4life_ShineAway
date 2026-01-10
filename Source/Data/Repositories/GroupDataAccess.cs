using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for group-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class GroupDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the current group ID for a user.
        /// </summary>
        public int GetUserCurrentGroupId(int userId)
        {
            string query = "SELECT groupid FROM groups_memberships WHERE userid = @userId AND is_current = '1' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the member rank for a user in a group.
        /// </summary>
        public int GetUserGroupMemberRank(int userId, int groupId)
        {
            string query = "SELECT member_rank FROM groups_memberships WHERE userid = @userId AND groupid = @groupId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@groupId", groupId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets group details by group ID.
        /// </summary>
        public GroupDetails GetGroupDetails(int groupId)
        {
            string query = "SELECT name, description, roomid FROM groups_details WHERE id = @groupId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@groupId", groupId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new GroupDetails
            {
                GroupId = groupId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Description = row.ContainsKey("description") ? row["description"] : string.Empty,
                RoomId = row.ContainsKey("roomid") ? int.Parse(row["roomid"]) : 0
            };
        }

        /// <summary>
        /// Checks if a group exists.
        /// </summary>
        public bool GroupExists(int groupId)
        {
            string query = "SELECT id FROM groups_details WHERE id = @groupId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@groupId", groupId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets the badge for a group.
        /// </summary>
        public string GetGroupBadge(int groupId)
        {
            string query = @"
                SELECT badge 
                FROM groups_details 
                WHERE id = @groupId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@groupId", groupId)
            };
            
            return ExecuteScalarString(query, parameters);
        }
    }

    /// <summary>
    /// Represents group details.
    /// </summary>
    public class GroupDetails
    {
        public int GroupId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int RoomId { get; set; }
    }
}
