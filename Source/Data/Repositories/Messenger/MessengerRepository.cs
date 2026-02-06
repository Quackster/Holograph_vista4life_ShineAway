using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Messenger;

/// <summary>
/// Repository for messenger_friendships and messenger_friendrequests tables.
/// </summary>
public class MessengerRepository : BaseRepository
{
    private static MessengerRepository? _instance;
    public static MessengerRepository Instance => _instance ??= new MessengerRepository();

    private MessengerRepository() { }

    #region Friendships
    public int[] GetFriendIds(int userId)
    {
        return ReadColumnInt(
            "SELECT friendid FROM messenger_friendships WHERE userid = @userid",
            0,
            Param("@userid", userId));
    }

    public int[] GetReverseFriendIds(int userId)
    {
        return ReadColumnInt(
            "SELECT userid FROM messenger_friendships WHERE friendid = @friendid",
            0,
            Param("@friendid", userId));
    }

    public bool AreFriends(int userId, int friendId)
    {
        return Exists(
            "SELECT userid FROM messenger_friendships WHERE (userid = @user AND friendid = @friend) OR (userid = @friend AND friendid = @user)",
            Param("@user", userId),
            Param("@friend", friendId));
    }

    public void AddFriendship(int userId, int friendId)
    {
        Execute(
            "INSERT INTO messenger_friendships (userid, friendid) VALUES (@user, @friend)",
            Param("@user", userId),
            Param("@friend", friendId));
    }

    public void RemoveFriendship(int userId, int friendId)
    {
        Execute(
            "DELETE FROM messenger_friendships WHERE (userid = @user AND friendid = @friend) OR (userid = @friend AND friendid = @user) LIMIT 1",
            Param("@user", userId),
            Param("@friend", friendId));
    }
    #endregion

    #region Friend Requests
    public int[] GetFriendRequestIds(int userId)
    {
        return ReadColumnInt(
            "SELECT userid_from FROM messenger_friendrequests WHERE userid_to = @userid",
            0,
            Param("@userid", userId));
    }

    public bool HasFriendRequest(int fromUserId, int toUserId)
    {
        return Exists(
            "SELECT userid_from FROM messenger_friendrequests WHERE userid_from = @from AND userid_to = @to",
            Param("@from", fromUserId),
            Param("@to", toUserId));
    }

    public void CreateFriendRequest(int fromUserId, int toUserId)
    {
        Execute(
            "INSERT INTO messenger_friendrequests (userid_from, userid_to) VALUES (@from, @to)",
            Param("@from", fromUserId),
            Param("@to", toUserId));
    }

    public void CreateFriendRequestWithId(int fromUserId, int toUserId, int requestId)
    {
        Execute(
            "INSERT INTO messenger_friendrequests (userid_to, userid_from, requestid) VALUES (@to, @from, @requestid)",
            Param("@to", toUserId),
            Param("@from", fromUserId),
            Param("@requestid", requestId));
    }

    public int GetMaxRequestId(int toUserId)
    {
        return ReadScalarIntUnsafe(
            "SELECT MAX(requestid) FROM messenger_friendrequests WHERE userid_to = @to",
            Param("@to", toUserId));
    }

    public int GetRequestSender(int toUserId, int requestId)
    {
        return ReadScalarInt(
            "SELECT userid_from FROM messenger_friendrequests WHERE userid_to = @to AND requestid = @requestid",
            Param("@to", toUserId),
            Param("@requestid", requestId));
    }

    public void DeleteFriendRequest(int fromUserId, int toUserId)
    {
        Execute(
            "DELETE FROM messenger_friendrequests WHERE userid_from = @from AND userid_to = @to LIMIT 1",
            Param("@from", fromUserId),
            Param("@to", toUserId));
    }

    public void DeleteFriendRequestById(int toUserId, int requestId)
    {
        Execute(
            "DELETE FROM messenger_friendrequests WHERE userid_to = @to AND requestid = @requestid LIMIT 1",
            Param("@to", toUserId),
            Param("@requestid", requestId));
    }

    public void DeleteAllFriendRequests(int userId)
    {
        Execute(
            "DELETE FROM messenger_friendrequests WHERE userid_to = @userid",
            Param("@userid", userId));
    }
    #endregion

    #region User Data for Messenger
    public string[] GetUserDataForMessenger(int userId)
    {
        return ReadRow(
            "SELECT name, figure, sex, mission FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public string? GetConsoleMission(int userId)
    {
        return ReadScalar(
            "SELECT consolemission FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public string? GetLastVisit(int userId)
    {
        return ReadScalar(
            "SELECT lastvisit FROM users WHERE id = @id",
            Param("@id", userId));
    }
    #endregion
}
