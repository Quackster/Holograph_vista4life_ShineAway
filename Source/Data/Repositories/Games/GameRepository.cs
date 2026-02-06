using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Games;

/// <summary>
/// Repository for games_lobbies, games_maps, and games_ranks tables.
/// </summary>
public class GameRepository : BaseRepository
{
    private static GameRepository? _instance;
    public static GameRepository Instance => _instance ??= new GameRepository();

    private GameRepository() { }

    #region Game Lobbies
    public bool IsGameLobby(int roomId)
    {
        return Exists(
            "SELECT id FROM games_lobbies WHERE id = @id",
            Param("@id", roomId));
    }

    public string[] GetLobbySettings(int roomId)
    {
        return ReadRow(
            "SELECT type, rank FROM games_lobbies WHERE id = @id",
            Param("@id", roomId));
    }
    #endregion

    #region Game Maps
    public string[] GetMapIds(string gameType)
    {
        return ReadColumn(
            "SELECT id FROM games_maps WHERE type = @type",
            0,
            Param("@type", gameType));
    }

    public string? GetMapName(int mapId)
    {
        return ReadScalar(
            "SELECT name FROM games_maps WHERE id = @id",
            Param("@id", mapId));
    }

    public string? GetMapData(int mapId)
    {
        return ReadScalar(
            "SELECT data FROM games_maps WHERE id = @id",
            Param("@id", mapId));
    }
    #endregion

    #region Game Ranks
    public string[] GetRankTitles(string gameType)
    {
        return ReadColumn(
            "SELECT title FROM games_ranks WHERE type = @type ORDER BY id ASC",
            0,
            Param("@type", gameType));
    }

    public int[] GetRankMinPoints(string gameType)
    {
        return ReadColumnInt(
            "SELECT minpoints FROM games_ranks WHERE type = @type ORDER BY id ASC",
            0,
            Param("@type", gameType));
    }

    public int[] GetRankMaxPoints(string gameType)
    {
        return ReadColumnInt(
            "SELECT maxpoints FROM games_ranks WHERE type = @type ORDER BY id ASC",
            0,
            Param("@type", gameType));
    }
    #endregion
}
