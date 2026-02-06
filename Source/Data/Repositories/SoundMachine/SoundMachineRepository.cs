using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.SoundMachine;

/// <summary>
/// Repository for soundmachine_songs and soundmachine_playlists tables.
/// </summary>
public class SoundMachineRepository : BaseRepository
{
    private static SoundMachineRepository? _instance;
    public static SoundMachineRepository Instance => _instance ??= new SoundMachineRepository();

    private SoundMachineRepository() { }

    #region Songs
    public int[] GetSongIds(int machineId)
    {
        return ReadColumnInt(
            "SELECT id FROM soundmachine_songs WHERE machineid = @id",
            0,
            Param("@id", machineId));
    }

    public string[] GetSongData(int songId)
    {
        return ReadRow(
            "SELECT title, length, data FROM soundmachine_songs WHERE id = @id",
            Param("@id", songId));
    }

    public int GetSongOwner(int songId)
    {
        return ReadScalarInt(
            "SELECT userid FROM soundmachine_songs WHERE id = @id",
            Param("@id", songId));
    }

    public void CreateSong(int userId, int machineId, string title, int length, string data)
    {
        Execute(
            "INSERT INTO soundmachine_songs (userid, machineid, title, length, data) VALUES (@user, @machine, @title, @length, @data)",
            Param("@user", userId),
            Param("@machine", machineId),
            Param("@title", title),
            Param("@length", length),
            Param("@data", data));
    }

    public void DeleteSong(int songId)
    {
        Execute(
            "DELETE FROM soundmachine_songs WHERE id = @id LIMIT 1",
            Param("@id", songId));
    }

    public void TransferSongToMachine(int songId, int machineId)
    {
        Execute(
            "UPDATE soundmachine_songs SET machineid = @machine WHERE id = @id LIMIT 1",
            Param("@id", songId),
            Param("@machine", machineId));
    }
    #endregion

    #region Playlists
    public int[] GetPlaylistSongIds(int machineId)
    {
        return ReadColumnInt(
            "SELECT songid FROM soundmachine_playlists WHERE machineid = @id ORDER BY pos ASC",
            0,
            Param("@id", machineId));
    }

    public void AddToPlaylist(int machineId, int songId, int position)
    {
        Execute(
            "INSERT INTO soundmachine_playlists (machineid, songid, pos) VALUES (@machine, @song, @pos)",
            Param("@machine", machineId),
            Param("@song", songId),
            Param("@pos", position));
    }

    public void RemoveFromPlaylist(int machineId, int songId)
    {
        Execute(
            "DELETE FROM soundmachine_playlists WHERE machineid = @machine AND songid = @song LIMIT 1",
            Param("@machine", machineId),
            Param("@song", songId));
    }

    public void ClearPlaylist(int machineId)
    {
        Execute(
            "DELETE FROM soundmachine_playlists WHERE machineid = @machine",
            Param("@machine", machineId));
    }

    public int GetPlaylistLength(int machineId)
    {
        return ReadScalarInt(
            "SELECT COUNT(*) FROM soundmachine_playlists WHERE machineid = @id",
            Param("@id", machineId));
    }
    #endregion
}
