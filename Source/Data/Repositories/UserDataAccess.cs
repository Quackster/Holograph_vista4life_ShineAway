using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for user-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class UserDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the user ID by SSO ticket.
        /// </summary>
        public int GetUserIdBySsoTicket(string ssoTicket)
        {
            string query = "SELECT id FROM users WHERE ticket_sso = @ssoTicket LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@ssoTicket", ssoTicket)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets basic user information by user ID.
        /// </summary>
        public UserBasicInfo GetUserBasicInfo(int userId)
        {
            string query = "SELECT name, figure, sex, mission, rank, consolemission FROM users WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new UserBasicInfo
            {
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Figure = row.ContainsKey("figure") ? row["figure"] : string.Empty,
                Sex = row.ContainsKey("sex") && row["sex"].Length > 0 ? row["sex"][0] : 'M',
                Mission = row.ContainsKey("mission") ? row["mission"] : string.Empty,
                Rank = row.ContainsKey("rank") ? byte.Parse(row["rank"]) : (byte)0,
                ConsoleMission = row.ContainsKey("consolemission") ? row["consolemission"] : string.Empty
            };
        }

        /// <summary>
        /// Gets the user ID by username.
        /// </summary>
        public int GetUserIdByUsername(string username)
        {
            string query = "SELECT id FROM users WHERE name = @username LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets whether the user is a guide.
        /// </summary>
        public bool IsUserGuide(int userId)
        {
            string query = "SELECT guide FROM users WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteScalarInt(query, parameters) == 1;
        }

        /// <summary>
        /// Gets the list of user IDs that this user has ignored.
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
        /// Sets the guide availability status for a user.
        /// </summary>
        public bool SetGuideAvailability(int userId, bool isAvailable)
        {
            string query = "UPDATE users SET guideavailable = @isAvailable WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@isAvailable", isAvailable ? 1 : 0),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the user's credits.
        /// </summary>
        public bool UpdateUserCredits(int userId, int credits)
        {
            string query = "UPDATE users SET credits = @credits WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@credits", credits),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Adds tickets to a user's account.
        /// </summary>
        public bool AddUserTickets(int userId, int ticketAmount)
        {
            string query = "UPDATE users SET tickets = tickets + @ticketAmount WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@ticketAmount", ticketAmount),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Searches for users by name pattern.
        /// </summary>
        public List<UserSearchResult> SearchUsersByName(string searchPattern, int maxResults = 50)
        {
            string query = "SELECT id FROM users WHERE name LIKE @searchPattern LIMIT @maxResults";
            var parameters = new[]
            {
                new MySqlParameter("@searchPattern", $"%{searchPattern}%"),
                new MySqlParameter("@maxResults", maxResults)
            };
            var userIds = ExecuteSingleColumnInt(query, maxResults, parameters);
            
            var results = new List<UserSearchResult>();
            foreach (var userId in userIds)
            {
                var userInfo = GetUserSearchInfo(userId);
                if (userInfo != null)
                {
                    results.Add(userInfo);
                }
            }
            return results;
        }

        /// <summary>
        /// Gets user search information by user ID.
        /// </summary>
        private UserSearchResult GetUserSearchInfo(int userId)
        {
            string query = "SELECT name, mission, lastvisit, figure FROM users WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new UserSearchResult
            {
                UserId = userId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Mission = row.ContainsKey("mission") ? row["mission"] : string.Empty,
                LastVisit = row.ContainsKey("lastvisit") ? row["lastvisit"] : string.Empty,
                Figure = row.ContainsKey("figure") ? row["figure"] : string.Empty
            };
        }

        /// <summary>
        /// Gets user details for moderation purposes (ID, rank, last IP address).
        /// </summary>
        public UserModerationInfo GetUserModerationInfo(string username)
        {
            string query = "SELECT id, rank, ipaddress_last FROM users WHERE name = @username LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new UserModerationInfo
            {
                UserId = row.ContainsKey("id") ? int.Parse(row["id"]) : 0,
                Rank = row.ContainsKey("rank") ? byte.Parse(row["rank"]) : (byte)0,
                LastIpAddress = row.ContainsKey("ipaddress_last") ? row["ipaddress_last"] : string.Empty
            };
        }

        /// <summary>
        /// Gets the count of rooms owned by a user.
        /// </summary>
        public int GetUserRoomCount(string username)
        {
            string query = "SELECT COUNT(id) FROM rooms WHERE owner = @username";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the maximum room ID owned by a user.
        /// </summary>
        public int GetMaxRoomIdByOwner(string username)
        {
            string query = "SELECT MAX(id) FROM rooms WHERE owner = @username";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets user appearance information (figure, sex, mission).
        /// </summary>
        public UserAppearanceInfo GetUserAppearance(int userId)
        {
            string query = "SELECT figure, sex, mission FROM users WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new UserAppearanceInfo
            {
                UserId = userId,
                Figure = row.ContainsKey("figure") ? row["figure"] : string.Empty,
                Sex = row.ContainsKey("sex") && row["sex"].Length > 0 ? row["sex"][0] : 'M',
                Mission = row.ContainsKey("mission") ? row["mission"] : string.Empty
            };
        }

        /// <summary>
        /// Gets the user's current credits.
        /// </summary>
        public int GetUserCredits(int userId)
        {
            string query = "SELECT credits FROM users WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the user's current tickets.
        /// </summary>
        public int GetUserTickets(int userId)
        {
            string query = "SELECT tickets FROM users WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Deducts credits from a user's account.
        /// </summary>
        public bool DeductUserCredits(int userId, int creditAmount)
        {
            string query = "UPDATE users SET credits = credits - @creditAmount WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@creditAmount", creditAmount),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the user's swim outfit.
        /// </summary>
        public bool UpdateUserSwimOutfit(int userId, string swimOutfit)
        {
            string query = "UPDATE users SET figure_swim = @swimOutfit WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@swimOutfit", swimOutfit ?? string.Empty),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets user details for moderation including ID, rank, and last IP address.
        /// </summary>
        public UserModerationDetails GetUserModerationDetails(string username)
        {
            string query = "SELECT id, rank, ipaddress_last FROM users WHERE name = @username LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new UserModerationDetails
            {
                UserId = row.ContainsKey("id") ? int.Parse(row["id"]) : 0,
                Rank = row.ContainsKey("rank") ? byte.Parse(row["rank"]) : (byte)0,
                LastIpAddress = row.ContainsKey("ipaddress_last") ? row["ipaddress_last"] : string.Empty
            };
        }

        /// <summary>
        /// Gets user details for moderation by user ID.
        /// </summary>
        public UserModerationDetails GetUserModerationDetailsById(int userId)
        {
            string query = "SELECT id, rank, ipaddress_last FROM users WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new UserModerationDetails
            {
                UserId = row.ContainsKey("id") ? int.Parse(row["id"]) : 0,
                Rank = row.ContainsKey("rank") ? byte.Parse(row["rank"]) : (byte)0,
                LastIpAddress = row.ContainsKey("ipaddress_last") ? row["ipaddress_last"] : string.Empty
            };
        }

        /// <summary>
        /// Gets the user's swim outfit figure.
        /// </summary>
        public string GetUserSwimOutfit(int userId)
        {
            string query = @"
                SELECT figure_swim 
                FROM users 
                WHERE id = @userId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets the user's game points for a specific game type.
        /// </summary>
        public int GetUserGamePoints(int userId, string gameType)
        {
            // Note: gameType should be validated to prevent SQL injection
            // Since we're using it as a column name, we need to validate it
            string[] validGameTypes = { "bb", "snowwar" }; // Add all valid game types
            if (Array.IndexOf(validGameTypes, gameType) == -1)
                return 0;

            string query = $"SELECT {gameType}_totalpoints FROM users WHERE id = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets username by user ID.
        /// </summary>
        public string GetUsername(int userId)
        {
            string query = @"
                SELECT name 
                FROM users 
                WHERE id = @userId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Deducts tickets from a user's account.
        /// </summary>
        public bool DeductUserTickets(int userId, int ticketAmount)
        {
            string query = @"
                UPDATE users 
                SET tickets = tickets - @ticketAmount 
                WHERE id = @userId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@ticketAmount", ticketAmount),
                new MySqlParameter("@userId", userId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }
    }

    /// <summary>
    /// Represents basic user information.
    /// </summary>
    public class UserBasicInfo
    {
        public string Name { get; set; }
        public string Figure { get; set; }
        public char Sex { get; set; }
        public string Mission { get; set; }
        public byte Rank { get; set; }
        public string ConsoleMission { get; set; }
    }

    /// <summary>
    /// Represents user search result information.
    /// </summary>
    public class UserSearchResult
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Mission { get; set; }
        public string LastVisit { get; set; }
        public string Figure { get; set; }
    }

    /// <summary>
    /// Represents user information for moderation purposes.
    /// </summary>
    public class UserModerationInfo
    {
        public int UserId { get; set; }
        public byte Rank { get; set; }
        public string LastIpAddress { get; set; }
    }

    /// <summary>
    /// Represents user appearance information.
    /// </summary>
    public class UserAppearanceInfo
    {
        public int UserId { get; set; }
        public string Figure { get; set; }
        public char Sex { get; set; }
        public string Mission { get; set; }
    }

    /// <summary>
    /// Represents user details for moderation purposes.
    /// </summary>
    public class UserModerationDetails
    {
        public int UserId { get; set; }
        public byte Rank { get; set; }
        public string LastIpAddress { get; set; }
    }
}
