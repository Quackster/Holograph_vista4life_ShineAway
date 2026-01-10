using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for room-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class RoomDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets room category information by category ID and user rank.
        /// </summary>
        public RoomCategoryInfo GetRoomCategoryInfo(int categoryId, byte userRank)
        {
            string query = "SELECT name, type, parent FROM room_categories WHERE id = @categoryId AND (access_rank_min <= @userRank OR access_rank_hideforlower = '0') LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId),
                new MySqlParameter("@userRank", userRank)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomCategoryInfo
            {
                CategoryId = categoryId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Type = row.ContainsKey("type") ? int.Parse(row["type"]) : 0,
                ParentId = row.ContainsKey("parent") ? int.Parse(row["parent"]) : 0
            };
        }

        /// <summary>
        /// Gets room IDs in a category with optional ordering.
        /// </summary>
        public List<int> GetRoomIdsByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT id FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets room states for rooms in a category.
        /// </summary>
        public List<int> GetRoomStatesByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT state FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets room names for rooms in a category.
        /// </summary>
        public List<string> GetRoomNamesByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT name FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets room details for a specific room.
        /// </summary>
        public RoomDetails GetRoomDetails(int roomId)
        {
            string query = "SELECT name, owner, description, model, state, superusers, showname, category, visitors_now, visitors_max FROM rooms WHERE id = @roomId AND NOT(owner IS NULL) LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomDetails
            {
                RoomId = roomId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Owner = row.ContainsKey("owner") ? row["owner"] : string.Empty,
                Description = row.ContainsKey("description") ? row["description"] : string.Empty,
                Model = row.ContainsKey("model") ? row["model"] : string.Empty,
                State = row.ContainsKey("state") ? int.Parse(row["state"]) : 0,
                SuperUsers = row.ContainsKey("superusers") ? row["superusers"] : "0",
                ShowName = row.ContainsKey("showname") ? int.Parse(row["showname"]) : 0,
                Category = row.ContainsKey("category") ? int.Parse(row["category"]) : 0,
                VisitorsNow = row.ContainsKey("visitors_now") ? int.Parse(row["visitors_now"]) : 0,
                VisitorsMax = row.ContainsKey("visitors_max") ? int.Parse(row["visitors_max"]) : 0
            };
        }

        /// <summary>
        /// Gets room basic information (name, description, state, showname, visitors).
        /// </summary>
        public RoomBasicInfo GetRoomBasicInfo(int roomId)
        {
            string query = "SELECT name, description, state, showname, visitors_now, visitors_max FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomBasicInfo
            {
                RoomId = roomId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Description = row.ContainsKey("description") ? row["description"] : string.Empty,
                State = row.ContainsKey("state") ? int.Parse(row["state"]) : 0,
                ShowName = row.ContainsKey("showname") ? int.Parse(row["showname"]) : 0,
                VisitorsNow = row.ContainsKey("visitors_now") ? int.Parse(row["visitors_now"]) : 0,
                VisitorsMax = row.ContainsKey("visitors_max") ? int.Parse(row["visitors_max"]) : 0
            };
        }

        /// <summary>
        /// Gets room IDs owned by a user.
        /// </summary>
        public List<int> GetRoomIdsByOwner(string username)
        {
            string query = "SELECT id FROM rooms WHERE owner = @username ORDER BY id ASC";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets room IDs matching a search pattern.
        /// </summary>
        public List<int> SearchRooms(string searchPattern, int maxResults)
        {
            string query = "SELECT id FROM rooms WHERE NOT(owner IS NULL) AND (owner = @searchPattern OR name LIKE @searchPatternLike) ORDER BY id ASC LIMIT @maxResults";
            var parameters = new[]
            {
                new MySqlParameter("@searchPattern", searchPattern),
                new MySqlParameter("@searchPatternLike", $"%{searchPattern}%"),
                new MySqlParameter("@maxResults", maxResults)
            };
            return ExecuteSingleColumnInt(query, maxResults, parameters);
        }

        /// <summary>
        /// Gets room information for search results.
        /// </summary>
        public RoomSearchInfo GetRoomSearchInfo(int roomId)
        {
            string query = "SELECT name, owner, description, state, showname, visitors_now, visitors_max FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomSearchInfo
            {
                RoomId = roomId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Owner = row.ContainsKey("owner") ? row["owner"] : string.Empty,
                Description = row.ContainsKey("description") ? row["description"] : string.Empty,
                State = row.ContainsKey("state") ? int.Parse(row["state"]) : 0,
                ShowName = row.ContainsKey("showname") ? int.Parse(row["showname"]) : 0,
                VisitorsNow = row.ContainsKey("visitors_now") ? int.Parse(row["visitors_now"]) : 0,
                VisitorsMax = row.ContainsKey("visitors_max") ? int.Parse(row["visitors_max"]) : 0
            };
        }

        /// <summary>
        /// Gets a random room that has an owner.
        /// </summary>
        public RoomDetails GetRandomRoomWithOwner()
        {
            string query = "SELECT id, name, owner, description, state, visitors_now, visitors_max FROM rooms WHERE NOT(owner IS NULL) ORDER BY RAND() LIMIT 1";
            var row = ExecuteSingleRow(query);
            
            if (row.Count == 0)
                return null;

            int roomId = row.ContainsKey("id") ? int.Parse(row["id"]) : 0;
            return new RoomDetails
            {
                RoomId = roomId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Owner = row.ContainsKey("owner") ? row["owner"] : string.Empty,
                Description = row.ContainsKey("description") ? row["description"] : string.Empty,
                State = row.ContainsKey("state") ? int.Parse(row["state"]) : 0,
                VisitorsNow = row.ContainsKey("visitors_now") ? int.Parse(row["visitors_now"]) : 0,
                VisitorsMax = row.ContainsKey("visitors_max") ? int.Parse(row["visitors_max"]) : 0
            };
        }

        /// <summary>
        /// Gets subcategory IDs for a parent category.
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
        /// Gets the sum of current visitors for rooms in a category.
        /// </summary>
        public int GetTotalVisitorsNowByCategory(int categoryId)
        {
            string query = "SELECT SUM(visitors_now) FROM rooms WHERE category = @categoryId";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the sum of max visitors for rooms in a category.
        /// </summary>
        public int GetTotalVisitorsMaxByCategory(int categoryId)
        {
            string query = "SELECT SUM(visitors_max) FROM rooms WHERE category = @categoryId";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets category IDs of type 2 (subcategories) accessible to a user rank.
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
        /// Gets category names of type 2 accessible to a user rank.
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
        /// Checks if a room exists.
        /// </summary>
        public bool RoomExists(int roomId)
        {
            string query = "SELECT id FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Checks if a user owns a room.
        /// </summary>
        public bool UserOwnsRoom(int roomId, string username)
        {
            string query = "SELECT id FROM rooms WHERE id = @roomId AND owner = @username LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@username", username)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets room access state.
        /// </summary>
        public int GetRoomState(int roomId)
        {
            string query = "SELECT state FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets room model.
        /// </summary>
        public string GetRoomModel(int roomId)
        {
            string query = "SELECT model FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets room wallpaper.
        /// </summary>
        public int GetRoomWallpaper(int roomId)
        {
            string query = "SELECT wallpaper FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets room floor.
        /// </summary>
        public int GetRoomFloor(int roomId)
        {
            string query = "SELECT floor FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets room landscape.
        /// </summary>
        public string GetRoomLandscape(int roomId)
        {
            string query = "SELECT landscape FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets room password.
        /// </summary>
        public string GetRoomPassword(int roomId)
        {
            string query = "SELECT password FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets room name.
        /// </summary>
        public string GetRoomName(int roomId)
        {
            string query = "SELECT name FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets room category.
        /// </summary>
        public int GetRoomCategory(int roomId)
        {
            string query = "SELECT category FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets room CCTs (custom colors/textures).
        /// </summary>
        public List<string> GetRoomCCTsByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT ccts FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
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
        /// Checks if a category is valid for a user rank.
        /// </summary>
        public bool IsCategoryValidForRank(int categoryId, int categoryType, int parentId, byte userRank)
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

        /// <summary>
        /// Gets current visitors count for a room.
        /// </summary>
        public int GetRoomVisitorsNow(int roomId)
        {
            string query = "SELECT SUM(visitors_now) FROM rooms WHERE id = @roomId";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets max visitors count for a room.
        /// </summary>
        public int GetRoomVisitorsMax(int roomId)
        {
            string query = "SELECT SUM(visitors_max) FROM rooms WHERE id = @roomId";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Creates a new room.
        /// </summary>
        public bool CreateRoom(string roomName, string owner, string model, int state, int showName)
        {
            string query = "INSERT INTO rooms (name, owner, model, state, showname) VALUES (@roomName, @owner, @model, @state, @showName)";
            var parameters = new[]
            {
                new MySqlParameter("@roomName", roomName),
                new MySqlParameter("@owner", owner),
                new MySqlParameter("@model", model),
                new MySqlParameter("@state", state),
                new MySqlParameter("@showName", showName)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates room settings.
        /// </summary>
        public bool UpdateRoomSettings(int roomId, string description, string superUsers, int maxVisitors, string password, string owner)
        {
            string query = "UPDATE rooms SET description = @description, superusers = @superUsers, visitors_max = @maxVisitors, password = @password WHERE id = @roomId AND owner = @owner LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@description", description),
                new MySqlParameter("@superUsers", superUsers),
                new MySqlParameter("@maxVisitors", maxVisitors),
                new MySqlParameter("@password", password),
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@owner", owner)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates room basic information.
        /// </summary>
        public bool UpdateRoomBasicInfo(int roomId, string roomName, int roomState, int showName, string owner)
        {
            string query = "UPDATE rooms SET name = @roomName, state = @roomState, showname = @showName WHERE id = @roomId AND owner = @owner LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomName", roomName),
                new MySqlParameter("@roomState", roomState),
                new MySqlParameter("@showName", showName),
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@owner", owner)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates room category.
        /// </summary>
        public bool UpdateRoomCategory(int roomId, int categoryId, string owner)
        {
            string query = "UPDATE rooms SET category = @categoryId WHERE id = @roomId AND owner = @owner LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId),
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@owner", owner)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes a room and all related data.
        /// </summary>
        public bool DeleteRoom(int roomId, string owner)
        {
            // Delete in order: rights, room, votes, bans, furniture, moodlight
            bool success = true;
            
            success &= ExecuteNonQuery("DELETE FROM room_rights WHERE roomid = @roomId", new MySqlParameter("@roomId", roomId));
            success &= ExecuteNonQuery("DELETE FROM rooms WHERE id = @roomId AND owner = @owner LIMIT 1", 
                new MySqlParameter("@roomId", roomId), new MySqlParameter("@owner", owner));
            success &= ExecuteNonQuery("DELETE FROM room_votes WHERE roomid = @roomId", new MySqlParameter("@roomId", roomId));
            success &= ExecuteNonQuery("DELETE FROM room_bans WHERE roomid = @roomId LIMIT 1", new MySqlParameter("@roomId", roomId));
            success &= ExecuteNonQuery("DELETE FROM furniture WHERE roomid = @roomId", new MySqlParameter("@roomId", roomId));
            success &= ExecuteNonQuery("DELETE FROM furniture_moodlight WHERE roomid = @roomId", new MySqlParameter("@roomId", roomId));
            
            return success;
        }

        /// <summary>
        /// Checks if a room has an advertisement.
        /// </summary>
        public bool RoomHasAdvertisement(int roomId)
        {
            string query = "SELECT roomid FROM room_ads WHERE roomid = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets room advertisement information.
        /// </summary>
        public RoomAdvertisement GetRoomAdvertisement(int roomId)
        {
            string query = "SELECT img, uri FROM room_ads WHERE roomid = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomAdvertisement
            {
                RoomId = roomId,
                ImageUrl = row.ContainsKey("img") ? row["img"] : string.Empty,
                Uri = row.ContainsKey("uri") ? row["uri"] : string.Empty
            };
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
        /// Gets room category by room ID and owner.
        /// </summary>
        public int GetRoomCategoryByOwner(int roomId, string owner)
        {
            string query = "SELECT category FROM rooms WHERE id = @roomId AND owner = @owner LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@owner", owner)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets room details for favorite rooms (name, owner, state, showname, visitors, description).
        /// </summary>
        public RoomFavoriteInfo GetRoomFavoriteInfo(int roomId)
        {
            string query = "SELECT name, owner, state, showname, visitors_now, visitors_max, description FROM rooms WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomFavoriteInfo
            {
                RoomId = roomId,
                Name = row.ContainsKey("name") ? row["name"] : string.Empty,
                Owner = row.ContainsKey("owner") ? row["owner"] : string.Empty,
                State = row.ContainsKey("state") ? int.Parse(row["state"]) : 0,
                ShowName = row.ContainsKey("showname") ? int.Parse(row["showname"]) : 0,
                VisitorsNow = row.ContainsKey("visitors_now") ? int.Parse(row["visitors_now"]) : 0,
                VisitorsMax = row.ContainsKey("visitors_max") ? int.Parse(row["visitors_max"]) : 0,
                Description = row.ContainsKey("description") ? row["description"] : string.Empty
            };
        }

        /// <summary>
        /// Gets room show name flags for rooms in a category.
        /// </summary>
        public List<int> GetRoomShowNameFlagsByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT showname FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets room owners for rooms in a category.
        /// </summary>
        public List<string> GetRoomOwnersByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT owner FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets room descriptions for rooms in a category.
        /// </summary>
        public List<string> GetRoomDescriptionsByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT description FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets current visitors for rooms in a category.
        /// </summary>
        public List<int> GetRoomVisitorsNowByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT visitors_now FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets max visitors for rooms in a category.
        /// </summary>
        public List<int> GetRoomVisitorsMaxByCategory(int categoryId, string orderByClause = "")
        {
            string query = $"SELECT visitors_max FROM rooms WHERE category = @categoryId {orderByClause}";
            var parameters = new[]
            {
                new MySqlParameter("@categoryId", categoryId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Updates room wallpaper.
        /// </summary>
        public bool UpdateRoomWallpaper(int roomId, string wallpaperValue)
        {
            string query = "UPDATE rooms SET wallpaper = @wallpaperValue WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@wallpaperValue", wallpaperValue),
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates room floor.
        /// </summary>
        public bool UpdateRoomFloor(int roomId, string floorValue)
        {
            string query = "UPDATE rooms SET floor = @floorValue WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@floorValue", floorValue),
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates room landscape.
        /// </summary>
        public bool UpdateRoomLandscape(int roomId, string landscapeValue)
        {
            string query = "UPDATE rooms SET landscape = @landscapeValue WHERE id = @roomId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@landscapeValue", landscapeValue),
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the visitor count for a room.
        /// </summary>
        public bool UpdateRoomVisitorCount(int roomId, int visitorCount)
        {
            string query = @"
                UPDATE rooms 
                SET visitors_now = @visitorCount 
                WHERE id = @roomId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@visitorCount", visitorCount),
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }
    }

    /// <summary>
    /// Represents room favorite information.
    /// </summary>
    public class RoomFavoriteInfo
    {
        public int RoomId { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public int State { get; set; }
        public int ShowName { get; set; }
        public int VisitorsNow { get; set; }
        public int VisitorsMax { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Represents room category information.
    /// </summary>
    public class RoomCategoryInfo
    {
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public int Type { get; set; }
        public int ParentId { get; set; }
    }

    /// <summary>
    /// Represents detailed room information.
    /// </summary>
    public class RoomDetails
    {
        public int RoomId { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Description { get; set; }
        public string Model { get; set; }
        public int State { get; set; }
        public string SuperUsers { get; set; }
        public int ShowName { get; set; }
        public int Category { get; set; }
        public int VisitorsNow { get; set; }
        public int VisitorsMax { get; set; }
    }

    /// <summary>
    /// Represents basic room information.
    /// </summary>
    public class RoomBasicInfo
    {
        public int RoomId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int State { get; set; }
        public int ShowName { get; set; }
        public int VisitorsNow { get; set; }
        public int VisitorsMax { get; set; }
    }

    /// <summary>
    /// Represents room search information.
    /// </summary>
    public class RoomSearchInfo
    {
        public int RoomId { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public string Description { get; set; }
        public int State { get; set; }
        public int ShowName { get; set; }
        public int VisitorsNow { get; set; }
        public int VisitorsMax { get; set; }
    }

    /// <summary>
    /// Represents room advertisement information.
    /// </summary>
    public class RoomAdvertisement
    {
        public int RoomId { get; set; }
        public string ImageUrl { get; set; }
        public string Uri { get; set; }
    }
}
