using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Bots;

/// <summary>
/// Repository for roombots, roombots_coords, and roombots_texts tables.
/// </summary>
public class BotRepository : BaseRepository
{
    private static BotRepository? _instance;
    public static BotRepository Instance => _instance ??= new BotRepository();

    private BotRepository() { }

    #region Room Bots
    public int[] GetRoomBotIds(int roomId)
    {
        return ReadColumnInt(
            "SELECT id FROM roombots WHERE roomid = @roomid",
            0,
            Param("@roomid", roomId));
    }

    public string[] GetBotData(int botId)
    {
        return ReadRow(
            "SELECT name, mission, figure, x, y, z, freeroam, message_noshouting FROM roombots WHERE id = @id",
            Param("@id", botId));
    }
    #endregion

    #region Bot Coordinates
    public int[] GetBotCoordIds(int botId)
    {
        return ReadColumnInt(
            "SELECT id FROM roombots_coords WHERE id = @id",
            0,
            Param("@id", botId));
    }

    public int[] GetBotXCoords(int botId)
    {
        return ReadColumnInt(
            "SELECT x FROM roombots_coords WHERE id = @id",
            0,
            Param("@id", botId));
    }

    public int[] GetBotYCoords(int botId)
    {
        return ReadColumnInt(
            "SELECT y FROM roombots_coords WHERE id = @id",
            0,
            Param("@id", botId));
    }
    #endregion

    #region Bot Texts
    public string[] GetBotShoutTexts(int botId)
    {
        return ReadColumn(
            "SELECT text FROM roombots_texts WHERE id = @id AND type = 'shout'",
            0,
            Param("@id", botId));
    }

    public string[] GetBotSayTexts(int botId)
    {
        return ReadColumn(
            "SELECT text FROM roombots_texts WHERE id = @id AND type = 'say'",
            0,
            Param("@id", botId));
    }

    public string[] GetBotTriggers(int botId)
    {
        return ReadColumn(
            "SELECT words FROM roombots_texts_triggers WHERE id = @id",
            0,
            Param("@id", botId));
    }

    public string[] GetBotReplies(int botId)
    {
        return ReadColumn(
            "SELECT replies FROM roombots_texts_triggers WHERE id = @id",
            0,
            Param("@id", botId));
    }

    public string[] GetBotServes(int botId)
    {
        return ReadColumn(
            "SELECT serve FROM roombots_texts_triggers WHERE id = @id",
            0,
            Param("@id", botId));
    }
    #endregion
}
