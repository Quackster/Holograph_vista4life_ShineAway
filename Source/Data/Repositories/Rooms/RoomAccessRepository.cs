using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Rooms;

/// <summary>
/// Repository for room_rights, room_bans, and room_votes tables.
/// </summary>
public class RoomAccessRepository : BaseRepository
{
    private static RoomAccessRepository? _instance;
    public static RoomAccessRepository Instance => _instance ??= new RoomAccessRepository();

    private RoomAccessRepository() { }

    #region Room Rights
    public bool HasRights(int roomId, int userId)
    {
        return Exists(
            "SELECT roomid FROM room_rights WHERE roomid = @roomid AND userid = @userid",
            Param("@roomid", roomId),
            Param("@userid", userId));
    }

    public int[] GetUsersWithRights(int roomId)
    {
        return ReadColumnInt(
            "SELECT userid FROM room_rights WHERE roomid = @roomid",
            0,
            Param("@roomid", roomId));
    }

    public void AddRights(int roomId, int userId)
    {
        Execute(
            "INSERT INTO room_rights (roomid, userid) VALUES (@roomid, @userid)",
            Param("@roomid", roomId),
            Param("@userid", userId));
    }

    public void RemoveRights(int roomId, int userId)
    {
        Execute(
            "DELETE FROM room_rights WHERE roomid = @roomid AND userid = @userid LIMIT 1",
            Param("@roomid", roomId),
            Param("@userid", userId));
    }

    public void RemoveAllRights(int roomId)
    {
        Execute(
            "DELETE FROM room_rights WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }
    #endregion

    #region Room Bans
    public bool IsBanned(int roomId, int userId)
    {
        return Exists(
            "SELECT roomid FROM room_bans WHERE roomid = @roomid AND userid = @userid",
            Param("@roomid", roomId),
            Param("@userid", userId));
    }

    public void AddBan(int roomId, int userId)
    {
        Execute(
            "INSERT INTO room_bans (roomid, userid) VALUES (@roomid, @userid)",
            Param("@roomid", roomId),
            Param("@userid", userId));
    }

    public void RemoveBan(int roomId, int userId)
    {
        Execute(
            "DELETE FROM room_bans WHERE roomid = @roomid AND userid = @userid LIMIT 1",
            Param("@roomid", roomId),
            Param("@userid", userId));
    }

    public void RemoveAllBans(int roomId)
    {
        Execute(
            "DELETE FROM room_bans WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }
    #endregion

    #region Room Votes
    public bool HasVoted(int roomId, int userId)
    {
        return Exists(
            "SELECT userid FROM room_votes WHERE userid = @userid AND roomid = @roomid",
            Param("@userid", userId),
            Param("@roomid", roomId));
    }

    public void AddVote(int roomId, int userId, int vote)
    {
        Execute(
            "INSERT INTO room_votes (roomid, userid, vote) VALUES (@roomid, @userid, @vote)",
            Param("@roomid", roomId),
            Param("@userid", userId),
            Param("@vote", vote));
    }

    public int GetVoteSum(int roomId)
    {
        return ReadScalarInt(
            "SELECT SUM(vote) FROM room_votes WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }

    public int GetVoteCount(int roomId)
    {
        return ReadScalarInt(
            "SELECT COUNT(vote) FROM room_votes WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }

    public void RemoveAllVotes(int roomId)
    {
        Execute(
            "DELETE FROM room_votes WHERE roomid = @roomid",
            Param("@roomid", roomId));
    }
    #endregion
}
