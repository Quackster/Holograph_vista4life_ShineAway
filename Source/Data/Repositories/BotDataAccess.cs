using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for bot-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class BotDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets bot details by bot ID.
        /// </summary>
        public BotDetails GetBotDetails(int botId)
        {
            string query = @"
                SELECT 
                    name, 
                    mission, 
                    figure, 
                    x, 
                    y, 
                    z, 
                    freeroam, 
                    message_noshouting 
                FROM roombots 
                WHERE id = @botId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new BotDetails
            {
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Mission = row.ContainsKey("mission") ? row["mission"] : string.Empty,
                Figure = row.ContainsKey("figure") ? row["figure"] : string.Empty,
                X = row.ContainsKey("x") ? int.Parse(row["x"]) : 0,
                Y = row.ContainsKey("y") ? int.Parse(row["y"]) : 0,
                Z = row.ContainsKey("z") ? byte.Parse(row["z"]) : (byte)0,
                FreeRoam = row.ContainsKey("freeroam") && row["freeroam"] == "1",
                NoShoutingMessage = row.ContainsKey("message_noshouting") ? row["message_noshouting"] : string.Empty
            };
        }

        /// <summary>
        /// Gets all say texts for a bot.
        /// </summary>
        public List<string> GetBotSayTexts(int botId)
        {
            string query = @"
                SELECT text 
                FROM roombots_texts 
                WHERE id = @botId 
                    AND type = 'say'";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all shout texts for a bot.
        /// </summary>
        public List<string> GetBotShoutTexts(int botId)
        {
            string query = @"
                SELECT text 
                FROM roombots_texts 
                WHERE id = @botId 
                    AND type = 'shout'";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all chat trigger words for a bot.
        /// </summary>
        public List<string> GetBotTriggerWords(int botId)
        {
            string query = @"
                SELECT words 
                FROM roombots_texts_triggers 
                WHERE id = @botId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all chat trigger replies for a bot.
        /// </summary>
        public List<string> GetBotTriggerReplies(int botId)
        {
            string query = @"
                SELECT replies 
                FROM roombots_texts_triggers 
                WHERE id = @botId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all chat trigger serve replies for a bot.
        /// </summary>
        public List<string> GetBotTriggerServeReplies(int botId)
        {
            string query = @"
                SELECT serve_replies 
                FROM roombots_texts_triggers 
                WHERE id = @botId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all chat trigger serve items for a bot.
        /// </summary>
        public List<string> GetBotTriggerServeItems(int botId)
        {
            string query = @"
                SELECT serve_item 
                FROM roombots_texts_triggers 
                WHERE id = @botId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all X coordinates for a bot's walk path.
        /// </summary>
        public List<int> GetBotXCoordinates(int botId)
        {
            string query = @"
                SELECT x 
                FROM roombots_coords 
                WHERE id = @botId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all Y coordinates for a bot's walk path.
        /// </summary>
        public List<int> GetBotYCoordinates(int botId)
        {
            string query = @"
                SELECT y 
                FROM roombots_coords 
                WHERE id = @botId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@botId", botId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all bot IDs for a room.
        /// </summary>
        public List<int> GetRoomBotIds(int roomId)
        {
            string query = @"
                SELECT id 
                FROM roombots 
                WHERE roomid = @roomId";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }
    }

    /// <summary>
    /// Represents bot details.
    /// </summary>
    public class BotDetails
    {
        public string Name { get; set; }
        public string Mission { get; set; }
        public string Figure { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public byte Z { get; set; }
        public bool FreeRoam { get; set; }
        public string NoShoutingMessage { get; set; }
    }
}
