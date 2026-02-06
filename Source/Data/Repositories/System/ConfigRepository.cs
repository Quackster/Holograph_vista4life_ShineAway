using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.System;

/// <summary>
/// Repository for system_config, system_fuserights, and ranks tables.
/// </summary>
public class ConfigRepository : BaseRepository
{
    private static ConfigRepository? _instance;
    public static ConfigRepository Instance => _instance ??= new ConfigRepository();

    private ConfigRepository() { }

    #region system_config
    public string? GetConfigValue(string key)
    {
        return ReadScalar(
            "SELECT sval FROM system_config WHERE skey = @key",
            Param("@key", key));
    }

    public void SetConfigValue(string key, string value)
    {
        Execute(
            "UPDATE system_config SET sval = @value WHERE skey = @key LIMIT 1",
            Param("@key", key),
            Param("@value", value));
    }
    #endregion

    #region system_fuserights
    public string[] GetFuseRightsForRank(byte rankId)
    {
        return ReadColumn(
            "SELECT fuseright FROM system_fuserights WHERE minrank <= @rank",
            0,
            Param("@rank", rankId));
    }
    #endregion

    #region ranks
    public string[] GetRankNames()
    {
        return ReadColumn("SELECT name FROM ranks ORDER BY id ASC", 0);
    }

    public int GetRankIdByName(string name)
    {
        return ReadScalarInt(
            "SELECT id FROM ranks WHERE name = @name",
            Param("@name", name));
    }
    #endregion

    #region system (stats table)
    public void UpdateSystemStats(int onlineCount, int peakOnlineCount, int roomCount, int peakRoomCount, int acceptedConnections)
    {
        Execute(
            "UPDATE system SET onlinecount = @online, onlinecount_peak = @peakOnline, activerooms = @rooms, activerooms_peak = @peakRooms, connections_accepted = @connections",
            Param("@online", onlineCount),
            Param("@peakOnline", peakOnlineCount),
            Param("@rooms", roomCount),
            Param("@peakRooms", peakRoomCount),
            Param("@connections", acceptedConnections));
    }

    public void ResetSystemStats()
    {
        Execute("UPDATE system SET onlinecount = 0, onlinecount_peak = 0, connections_accepted = 0, activerooms = 0");
    }
    #endregion

    #region system_strings
    public string[] GetStringKeys()
    {
        return ReadColumn("SELECT stringid FROM system_strings ORDER BY id ASC", 0);
    }

    public string[] GetStringValues(string langExt)
    {
        return ReadColumn($"SELECT var_{langExt} FROM system_strings ORDER BY id ASC", 0);
    }
    #endregion

    #region games_ranks
    public string[] GetGameRankTitles(string gameType)
    {
        return ReadColumn(
            "SELECT title FROM games_ranks WHERE type = @type ORDER BY id ASC",
            0,
            Param("@type", gameType));
    }

    public int[] GetGameRankMinPoints(string gameType)
    {
        return ReadColumnInt(
            "SELECT minpoints FROM games_ranks WHERE type = @type ORDER BY id ASC",
            0,
            Param("@type", gameType));
    }

    public int[] GetGameRankMaxPoints(string gameType)
    {
        return ReadColumnInt(
            "SELECT maxpoints FROM games_ranks WHERE type = @type ORDER BY id ASC",
            0,
            Param("@type", gameType));
    }
    #endregion

    #region system_recycler
    public int[] GetRecyclerCosts()
    {
        return ReadColumnInt("SELECT rclr_cost FROM system_recycler", 0);
    }

    public int[] GetRecyclerRewards()
    {
        return ReadColumnInt("SELECT rclr_reward FROM system_recycler", 0);
    }
    #endregion
}
