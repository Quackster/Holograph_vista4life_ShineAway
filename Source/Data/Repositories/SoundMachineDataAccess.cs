using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for sound machine-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class SoundMachineDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Checks if a song exists for a user and machine.
        /// </summary>
        public bool SongExists(int songId, int userId, int machineId)
        {
            string query = "SELECT id FROM soundmachine_songs WHERE id = @songId AND userid = @userId AND machineid = @machineId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId),
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@machineId", machineId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets song details by song ID.
        /// </summary>
        public SongDetails GetSongDetails(int songId)
        {
            string query = "SELECT title, length FROM soundmachine_songs WHERE id = @songId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new SongDetails
            {
                SongId = songId,
                Title = row.ContainsKey("title") ? row["title"] : string.Empty,
                Length = row.ContainsKey("length") ? int.Parse(row["length"]) : 0
            };
        }

        /// <summary>
        /// Gets the length of a song.
        /// </summary>
        public int GetSongLength(int songId)
        {
            string query = "SELECT length FROM soundmachine_songs WHERE id = @songId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Checks if a song exists for a machine.
        /// </summary>
        public bool SongExistsForMachine(int songId, int machineId)
        {
            string query = "SELECT id FROM soundmachine_songs WHERE id = @songId AND machineid = @machineId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId),
                new MySqlParameter("@machineId", machineId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Marks a song as burnt.
        /// </summary>
        public bool MarkSongAsBurnt(int songId)
        {
            string query = "UPDATE soundmachine_songs SET burnt = '1' WHERE id = @songId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Removes a song from a machine (sets machineid to 0) if the song is burnt.
        /// </summary>
        public bool RemoveSongFromMachineIfBurnt(int songId)
        {
            string query = "UPDATE soundmachine_songs SET machineid = '0' WHERE id = @songId AND burnt = '1' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes a song that is not burnt.
        /// </summary>
        public bool DeleteUnburntSong(int songId)
        {
            string query = "DELETE FROM soundmachine_songs WHERE id = @songId AND burnt = '0' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes a song from a playlist.
        /// </summary>
        public bool DeleteSongFromPlaylist(int machineId, int songId)
        {
            string query = "DELETE FROM soundmachine_playlists WHERE machineid = @machineId AND songid = @songId";
            var parameters = new[]
            {
                new MySqlParameter("@machineId", machineId),
                new MySqlParameter("@songId", songId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes all playlists for a machine.
        /// </summary>
        public bool DeleteAllPlaylistsForMachine(int machineId)
        {
            string query = "DELETE FROM soundmachine_playlists WHERE machineid = @machineId";
            var parameters = new[]
            {
                new MySqlParameter("@machineId", machineId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Adds a song to a playlist.
        /// </summary>
        public bool AddSongToPlaylist(int machineId, int songId, int position)
        {
            string query = "INSERT INTO soundmachine_playlists(machineid, songid, pos) VALUES (@machineId, @songId, @position)";
            var parameters = new[]
            {
                new MySqlParameter("@machineId", machineId),
                new MySqlParameter("@songId", songId),
                new MySqlParameter("@position", position)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Creates a new song.
        /// </summary>
        public bool CreateSong(int userId, int machineId, string title, int length, string data)
        {
            string query = "INSERT INTO soundmachine_songs (userid, machineid, title, length, data) VALUES (@userId, @machineId, @title, @length, @data)";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId),
                new MySqlParameter("@machineId", machineId),
                new MySqlParameter("@title", title),
                new MySqlParameter("@length", length),
                new MySqlParameter("@data", data)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates an existing song.
        /// </summary>
        public bool UpdateSong(int songId, string title, string data, int length)
        {
            string query = "UPDATE soundmachine_songs SET title = @title, data = @data, length = @length WHERE id = @songId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@title", title),
                new MySqlParameter("@data", data),
                new MySqlParameter("@length", length),
                new MySqlParameter("@songId", songId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets all sound machine item IDs for a user's inventory.
        /// </summary>
        public List<int> GetUserSoundMachineItemIds(int userId)
        {
            string query = @"
                SELECT id 
                FROM furniture 
                WHERE ownerid = @userId 
                    AND roomid = '0' 
                    AND soundmachine_soundset > 0 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all sound machine soundset IDs for a user's inventory.
        /// </summary>
        public List<int> GetUserSoundMachineSoundsets(int userId)
        {
            string query = @"
                SELECT soundmachine_soundset 
                FROM furniture 
                WHERE ownerid = @userId 
                    AND roomid = '0' 
                    AND soundmachine_soundset > 0 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all song IDs for a machine.
        /// </summary>
        public List<int> GetMachineSongIds(int machineId)
        {
            string query = @"
                SELECT id 
                FROM soundmachine_songs 
                WHERE machineid = @machineId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@machineId", machineId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all song lengths for a machine.
        /// </summary>
        public List<int> GetMachineSongLengths(int machineId)
        {
            string query = @"
                SELECT length 
                FROM soundmachine_songs 
                WHERE machineid = @machineId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@machineId", machineId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all song titles for a machine.
        /// </summary>
        public List<string> GetMachineSongTitles(int machineId)
        {
            string query = @"
                SELECT title 
                FROM soundmachine_songs 
                WHERE machineid = @machineId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@machineId", machineId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all burnt flags for songs in a machine.
        /// </summary>
        public List<string> GetMachineSongBurntFlags(int machineId)
        {
            string query = @"
                SELECT burnt 
                FROM soundmachine_songs 
                WHERE machineid = @machineId 
                ORDER BY id ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@machineId", machineId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all song IDs in a machine's playlist.
        /// </summary>
        public List<int> GetMachinePlaylistSongIds(int machineId)
        {
            string query = @"
                SELECT songid 
                FROM soundmachine_playlists 
                WHERE machineid = @machineId 
                ORDER BY pos ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@machineId", machineId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets song title by song ID.
        /// </summary>
        public string GetSongTitle(int songId)
        {
            string query = @"
                SELECT title 
                FROM soundmachine_songs 
                WHERE id = @songId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets user ID who created a song.
        /// </summary>
        public int GetSongUserId(int songId)
        {
            string query = @"
                SELECT userid 
                FROM soundmachine_songs 
                WHERE id = @songId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId)
            };
            
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets song data (title and data) by song ID.
        /// </summary>
        public SongData GetSongData(int songId)
        {
            string query = @"
                SELECT 
                    title, 
                    data 
                FROM soundmachine_songs 
                WHERE id = @songId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@songId", songId)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new SongData
            {
                Title = row.ContainsKey("title") ? row["title"] : string.Empty,
                Data = row.ContainsKey("data") ? row["data"] : string.Empty
            };
        }
    }

    /// <summary>
    /// Represents song details.
    /// </summary>
    public class SongDetails
    {
        public int SongId { get; set; }
        public string Title { get; set; }
        public int Length { get; set; }
    }

    /// <summary>
    /// Represents song data (title and data).
    /// </summary>
    public class SongData
    {
        public string Title { get; set; }
        public string Data { get; set; }
    }
}
