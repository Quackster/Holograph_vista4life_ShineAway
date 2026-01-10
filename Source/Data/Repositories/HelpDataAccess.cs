using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for Call For Help (CFH) related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class HelpDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Gets pending CFH statistics for a user.
        /// </summary>
        public HelpRequestInfo GetPendingHelpRequest(string username)
        {
            string query = "SELECT id, date, message, picked_up FROM cms_help WHERE username = @username AND picked_up = '0' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new HelpRequestInfo
            {
                Id = row.ContainsKey("id") ? int.Parse(row["id"]) : 0,
                Date = row.ContainsKey("date") ? row["date"] : string.Empty,
                Message = row.ContainsKey("message") ? row["message"] : string.Empty,
                PickedUp = row.ContainsKey("picked_up") ? row["picked_up"] : "0"
            };
        }

        /// <summary>
        /// Gets the ID of a pending help request for a user.
        /// </summary>
        public int GetPendingHelpRequestId(string username)
        {
            string query = "SELECT id FROM cms_help WHERE username = @username AND picked_up = '0' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Checks if a user has a pending help request.
        /// </summary>
        public bool HasPendingHelpRequest(string username)
        {
            string query = "SELECT id FROM cms_help WHERE username = @username AND picked_up = '0' LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Deletes a pending help request for a user.
        /// </summary>
        public bool DeletePendingHelpRequest(string username)
        {
            string query = "DELETE FROM cms_help WHERE picked_up = '0' AND username = @username LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@username", username)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Creates a new help request.
        /// </summary>
        public bool CreateHelpRequest(string username, string ipAddress, string message, string date, int roomId)
        {
            string query = "INSERT INTO cms_help (username, ip, message, date, picked_up, subject, roomid) VALUES (@username, @ipAddress, @message, @date, '0', 'CFH message [hotel]', @roomId)";
            var parameters = new[]
            {
                new MySqlParameter("@username", username),
                new MySqlParameter("@ipAddress", ipAddress),
                new MySqlParameter("@message", message),
                new MySqlParameter("@date", date),
                new MySqlParameter("@roomId", roomId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets help request details by ID.
        /// </summary>
        public HelpRequestDetails GetHelpRequestDetails(int helpRequestId)
        {
            string query = "SELECT username, message, date, picked_up, roomid FROM cms_help WHERE id = @helpRequestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@helpRequestId", helpRequestId)
            };
            var row = ExecuteSingleRow(query, parameters);
            
            if (row.Count == 0)
                return null;

            return new HelpRequestDetails
            {
                Id = helpRequestId,
                Username = row.ContainsKey("username") ? row["username"] : string.Empty,
                Message = row.ContainsKey("message") ? row["message"] : string.Empty,
                Date = row.ContainsKey("date") ? row["date"] : string.Empty,
                PickedUp = row.ContainsKey("picked_up") ? row["picked_up"] : "0",
                RoomId = row.ContainsKey("roomid") ? int.Parse(row["roomid"]) : 0
            };
        }

        /// <summary>
        /// Gets the username from a help request.
        /// </summary>
        public string GetHelpRequestUsername(int helpRequestId)
        {
            string query = "SELECT username FROM cms_help WHERE id = @helpRequestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@helpRequestId", helpRequestId)
            };
            return ExecuteScalarString(query, parameters);
        }

        /// <summary>
        /// Gets the room ID from a help request.
        /// </summary>
        public int GetHelpRequestRoomId(int helpRequestId)
        {
            string query = "SELECT roomid FROM cms_help WHERE id = @helpRequestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@helpRequestId", helpRequestId)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Updates a help request to mark it as picked up.
        /// </summary>
        public bool PickupHelpRequest(int helpRequestId, string pickedUpBy)
        {
            string query = "UPDATE cms_help SET picked_up = @pickedUpBy WHERE id = @helpRequestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@pickedUpBy", pickedUpBy),
                new MySqlParameter("@helpRequestId", helpRequestId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Marks a help request as picked up (using '1' as value).
        /// </summary>
        public bool MarkHelpRequestAsPickedUp(int helpRequestId)
        {
            string query = "UPDATE cms_help SET picked_up = '1' WHERE id = @helpRequestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@helpRequestId", helpRequestId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes a help request.
        /// </summary>
        public bool DeleteHelpRequest(int helpRequestId)
        {
            string query = "DELETE FROM cms_help WHERE id = @helpRequestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@helpRequestId", helpRequestId)
            };
            return ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Checks if a help request exists.
        /// </summary>
        public bool HelpRequestExists(int helpRequestId)
        {
            string query = "SELECT id FROM cms_help WHERE id = @helpRequestId LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@helpRequestId", helpRequestId)
            };
            return RecordExists(query, parameters);
        }
    }

    /// <summary>
    /// Represents help request information.
    /// </summary>
    public class HelpRequestInfo
    {
        public int Id { get; set; }
        public string Date { get; set; }
        public string Message { get; set; }
        public string PickedUp { get; set; }
    }

    /// <summary>
    /// Represents detailed help request information.
    /// </summary>
    public class HelpRequestDetails
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Message { get; set; }
        public string Date { get; set; }
        public string PickedUp { get; set; }
        public int RoomId { get; set; }
    }
}
