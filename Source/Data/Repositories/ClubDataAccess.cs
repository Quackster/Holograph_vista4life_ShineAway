using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for club subscription-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class ClubDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets club subscription details for a user.
        /// </summary>
        public ClubSubscriptionInfo GetClubSubscription(int userId)
        {
            string query = "SELECT months_expired, months_left, date_monthstarted FROM users_club WHERE userid = @userId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new ClubSubscriptionInfo
            {
                UserId = userId,
                MonthsExpired = row.ContainsKey("months_expired") ? int.Parse(row["months_expired"]) : 0,
                MonthsLeft = row.ContainsKey("months_left") ? int.Parse(row["months_left"]) : 0,
                DateMonthStarted = row.ContainsKey("date_monthstarted") ? row["date_monthstarted"] : string.Empty
            };
        }
    }

    /// <summary>
    /// Represents club subscription information.
    /// </summary>
    public class ClubSubscriptionInfo
    {
        public int UserId { get; set; }
        public int MonthsExpired { get; set; }
        public int MonthsLeft { get; set; }
        public string DateMonthStarted { get; set; }
    }
}
