using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for game-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class GameDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the heightmap for a game map.
        /// </summary>
        public string GetGameMapHeightmap(string gameType, int mapId)
        {
            string query = @"
                SELECT heightmap 
                FROM games_maps 
                WHERE type = @gameType 
                    AND id = @mapId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@gameType", gameType),
                new MySqlParameter("@mapId", mapId)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets the battle ball tilemap for a game map.
        /// </summary>
        public string GetBattleBallTilemap(int mapId)
        {
            string query = @"
                SELECT bb_tilemap 
                FROM games_maps 
                WHERE type = 'bb' 
                    AND id = @mapId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@mapId", mapId)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets player spawn data for a game map and team.
        /// </summary>
        public PlayerSpawnData GetPlayerSpawnData(string gameType, int mapId, int teamId)
        {
            string query = @"
                SELECT 
                    x, 
                    y, 
                    z 
                FROM games_maps_playerspawns 
                WHERE type = @gameType 
                    AND mapid = @mapId 
                    AND teamid = @teamId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@gameType", gameType),
                new MySqlParameter("@mapId", mapId),
                new MySqlParameter("@teamId", teamId)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new PlayerSpawnData
            {
                X = row.ContainsKey("x") ? int.Parse(row["x"]) : 0,
                Y = row.ContainsKey("y") ? int.Parse(row["y"]) : 0,
                Z = row.ContainsKey("z") ? byte.Parse(row["z"]) : (byte)0
            };
        }

        /// <summary>
        /// Updates a user's tickets.
        /// </summary>
        public bool UpdateUserTickets(int userId, int tickets)
        {
            string query = @"
                UPDATE users 
                SET tickets = @tickets 
                WHERE id = @userId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@tickets", tickets),
                new MySqlParameter("@userId", userId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates a user's battle ball statistics.
        /// </summary>
        public bool UpdateUserBattleBallStats(int userId, int score)
        {
            string query = @"
                UPDATE users 
                SET bb_playedgames = bb_playedgames + 1, 
                    bb_totalpoints = bb_totalpoints + @score 
                WHERE id = @userId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@score", score),
                new MySqlParameter("@userId", userId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }
    }

    /// <summary>
    /// Represents player spawn data.
    /// </summary>
    public class PlayerSpawnData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public byte Z { get; set; }
    }
}
