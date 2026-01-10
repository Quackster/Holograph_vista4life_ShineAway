using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for room model-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class RoomModelDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets the room model for a room.
        /// </summary>
        public string GetRoomModel(int roomId)
        {
            string query = @"
                SELECT model 
                FROM rooms 
                WHERE id = @roomId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@roomId", roomId)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets door coordinates for a room model.
        /// </summary>
        public RoomModelDoorData GetRoomModelDoorData(string model)
        {
            string query = @"
                SELECT 
                    door_x, 
                    door_y, 
                    door_h, 
                    door_z 
                FROM room_modeldata 
                WHERE model = @model 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@model", model)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomModelDoorData
            {
                DoorX = row.ContainsKey("door_x") ? row["door_x"] : string.Empty,
                DoorY = row.ContainsKey("door_y") ? row["door_y"] : string.Empty,
                DoorH = row.ContainsKey("door_h") ? row["door_h"] : string.Empty,
                DoorZ = row.ContainsKey("door_z") ? row["door_z"] : string.Empty
            };
        }

        /// <summary>
        /// Gets the heightmap for a room model.
        /// </summary>
        public string GetRoomModelHeightmap(string model)
        {
            string query = @"
                SELECT heightmap 
                FROM room_modeldata 
                WHERE model = @model 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@model", model)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets public room items for a room model.
        /// </summary>
        public string GetPublicRoomItems(string model)
        {
            string query = @"
                SELECT publicroom_items 
                FROM room_modeldata 
                WHERE model = @model 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@model", model)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets all trigger IDs for a room model.
        /// </summary>
        public List<int> GetRoomModelTriggerIds(string model)
        {
            string query = @"
                SELECT id 
                FROM room_modeldata_triggers 
                WHERE model = @model";
            
            var parameters = new[]
            {
                new MySqlParameter("@model", model)
            };
            
            return ExecuteSingleColumnInt(query, null, parameters);
        }

        /// <summary>
        /// Gets trigger object name by trigger ID.
        /// </summary>
        public string GetTriggerObject(int triggerId)
        {
            string query = @"
                SELECT object 
                FROM room_modeldata_triggers 
                WHERE id = @triggerId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@triggerId", triggerId)
            };
            
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets trigger data by trigger ID.
        /// </summary>
        public RoomModelTriggerData GetTriggerData(int triggerId)
        {
            string query = @"
                SELECT 
                    x, 
                    y, 
                    goalx, 
                    goaly, 
                    stepx, 
                    stepy, 
                    roomid, 
                    state 
                FROM room_modeldata_triggers 
                WHERE id = @triggerId 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@triggerId", triggerId)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomModelTriggerData
            {
                X = row.ContainsKey("x") ? int.Parse(row["x"]) : 0,
                Y = row.ContainsKey("y") ? int.Parse(row["y"]) : 0,
                GoalX = row.ContainsKey("goalx") ? int.Parse(row["goalx"]) : 0,
                GoalY = row.ContainsKey("goaly") ? int.Parse(row["goaly"]) : 0,
                StepX = row.ContainsKey("stepx") ? int.Parse(row["stepx"]) : 0,
                StepY = row.ContainsKey("stepy") ? int.Parse(row["stepy"]) : 0,
                RoomId = row.ContainsKey("roomid") ? int.Parse(row["roomid"]) : 0,
                State = row.ContainsKey("state") ? int.Parse(row["state"]) : 0
            };
        }

        /// <summary>
        /// Checks if a room model has special cast interval.
        /// </summary>
        public bool HasSpecialCastInterval(string model)
        {
            string query = @"
                SELECT specialcast_interval 
                FROM room_modeldata 
                WHERE model = @model 
                    AND specialcast_interval > 0 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@model", model)
            };
            
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Checks if a room model has a swimming pool.
        /// </summary>
        public bool HasSwimmingPool(string model)
        {
            string query = @"
                SELECT swimmingpool 
                FROM room_modeldata 
                WHERE model = @model 
                    AND swimmingpool = '1' 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@model", model)
            };
            
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets special cast data for a room model.
        /// </summary>
        public RoomModelSpecialCastData GetSpecialCastData(string model)
        {
            string query = @"
                SELECT 
                    specialcast_emitter, 
                    specialcast_interval, 
                    specialcast_rnd_min, 
                    specialcast_rnd_max 
                FROM room_modeldata 
                WHERE model = @model 
                LIMIT 1";
            
            var parameters = new[]
            {
                new MySqlParameter("@model", model)
            };
            
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new RoomModelSpecialCastData
            {
                Emitter = row.ContainsKey("specialcast_emitter") ? row["specialcast_emitter"] : string.Empty,
                Interval = row.ContainsKey("specialcast_interval") ? int.Parse(row["specialcast_interval"]) : 0,
                RndMin = row.ContainsKey("specialcast_rnd_min") ? int.Parse(row["specialcast_rnd_min"]) : 0,
                RndMax = row.ContainsKey("specialcast_rnd_max") ? int.Parse(row["specialcast_rnd_max"]) : 0
            };
        }
    }

    /// <summary>
    /// Represents room model door data.
    /// </summary>
    public class RoomModelDoorData
    {
        public string DoorX { get; set; }
        public string DoorY { get; set; }
        public string DoorH { get; set; }
        public string DoorZ { get; set; }
    }

    /// <summary>
    /// Represents room model trigger data.
    /// </summary>
    public class RoomModelTriggerData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int GoalX { get; set; }
        public int GoalY { get; set; }
        public int StepX { get; set; }
        public int StepY { get; set; }
        public int RoomId { get; set; }
        public int State { get; set; }
    }

    /// <summary>
    /// Represents room model special cast data.
    /// </summary>
    public class RoomModelSpecialCastData
    {
        public string Emitter { get; set; }
        public int Interval { get; set; }
        public int RndMin { get; set; }
        public int RndMax { get; set; }
    }
}
