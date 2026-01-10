using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for room category-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class RoomCategoryDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the name of a room category by category ID and user rank.
        /// </summary>
        public string GetCategoryName(int categoryId, byte userRank)
        {
            string query = "SELECT name FROM room_categories WHERE id = @categoryId AND (access_rank_min <= @userRank OR access_rank_hideforlower = '0') LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId),
                new MySqlParameter("@userRank", userRank)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets the type of a room category.
        /// </summary>
        public int GetCategoryType(int categoryId)
        {
            string query = "SELECT type FROM room_categories WHERE id = @categoryId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the parent category ID of a room category.
        /// </summary>
        public int GetCategoryParentId(int categoryId)
        {
            string query = "SELECT parent FROM room_categories WHERE id = @categoryId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets subcategory IDs for a parent category that are accessible to a user rank.
        /// </summary>
        public List<int> GetSubCategoryIds(int parentCategoryId, byte userRank)
        {
            string query = "SELECT id FROM room_categories WHERE parent = @parentCategoryId AND (access_rank_min <= @userRank OR access_rank_hideforlower = '0') ORDER BY id ASC";
            var parameters = new[]
            {
                new MySqlParameter("@parentCategoryId", parentCategoryId),
                new MySqlParameter("@userRank", userRank)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets the name of a subcategory by its ID.
        /// </summary>
        public string GetSubCategoryName(int subCategoryId)
        {
            string query = "SELECT name FROM room_categories WHERE id = @subCategoryId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@subCategoryId", subCategoryId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets category IDs of a specific type that are accessible to a user rank.
        /// </summary>
        public List<int> GetCategoryIdsByTypeAndRank(int categoryType, byte userRank)
        {
            string query = "SELECT id FROM room_categories WHERE type = @categoryType AND parent > 0 AND access_rank_min <= @userRank ORDER BY id ASC";
            var parameters = new[]
            {
                new MySqlParameter("@categoryType", categoryType),
                new MySqlParameter("@userRank", userRank)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets category names of a specific type that are accessible to a user rank.
        /// </summary>
        public List<string> GetCategoryNamesByTypeAndRank(int categoryType, byte userRank)
        {
            string query = "SELECT name FROM room_categories WHERE type = @categoryType AND parent > 0 AND access_rank_min <= @userRank ORDER BY id ASC";
            var parameters = new[]
            {
                new MySqlParameter("@categoryType", categoryType),
                new MySqlParameter("@userRank", userRank)
            };
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Checks if a category allows trading.
        /// </summary>
        public bool CategoryAllowsTrading(int categoryId)
        {
            string query = "SELECT id FROM room_categories WHERE id = @categoryId AND trading = '1' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Checks if a category is valid for a user rank (type 2, has parent, accessible by rank).
        /// </summary>
        public bool IsCategoryValidForRank(int categoryId, int categoryType, byte userRank)
        {
            string query = "SELECT id FROM room_categories WHERE id = @categoryId AND type = @categoryType AND parent > 0 AND access_rank_min <= @userRank LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId),
                new MySqlParameter("@categoryType", categoryType),
                new MySqlParameter("@userRank", userRank)
            };
            return RecordExists(query, parameters);
        }
    }
}
