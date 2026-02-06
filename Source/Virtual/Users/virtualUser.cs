using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;
using Holo.Virtual.Users.Items;
using Holo.Virtual.Users.Messenger;
using Holo.Virtual.Rooms.Games;
using Holo.Virtual;
using Microsoft.VisualBasic;
using System.Collections.Generic;

namespace Holo.Virtual.Users
{
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
        /// The socket that connects the client with the emulator. Operates asynchronous.
        /// </summary>
        internal void deadSocketKiller()
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
        private Socket connectionSocket;
        /// <summary>
        /// The byte array where the data is saved in while receiving the data asynchronously.
        /// </summary>
        private byte[] dataBuffer = new byte[1024];
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
        /// <summary>
        /// The room user ID (rUID) of the virtual user where this virtual user is currently trading items with. If not trading, then this value is -1.
        /// </summary>
        ///
        internal List<string> _Badges = new List<string>();
        internal List<int> _badgeSlotIDs = new List<int>();
        internal int _tradePartnerRoomUID = -1;
        /// Specifies if the user has received the sprite index packet (Dg) already. This packet only requires being sent once, and since it's a BIG packet, we limit it to send it once.
        /// </summary>
        private bool _receivedSpriteIndex;
        /// <summary>
        /// The number of the page of the Hand (item inventory) the user is currently on.
        /// </summary>
        private int _handPage;
        private delegate void timedDisconnector(int ms);
        public bool _isInvisible;
        public bool spyMode = false;
        public int imitateID;
        public bool imitateEnabled;
        public bool imitateSpeech;
        internal string _Badge;
        internal string swimOutfit;
        public bool _isPet;
        public string _petColour = "98A1C5"; // Pet Colour (HEX)
        public string _petType = "1"; // Cat = 1, Dog = 2, Croc = 3
        /// <summary>
        /// The virtual room the user is in.
        /// </summary>
        internal virtualRoom Room;
        /// <summary>
        /// The virtualRoomUser that represents this virtual user in room. Contains in-room only objects such as position, rotation and walk related objects.
        /// </summary>
        internal virtualRoomUser roomUser;
        /// <summary>
        /// The status manager that keeps status strings for the user in room.
        /// </summary>
        internal virtualRoomUserStatusManager statusManager;
        /// <summary>
        /// The messenger that provides instant messaging, friendlist etc for this virtual user.
        /// </summary>
        internal Messenger.virtualMessenger Messenger;
        /// <summary>
        /// Variant of virtualRoomUser object. Represents this virtual user in a game arena, aswell as in a game team in the navigator.
        /// </summary>
        internal gamePlayer gamePlayer;

        public bool filterEnabled;

        public List<int> ignoreList = new List<int>();


        #region Personal
        internal int userID;
        internal string _Username;
        internal string _Figure;
        internal char _Sex;
        internal string _Mission;
        internal string _consoleMission;
        internal byte _Rank;
        internal int _Credits;
        internal int _Tickets;

        internal string _nowBadge;
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

        private virtualSongEditor songEditor;
        #endregion

        #region Constructors/destructors
        /// <summary>
        /// Initializes a new virtual user, and starts packet transfer between client and asynchronous socket server.
        /// </summary>
        /// <param name="connectionID">The ID of the new connection.</param>
        /// <param name="connectionSocket">The socket of the new connection.</param>
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
                    connectionSocket.BeginReceive(dataBuffer, 0, dataBuffer.Length, SocketFlags.None, new AsyncCallback(dataArrival), null);
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

            try { connectionSocket.Close(); }
            catch { }
            connectionSocket = null;

            if (Room != null && roomUser != null)
                Room.removeUser(roomUser.roomUID, false, "");

            if (Messenger != null)
                Messenger.Clear();

            userManager.removeUser(userID);
            Socketservers.gameSocketServer.freeConnection(connectionID);
            _isDisconnected = true;
        }

        /// <summary>
        /// Disables receiving on the socket, sleeps for a specified amount of time [ms] and disconnects via normal Disconnect() void. Asynchronous.
        /// </summary>
        /// <param name="ms"></param>
        internal void Disconnect(int ms)
        {
            new timedDisconnector(delDisconnectTimed).BeginInvoke(ms, null, null);
        }
        private void delDisconnectTimed(int ms)
        {
            connectionSocket.Shutdown(SocketShutdown.Receive);
            Thread.Sleep(ms);
            Disconnect();
        }

        /// <summary>
        /// Returns the IP address of this connection as a string.
        /// </summary>
        internal string connectionRemoteIP
        {
            get
            {
                return connectionSocket.RemoteEndPoint.ToString().Split(':')[0];
            }
        }
        #endregion

        #region Data receiving
        /// <summary>
        /// This void is triggered when a new datapacket arrives at the socket of this user. The packet is separated and processed. On errors, the client is disconnected.
        /// </summary>
        /// <param name="iAr">The IAsyncResult of this BeginReceive asynchronous action.</param>
        private void dataArrival(IAsyncResult iAr)
        {
            try
            {
                int bytesReceived = connectionSocket.EndReceive(iAr);
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
        /// Sends a single packet to the client via an asynchronous BeginSend action.
        /// </summary>
        /// <param name="Data">The string of data to send. char[01] is added.</param>
        internal void sendData(string Data)
        {
            try
            {// Packetlogger
                byte[] dataBytes = System.Text.Encoding.Default.GetBytes(Data + Convert.ToChar(1));
                connectionSocket.BeginSend(dataBytes, 0, dataBytes.Length, 0, new AsyncCallback(sentData), null);
                Out.WriteSpecialLine(Data.Replace(Convert.ToChar(13).ToString(), "{13}"), Out.logFlags.MehAction, ConsoleColor.White, ConsoleColor.DarkGreen, "> [" + Thread.GetDomainID() + "]", 2, ConsoleColor.DarkYellow);
            }
            catch
            {
                Disconnect();
            }
        }
        /// <summary>
        /// Triggered when an asynchronous BeginSend action is completed. Virtual user completes the transfer action and leaves asynchronous action.
        /// </summary>
        /// <param name="iAr">The IAsyncResult of this BeginSend asynchronous action.</param>
        private void sentData(IAsyncResult iAr)
        {
            try { connectionSocket.EndSend(iAr); }
            catch { Disconnect(); }
        }
        #endregion

        private void sendData(int myID)
        {
            throw new NotImplementedException();
        }

        private void Refresh()
        {
            roomUser.Refresh();
        }
    }
}
