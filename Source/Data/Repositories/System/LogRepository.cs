using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.System;

/// <summary>
/// Repository for system_chatlog and system_stafflog tables.
/// </summary>
public class LogRepository : BaseRepository
{
    private static LogRepository? _instance;
    public static LogRepository Instance => _instance ??= new LogRepository();

    private LogRepository() { }

    #region Chat Log
    public void AddChatMessage(string username, int roomId, string message)
    {
        Execute(
            "INSERT INTO system_chatlog (username, roomid, mtime, message) VALUES (@user, @room, CURRENT_TIMESTAMP, @msg)",
            Param("@user", username),
            Param("@room", roomId),
            Param("@msg", message));
    }

    public string[] GetChatHistory(int roomId, int limit)
    {
        return ReadColumn(
            "SELECT CONCAT(username, ': ', message) FROM system_chatlog WHERE roomid = @room ORDER BY id DESC",
            limit,
            Param("@room", roomId));
    }

    public string[] GetUserChatHistory(string username, int limit)
    {
        return ReadColumn(
            "SELECT CONCAT('[', mtime, '] ', message) FROM system_chatlog WHERE username = @user ORDER BY id DESC",
            limit,
            Param("@user", username));
    }
    #endregion

    #region Staff Log
    public void AddStaffLog(int userId, string action, int targetId, string note)
    {
        Execute(
            "INSERT INTO system_stafflog (userid, action, targetid, note, timestamp) VALUES (@user, @action, @target, @note, @time)",
            Param("@user", userId),
            Param("@action", action),
            Param("@target", targetId),
            Param("@note", note),
            Param("@time", DateTime.Now.ToString()));
    }

    public string[] GetStaffLogEntry(string action, int targetId)
    {
        return ReadRow(
            "SELECT userid, note, timestamp FROM system_stafflog WHERE action = @action AND targetid = @target ORDER BY id DESC",
            Param("@action", action),
            Param("@target", targetId));
    }
    #endregion
}
