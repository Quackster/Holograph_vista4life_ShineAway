using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Users;

/// <summary>
/// Repository for users, users_club, and users_badges tables.
/// </summary>
public class UserRepository : BaseRepository
{
    private static UserRepository? _instance;
    public static UserRepository Instance => _instance ??= new UserRepository();

    private UserRepository() { }

    #region Users - Basic Operations
    public int GetUserId(string username)
    {
        return ReadScalarInt(
            "SELECT id FROM users WHERE name = @name",
            Param("@name", username));
    }

    public string? GetUsername(int userId)
    {
        return ReadScalar(
            "SELECT name FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public bool UserExists(int userId)
    {
        return Exists(
            "SELECT id FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public bool UsernameExists(string username)
    {
        return Exists(
            "SELECT id FROM users WHERE name = @name",
            Param("@name", username));
    }

    public int GetUserCount()
    {
        return ReadScalarInt("SELECT COUNT(*) FROM users");
    }
    #endregion

    #region Users - Authentication
    public string? GetLastIpAddress(string username)
    {
        return ReadScalar(
            "SELECT ipaddress_last FROM users WHERE name = @name",
            Param("@name", username));
    }

    public void ClearTicketSso(int userId)
    {
        Execute(
            "UPDATE users SET ticket_sso = NULL WHERE id = @id LIMIT 1",
            Param("@id", userId));
    }

    public void ClearAllTickets()
    {
        Execute("UPDATE users SET ticket_sso = NULL");
    }

    public string[] GetUserByTicket(string ticket)
    {
        return ReadRow(
            "SELECT id, name, figure, sex, mission, rank, credits, tickets FROM users WHERE ticket_sso = @ticket",
            Param("@ticket", ticket));
    }

    public void UpdateLastVisit(int userId, string ipAddress)
    {
        Execute(
            "UPDATE users SET lastvisit = NOW(), ipaddress_last = @ip WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@ip", ipAddress));
    }
    #endregion

    #region Users - Profile
    public string[] GetUserDetails(int userId)
    {
        return ReadRow(
            "SELECT name, rank, mission, credits, tickets, email, birth, hbirth, ipaddress_last, lastvisit FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public string[] GetUserDetailsForRank(int userId, byte maxRank)
    {
        return ReadRow(
            "SELECT name, rank, mission, credits, tickets, email, birth, hbirth, ipaddress_last, lastvisit FROM users WHERE id = @id AND rank <= @maxRank",
            Param("@id", userId),
            Param("@maxRank", maxRank));
    }

    public void UpdateMission(int userId, string mission)
    {
        Execute(
            "UPDATE users SET mission = @mission WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@mission", mission));
    }

    public void UpdateFigure(int userId, string figure, char sex)
    {
        Execute(
            "UPDATE users SET figure = @figure, sex = @sex WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@figure", figure),
            Param("@sex", sex.ToString()));
    }

    public void UpdateConsoleMission(int userId, string mission)
    {
        Execute(
            "UPDATE users SET consolemission = @mission WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@mission", mission));
    }
    #endregion

    #region Users - Credits & Tickets
    public int GetCredits(int userId)
    {
        return ReadScalarInt(
            "SELECT credits FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public void UpdateCredits(int userId, int credits)
    {
        Execute(
            "UPDATE users SET credits = @credits WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@credits", credits));
    }

    public void AddCredits(int userId, int amount)
    {
        Execute(
            "UPDATE users SET credits = credits + @amount WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@amount", amount));
    }

    public int GetTickets(int userId)
    {
        return ReadScalarInt(
            "SELECT tickets FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public void UpdateTickets(int userId, int tickets)
    {
        Execute(
            "UPDATE users SET tickets = @tickets WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@tickets", tickets));
    }
    #endregion

    #region Users - Swimming
    public string? GetSwimFigure(int userId)
    {
        return ReadScalar(
            "SELECT figure_swim FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public void UpdateSwimFigure(int userId, string swimFigure)
    {
        Execute(
            "UPDATE users SET figure_swim = @figure WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@figure", swimFigure));
    }
    #endregion

    #region Users - Club
    public bool IsClubMember(int userId)
    {
        return Exists(
            "SELECT userid FROM users_club WHERE userid = @id AND date_expire > NOW()",
            Param("@id", userId));
    }

    public string[] GetClubSubscription(int userId)
    {
        return ReadRow(
            "SELECT months_expired, months_left, date_expire FROM users_club WHERE userid = @id",
            Param("@id", userId));
    }

    public void UpdateClubSubscription(int userId, int monthsExpired, int monthsLeft, DateTime expireDate)
    {
        if (Exists("SELECT userid FROM users_club WHERE userid = @id", Param("@id", userId)))
        {
            Execute(
                "UPDATE users_club SET months_expired = @expired, months_left = @left, date_expire = @expire WHERE userid = @id",
                Param("@id", userId),
                Param("@expired", monthsExpired),
                Param("@left", monthsLeft),
                Param("@expire", expireDate));
        }
        else
        {
            Execute(
                "INSERT INTO users_club (userid, months_expired, months_left, date_expire) VALUES (@id, @expired, @left, @expire)",
                Param("@id", userId),
                Param("@expired", monthsExpired),
                Param("@left", monthsLeft),
                Param("@expire", expireDate));
        }
    }
    #endregion

    #region Users - Badges
    public string[] GetUserBadges(int userId)
    {
        return ReadColumn(
            "SELECT badge FROM users_badges WHERE userid = @id",
            0,
            Param("@id", userId));
    }

    public int[] GetUserBadgeSlots(int userId)
    {
        return ReadColumnInt(
            "SELECT slotid FROM users_badges WHERE userid = @id",
            0,
            Param("@id", userId));
    }

    public void AddBadge(int userId, string badge)
    {
        Execute(
            "INSERT INTO users_badges (userid, badge) VALUES (@id, @badge)",
            Param("@id", userId),
            Param("@badge", badge));
    }

    public void UpdateBadgeSlot(int userId, string badge, int slotId)
    {
        Execute(
            "UPDATE users_badges SET slotid = @slot WHERE userid = @id AND badge = @badge LIMIT 1",
            Param("@id", userId),
            Param("@badge", badge),
            Param("@slot", slotId));
    }

    public bool HasBadge(int userId, string badge)
    {
        return Exists(
            "SELECT userid FROM users_badges WHERE userid = @id AND badge = @badge",
            Param("@id", userId),
            Param("@badge", badge));
    }
    #endregion

    #region Users - Favorite Rooms
    public int[] GetFavoriteRooms(int userId, int maxRooms)
    {
        return ReadColumnInt(
            "SELECT roomid FROM users_favouriterooms WHERE userid = @id ORDER BY roomid DESC",
            maxRooms,
            Param("@id", userId));
    }

    public void AddFavoriteRoom(int userId, int roomId)
    {
        Execute(
            "INSERT INTO users_favouriterooms (userid, roomid) VALUES (@userid, @roomid)",
            Param("@userid", userId),
            Param("@roomid", roomId));
    }

    public void RemoveFavoriteRoom(int userId, int roomId)
    {
        Execute(
            "DELETE FROM users_favouriterooms WHERE userid = @userid AND roomid = @roomid LIMIT 1",
            Param("@userid", userId),
            Param("@roomid", roomId));
    }

    public bool IsFavoriteRoom(int userId, int roomId)
    {
        return Exists(
            "SELECT userid FROM users_favouriterooms WHERE userid = @userid AND roomid = @roomid",
            Param("@userid", userId),
            Param("@roomid", roomId));
    }

    public int GetFavoriteRoomCount(int userId)
    {
        return ReadScalarInt(
            "SELECT COUNT(userid) FROM users_favouriterooms WHERE userid = @id",
            Param("@id", userId));
    }
    #endregion

    #region Users - Recycler
    public bool HasRecyclerSession(int userId)
    {
        return Exists(
            "SELECT userid FROM users_recycler WHERE userid = @id",
            Param("@id", userId));
    }

    public string[] GetRecyclerSession(int userId)
    {
        return ReadRow(
            "SELECT session_started, session_reward FROM users_recycler WHERE userid = @id",
            Param("@id", userId));
    }

    public int GetRecyclerRewardId(int userId)
    {
        return ReadScalarInt(
            "SELECT session_reward FROM users_recycler WHERE userid = @id",
            Param("@id", userId));
    }

    public void CreateRecyclerSession(int userId, int rewardTemplateId)
    {
        Execute(
            "INSERT INTO users_recycler (userid, session_started, session_reward) VALUES (@id, @started, @reward)",
            Param("@id", userId),
            Param("@started", DateTime.Now.ToString()),
            Param("@reward", rewardTemplateId));
    }

    public void DeleteRecyclerSession(int userId)
    {
        Execute(
            "DELETE FROM users_recycler WHERE userid = @id LIMIT 1",
            Param("@id", userId));
    }
    #endregion

    #region Users - Game Points
    public int GetGamePoints(int userId, string gameType)
    {
        return ReadScalarIntUnsafe(
            $"SELECT {gameType}_totalpoints FROM users WHERE id = @id",
            Param("@id", userId));
    }

    public void UpdateGamePoints(int userId, string gameType, int points)
    {
        Execute(
            $"UPDATE users SET {gameType}_totalpoints = @points WHERE id = @id LIMIT 1",
            Param("@id", userId),
            Param("@points", points));
    }
    #endregion

    #region Users - Groups
    public string? GetGroupBadge(int groupId)
    {
        return ReadScalar(
            "SELECT badge FROM groups_details WHERE id = @id",
            Param("@id", groupId));
    }
    #endregion

    #region Users - Profile Details (for ban reports)
    public string[] GetUserDetailsForBanReport(int userId)
    {
        return ReadRow(
            "SELECT name, rank, ipaddress_last FROM users WHERE id = @id",
            Param("@id", userId));
    }
    #endregion
}
