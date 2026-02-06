using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Rooms;

/// <summary>
/// Repository for rooms table.
/// </summary>
public class RoomRepository : BaseRepository
{
    private static RoomRepository? _instance;
    public static RoomRepository Instance => _instance ??= new RoomRepository();

    private RoomRepository() { }

    #region Room - Basic Operations
    public bool RoomExists(int roomId)
    {
        return Exists(
            "SELECT id FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public int GetRoomCount()
    {
        return ReadScalarInt("SELECT COUNT(*) FROM rooms");
    }

    public string? GetRoomModel(int roomId)
    {
        return ReadScalar(
            "SELECT model FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public string? GetRoomName(int roomId)
    {
        return ReadScalarUnsafe(
            "SELECT name FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public string? GetRoomOwner(int roomId)
    {
        return ReadScalarUnsafe(
            "SELECT owner FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }
    #endregion

    #region Room - Details
    public string[] GetRoomDetails(int roomId)
    {
        return ReadRow(
            "SELECT name, owner, description, model, state, superusers, showname, category, visitors_now, visitors_max FROM rooms WHERE id = @id AND NOT(owner IS NULL)",
            Param("@id", roomId));
    }

    public string[] GetRoomByOwner(string owner)
    {
        return ReadRow(
            "SELECT id, name, description, state, showname, visitors_now, visitors_max FROM rooms WHERE owner = @owner",
            Param("@owner", owner));
    }

    public string[] GetRandomGuestRoom()
    {
        return ReadRow("SELECT id, name, owner, description, state, visitors_now, visitors_max FROM rooms WHERE NOT(owner IS NULL) ORDER BY RAND()");
    }
    #endregion

    #region Room - Search
    public string[] SearchRooms(string searchTerm, int maxResults)
    {
        return ReadColumn(
            "SELECT id FROM rooms WHERE NOT(owner IS NULL) AND (owner = @search OR name LIKE @pattern) ORDER BY id ASC",
            maxResults,
            Param("@search", searchTerm),
            Param("@pattern", $"%{searchTerm}%"));
    }

    public string[] GetRoomsByOwner(string owner)
    {
        return ReadColumn(
            "SELECT id FROM rooms WHERE owner = @owner ORDER BY id ASC",
            0,
            Param("@owner", owner));
    }
    #endregion

    #region Room - Create/Update/Delete
    public long CreateRoom(string name, string owner, string model, int state, int showName)
    {
        return ExecuteAndGetLastInsertId(
            "INSERT INTO rooms (name, owner, model, state, showname) VALUES (@name, @owner, @model, @state, @showname)",
            Param("@name", name),
            Param("@owner", owner),
            Param("@model", model),
            Param("@state", state),
            Param("@showname", showName));
    }

    public int GetLastRoomIdByOwner(string owner)
    {
        return ReadScalarInt(
            "SELECT MAX(id) FROM rooms WHERE owner = @owner",
            Param("@owner", owner));
    }

    public void UpdateRoomSettings(int roomId, string owner, string description, int superUsers, int maxVisitors, string password)
    {
        Execute(
            "UPDATE rooms SET description = @desc, superusers = @super, visitors_max = @max, password = @pass WHERE id = @id AND owner = @owner LIMIT 1",
            Param("@id", roomId),
            Param("@owner", owner),
            Param("@desc", description),
            Param("@super", superUsers),
            Param("@max", maxVisitors),
            Param("@pass", password));
    }

    public void UpdateRoomNameAndState(int roomId, string owner, string name, int state, int showName)
    {
        Execute(
            "UPDATE rooms SET name = @name, state = @state, showname = @showname WHERE id = @id AND owner = @owner LIMIT 1",
            Param("@id", roomId),
            Param("@owner", owner),
            Param("@name", name),
            Param("@state", state),
            Param("@showname", showName));
    }

    public void UpdateRoomCategory(int roomId, string owner, int categoryId)
    {
        Execute(
            "UPDATE rooms SET category = @cat WHERE id = @id AND owner = @owner LIMIT 1",
            Param("@id", roomId),
            Param("@owner", owner),
            Param("@cat", categoryId));
    }

    public void DeleteRoom(int roomId)
    {
        Execute(
            "DELETE FROM rooms WHERE id = @id LIMIT 1",
            Param("@id", roomId));
    }

    public bool IsRoomOwner(int roomId, string owner)
    {
        return Exists(
            "SELECT id FROM rooms WHERE id = @id AND owner = @owner",
            Param("@id", roomId),
            Param("@owner", owner));
    }

    public int GetRoomCountByOwner(string owner)
    {
        return ReadScalarInt(
            "SELECT COUNT(id) FROM rooms WHERE owner = @owner",
            Param("@owner", owner));
    }
    #endregion

    #region Room - Visitors
    public void UpdateVisitorCount(int roomId, int count)
    {
        Execute(
            "UPDATE rooms SET visitors_now = @count WHERE id = @id LIMIT 1",
            Param("@id", roomId),
            Param("@count", count));
    }

    public void ResetAllVisitorCounts()
    {
        Execute("UPDATE rooms SET visitors_now = 0");
    }

    public int GetRoomVisitorsNow(int roomId)
    {
        return ReadScalarIntUnsafe(
            "SELECT SUM(visitors_now) FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public int GetRoomVisitorsMax(int roomId)
    {
        return ReadScalarIntUnsafe(
            "SELECT SUM(visitors_max) FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }
    #endregion

    #region Room - Decorations
    public void UpdateWallpaper(int roomId, string wallpaper)
    {
        Execute(
            "UPDATE rooms SET wallpaper = @value WHERE id = @id LIMIT 1",
            Param("@id", roomId),
            Param("@value", wallpaper));
    }

    public void UpdateFloor(int roomId, string floor)
    {
        Execute(
            "UPDATE rooms SET floor = @value WHERE id = @id LIMIT 1",
            Param("@id", roomId),
            Param("@value", floor));
    }

    public void UpdateLandscape(int roomId, string landscape)
    {
        Execute(
            "UPDATE rooms SET landscape = @value WHERE id = @id LIMIT 1",
            Param("@id", roomId),
            Param("@value", landscape));
    }

    public int GetRoomWallpaper(int roomId)
    {
        return ReadScalarInt(
            "SELECT wallpaper FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public int GetRoomFloor(int roomId)
    {
        return ReadScalarInt(
            "SELECT floor FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public string? GetRoomLandscape(int roomId)
    {
        return ReadScalar(
            "SELECT landscape FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }
    #endregion

    #region Room - Password/State
    public string? GetRoomPassword(int roomId)
    {
        return ReadScalar(
            "SELECT password FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public int GetRoomState(int roomId)
    {
        return ReadScalarInt(
            "SELECT state FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public int GetRoomSuperUsers(int roomId)
    {
        return ReadScalarInt(
            "SELECT superusers FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public bool HasSuperUsers(int roomId)
    {
        return Exists(
            "SELECT id FROM rooms WHERE id = @id AND superusers = 1",
            Param("@id", roomId));
    }
    #endregion

    #region Room - Advertisements
    public bool RoomHasAd(int roomId)
    {
        return Exists(
            "SELECT roomid FROM room_ads WHERE roomid = @id",
            Param("@id", roomId));
    }

    public string[] GetRoomAd(int roomId)
    {
        return ReadRow(
            "SELECT img, uri FROM room_ads WHERE roomid = @id",
            Param("@id", roomId));
    }
    #endregion

    #region Room - Navigator Queries
    public int[] GetRoomsByCategory(int categoryId, string orderClause, int limit)
    {
        string sql = $"SELECT id FROM rooms WHERE category = @cat {orderClause}";
        if (limit > 0)
            sql += $" LIMIT {limit}";
        return ReadColumnInt(sql, 0, Param("@cat", categoryId));
    }

    public int[] GetRoomStates(int categoryId, string orderClause)
    {
        return ReadColumnInt(
            $"SELECT state FROM rooms WHERE category = @cat {orderClause}",
            0,
            Param("@cat", categoryId));
    }

    public int[] GetRoomShowNames(int categoryId, string orderClause)
    {
        return ReadColumnInt(
            $"SELECT showname FROM rooms WHERE category = @cat {orderClause}",
            0,
            Param("@cat", categoryId));
    }

    public int[] GetRoomNowVisitors(int categoryId, string orderClause)
    {
        return ReadColumnInt(
            $"SELECT visitors_now FROM rooms WHERE category = @cat {orderClause}",
            0,
            Param("@cat", categoryId));
    }

    public int[] GetRoomMaxVisitors(int categoryId, string orderClause)
    {
        return ReadColumnInt(
            $"SELECT visitors_max FROM rooms WHERE category = @cat {orderClause}",
            0,
            Param("@cat", categoryId));
    }

    public string[] GetRoomNames(int categoryId, string orderClause)
    {
        return ReadColumn(
            $"SELECT name FROM rooms WHERE category = @cat {orderClause}",
            0,
            Param("@cat", categoryId));
    }

    public string[] GetRoomDescriptions(int categoryId, string orderClause)
    {
        return ReadColumn(
            $"SELECT description FROM rooms WHERE category = @cat {orderClause}",
            0,
            Param("@cat", categoryId));
    }

    public string[] GetRoomOwners(int categoryId, string orderClause)
    {
        return ReadColumn(
            $"SELECT owner FROM rooms WHERE category = @cat {orderClause}",
            0,
            Param("@cat", categoryId));
    }

    public string[] GetRoomCcts(int categoryId, string orderClause)
    {
        return ReadColumn(
            $"SELECT ccts FROM rooms WHERE category = @cat {orderClause}",
            0,
            Param("@cat", categoryId));
    }

    public int GetCategoryVisitorSum(int categoryId)
    {
        return ReadScalarIntUnsafe(
            "SELECT SUM(visitors_now) FROM rooms WHERE category = @cat",
            Param("@cat", categoryId));
    }

    public int GetCategoryMaxVisitorSum(int categoryId)
    {
        return ReadScalarIntUnsafe(
            "SELECT SUM(visitors_max) FROM rooms WHERE category = @cat",
            Param("@cat", categoryId));
    }

    public string? GetRoomCct(int roomId)
    {
        return ReadScalar(
            "SELECT ccts FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public int GetRoomCategory(int roomId)
    {
        return ReadScalarInt(
            "SELECT category FROM rooms WHERE id = @id",
            Param("@id", roomId));
    }

    public string? GetRoomCategoryForOwner(int roomId, string owner)
    {
        return ReadScalar(
            "SELECT category FROM rooms WHERE id = @id AND owner = @owner",
            Param("@id", roomId),
            Param("@owner", owner));
    }
    #endregion
}
