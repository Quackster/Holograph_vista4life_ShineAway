using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for furniture-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class FurnitureDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets furniture item IDs owned by a user that are not placed in a room (in inventory).
        /// </summary>
        public List<int> GetInventoryItemIds(int userId)
        {
            string query = "SELECT id FROM furniture WHERE ownerid = @userId AND roomid = '0' ORDER BY id ASC";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets the template ID of a furniture item.
        /// </summary>
        public int GetFurnitureTemplateId(int itemId)
        {
            string query = "SELECT tid FROM furniture WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the template ID of a furniture item owned by a user that is in inventory.
        /// </summary>
        public int GetInventoryItemTemplateId(int itemId, int userId)
        {
            string query = "SELECT tid FROM furniture WHERE id = @itemId AND ownerid = @userId AND roomid = '0' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId),
                new MySqlParameter("@userId", userId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the variable value of a furniture item.
        /// </summary>
        public string GetFurnitureVariable(int itemId)
        {
            string query = "SELECT var FROM furniture WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets the teleporter ID linked to a furniture item.
        /// </summary>
        public int GetFurnitureTeleporterId(int itemId)
        {
            string query = "SELECT teleportid FROM furniture WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets the room ID where a furniture item is placed.
        /// </summary>
        public int GetFurnitureRoomId(int itemId)
        {
            string query = "SELECT roomid FROM furniture WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Gets item IDs from a furniture present.
        /// </summary>
        public List<int> GetPresentItemIds(int presentId)
        {
            string query = "SELECT itemid FROM furniture_presents WHERE id = @presentId";
            var parameters = new[]
            {
                new MySqlParameter("@presentId", presentId)
            };
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets the text from a sticky note.
        /// </summary>
        public string GetStickyNoteText(int itemId)
        {
            string query = "SELECT text FROM furniture_stickies WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Checks if a user has any furniture items in inventory.
        /// </summary>
        public bool UserHasInventoryItems(int userId)
        {
            string query = "SELECT id FROM furniture WHERE ownerid = @userId AND roomid = '0' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Creates a new furniture item.
        /// </summary>
        public bool CreateFurnitureItem(int templateId, int ownerId, int? roomId = null, string variable = null)
        {
            string query;
            MySqlParameter[] parameters;

            if (roomId.HasValue && variable != null)
            {
                query = "INSERT INTO furniture(tid, ownerid, roomid, var) VALUES (@templateId, @ownerId, @roomId, @variable)";
                parameters = new[]
                {
                    new MySqlParameter("@templateId", templateId),
                    new MySqlParameter("@ownerId", ownerId),
                    new MySqlParameter("@roomId", roomId.Value),
                    new MySqlParameter("@variable", variable ?? string.Empty)
                };
            }
            else if (roomId.HasValue)
            {
                query = "INSERT INTO furniture(tid, ownerid, roomid) VALUES (@templateId, @ownerId, @roomId)";
                parameters = new[]
                {
                    new MySqlParameter("@templateId", templateId),
                    new MySqlParameter("@ownerId", ownerId),
                    new MySqlParameter("@roomId", roomId.Value)
                };
            }
            else if (variable != null)
            {
                query = "INSERT INTO furniture(tid, ownerid, var) VALUES (@templateId, @ownerId, @variable)";
                parameters = new[]
                {
                    new MySqlParameter("@templateId", templateId),
                    new MySqlParameter("@ownerId", ownerId),
                    new MySqlParameter("@variable", variable)
                };
            }
            else
            {
                query = "INSERT INTO furniture(tid, ownerid) VALUES (@templateId, @ownerId)";
                parameters = new[]
                {
                    new MySqlParameter("@templateId", templateId),
                    new MySqlParameter("@ownerId", ownerId)
                };
            }

            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the variable value of a furniture item.
        /// </summary>
        public bool UpdateFurnitureVariable(int itemId, string variable)
        {
            string query = "UPDATE furniture SET var = @variable WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@variable", variable ?? string.Empty),
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the variable value by decrementing it by 1.
        /// </summary>
        public bool DecrementFurnitureVariable(int itemId)
        {
            string query = "UPDATE furniture SET var = var - 1 WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the room ID of a furniture item.
        /// </summary>
        public bool UpdateFurnitureRoomId(int itemId, int roomId)
        {
            string query = "UPDATE furniture SET roomid = @roomId WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the owner and room ID of a furniture item.
        /// </summary>
        public bool UpdateFurnitureOwnerAndRoom(int itemId, int newOwnerId, int roomId)
        {
            string query = "UPDATE furniture SET ownerid = @newOwnerId, roomid = @roomId WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@newOwnerId", newOwnerId),
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the owner of a furniture item and sets room ID to 0 (inventory).
        /// </summary>
        public bool UpdateFurnitureOwnerToInventory(int itemId, int newOwnerId)
        {
            string query = "UPDATE furniture SET ownerid = @newOwnerId, roomid = '0' WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@newOwnerId", newOwnerId),
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes a furniture item.
        /// </summary>
        public bool DeleteFurnitureItem(int itemId)
        {
            string query = "DELETE FROM furniture WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes all furniture items in a room.
        /// </summary>
        public bool DeleteFurnitureByRoomId(int roomId)
        {
            string query = "DELETE FROM furniture WHERE roomid = @roomId";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes all furniture items in a user's inventory.
        /// </summary>
        public bool DeleteUserInventoryFurniture(int userId)
        {
            string query = "DELETE FROM furniture WHERE ownerid = @userId AND roomid = '0'";
            var parameters = new[]
            {
                new MySqlParameter("@userId", userId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Creates a sticky note entry.
        /// </summary>
        public bool CreateStickyNote(int itemId)
        {
            string query = "INSERT INTO furniture_stickies(id) VALUES (@itemId)";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the text of a sticky note.
        /// </summary>
        public bool UpdateStickyNoteText(int itemId, string text)
        {
            string query = "UPDATE furniture_stickies SET text = @text WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@text", text ?? string.Empty),
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes a sticky note entry.
        /// </summary>
        public bool DeleteStickyNote(int itemId)
        {
            string query = "DELETE FROM furniture_stickies WHERE id = @itemId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes furniture present entries.
        /// </summary>
        public bool DeleteFurniturePresent(int presentId, int itemCount)
        {
            string query = "DELETE FROM furniture_presents WHERE id = @presentId LIMIT @itemCount";
            var parameters = new[]
            {
                new MySqlParameter("@presentId", presentId),
                new MySqlParameter("@itemCount", itemCount)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates furniture items from a present to be in inventory (roomid = 0).
        /// </summary>
        public bool UpdatePresentItemsToInventory(List<int> itemIds)
        {
            if (itemIds == null || itemIds.Count == 0)
                return true;

            bool success = true;
            foreach (var itemId in itemIds)
            {
                success &= UpdateFurnitureRoomId(itemId, 0);
            }
            return success;
        }

        /// <summary>
        /// Deletes furniture moodlight entries for a room.
        /// </summary>
        public bool DeleteFurnitureMoodlight(int roomId)
        {
            string query = "DELETE FROM furniture_moodlight WHERE roomid = @roomId";
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets all furniture item IDs in a room.
        /// </summary>
        public List<int> GetRoomFurnitureItemIds(int roomId)
        {
            string query = @"
                SELECT id 
                FROM furniture 
                WHERE roomid = @roomId 
                ORDER BY h ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all template IDs for furniture in a room.
        /// </summary>
        public List<int> GetRoomFurnitureTemplateIds(int roomId)
        {
            string query = @"
                SELECT tid 
                FROM furniture 
                WHERE roomid = @roomId 
                ORDER BY h ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all X coordinates for furniture in a room.
        /// </summary>
        public List<int> GetRoomFurnitureXCoordinates(int roomId)
        {
            string query = @"
                SELECT x 
                FROM furniture 
                WHERE roomid = @roomId 
                ORDER BY h ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all Y coordinates for furniture in a room.
        /// </summary>
        public List<int> GetRoomFurnitureYCoordinates(int roomId)
        {
            string query = @"
                SELECT y 
                FROM furniture 
                WHERE roomid = @roomId 
                ORDER BY h ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all Z coordinates for furniture in a room.
        /// </summary>
        public List<int> GetRoomFurnitureZCoordinates(int roomId)
        {
            string query = @"
                SELECT z 
                FROM furniture 
                WHERE roomid = @roomId 
                ORDER BY h ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets all H (height) values for furniture in a room.
        /// </summary>
        public List<string> GetRoomFurnitureHeights(int roomId)
        {
            string query = @"
                SELECT h 
                FROM furniture 
                WHERE roomid = @roomId 
                ORDER BY h ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all variable values for furniture in a room.
        /// </summary>
        public List<string> GetRoomFurnitureVariables(int roomId)
        {
            string query = @"
                SELECT var 
                FROM furniture 
                WHERE roomid = @roomId 
                ORDER BY h ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Gets all wall positions for furniture in a room.
        /// </summary>
        public List<string> GetRoomFurnitureWallPositions(int roomId)
        {
            string query = @"
                SELECT wallpos 
                FROM furniture 
                WHERE roomid = @roomId 
                ORDER BY h ASC";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteSingleColumnString(query, null, parameters);
        }

        /// <summary>
        /// Updates furniture position and placement.
        /// </summary>
        public bool UpdateFurniturePosition(int itemId, int roomId, int x, int y, int z, double h)
        {
            string query = @"
                UPDATE furniture 
                SET roomid = @roomId, 
                    x = @x, 
                    y = @y, 
                    z = @z, 
                    h = @h 
                WHERE id = @itemId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@x", x),
                new MySqlParameter("@y", y),
                new MySqlParameter("@z", z),
                new MySqlParameter("@h", h.ToString().Replace(',', '.')),
                new MySqlParameter("@itemId", itemId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates furniture position only.
        /// </summary>
        public bool UpdateFurniturePositionOnly(int itemId, int x, int y, int z, double h)
        {
            string query = @"
                UPDATE furniture 
                SET x = @x, 
                    y = @y, 
                    z = @z, 
                    h = @h 
                WHERE id = @itemId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@x", x),
                new MySqlParameter("@y", y),
                new MySqlParameter("@z", z),
                new MySqlParameter("@h", h.ToString().Replace(',', '.')),
                new MySqlParameter("@itemId", itemId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates furniture to inventory (removes from room).
        /// </summary>
        public bool UpdateFurnitureToInventory(int itemId, int ownerId)
        {
            string query = @"
                UPDATE furniture 
                SET x = '0', 
                    y = '0', 
                    z = '0', 
                    h = '0', 
                    ownerid = @ownerId, 
                    roomid = '0' 
                WHERE id = @itemId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@ownerId", ownerId),
                new MySqlParameter("@itemId", itemId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates wall item position.
        /// </summary>
        public bool UpdateWallItemPosition(int itemId, int roomId, string wallPosition)
        {
            string query = @"
                UPDATE furniture 
                SET roomid = @roomId, 
                    wallpos = @wallPosition 
                WHERE id = @itemId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@wallPosition", wallPosition ?? string.Empty),
                new MySqlParameter("@itemId", itemId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Checks if a moodlight item exists.
        /// </summary>
        public bool MoodlightItemExists(int itemId)
        {
            string query = @"
                SELECT id 
                FROM furniture_moodlight 
                WHERE id = @itemId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Updates moodlight room ID.
        /// </summary>
        public bool UpdateMoodlightRoomId(int itemId, int roomId)
        {
            string query = @"
                UPDATE furniture_moodlight 
                SET roomid = @roomId 
                WHERE id = @itemId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId),
                new MySqlParameter("@itemId", itemId)
            };
            
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets wall position for a furniture item.
        /// </summary>
        public string GetFurnitureWallPosition(int itemId)
        {
            string query = @"
                SELECT wallpos 
                FROM furniture 
                WHERE id = @itemId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@itemId", itemId)
            };
            
            return ExecuteScalarString(query, parameters);
        }
    }
}
