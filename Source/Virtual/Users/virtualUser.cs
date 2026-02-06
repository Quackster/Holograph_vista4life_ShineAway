using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Transport.Channels;

using Holo.Managers;
using Holo.Network;
using Holo.Protocol;
using Holo.Virtual.Rooms;
using Holo.Virtual.Users.Messenger;
using Holo.Virtual.Users.Items;
using Holo.Virtual.Rooms.Games;

namespace Holo.Virtual.Users;


/// <summary>
/// Represents a virtual user, with connection and packet handling, access management etc etc. The details about the user are kept separate in a different class.
/// </summary>
public partial class virtualUser
{
    /// <summary>
    /// The ID of the connection for this virtual user. Assigned by the game socket server.
    /// </summary>
    private int connectionID;

    /// <summary>
    /// The DotNetty channel context for this connection.
    /// </summary>
    private IChannelHandlerContext? _channelContext;

    /// <summary>
    /// Legacy socket (kept for compatibility, will be null when using DotNetty).
    /// </summary>
    private Socket? connectionSocket;

    /// <summary>
    /// Specifies if the client has sent the 'CD' packet on time. Being checked by the user manager every minute.
    /// </summary>
    internal bool pingOK;

    /// <summary>
    /// Specifies if the client is disconnected already.
    /// </summary>
    private bool _isDisconnected;

    /// <summary>
    /// Specifies if the client has logged in and the user details are loaded. If false, then the user is just a connected client and shouldn't be able to send 'logged in' packets.
    /// </summary>
    private bool _isLoggedIn;

    internal List<string> _Badges = new List<string>();
    internal List<int> _badgeSlotIDs = new List<int>();
    internal int _tradePartnerRoomUID = -1;

    /// <summary>
    /// Specifies if the user has received the sprite index packet (Dg) already. This packet only requires being sent once, and since it's a BIG packet, we limit it to send it once.
    /// </summary>
    private bool _receivedSpriteIndex;

    /// <summary>
    /// The number of the page of the Hand (item inventory) the user is currently on.
    /// </summary>
    private int _handPage;

    public bool _isInvisible;
    public bool spyMode = false;
    public int imitateID;
    public bool imitateEnabled;
    public bool imitateSpeech;
    internal string? _Badge;
    internal string? swimOutfit;
    public bool _isPet;
    public string _petColour = "98A1C5"; // Pet Colour (HEX)
    public string _petType = "1"; // Cat = 1, Dog = 2, Croc = 3

    /// <summary>
    /// The virtual room the user is in.
    /// </summary>
    internal virtualRoom? Room;

    /// <summary>
    /// The virtualRoomUser that represents this virtual user in room. Contains in-room only objects such as position, rotation and walk related objects.
    /// </summary>
    internal virtualRoomUser? roomUser;

    /// <summary>
    /// The status manager that keeps status strings for the user in room.
    /// </summary>
    internal virtualRoomUserStatusManager? statusManager;

    /// <summary>
    /// The messenger that provides instant messaging, friendlist etc for this virtual user.
    /// </summary>
    internal Messenger.virtualMessenger? Messenger;

    /// <summary>
    /// Variant of virtualRoomUser object. Represents this virtual user in a game arena, aswell as in a game team in the navigator.
    /// </summary>
    internal gamePlayer? gamePlayer;

    public bool filterEnabled;

    public List<int> ignoreList = new List<int>();


    #region Personal
    internal int userID;
    internal string? _Username;
    internal string? _Figure;
    internal char _Sex;
    internal string? _Mission;
    internal string? _consoleMission;
    internal byte _Rank;
    internal int _Credits;
    internal int _Tickets;

    internal string? _nowBadge;
    internal bool _clubMember;

    internal int _roomID;
    internal bool _inPublicroom;
    internal bool _ROOMACCESS_PRIMARY_OK;
    internal bool _ROOMACCESS_SECONDARY_OK;
    internal bool _isOwner;
    internal bool _hasRights;
    internal bool _isMuted;

    internal int _groupID;
    internal int _groupMemberRank;

    internal int _tradePartnerUID = -1;
    internal bool _tradeAccept;
    internal int[] _tradeItems = new int[65];
    internal int _tradeItemCount;

    internal int _teleporterID;
    internal bool _hostsEvent;

    private virtualSongEditor? songEditor;
    #endregion

    #region Constructors/destructors
    /// <summary>
    /// Initializes a new virtual user with DotNetty channel context.
    /// </summary>
    /// <param name="connectionID">The ID of the new connection.</param>
    /// <param name="channelContext">The DotNetty channel context.</param>
    public virtualUser(int connectionID, IChannelHandlerContext channelContext)
    {
        this.connectionID = connectionID;
        this._channelContext = channelContext;

        pingOK = true;
        sendData("@@");
    }

    /// <summary>
    /// Legacy constructor for raw socket connections.
    /// </summary>
    /// <param name="connectionID">The ID of the new connection.</param>
    /// <param name="connectionSocket">The socket of the new connection.</param>
    [Obsolete("Use the DotNetty constructor instead")]
    public virtualUser(int connectionID, Socket connectionSocket)
    {
        this.connectionID = connectionID;
        this.connectionSocket = connectionSocket;

        try
        {
            string banReason = userManager.getBanReason(this.connectionRemoteIP);
            if (banReason != "")
            {
                sendData(new HabboPacketBuilder("@c").Append(banReason).Build());
                Disconnect();
            }
            else
            {
                pingOK = true;
                sendData("@@");
                connectionSocket.BeginReceive(new byte[1024], 0, 1024, SocketFlags.None, new AsyncCallback(dataArrival), null);
            }
        }
        catch { }
    }
    #endregion

