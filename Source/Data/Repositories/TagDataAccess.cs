using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for tag-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class TagDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets tags for a user by owner ID.
        /// </summary>
        public List<string> GetUserTags(int ownerId, int maxResults = 20)
        {
            string query = "SELECT tag FROM cms_tags WHERE ownerid = @ownerId LIMIT @maxResults";
            var parameters = new[]
            {
                new MySqlParameter("@ownerId", ownerId),
                new MySqlParameter("@maxResults", maxResults)
            };
            return ExecuteSingleColumnString(query, maxResults, parameters);
        }
    }
}
