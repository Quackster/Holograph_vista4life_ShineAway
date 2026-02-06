using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Groups;

/// <summary>
/// Repository for groups_details table.
/// </summary>
public class GroupRepository : BaseRepository
{
    private static GroupRepository? _instance;
    public static GroupRepository Instance => _instance ??= new GroupRepository();

    private GroupRepository() { }

    #region Group - Basic Operations
    public bool GroupExists(int groupId)
    {
        return Exists(
            "SELECT id FROM groups_details WHERE id = @id",
            Param("@id", groupId));
    }

    public string? GetGroupName(int groupId)
    {
        return ReadScalar(
            "SELECT name FROM groups_details WHERE id = @id",
            Param("@id", groupId));
    }

    public string? GetGroupDescription(int groupId)
    {
        return ReadScalar(
            "SELECT description FROM groups_details WHERE id = @id",
            Param("@id", groupId));
    }

    public int GetGroupRoomId(int groupId)
    {
        return ReadScalarIntUnsafe(
            "SELECT roomid FROM groups_details WHERE id = @id",
            Param("@id", groupId));
    }

    public string? GetGroupBadge(int groupId)
    {
        return ReadScalar(
            "SELECT badge FROM groups_details WHERE id = @id",
            Param("@id", groupId));
    }
    #endregion

    #region Group - Details
    public string[] GetGroupDetails(int groupId)
    {
        return ReadRow(
            "SELECT name, description, roomid FROM groups_details WHERE id = @id",
            Param("@id", groupId));
    }
    #endregion
}