    #region Connection management
    /// <summary>
    /// Immediately completes the current data transfer [if any], disconnects the client and flags the connection slot as free.
    /// </summary>
    internal void Disconnect()
    {
        if (_isDisconnected)
            return;

        _isDisconnected = true;

        // Close DotNetty channel
        if (_channelContext != null)
        {
            try
            {
                _channelContext.CloseAsync();
            }
            catch { }
            _channelContext = null;
        }

        // Close legacy socket
        if (connectionSocket != null)
        {
            try { connectionSocket.Close(); }
            catch { }
            connectionSocket = null;
        }

        if (Room != null && roomUser != null)
            Room.removeUser(roomUser.roomUID, false, "");

        if (Messenger != null)
            Messenger.Clear();

        userManager.removeUser(userID);
        GameSocketServer.FreeConnection(connectionID);
    }

    /// <summary>
    /// Disconnects after a specified delay in milliseconds.
    /// </summary>
    /// <param name="ms">Delay in milliseconds before disconnecting.</param>
    internal void Disconnect(int ms)
    {
        _ = DisconnectDelayedAsync(ms);
    }

    private async Task DisconnectDelayedAsync(int ms)
    {
        await Task.Delay(ms);
        Disconnect();
    }

    /// <summary>
    /// Returns the IP address of this connection as a string.
    /// </summary>
    internal string connectionRemoteIP
    {
        get
        {
            if (_channelContext != null)
            {
                return _channelContext.Channel.RemoteAddress?.ToString()?.Split(':')[0] ?? "unknown";
            }
            if (connectionSocket != null)
            {
                return connectionSocket.RemoteEndPoint?.ToString()?.Split(':')[0] ?? "unknown";
            }
            return "unknown";
        }
    }

    /// <summary>
    /// Dead socket checker (legacy, not needed with DotNetty).
    /// </summary>
    internal void deadSocketKiller()
    {
        if (_channelContext != null)
        {
            // DotNetty handles connection health automatically
            return;
        }

        if (connectionSocket != null)
        {
            try
            {
                byte[] tmp = new byte[1];
                connectionSocket.Send(tmp, 0);
            }
            catch
            {
                Disconnect();
            }
        }
    }
    #endregion

    #region Data receiving (DotNetty)
    /// <summary>
    /// Processes a packet received via DotNetty.
    /// Called by GameClientHandler when a complete packet is decoded.
    /// </summary>
    /// <param name="packet">The decoded packet content.</param>
    public void ProcessPacketAsync(string packet)
    {
        try
        {
            processPacket(packet);
        }
        catch (Exception ex)
        {
            Out.WriteError($"Error processing packet for connection [{connectionID}]: {ex.Message}");
            Disconnect();
        }
    }
    #endregion

    #region Data receiving (Legacy Socket)
    /// <summary>
    /// Legacy data arrival handler for raw socket connections.
    /// </summary>
    private void dataArrival(IAsyncResult iAr)
    {
        if (connectionSocket == null)
            return;

        try
        {
            int bytesReceived = connectionSocket.EndReceive(iAr);
            byte[] dataBuffer = new byte[1024];
            string connectionData = System.Text.Encoding.Default.GetString(dataBuffer, 0, bytesReceived);

            if (connectionData.Contains("\x01") || connectionData.Contains("\x02") || connectionData.Contains("\x05") || connectionData.Contains("\x09"))
            {
                Disconnect();
                return;
            }

            while (connectionData != "")
            {
                int v = Encoding.decodeB64(connectionData.Substring(1, 2));
                processPacket(connectionData.Substring(3, v));
                connectionData = connectionData.Substring(v + 3);
            }

            connectionSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(dataArrival), null);
        }
        catch { Disconnect(); }
    }
    #endregion

    #region Data sending
    /// <summary>
    /// Sends a single packet to the client.
    /// </summary>
    /// <param name="Data">The string of data to send.</param>
    internal void sendData(string Data)
    {
        try
        {
            Out.WriteSpecialLine(Data.Replace(Convert.ToChar(13).ToString(), "{13}"), Out.logFlags.MehAction, ConsoleColor.White, ConsoleColor.DarkGreen, "> [" + Thread.GetDomainID() + "]", 2, ConsoleColor.DarkYellow);

            if (_channelContext != null)
            {
                // Use DotNetty channel
                _channelContext.WriteAndFlushAsync(Data);
            }
            else if (connectionSocket != null)
            {
                // Legacy socket fallback
                byte[] dataBytes = System.Text.Encoding.Default.GetBytes(Data + Convert.ToChar(1));
                connectionSocket.BeginSend(dataBytes, 0, dataBytes.Length, 0, new AsyncCallback(sentData), null);
            }
        }
        catch
        {
            Disconnect();
        }
    }

    /// <summary>
    /// Legacy callback for socket BeginSend completion.
    /// </summary>
    private void sentData(IAsyncResult iAr)
    {
        try
        {
            connectionSocket?.EndSend(iAr);
        }
        catch
        {
            Disconnect();
        }
    }
    #endregion

    private void sendData(int myID)
    {
        throw new NotImplementedException();
    }

    private void Refresh()
    {
        roomUser?.Refresh();
    }
}
