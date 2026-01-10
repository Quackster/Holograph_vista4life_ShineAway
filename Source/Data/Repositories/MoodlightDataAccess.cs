using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for moodlight-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class MoodlightDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets moodlight settings for a room.
        /// </summary>
        public MoodlightSettings GetMoodlightSettings(int roomId)
        {
            string query = @"
                SELECT 
                    preset_cur, 
                    preset_1, 
                    preset_2, 
                    preset_3 
                FROM furniture_moodlight 
                WHERE roomid = @roomId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new MoodlightSettings
            {
                PresetCurrent = row.ContainsKey("preset_cur") ? row["preset_cur"] : string.Empty,
                Preset1 = row.ContainsKey("preset_1") ? row["preset_1"] : string.Empty,
                Preset2 = row.ContainsKey("preset_2") ? row["preset_2"] : string.Empty,
                Preset3 = row.ContainsKey("preset_3") ? row["preset_3"] : string.Empty
            };
        }

        /// <summary>
        /// Gets the moodlight item ID for a room.
        /// </summary>
        public int GetMoodlightItemId(int roomId)
        {
            string query = @"
                SELECT id 
                FROM furniture_moodlight 
                WHERE roomid = @roomId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Updates moodlight preset settings.
        /// </summary>
        public bool UpdateMoodlightPreset(int itemId, int presetId, int presetCurrent, string presetValue)
        {
            string query = $@"
                UPDATE furniture_moodlight 
                SET preset_cur = @presetCurrent, 
                    preset_{presetId} = @presetValue 
                WHERE id = @itemId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@presetCurrent", presetCurrent),
                new MySqlParameter("@presetValue", presetValue ?? string.Empty),
                new MySqlParameter("@itemId", itemId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }
    }

    /// <summary>
    /// Represents moodlight settings.
    /// </summary>
    public class MoodlightSettings
    {
        public string PresetCurrent { get; set; }
        public string Preset1 { get; set; }
        public string Preset2 { get; set; }
        public string Preset3 { get; set; }
    }
}
