using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Rooms;

/// <summary>
/// Repository for room_categories table.
/// </summary>
public class RoomCategoryRepository : BaseRepository
{
    private static RoomCategoryRepository? _instance;
    public static RoomCategoryRepository Instance => _instance ??= new RoomCategoryRepository();

    private RoomCategoryRepository() { }

    #region Categories - Basic Queries
    public string? GetCategoryName(int categoryId, byte userRank)
    {
        return ReadScalarUnsafe(
            "SELECT name FROM room_categories WHERE id = @id AND (access_rank_min <= @rank OR access_rank_hideforlower = '0')",
            Param("@id", categoryId),
            Param("@rank", userRank));
    }

    public int GetCategoryType(int categoryId)
    {
        return ReadScalarInt(
            "SELECT type FROM room_categories WHERE id = @id",
            Param("@id", categoryId));
    }

    public int GetCategoryParent(int categoryId)
    {
        return ReadScalarInt(
            "SELECT parent FROM room_categories WHERE id = @id",
            Param("@id", categoryId));
    }

    public bool IsTradingCategory(int categoryId)
    {
        return Exists(
            "SELECT id FROM room_categories WHERE id = @id AND trading = '1'",
            Param("@id", categoryId));
    }

    public bool CategoryExistsForRank(int categoryId, byte userRank)
    {
        return Exists(
            "SELECT id FROM room_categories WHERE id = @id AND type = '2' AND parent > 0 AND access_rank_min <= @rank",
            Param("@id", categoryId),
            Param("@rank", userRank));
    }
    #endregion

    #region Categories - Navigator
    public int[] GetSubCategoryIds(int parentId, byte userRank)
    {
        return ReadColumnInt(
            "SELECT id FROM room_categories WHERE parent = @parent AND (access_rank_min <= @rank OR access_rank_hideforlower = '0') ORDER BY id ASC",
            0,
            Param("@parent", parentId),
            Param("@rank", userRank));
    }

    public int[] GetGuestroomCategoryIds(byte userRank)
    {
        return ReadColumnInt(
            "SELECT id FROM room_categories WHERE type = '2' AND parent > 0 AND access_rank_min <= @rank ORDER BY id ASC",
            0,
            Param("@rank", userRank));
    }

    public string[] GetGuestroomCategoryNames(byte userRank)
    {
        return ReadColumn(
            "SELECT name FROM room_categories WHERE type = '2' AND parent > 0 AND access_rank_min <= @rank ORDER BY id ASC",
            0,
            Param("@rank", userRank));
    }
    #endregion
}
