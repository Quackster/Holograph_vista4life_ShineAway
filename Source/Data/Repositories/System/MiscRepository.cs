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
}
