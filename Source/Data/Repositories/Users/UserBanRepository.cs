using Holo.Data.Repositories.Base;
using MySqlConnector;

namespace Holo.Data.Repositories.Users;

/// <summary>
/// Repository for users_bans and user_bans tables.
/// </summary>
public class UserBanRepository : BaseRepository
{
    private static UserBanRepository? _instance;
    public static UserBanRepository Instance => _instance ??= new UserBanRepository();

    private UserBanRepository() { }

    #region User Bans (by user ID)
    public bool IsBanned(int userId)
    {
        return Exists(
            "SELECT userid FROM users_bans WHERE userid = @id",
            Param("@id", userId));
    }

    public string? GetBanExpiry(int userId)
    {
        return ReadScalar(
            "SELECT date_expire FROM users_bans WHERE userid = @id",
            Param("@id", userId));
    }

    public string? GetBanReason(int userId)
    {
        return ReadScalar(
            "SELECT descr FROM users_bans WHERE userid = @id",
            Param("@id", userId));
    }

    public string[] GetBanDetails(int userId)
    {
        return ReadRow(
            "SELECT date_expire, descr, ipaddress FROM users_bans WHERE userid = @id",
            Param("@id", userId));
    }

    public void CreateBan(int userId, DateTime expiry, string reason)
    {
        // Delete previous bans first
        Execute(
            "DELETE FROM users_bans WHERE userid = @id LIMIT 1",
            Param("@id", userId));

        Execute(
            "INSERT INTO users_bans (userid, date_expire, descr) VALUES (@id, @expire, @reason)",
            Param("@id", userId),
            Param("@expire", expiry.ToString()),
            Param("@reason", reason));
    }

    public void DeleteBan(int userId)
    {
        Execute(
            "DELETE FROM users_bans WHERE userid = @id LIMIT 1",
            Param("@id", userId));
    }
    #endregion

    #region IP Bans
    public bool IsIpBanned(string ipAddress)
    {
        return Exists(
            "SELECT ipaddress FROM users_bans WHERE ipaddress = @ip",
            Param("@ip", ipAddress));
    }

    public string? GetIpBanExpiry(string ipAddress)
    {
        return ReadScalar(
            "SELECT date_expire FROM users_bans WHERE ipaddress = @ip",
            Param("@ip", ipAddress));
    }

    public string? GetIpBanReason(string ipAddress)
    {
        return ReadScalar(
            "SELECT descr FROM users_bans WHERE ipaddress = @ip",
            Param("@ip", ipAddress));
    }

    public string[] GetIpBanDetails(string ipAddress)
    {
        return ReadRow(
            "SELECT userid, date_expire, descr FROM users_bans WHERE ipaddress = @ip",
            Param("@ip", ipAddress));
    }

    public void CreateIpBan(string ipAddress, DateTime expiry, string reason)
    {
        // Delete previous bans first
        Execute(
            "DELETE FROM users_bans WHERE ipaddress = @ip",
            Param("@ip", ipAddress));

        Execute(
            "INSERT INTO users_bans (ipaddress, date_expire, descr) VALUES (@ip, @expire, @reason)",
            Param("@ip", ipAddress),
            Param("@expire", expiry.ToString()),
            Param("@reason", reason));
    }

    public void DeleteIpBan(string ipAddress)
    {
        Execute(
            "DELETE FROM users_bans WHERE ipaddress = @ip LIMIT 1",
            Param("@ip", ipAddress));
    }

    public int[] GetUserIdsByIp(string ipAddress)
    {
        return ReadColumnInt(
            "SELECT id FROM users WHERE ipaddress_last = @ip",
            0,
            Param("@ip", ipAddress));
    }

    public string[] GetUsernamesByIp(string ipAddress)
    {
        return ReadColumn(
            "SELECT name FROM users WHERE ipaddress_last = @ip",
            0,
            Param("@ip", ipAddress));
    }
    #endregion

    #region Staff Log (for ban reports)
    public string[] GetBanLogEntry(int targetUserId)
    {
        return ReadRow(
            "SELECT userid, note, timestamp FROM system_stafflog WHERE action = 'ban' AND targetid = @target ORDER BY id DESC",
            Param("@target", targetUserId));
    }
    #endregion

    #region Ban Management (check and lift expired bans)
    public void DeleteBanByUserId(int userId)
    {
        Execute(
            "DELETE FROM users_bans WHERE userid = @id LIMIT 1",
            Param("@id", userId));
    }

    public void DeleteBanByIp(string ipAddress)
    {
        Execute(
            "DELETE FROM users_bans WHERE ipaddress = @ip LIMIT 1",
            Param("@ip", ipAddress));
    }
    #endregion
}
