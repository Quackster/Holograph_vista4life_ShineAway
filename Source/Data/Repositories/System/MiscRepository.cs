using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.System;

/// <summary>
/// Repository for wordfilter, vouchers, and system_recycler tables.
/// </summary>
public class MiscRepository : BaseRepository
{
    private static MiscRepository? _instance;
    public static MiscRepository Instance => _instance ??= new MiscRepository();

    private MiscRepository() { }

    #region Word Filter
    public string[] GetFilteredWords()
    {
        return ReadColumn("SELECT word FROM wordfilter", 0);
    }

    public void AddFilteredWord(string word)
    {
        Execute(
            "INSERT INTO wordfilter (word) VALUES (@word)",
            Param("@word", word));
    }

    public void RemoveFilteredWord(string word)
    {
        Execute(
            "DELETE FROM wordfilter WHERE word = @word LIMIT 1",
            Param("@word", word));
    }
    #endregion

    #region Vouchers
    public bool VoucherExists(string code)
    {
        return Exists(
            "SELECT code FROM vouchers WHERE code = @code",
            Param("@code", code));
    }

    public int GetVoucherCredits(string code)
    {
        return ReadScalarInt(
            "SELECT credits FROM vouchers WHERE code = @code",
            Param("@code", code));
    }

    public void DeleteVoucher(string code)
    {
        Execute(
            "DELETE FROM vouchers WHERE code = @code LIMIT 1",
            Param("@code", code));
    }

    public void CreateVoucher(string code, int credits)
    {
        Execute(
            "INSERT INTO vouchers (code, credits) VALUES (@code, @credits)",
            Param("@code", code),
            Param("@credits", credits));
    }
    #endregion

    #region Events
    public void CreateEvent(string name, string description, int userId, int roomId, int category)
    {
        Execute(
            "INSERT INTO events (name, description, userid, roomid, category, date) VALUES (@name, @desc, @user, @room, @cat, @date)",
            Param("@name", name),
            Param("@desc", description),
            Param("@user", userId),
            Param("@room", roomId),
            Param("@cat", category),
            Param("@date", DateTime.Now.TimeOfDay.ToString()));
    }

    public void UpdateEvent(int roomId, string name, string description, int category)
    {
        Execute(
            "UPDATE events SET name = @name, description = @desc, category = @cat, date = @date WHERE roomid = @room",
            Param("@name", name),
            Param("@desc", description),
            Param("@cat", category),
            Param("@date", DateTime.Now.TimeOfDay.ToString()),
            Param("@room", roomId));
    }

    public void DeleteEvent(int roomId)
    {
        Execute(
            "DELETE FROM events WHERE roomid = @room",
            Param("@room", roomId));
    }
    #endregion

    #region Tags
    public string[] GetUserTags(int ownerId, int maxTags)
    {
        return ReadColumn(
            "SELECT tag FROM cms_tags WHERE ownerid = @id",
            maxTags,
            Param("@id", ownerId));
    }
    #endregion

    #region Call For Help (cms_help)
    public string[] GetPendingCfhByUsername(string username)
    {
        return ReadRow(
            "SELECT id, date, message, picked_up FROM cms_help WHERE username = @username AND picked_up = '0'",
            Param("@username", username));
    }

    public int GetPendingCfhIdByUsername(string username)
    {
        return ReadScalarInt(
            "SELECT id FROM cms_help WHERE username = @username AND picked_up = '0'",
            Param("@username", username));
    }

    public bool HasPendingCfh(string username)
    {
        return Exists(
            "SELECT id FROM cms_help WHERE username = @username AND picked_up = '0'",
            Param("@username", username));
    }

    public void DeletePendingCfhByUsername(string username)
    {
        Execute(
            "DELETE FROM cms_help WHERE picked_up = '0' AND username = @username LIMIT 1",
            Param("@username", username));
    }

    public void CreateCfh(string username, string ip, string message, int roomId)
    {
        Execute(
            "INSERT INTO cms_help (username, ip, message, date, picked_up, subject, roomid) VALUES (@username, @ip, @message, @date, '0', 'CFH message [hotel]', @roomid)",
            Param("@username", username),
            Param("@ip", ip),
            Param("@message", message),
            Param("@date", DateTime.Now.ToString()),
            Param("@roomid", roomId));
    }

    public string? GetCfhUsername(int cfhId)
    {
        return ReadScalar(
            "SELECT username FROM cms_help WHERE id = @id",
            Param("@id", cfhId));
    }

    public void MarkCfhPickedUp(int cfhId, string pickedUpBy)
    {
        Execute(
            "UPDATE cms_help SET picked_up = @pickedUp WHERE id = @id LIMIT 1",
            Param("@id", cfhId),
            Param("@pickedUp", pickedUpBy));
    }

    public bool CfhExists(int cfhId)
    {
        return Exists(
            "SELECT id FROM cms_help WHERE id = @id",
            Param("@id", cfhId));
    }

    public string[] GetCfhDetails(int cfhId)
    {
        return ReadRow(
            "SELECT username, message, date, picked_up, roomid FROM cms_help WHERE id = @id",
            Param("@id", cfhId));
    }

    public void DeleteCfh(int cfhId)
    {
        Execute(
            "DELETE FROM cms_help WHERE id = @id LIMIT 1",
            Param("@id", cfhId));
    }

    public int GetCfhRoomId(int cfhId)
    {
        return ReadScalarInt(
            "SELECT roomid FROM cms_help WHERE id = @id",
            Param("@id", cfhId));
    }
    #endregion
}
