using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Furniture;

/// <summary>
/// Repository for furniture (main table).
/// </summary>
public class FurnitureRepository : BaseRepository
{
    private static FurnitureRepository? _instance;
    public static FurnitureRepository Instance => _instance ??= new FurnitureRepository();

    private FurnitureRepository() { }

    #region Furniture - Basic Operations
    public int GetItemCount()
    {
        return ReadScalarInt("SELECT COUNT(*) FROM furniture");
    }

    public int GetTemplateId(int itemId)
    {
        return ReadScalarInt(
            "SELECT tid FROM furniture WHERE id = @id",
            Param("@id", itemId));
    }

    public int GetOwnerId(int itemId)
    {
        return ReadScalarInt(
            "SELECT ownerid FROM furniture WHERE id = @id",
            Param("@id", itemId));
    }

    public bool IsOwner(int itemId, int userId)
    {
        return Exists(
            "SELECT id FROM furniture WHERE id = @id AND ownerid = @owner",
            Param("@id", itemId),
            Param("@owner", userId));
    }

    public string? GetVar(int itemId)
    {
        return ReadScalar(
            "SELECT var FROM furniture WHERE id = @id",
            Param("@id", itemId));
    }

    public void UpdateVar(int itemId, string var)
    {
        Execute(
            "UPDATE furniture SET var = @var WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@var", var));
    }

    public int GetLastInsertedId()
    {
        return ReadScalarInt("SELECT MAX(id) FROM furniture");
    }
    #endregion

    #region Furniture - Room Items
    public int[] GetRoomItemIds(int roomId)
    {
        return ReadColumnInt(
            "SELECT id FROM furniture WHERE roomid = @roomid ORDER BY h ASC",
            0,
            Param("@roomid", roomId));
    }

    public int[] GetRoomItemTemplateIds(int roomId)
    {
        return ReadColumnInt(
            "SELECT tid FROM furniture WHERE roomid = @roomid ORDER BY h ASC",
            0,
            Param("@roomid", roomId));
    }

    public int[] GetRoomItemXs(int roomId)
    {
        return ReadColumnInt(
            "SELECT x FROM furniture WHERE roomid = @roomid ORDER BY h ASC",
            0,
            Param("@roomid", roomId));
    }

    public int[] GetRoomItemYs(int roomId)
    {
        return ReadColumnInt(
            "SELECT y FROM furniture WHERE roomid = @roomid ORDER BY h ASC",
            0,
            Param("@roomid", roomId));
    }

    public int[] GetRoomItemZs(int roomId)
    {
        return ReadColumnInt(
            "SELECT z FROM furniture WHERE roomid = @roomid ORDER BY h ASC",
            0,
            Param("@roomid", roomId));
    }

    public string[] GetRoomItemHs(int roomId)
    {
        return ReadColumn(
            "SELECT h FROM furniture WHERE roomid = @roomid ORDER BY h ASC",
            0,
            Param("@roomid", roomId));
    }

    public string[] GetRoomItemVars(int roomId)
    {
        return ReadColumn(
            "SELECT var FROM furniture WHERE roomid = @roomid ORDER BY h ASC",
            0,
            Param("@roomid", roomId));
    }

    public string[] GetRoomItemWallPositions(int roomId)
    {
        return ReadColumn(
            "SELECT wallpos FROM furniture WHERE roomid = @roomid ORDER BY h ASC",
            0,
            Param("@roomid", roomId));
    }
    #endregion

    #region Furniture - User Hand
    public int[] GetHandItemIds(int userId)
    {
        return ReadColumnInt(
            "SELECT id FROM furniture WHERE ownerid = @owner AND roomid = 0",
            0,
            Param("@owner", userId));
    }

    public int[] GetHandItemTemplateIds(int userId)
    {
        return ReadColumnInt(
            "SELECT tid FROM furniture WHERE ownerid = @owner AND roomid = 0",
            0,
            Param("@owner", userId));
    }

    public string[] GetHandItemVars(int userId)
    {
        return ReadColumn(
            "SELECT var FROM furniture WHERE ownerid = @owner AND roomid = 0",
            0,
            Param("@owner", userId));
    }
    #endregion

    #region Furniture - Placement/Movement
    public void PlaceFloorItem(int itemId, int roomId, int x, int y, int z, double h)
    {
        Execute(
            "UPDATE furniture SET roomid = @room, x = @x, y = @y, z = @z, h = @h, wallpos = '' WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@room", roomId),
            Param("@x", x),
            Param("@y", y),
            Param("@z", z),
            Param("@h", h));
    }

    public void PlaceWallItem(int itemId, int roomId, string wallPosition)
    {
        Execute(
            "UPDATE furniture SET roomid = @room, wallpos = @wallpos WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@room", roomId),
            Param("@wallpos", wallPosition));
    }

    public void MoveFloorItem(int itemId, int x, int y, int z, double h)
    {
        Execute(
            "UPDATE furniture SET x = @x, y = @y, z = @z, h = @h WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@x", x),
            Param("@y", y),
            Param("@z", z),
            Param("@h", h));
    }

    public void MoveWallItem(int itemId, string wallPosition)
    {
        Execute(
            "UPDATE furniture SET wallpos = @wallpos WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@wallpos", wallPosition));
    }

    public void PickupItem(int itemId)
    {
        Execute(
            "UPDATE furniture SET roomid = 0, x = 0, y = 0, z = 0, h = 0, wallpos = '' WHERE id = @id LIMIT 1",
            Param("@id", itemId));
    }

    public void DeleteRoomItems(int roomId)
    {
        Execute(
            "DELETE FROM furniture WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }
    #endregion

    #region Furniture - Trading/Transfers
    public void TransferItem(int itemId, int newOwnerId)
    {
        Execute(
            "UPDATE furniture SET ownerid = @owner, roomid = 0 WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@owner", newOwnerId));
    }

    public void TransferItemToRoom(int itemId, int newOwnerId, int roomId)
    {
        Execute(
            "UPDATE furniture SET ownerid = @owner, roomid = @room WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@owner", newOwnerId),
            Param("@room", roomId));
    }
    #endregion

    #region Furniture - Create/Delete
    public long CreateItem(int templateId, int ownerId, int roomId = 0)
    {
        return ExecuteAndGetLastInsertId(
            "INSERT INTO furniture (tid, ownerid, roomid) VALUES (@tid, @owner, @room)",
            Param("@tid", templateId),
            Param("@owner", ownerId),
            Param("@room", roomId));
    }

    public void DeleteItem(int itemId)
    {
        Execute(
            "DELETE FROM furniture WHERE id = @id LIMIT 1",
            Param("@id", itemId));
    }
    #endregion

    #region Furniture - Teleporters
    public int GetTeleporterId(int itemId)
    {
        return ReadScalarInt(
            "SELECT teleportid FROM furniture WHERE id = @id",
            Param("@id", itemId));
    }

    public void SetTeleporterId(int itemId, int teleporterId)
    {
        Execute(
            "UPDATE furniture SET teleportid = @tid WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@tid", teleporterId));
    }

    public int GetItemRoomId(int itemId)
    {
        return ReadScalarInt(
            "SELECT roomid FROM furniture WHERE id = @id",
            Param("@id", itemId));
    }
    #endregion

    #region Furniture - Sound Machine
    public int GetSoundMachineSoundSet(int itemId)
    {
        return ReadScalarInt(
            "SELECT soundmachine_soundset FROM furniture WHERE id = @id",
            Param("@id", itemId));
    }

    public void SetSoundMachineSoundSet(int itemId, int soundSet)
    {
        Execute(
            "UPDATE furniture SET soundmachine_soundset = @set WHERE id = @id LIMIT 1",
            Param("@id", itemId),
            Param("@set", soundSet));
    }
    #endregion

    #region Furniture - Wall Position
    public string? GetWallPosition(int itemId)
    {
        return ReadScalar(
            "SELECT wallpos FROM furniture WHERE id = @id",
            Param("@id", itemId));
    }
    #endregion

    #region Furniture - Recycler
    public void MoveItemsToRecycler(int userId)
    {
        Execute(
            "UPDATE furniture SET roomid = -2 WHERE ownerid = @owner AND roomid = 0",
            Param("@owner", userId));
    }

    public void DeleteRecyclerItems(int userId)
    {
        Execute(
            "DELETE FROM furniture WHERE ownerid = @owner AND roomid = -2",
            Param("@owner", userId));
    }

    public void ReturnRecyclerItems(int userId)
    {
        Execute(
            "UPDATE furniture SET roomid = 0 WHERE ownerid = @owner AND roomid = -2",
            Param("@owner", userId));
    }
    #endregion
}
