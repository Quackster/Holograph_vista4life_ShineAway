using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Furniture;

/// <summary>
/// Repository for furniture_moodlight, furniture_presents, and furniture_stickies tables.
/// </summary>
public class FurnitureExtrasRepository : BaseRepository
{
    private static FurnitureExtrasRepository? _instance;
    public static FurnitureExtrasRepository Instance => _instance ??= new FurnitureExtrasRepository();

    private FurnitureExtrasRepository() { }

    #region Moodlight
    public string[] GetMoodlightSettings(int roomId)
    {
        return ReadRow(
            "SELECT preset_cur, preset_1, preset_2, preset_3 FROM furniture_moodlight WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }

    public int GetMoodlightItemId(int roomId)
    {
        return ReadScalarInt(
            "SELECT id FROM furniture_moodlight WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }

    public void UpdateMoodlightPreset(int itemId, int presetId, string presetValue)
    {
        Execute(
            $"UPDATE furniture_moodlight SET preset_cur = @preset, preset_{presetId} = @value WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@preset", presetId),
            Param("@value", presetValue));
    }

    public void CreateMoodlight(int itemId, int roomId, string defaultPreset, string defaultSetPreset)
    {
        Execute(
            "INSERT INTO furniture_moodlight (id, roomid, preset_cur, preset_1, preset_2, preset_3) VALUES (@id, @room, '1', @preset, @preset, @preset)",
            Param("@id", itemId),
            Param("@room", roomId),
            Param("@preset", defaultPreset));
    }

    public void DeleteMoodlight(int roomId)
    {
        Execute(
            "DELETE FROM furniture_moodlight WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }

    public bool MoodlightExists(int itemId)
    {
        return Exists(
            "SELECT id FROM furniture_moodlight WHERE id = @id",
            Param("@id", itemId));
    }

    public void UpdateMoodlightRoomId(int itemId, int roomId)
    {
        Execute(
            "UPDATE furniture_moodlight SET roomid = @roomid WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@roomid", roomId));
    }
    #endregion

    #region Presents
    public int[] GetPresentItems(int presentId)
    {
        return ReadColumnInt(
            "SELECT itemid FROM furniture_presents WHERE id = @id",
            0,
            Param("@id", presentId));
    }

    public void AddPresentItem(int presentId, int itemId)
    {
        Execute(
            "INSERT INTO furniture_presents (id, itemid) VALUES (@id, @itemid)",
            Param("@id", presentId),
            Param("@itemid", itemId));
    }

    public void DeletePresent(int presentId)
    {
        Execute(
            "DELETE FROM furniture_presents WHERE id = @id",
            Param("@id", presentId));
    }
    #endregion

    #region Stickies (Post-its)
    public string? GetStickyText(int itemId)
    {
        return ReadScalar(
            "SELECT text FROM furniture_stickies WHERE id = @id",
            Param("@id", itemId));
    }

    public void CreateSticky(int itemId)
    {
        Execute(
            "INSERT INTO furniture_stickies (id) VALUES (@id)",
            Param("@id", itemId));
    }

    public void UpdateStickyText(int itemId, string text)
    {
        if (Exists("SELECT id FROM furniture_stickies WHERE id = @id", Param("@id", itemId)))
        {
            Execute(
                "UPDATE furniture_stickies SET text = @text WHERE id = @id LIMIT 1",
                Param("@id", itemId),
                Param("@text", text));
        }
        else
        {
            Execute(
                "INSERT INTO furniture_stickies (id, text) VALUES (@id, @text)",
                Param("@id", itemId),
                Param("@text", text));
        }
    }

    public void DeleteSticky(int itemId)
    {
        Execute(
            "DELETE FROM furniture_stickies WHERE id = @id LIMIT 1",
            Param("@id", itemId));
    }
    #endregion
}
