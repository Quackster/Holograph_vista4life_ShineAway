using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Rooms;

/// <summary>
/// Repository for room_modeldata and room_modeldata_triggers tables.
/// </summary>
public class RoomModelRepository : BaseRepository
{
    private static RoomModelRepository? _instance;
    public static RoomModelRepository Instance => _instance ??= new RoomModelRepository();

    private RoomModelRepository() { }

    #region Model Data
    public int GetDoorX(string model)
    {
        return ReadScalarInt(
            "SELECT door_x FROM room_modeldata WHERE model = @model",
            Param("@model", model));
    }

    public int GetDoorY(string model)
    {
        return ReadScalarInt(
            "SELECT door_y FROM room_modeldata WHERE model = @model",
            Param("@model", model));
    }

    public int GetDoorH(string model)
    {
        return ReadScalarInt(
            "SELECT door_h FROM room_modeldata WHERE model = @model",
            Param("@model", model));
    }

    public string? GetDoorZ(string model)
    {
        return ReadScalar(
            "SELECT door_z FROM room_modeldata WHERE model = @model",
            Param("@model", model));
    }

    public string? GetHeightmap(string model)
    {
        return ReadScalar(
            "SELECT heightmap FROM room_modeldata WHERE model = @model",
            Param("@model", model));
    }

    public string? GetPublicroomItems(string model)
    {
        return ReadScalar(
            "SELECT publicroom_items FROM room_modeldata WHERE model = @model",
            Param("@model", model));
    }

    public bool HasSwimmingPool(string model)
    {
        return Exists(
            "SELECT swimmingpool FROM room_modeldata WHERE model = @model AND swimmingpool = '1'",
            Param("@model", model));
    }

    public bool HasSpecialCast(string model)
    {
        return Exists(
            "SELECT specialcast_interval FROM room_modeldata WHERE model = @model AND specialcast_interval > 0",
            Param("@model", model));
    }

    public int GetSpecialCastInterval(string model)
    {
        return ReadScalarInt(
            "SELECT specialcast_interval FROM room_modeldata WHERE model = @model",
            Param("@model", model));
    }

    public string? GetSpecialCastEmitter(string model)
    {
        return ReadScalar(
            "SELECT specialcast_emitter FROM room_modeldata WHERE model = @model",
            Param("@model", model));
    }

    public string[] GetSpecialCastData(string model)
    {
        return ReadColumn(
            "SELECT specialcast_data FROM room_modeldata WHERE model = @model",
            0,
            Param("@model", model));
    }
    #endregion

    #region Model Triggers
    public int[] GetTriggerIds(string model)
    {
        return ReadColumnInt(
            "SELECT id FROM room_modeldata_triggers WHERE model = @model",
            0,
            Param("@model", model));
    }

    public string? GetTriggerObject(int triggerId)
    {
        return ReadScalar(
            "SELECT object FROM room_modeldata_triggers WHERE id = @id",
            Param("@id", triggerId));
    }

    public int[] GetTriggerData(int triggerId)
    {
        return ReadRowInt(
            "SELECT x, y, goalx, goaly, stepx, stepy, roomid, state FROM room_modeldata_triggers WHERE id = @id",
            Param("@id", triggerId));
    }
    #endregion
}
