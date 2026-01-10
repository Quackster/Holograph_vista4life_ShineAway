using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for catalogue-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class CatalogueDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the page ID by index name and user rank.
        /// </summary>
        public int GetPageIdByIndexName(string indexName, byte userRank)
        {
            string query = "SELECT indexid FROM catalogue_pages WHERE indexname = @indexName AND minrank <= @userRank LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@indexName", indexName),
                new MySqlParameter("@userRank", userRank)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the template ID by item name.
        /// </summary>
        public int GetTemplateIdByItemName(string itemName)
        {
            string query = "SELECT tid FROM catalogue_items WHERE name_cct = @itemName LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemName", itemName)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the catalogue cost for an item.
        /// </summary>
        public int GetCatalogueCost(int pageId, int templateId)
        {
            string query = "SELECT catalogue_cost FROM catalogue_items WHERE catalogue_id_page = @pageId AND tid = @templateId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@pageId", pageId),
                new MySqlParameter("@templateId", templateId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets template IDs from a catalogue deal.
        /// </summary>
        public List<int> GetDealTemplateIds(int dealId)
        {
            string query = "SELECT tid FROM catalogue_deals WHERE id = @dealId";
            var parameters = new[]
            {
                new MySqlParameter("@dealId", dealId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets item amounts from a catalogue deal.
        /// </summary>
        public List<int> GetDealItemAmounts(int dealId)
        {
            string query = "SELECT amount FROM catalogue_deals WHERE id = @dealId";
            var parameters = new[]
            {
                new MySqlParameter("@dealId", dealId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }
    }
}
