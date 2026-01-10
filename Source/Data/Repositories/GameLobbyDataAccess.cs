using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for game lobby-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class GameLobbyDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Checks if a game lobby exists for a room.
        /// </summary>
        public bool GameLobbyExists(int roomId)
        {
            string query = @"
                SELECT id 
                FROM games_lobbies 
                WHERE id = @roomId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets game lobby settings for a room.
        /// </summary>
        public GameLobbySettings GetGameLobbySettings(int roomId)
        {
            string query = @"
                SELECT 
                    type, 
                    rank 
                FROM games_lobbies 
                WHERE id = @roomId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new GameLobbySettings
            {
                Type = row.ContainsKey("type") ? row["type"] : string.Empty,
                Rank = row.ContainsKey("rank") ? row["rank"] : string.Empty
            };
        }
    }

    /// <summary>
    /// Represents game lobby settings.
    /// </summary>
    public class GameLobbySettings
    {
        public string Type { get; set; }
        public string Rank { get; set; }
    }
}
