using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Holo.Managers;
using Holo.Virtual.Rooms;
using Holo.Virtual.Users.Items;
using Holo.Virtual.Users.Messenger;
using Holo.Virtual.Rooms.Games;
using Holo.Virtual;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using Holo.Data.Repositories;
using Holo.Core;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Represents a virtual user, with connection and packet handling, access management etc etc. The details about the user are kept separate in a different class.
    /// </summary>
    public class virtualUser
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

        // Data access instances
        private static readonly UserDataAccess _userDataAccess = new UserDataAccess();
        private static readonly BadgeDataAccess _badgeDataAccess = new BadgeDataAccess();
        private static readonly ClubDataAccess _clubDataAccess = new ClubDataAccess();
        private static readonly IgnoreDataAccess _ignoreDataAccess = new IgnoreDataAccess();
        private static readonly MessengerDataAccess _messengerDataAccess = new MessengerDataAccess();
        private static readonly VoucherDataAccess _voucherDataAccess = new VoucherDataAccess();
        private static readonly RoomDataAccess _roomDataAccess = new RoomDataAccess();
        private static readonly FavoriteRoomDataAccess _favoriteRoomDataAccess = new FavoriteRoomDataAccess();
        private static readonly HelpDataAccess _helpDataAccess = new HelpDataAccess();
        private static readonly GroupDataAccess _groupDataAccess = new GroupDataAccess();
        private static readonly TagDataAccess _tagDataAccess = new TagDataAccess();
        private static readonly PollDataAccess _pollDataAccess = new PollDataAccess();
        private static readonly RoomCategoryDataAccess _roomCategoryDataAccess = new RoomCategoryDataAccess();
        private static readonly RoomVoteDataAccess _roomVoteDataAccess = new RoomVoteDataAccess();
        private static readonly RoomBanDataAccess _roomBanDataAccess = new RoomBanDataAccess();
        private static readonly FurnitureDataAccess _furnitureDataAccess = new FurnitureDataAccess();
        private static readonly RoomRightsDataAccess _roomRightsDataAccess = new RoomRightsDataAccess();
        private static readonly CatalogueDataAccess _catalogueDataAccess = new CatalogueDataAccess();
        private static readonly SoundMachineDataAccess _soundMachineDataAccess = new SoundMachineDataAccess();
        #endregion

        #region Packet Handler Registration
        /// <summary>
        /// Dictionary of packet handlers, keyed by packet header.
        /// </summary>
        private static readonly Dictionary<string, PacketHandler> _packetHandlers = new Dictionary<string, PacketHandler>();

        /// <summary>
        /// Static constructor to initialize packet handlers.
        /// </summary>
        static virtualUser()
        {
            RegisterPacketHandlers();
        }

        /// <summary>
        /// Registers all packet handlers.
        /// </summary>
        private static void RegisterPacketHandlers()
        {
            // Non-logged in packets (no login required)
            RegisterHandler("CD", HandlePing, false);
            RegisterHandler("CN", HandleClientVersion, false);
            RegisterHandler("CJ", HandleClientInfo, false);
            RegisterHandler("_R", HandleSSORequest, false);
            RegisterHandler("CL", HandleLogin, false);

            // Logged in packets (require login)
            RegisterHandler("@q", HandleRequestDate, true);
            RegisterHandler("@L", HandleInitializeMessenger, true);
            RegisterHandler("@Z", HandleInitializeClub, true);
            RegisterHandler("@G", HandleRefreshAppearance, true);
            RegisterHandler("@H", HandleRefreshValueables, true);
            RegisterHandler("B]", HandleRefreshBadges, true);
            RegisterHandler("Cd", HandleRefreshGroupStatus, true);
            RegisterHandler("C^", HandleRecyclerSetup, true);
            RegisterHandler("C_", HandleRecyclerSession, true);
            RegisterHandler("Ej", HandleSetGuideAvailable, true);
            RegisterHandler("Ek", HandleSetGuideUnavailable, true);
            RegisterHandler("Ai", HandleBuyGameTickets, true);
            RegisterHandler("BA", HandleRedeemVoucher, true);
            RegisterHandler("Fs", HandleSearchConsole, true);
            RegisterHandler("@i", HandleSearchInConsole, true);
            RegisterHandler("@g", HandleRequestFriend, true);
            RegisterHandler("@e", HandleAcceptFriendRequest, true);
            RegisterHandler("@f", HandleDeclineFriendRequest, true);
            RegisterHandler("@h", HandleRemoveBuddy, true);
            RegisterHandler("@a", HandleSendInstantMessage, true);
            RegisterHandler("@O", HandleRefreshFriendList, true);
            RegisterHandler("DF", HandleFollowBuddy, true);
            RegisterHandler("@b", HandleInviteBuddies, true);
            RegisterHandler("BV", HandleNavigateRooms, true);
            RegisterHandler("BW", HandleRequestCategoryIndex, true);
            RegisterHandler("DH", HandleRefreshRecommendedRooms, true);
            RegisterHandler("@P", HandleViewOwnRooms, true);
            RegisterHandler("@Q", HandleSearchRooms, true);
            RegisterHandler("@U", HandleGetRoomDetails, true);
            RegisterHandler("@R", HandleInitializeFavorites, true);
            RegisterHandler("@S", HandleAddFavorite, true);
            RegisterHandler("@T", HandleRemoveFavorite, true);
            RegisterHandler("EA", HandleGetEventSetup, true);
            RegisterHandler("EY", HandleToggleEventButton, true);
            RegisterHandler("D{", HandleCheckEventCategory, true);
            RegisterHandler("E^", HandleOpenEventCategory, true);
            RegisterHandler("EZ", HandleCreateEvent, true);
            RegisterHandler("E[", HandleEndEvent, true);
            RegisterHandler("@]", HandleCreateRoomPhase1, true);
            RegisterHandler("@Y", HandleCreateRoomPhase2, true);
            RegisterHandler("@X", HandleModifyRoom, true);
            RegisterHandler("BX", HandleTriggerRoomModify, true);
            RegisterHandler("BY", HandleEditRoomCategory, true);
            RegisterHandler("@W", HandleDeleteRoom, true);
            RegisterHandler("BZ", HandleWhoIsInRoom, true);
            RegisterHandler("@u", HandleLeaveRoom, true);
            RegisterHandler("Bv", HandleRoomAdvertisement, true);
            RegisterHandler("@B", HandleEnterRoom, true);
            RegisterHandler("@v", HandleEnterRoomTeleporter, true);
            RegisterHandler("@y", HandleCheckRoomAccess, true);
            RegisterHandler("Ab", HandleAnswerDoorbell, true);
            RegisterHandler("@{", HandleEnterRoomData, true);
            RegisterHandler("A~", HandleGetRoomAd, true);
            RegisterHandler("@|", HandleGetRoomClass, true);
            RegisterHandler("@}", HandleGetRoomItems, true);
            RegisterHandler("@~", HandleGetRoomGroupBadges, true);
            RegisterHandler("@\x7F", HandleGetWallItems, true);
            RegisterHandler("A@", HandleAddUserToRoom, true);
            RegisterHandler("CH", HandleModTool, true);
            RegisterHandler("Cm", HandleSendCFHMessage, true);
            RegisterHandler("Cn", HandleDeleteCFHMessage, true);
            RegisterHandler("AV", HandleSubmitCFHMessage, true);
            RegisterHandler("CG", HandleReplyCFH, true);
            RegisterHandler("CF", HandleDeleteCFH, true);
            RegisterHandler("@p", HandlePickupCFH, true);
            RegisterHandler("EC", HandleGoToCFHRoom, true);
            RegisterHandler("cI", HandleIgnoreUser, true);
            RegisterHandler("cK", HandleUnignoreUser, true);
            RegisterHandler("AO", HandleRotateUser, true);
            RegisterHandler("AK", HandleWalkTo, true);
            RegisterHandler("As", HandleClickDoor, true);
            RegisterHandler("At", HandleSelectSwimmingOutfit, true);
            RegisterHandler("B^", HandleSwitchBadge, true);
            RegisterHandler("DG", HandleGetTags, true);
            RegisterHandler("Cg", HandleGetGroupBadgeDetails, true);
            RegisterHandler("D\x7F", HandleIgnoreUser2, true);
            RegisterHandler("EB", HandleUnignoreUser2, true);
            RegisterHandler("AX", HandleStopStatus, true);
            RegisterHandler("A^", HandleWave, true);
            RegisterHandler("A]", HandleDance, true);
            RegisterHandler("AP", HandleCarryItem, true);
            RegisterHandler("Cl", HandleAnswerPoll, true);
            RegisterHandler("@t", HandleSay, true);
            RegisterHandler("@w", HandleShout, true);
            RegisterHandler("@x", HandleWhisper, true);
            RegisterHandler("D}", HandleShowSpeechBubble, true);
            RegisterHandler("D~", HandleHideSpeechBubble, true);
            RegisterHandler("A`", HandleGiveRights, true);
            RegisterHandler("Aa", HandleTakeRights, true);
            RegisterHandler("A_", HandleKickUser, true);
            RegisterHandler("E@", HandleKickAndBan, true);
            RegisterHandler("DE", HandleVoteRoom, true);
            RegisterHandler("Ae", HandleOpenCatalogue, true);
            RegisterHandler("Af", HandleOpenCataloguePage, true);
            RegisterHandler("Ad", HandlePurchaseItem, true);
            RegisterHandler("Ca", HandleRecyclerProceed, true);
            RegisterHandler("Cb", HandleRecyclerRedeem, true);
            RegisterHandler("AA", HandleHand, true);
            RegisterHandler("LB", HandleHand2, true);
            RegisterHandler("AB", HandleApplyWallpaper, true);
            RegisterHandler("AZ", HandlePlaceItem, true);
            RegisterHandler("AC", HandlePickupItem, true);
            RegisterHandler("AI", HandleMoveItem, true);
            RegisterHandler("CV", HandleToggleWallItem, true);
            RegisterHandler("AJ", HandleToggleFloorItem, true);
            RegisterHandler("AN", HandleOpenPresent, true);
            RegisterHandler("Bw", HandleRedeemCreditItem, true);
            RegisterHandler("AQ", HandleEnterTeleporter, true);
            RegisterHandler("AM", HandleCloseDice, true);
            RegisterHandler("AL", HandleSpinDice, true);
            RegisterHandler("Cw", HandleSpinWheel, true);
            RegisterHandler("Dz", HandleActivateLoveShuffler, true);
            RegisterHandler("AS", HandleOpenStickie, true);
            RegisterHandler("AT", HandleEditStickie, true);
            RegisterHandler("AU", HandleDeleteStickie, true);
            RegisterHandler("Ah", HandleLidoVoting, true);
            RegisterHandler("Ct", HandleInitializeSoundMachine, true);
            RegisterHandler("Cu", HandleEnterRoomPlaylist, true);
            RegisterHandler("C]", HandleGetSongData, true);
            RegisterHandler("Cs", HandleSavePlaylist, true);
            RegisterHandler("C~", HandleBurnSong, true);
            RegisterHandler("Cx", HandleDeleteSong, true);
            RegisterHandler("Co", HandleInitializeSongEditor, true);
            RegisterHandler("C[", HandleAddSoundSet, true);
            RegisterHandler("Cp", HandleSaveNewSong, true);
            RegisterHandler("Cq", HandleRequestEditSong, true);
            RegisterHandler("Cr", HandleSaveEditedSong, true);
            RegisterHandler("AG", HandleStartTrading, true);
            RegisterHandler("AH", HandleOfferTradeItem, true);
            RegisterHandler("AD", HandleDeclineTrade, true);
            RegisterHandler("AE", HandleAcceptTrade, true);
            RegisterHandler("AF", HandleAbortTrade, true);
            RegisterHandler("B_", HandleRefreshGameList, true);
            RegisterHandler("B`", HandleCheckoutGame, true);
            RegisterHandler("Bb", HandleRequestNewGame, true);
            RegisterHandler("Bc", HandleProcessNewGame, true);
            RegisterHandler("Be", HandleSwitchTeam, true);
            RegisterHandler("Bg", HandleLeaveGame, true);
            RegisterHandler("Bh", HandleKickPlayer, true);
            RegisterHandler("Bj", HandleStartGame, true);
            RegisterHandler("Bk", HandleGameMove, true);
            RegisterHandler("Bl", HandleGameRestart, true);
            RegisterHandler("EW", HandleToggleMoodlight, true);
            RegisterHandler("EU", HandleLoadMoodlight, true);
            RegisterHandler("EV", HandleUpdateDimmer, true);
        }

        /// <summary>
        /// Registers a packet handler.
        /// </summary>
        private static void RegisterHandler(string header, PacketHandlerDelegate handler, bool requiresLogin)
        {
            _packetHandlers[header] = new PacketHandler(header, handler, requiresLogin);
        }
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
                    sendData("@c" + banReason);
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
        /// Sends a packet built with PacketBuilder to the client.
        /// </summary>
        /// <param name="builder">The PacketBuilder instance containing the packet data.</param>
        internal void sendData(PacketBuilder builder)
        {
            if (builder != null)
                sendData(builder.Build());
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

        #region Packet processing
        /// <summary>
        /// Processes a single packet from the client.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        private void processPacket(string currentPacket)
        {
            Out.WriteSpecialLine(currentPacket.Replace(Convert.ToChar(13).ToString(), "{13}"), Out.logFlags.MehAction, ConsoleColor.DarkGray, ConsoleColor.DarkYellow, "< [" + Thread.GetDomainID() + "]", 2, ConsoleColor.Blue);
            
            if (string.IsNullOrEmpty(currentPacket) || currentPacket.Length < 2)
            {
                Disconnect();
                return;
            }

            string header = currentPacket.Substring(0, 2);
            
            if (!_packetHandlers.TryGetValue(header, out PacketHandler handler))
            {
                Disconnect();
                return;
            }

            // Check if login is required
            if (handler.RequiresLogin && !_isLoggedIn)
            {
                Disconnect();
                return;
            }

            // Create packet reader (skip header - first 2 characters)
            string packetData = currentPacket.Length > 2 ? currentPacket.Substring(2) : string.Empty;
            PacketReader reader = new PacketReader(packetData);
            
            // Execute handler
            try
            {
                handler.Handler(this, reader);
            }
            catch (Exception ex)
            {
                Out.WriteSpecialLine($"Error handling packet {header}: {ex.Message}", Out.logFlags.MehAction, ConsoleColor.Red, ConsoleColor.DarkRed, "ERROR", 2, ConsoleColor.Red);
                Disconnect();
            }
        }
      
        #endregion

        #region Packet Handlers
        // Non-logged in handlers
        private static void HandlePing(virtualUser user, PacketReader reader)
        {
            user.pingOK = true;
        }

        private static void HandleClientVersion(virtualUser user, PacketReader reader)
        {
            user.sendData("DUIH");
        }

        private static void HandleClientInfo(virtualUser user, PacketReader reader)
        {
            user.sendData(PacketBuilder.Create("DA")
                .AppendB64String("QBHHIIKHJIPAHQAdd-MM-yyyy")
                .AppendDelimiter()
                .AppendB64String("SAHPBhotel-co.uk")
                .AppendDelimiter()
                .Append("QBH"));
        }

        private static void HandleSSORequest(virtualUser user, PacketReader reader)
        {
            user.sendData(PacketBuilder.Create("DA")
                .AppendB64String("QBHIIIKHJIPAIQAdd-MM-yyyy")
                .AppendDelimiter()
                .AppendB64String("SAHPB/client")
                .AppendDelimiter()
                .Append("QBH")
                .Append("IJWVVVSNKQCFUBJASMSLKUUOJCOLJQPNSBIRSVQBRXZQOTGPMNJIHLVJCRRULBLUO")
                .AppendChar((char)1));
        }

        private static void HandleLogin(virtualUser user, PacketReader reader)
        {
            string ssoTicket = DB.Stripslash(reader.GetRemaining());
            int myID = _userDataAccess.GetUserIdBySsoTicket(ssoTicket);
            if (myID == 0)
            {
                user.Disconnect();
                return;
            }

            string banReason = userManager.getBanReason(myID);
            if (banReason != "")
            {
                user.sendData("@c" + banReason);
                user.Disconnect(1000);
                return;
            }

            user.userID = myID;
            var userData = _userDataAccess.GetUserBasicInfo(myID);
            if (userData == null)
            {
                user.Disconnect();
                return;
            }

            user._Username = userData.Name;
            user._Figure = userData.Figure;
            user._Sex = userData.Sex;
            user._Mission = userData.Mission;
            user._Rank = userData.Rank;
            user._consoleMission = userData.ConsoleMission;
            userManager.addUser(myID, user);
            user._isLoggedIn = true;

            user.sendData("@B" + rankManager.fuseRights(user._Rank));
            user.sendData("DbIH");
            user.sendData("@C");

            bool isguide = _userDataAccess.IsUserGuide(user.userID);
            if (isguide)
                user.sendData("BKguide");
            
            user.sendData("Fi" + "I");
            user.sendData("FC");

            if (Config.enableWelcomeMessage)
                user.sendData("BK" + stringManager.getString("welcomemessage_text"));

            var ignoredUsers = _ignoreDataAccess.GetIgnoredUserIds(user.userID);
            if (ignoredUsers.Count > 0)
            {
                PacketBuilder builder = PacketBuilder.Create("Fd").AppendVL64(ignoredUsers.Count);
                for (int x = 0; x < ignoredUsers.Count; x++)
                {
                    user.ignoreList.Add(ignoredUsers[x]);
                    builder.AppendInt(ignoredUsers[x]).AppendDelimiter();
                }
                user.sendData(builder);
            }
        }

        // Logged in handlers
        private static void HandleRequestDate(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.sendData("Bc" + DateTime.Today.ToShortDateString());
        }

        private static void HandleInitializeMessenger(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.Messenger = new Messenger.virtualMessenger(user.userID);
            user.sendData("@L" + user.Messenger.friendList());
            user.sendData("Dz" + user.Messenger.friendRequests());
        }

        private static void HandleInitializeClub(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.refreshClub();
        }

        private static void HandleRefreshAppearance(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.refreshAppearance(false, true, false);
        }

        private static void HandleRefreshValueables(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.refreshValueables(true, true);
        }

        private static void HandleRefreshBadges(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.refreshBadges();
        }

        private static void HandleRefreshGroupStatus(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.refreshGroupStatus();
        }

        private static void HandleRecyclerSetup(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.sendData("Do" + recyclerManager.setupString);
        }

        private static void HandleRecyclerSession(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.sendData("Dp" + recyclerManager.sessionString(user.userID));
        }

        private static void HandleSetGuideAvailable(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            _userDataAccess.SetGuideAvailability(user.userID, true);
        }

        private static void HandleSetGuideUnavailable(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            _userDataAccess.SetGuideAvailability(user.userID, false);
        }

        private static void HandleBuyGameTickets(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int amount = reader.PopVL64();
            string receiver = reader.PopString();
            
            int ticketAmount = 0;
            int price = 0;

            if (amount == 1)
            {
                ticketAmount = 2;
                price = 1;
            }
            else if (amount == 2)
            {
                ticketAmount = 20;
                price = 6;
            }
            else
                return;

            if (price > user._Credits)
            {
                user.sendData("AD");
                return;
            }

            int receiverID = _userDataAccess.GetUserIdByUsername(DB.Stripslash(receiver));
            if (receiverID <= 0)
            {
                user.sendData("AL" + receiver);
                return;
            }

            user._Credits -= price;
            user.sendData(PacketBuilder.Create("@F").AppendInt(user._Credits));
            _userDataAccess.UpdateUserCredits(user.userID, user._Credits);
            _userDataAccess.AddUserTickets(receiverID, ticketAmount);

            if (userManager.containsUser(receiverID))
            {
                virtualUser receiverUser = userManager.getUser(receiverID);
                receiverUser._Tickets += ticketAmount;

                if (receiverID == user.userID)
                    receiverUser.refreshValueables(false, true);
                else
                    receiverUser.refreshValueables(true, true);
            }
        }

        private static void HandleRedeemVoucher(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            string code = DB.Stripslash(reader.PopString());
            if (_voucherDataAccess.VoucherExists(code))
            {
                int voucherAmount = _voucherDataAccess.GetVoucherCredits(code);
                _voucherDataAccess.RedeemVoucher(code);

                user._Credits += voucherAmount;
                user.sendData(PacketBuilder.Create("@F").AppendInt(user._Credits));
                user.sendData("CT");
                _userDataAccess.UpdateUserCredits(user.userID, user._Credits);
            }
            else
            {
                user.sendData("CU1");
            }
        }

        private static void HandleSearchConsole(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.sendData("HR" + "L");
        }

        private static void HandleSearchInConsole(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null) return;

            string search = DB.Stripslash(reader.PopString());
            var searchResults = _userDataAccess.SearchUsersByName(search, 50);

            PacketBuilder friendsBuilder = PacketBuilder.Create("Fs");
            PacketBuilder othersBuilder = new PacketBuilder();
            int countFriends = 0;
            int countOthers = 0;

            foreach (var result in searchResults)
            {
                int thisID = result.UserId;
                bool online = userManager.containsUser(thisID);
                string onlineStr = online ? "I" : "H";

                PacketBuilder userBuilder = new PacketBuilder()
                    .AppendVL64(thisID)
                    .AppendString(result.Name)
                    .AppendString(result.Mission)
                    .Append(onlineStr).Append(onlineStr).AppendDelimiter()
                    .Append(onlineStr).Append(online ? result.Figure : "").AppendDelimiter()
                    .Append(online ? "" : result.LastVisit).AppendDelimiter();

                if (user.Messenger.hasFriendship(thisID))
                {
                    countFriends++;
                    friendsBuilder.Append(userBuilder.Build());
                }
                else
                {
                    countOthers++;
                    othersBuilder.Append(userBuilder.Build());
                }
            }

            PacketBuilder finalBuilder = PacketBuilder.Create("Fs")
                .AppendVL64(countFriends)
                .Append(friendsBuilder.Build().Substring(2)) // Skip "Fs" header
                .AppendVL64(countOthers)
                .Append(othersBuilder.Build());

            user.sendData(finalBuilder);
        }

        private static void HandleRequestFriend(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null) return;

            string username = DB.Stripslash(reader.PopString());
            int toID = _userDataAccess.GetUserIdByUsername(username);
            
            if (toID > 0 && !user.Messenger.hasFriendRequests(toID) && !user.Messenger.hasFriendship(toID))
            {
                int requestID = _messengerDataAccess.GetNextFriendRequestId(toID);
                _messengerDataAccess.CreateFriendRequest(toID, user.userID, requestID);
                
                virtualUser targetUser = userManager.getUser(toID);
                if (targetUser != null)
                {
                    targetUser.sendData(PacketBuilder.Create("BD")
                        .Append("I")
                        .AppendString(user._Username)
                        .AppendInt(user.userID)
                        .AppendDelimiter());
                }
            }
        }
        private static void HandleAcceptFriendRequest(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null) return;

            int amount = reader.PopVL64();
            if (amount <= 0) return;

            PacketBuilder updatesBuilder = new PacketBuilder();
            virtualBuddy me = new virtualBuddy(user.userID);
            int updateAmount = 0;

            for (int i = 0; i < amount; i++)
            {
                if (!reader.HasMore) break;

                int requestID = reader.PopVL64();
                int fromUserID = _messengerDataAccess.GetFriendRequestSenderId(user.userID, requestID);
                if (fromUserID == 0) continue;

                virtualBuddy buddy = new virtualBuddy(fromUserID);
                updatesBuilder.Append(buddy.ToString(false));
                updateAmount++;

                user.Messenger.addBuddy(buddy, true);
                if (userManager.containsUser(fromUserID))
                {
                    virtualUser fromUser = userManager.getUser(fromUserID);
                    if (fromUser != null && fromUser.Messenger != null)
                        fromUser.Messenger.addBuddy(me, true);
                }

                _messengerDataAccess.CreateFriendship(fromUserID, user.userID);
                _messengerDataAccess.DeleteFriendRequest(user.userID, requestID);
            }

            if (updateAmount > 0)
            {
                user.sendData(PacketBuilder.Create("@M")
                    .Append("HH")
                    .AppendVL64(updateAmount)
                    .Append(updatesBuilder.Build()));
            }
        }

        private static void HandleDeclineFriendRequest(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null) return;

            int amount = reader.PopVL64();
            for (int i = 0; i < amount; i++)
            {
                if (!reader.HasMore) break;
                int requestID = reader.PopVL64();
                _messengerDataAccess.DeleteFriendRequest(user.userID, requestID);
            }
        }

        private static void HandleRemoveBuddy(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null) return;

            reader.Skip(1); // Skip first character after header
            int buddyID = reader.PopVL64();
            
            user.Messenger.removeBuddy(buddyID);
            if (userManager.containsUser(buddyID))
            {
                virtualUser buddy = userManager.getUser(buddyID);
                if (buddy != null && buddy.Messenger != null)
                    buddy.Messenger.removeBuddy(user.userID);
            }
            _messengerDataAccess.DeleteFriendship(user.userID, buddyID);
        }

        private static void HandleSendInstantMessage(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null) return;

            int buddyID = reader.PopVL64();
            string message = reader.PopString();
            message = stringManager.filterSwearwords(message);

            if (user.Messenger.containsOnlineBuddy(buddyID))
            {
                virtualUser buddy = userManager.getUser(buddyID);
                if (buddy != null)
                {
                    buddy.sendData(PacketBuilder.Create("BF")
                        .AppendVL64(user.userID)
                        .AppendString(message));
                }
            }
            else
            {
                user.sendData(PacketBuilder.Create("DE")
                    .AppendVL64(5)
                    .AppendVL64(user.userID));
            }
        }

        private static void HandleRefreshFriendList(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null) return;
            user.sendData("@M" + user.Messenger.getUpdates());
        }
        private static void HandleFollowBuddy(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null) return;

            int id = reader.PopVL64();
            int errorID = -1;

            if (user.Messenger.hasFriendship(id))
            {
                if (userManager.containsUser(id))
                {
                    virtualUser targetUser = userManager.getUser(id);
                    if (targetUser._roomID > 0)
                    {
                        user.sendData(PacketBuilder.Create("D^")
                            .Append(targetUser._inPublicroom ? "I" : "H")
                            .AppendVL64(targetUser._roomID));
                        return;
                    }
                    else
                    {
                        errorID = 2;
                    }
                }
                else
                {
                    errorID = 1;
                }
            }
            else
            {
                errorID = 0;
            }

            if (errorID != -1)
            {
                user.sendData(PacketBuilder.Create("E]").AppendVL64(errorID));
            }
        }

        private static void HandleInviteBuddies(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Messenger == null || user.roomUser == null) return;

            int amount = reader.PopVL64();
            List<int> validIDs = new List<int>();

            for (int i = 0; i < amount; i++)
            {
                if (!reader.HasMore) break;
                int id = reader.PopVL64();
                if (user.Messenger.hasFriendship(id) && userManager.containsUser(id))
                    validIDs.Add(id);
            }

            foreach (int id in validIDs)
            {
                virtualUser buddy = userManager.getUser(id);
                if (buddy != null && user.roomUser != null && user.Room != null)
                {
                    string roomName = _roomDataAccess.GetRoomName(user._roomID);
                    buddy.sendData(PacketBuilder.Create("BG")
                        .AppendVL64(user.userID)
                        .AppendString(roomName + " - " + user._Username)
                        .AppendDelimiter());
                }
            }
        }
        private static void HandleNavigateRooms(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int hideFull = reader.PopVL64();
            int cataID = reader.PopVL64();
            
            string name = _roomCategoryDataAccess.GetCategoryName(cataID, user._Rank);
            if (string.IsNullOrEmpty(name))
                return;
            
            int type = _roomCategoryDataAccess.GetCategoryType(cataID);
            int parentID = _roomCategoryDataAccess.GetCategoryParentId(cataID);
            
            PacketBuilder navigator = PacketBuilder.Create(@"C\")
                .AppendVL64(hideFull)
                .AppendVL64(cataID)
                .AppendVL64(type)
                .AppendString(name)
                .AppendVL64(0)
                .AppendVL64(10000)
                .AppendVL64(parentID);
            
            string sqlOrderHelper = "";
            if (type == 0) // Publicrooms
            {
                sqlOrderHelper = hideFull == 1 
                    ? "AND visitors_now < visitors_max ORDER BY id ASC"
                    : "ORDER BY id ASC";
            }
            else // Guestrooms
            {
                sqlOrderHelper = hideFull == 1
                    ? "AND visitors_now < visitors_max ORDER BY visitors_now DESC LIMIT 30"
                    : "ORDER BY visitors_now DESC LIMIT " + Config.Navigator_openCategory_maxResults;
            }
            
            List<int> roomIDs = _roomDataAccess.GetRoomIdsByCategory(cataID, sqlOrderHelper);
            if (type == 2)
                navigator.AppendVL64(roomIDs.Count);
            
            if (roomIDs.Count > 0)
            {
                bool canSeeHiddenNames = false;
                List<int> roomStates = _roomDataAccess.GetRoomStatesByCategory(cataID, sqlOrderHelper);
                List<int> showNameFlags = _roomDataAccess.GetRoomShowNameFlagsByCategory(cataID, sqlOrderHelper);
                List<int> nowVisitors = _roomDataAccess.GetRoomVisitorsNowByCategory(cataID, sqlOrderHelper);
                List<int> maxVisitors = _roomDataAccess.GetRoomVisitorsMaxByCategory(cataID, sqlOrderHelper);
                List<string> roomNames = _roomDataAccess.GetRoomNamesByCategory(cataID, sqlOrderHelper);
                List<string> roomDescriptions = _roomDataAccess.GetRoomDescriptionsByCategory(cataID, sqlOrderHelper);
                List<string> roomOwners = _roomDataAccess.GetRoomOwnersByCategory(cataID, sqlOrderHelper);
                List<string> roomCCTs = null;
                
                if (type == 0)
                    roomCCTs = _roomDataAccess.GetRoomCCTsByCategory(cataID, sqlOrderHelper);
                else
                    canSeeHiddenNames = rankManager.containsRight(user._Rank, "fuse_enter_locked_rooms");
                
                for (int i = 0; i < roomIDs.Count; i++)
                {
                    if (type == 0) // Publicroom
                    {
                        navigator.AppendVL64(roomIDs[i])
                            .AppendVL64(1)
                            .AppendString(roomNames[i])
                            .AppendVL64(nowVisitors[i])
                            .AppendVL64(maxVisitors[i])
                            .AppendVL64(cataID)
                            .AppendString(roomDescriptions[i])
                            .AppendVL64(roomIDs[i])
                            .AppendVL64(0)
                            .AppendString(roomCCTs[i])
                            .Append("HI");
                    }
                    else // Guestroom
                    {
                        if (showNameFlags[i] == 0 && !canSeeHiddenNames)
                            continue;
                        navigator.AppendVL64(roomIDs[i])
                            .AppendString(roomNames[i])
                            .AppendString(roomOwners[i])
                            .AppendString(roomManager.getRoomState(roomStates[i]))
                            .AppendVL64(nowVisitors[i])
                            .AppendVL64(maxVisitors[i])
                            .AppendString(roomDescriptions[i]);
                    }
                }
            }
            
            List<int> subCataIDs = _roomCategoryDataAccess.GetSubCategoryIds(cataID, user._Rank);
            if (subCataIDs.Count > 0)
            {
                for (int i = 0; i < subCataIDs.Count; i++)
                {
                    int visitorCount = _roomDataAccess.GetTotalVisitorsNowByCategory(subCataIDs[i]);
                    int visitorMax = _roomDataAccess.GetTotalVisitorsMaxByCategory(subCataIDs[i]);
                    if (visitorMax > 0 && hideFull == 1 && visitorCount >= visitorMax)
                        continue;
                    
                    string subName = _roomCategoryDataAccess.GetSubCategoryName(subCataIDs[i]);
                    navigator.AppendVL64(subCataIDs[i])
                        .AppendVL64(0)
                        .AppendString(subName)
                        .AppendVL64(visitorCount)
                        .AppendVL64(visitorMax)
                        .AppendVL64(cataID);
                }
            }
            
            user.sendData(navigator);
        }

        private static void HandleRequestCategoryIndex(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            PacketBuilder categories = PacketBuilder.Create("C]");
            List<int> cataIDs = _roomCategoryDataAccess.GetCategoryIdsByTypeAndRank(2, user._Rank);
            List<string> cataNames = _roomCategoryDataAccess.GetCategoryNamesByTypeAndRank(2, user._Rank);
            
            categories.AppendVL64(cataIDs.Count);
            for (int i = 0; i < cataIDs.Count; i++)
            {
                categories.AppendVL64(cataIDs[i]).AppendString(cataNames[i]);
            }
            
            user.sendData(categories);
        }

        private static void HandleRefreshRecommendedRooms(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            PacketBuilder rooms = PacketBuilder.Create("E_").AppendVL64(3);
            for (int i = 0; i <= 3; i++)
            {
                var roomDetails = _roomDataAccess.GetRandomRoomWithOwner();
                if (roomDetails == null)
                    return;
                
                rooms.AppendVL64(roomDetails.RoomId)
                    .AppendString(roomDetails.Name)
                    .AppendString(roomDetails.Owner)
                    .AppendString(roomManager.getRoomState(roomDetails.State))
                    .AppendVL64(roomDetails.VisitorsNow)
                    .AppendVL64(roomDetails.VisitorsMax)
                    .AppendString(roomDetails.Description);
            }
            
            user.sendData(rooms);
        }

        private static void HandleViewOwnRooms(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            List<int> roomIDs = _roomDataAccess.GetRoomIdsByOwner(user._Username);
            if (roomIDs.Count > 0)
            {
                PacketBuilder rooms = PacketBuilder.Create("@P");
                for (int i = 0; i < roomIDs.Count; i++)
                {
                    var roomDetails = _roomDataAccess.GetRoomBasicInfo(roomIDs[i]);
                    if (roomDetails != null)
                    {
                        rooms.AppendInt(roomIDs[i]).AppendChar((char)9)
                            .Append(roomDetails.Name).AppendChar((char)9)
                            .Append(user._Username).AppendChar((char)9)
                            .Append(roomManager.getRoomState(roomDetails.State)).AppendChar((char)9)
                            .Append("x").AppendChar((char)9)
                            .AppendInt(roomDetails.VisitorsNow).AppendChar((char)9)
                            .AppendInt(roomDetails.VisitorsMax).AppendChar((char)9)
                            .Append("null").AppendChar((char)9)
                            .Append(roomDetails.Description).AppendChar((char)9)
                            .Append(roomDetails.Description).AppendChar((char)9)
                            .AppendChar((char)13);
                    }
                }
                user.sendData(rooms);
            }
            else
            {
                user.sendData("@y" + user._Username);
            }
        }

        private static void HandleSearchRooms(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            bool seeAllRoomOwners = rankManager.containsRight(user._Rank, "fuse_see_all_roomowners");
            string search = DB.Stripslash(reader.PopString());
            List<int> roomIDs = _roomDataAccess.SearchRooms(search, Config.Navigator_roomSearch_maxResults);
            
            if (roomIDs.Count > 0)
            {
                PacketBuilder rooms = PacketBuilder.Create("@w");
                for (int i = 0; i < roomIDs.Count; i++)
                {
                    var roomDetails = _roomDataAccess.GetRoomSearchInfo(roomIDs[i]);
                    if (roomDetails != null)
                    {
                        string ownerName = roomDetails.Owner;
                        if (roomDetails.ShowName == 0 && ownerName != user._Username && !seeAllRoomOwners)
                            ownerName = "-";
                        
                        rooms.AppendInt(roomIDs[i]).AppendChar((char)9)
                            .Append(roomDetails.Name).AppendChar((char)9)
                            .Append(ownerName).AppendChar((char)9)
                            .Append(roomManager.getRoomState(roomDetails.State)).AppendChar((char)9)
                            .Append("x").AppendChar((char)9)
                            .AppendInt(roomDetails.VisitorsNow).AppendChar((char)9)
                            .AppendInt(roomDetails.VisitorsMax).AppendChar((char)9)
                            .Append("null").AppendChar((char)9)
                            .Append(roomDetails.Description).AppendChar((char)9)
                            .AppendChar((char)13);
                    }
                }
                user.sendData(rooms);
            }
            else
            {
                user.sendData("@z");
            }
        }

        private static void HandleGetRoomDetails(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int roomID = reader.PopInt();
            var roomDetails = _roomDataAccess.GetRoomDetails(roomID);
            
            if (roomDetails != null)
            {
                PacketBuilder details = PacketBuilder.Create("@v")
                    .AppendVL64(int.Parse(roomDetails.SuperUsers))
                    .AppendVL64(roomDetails.State)
                    .AppendVL64(roomID);
                
                if (roomDetails.ShowName == 0 && rankManager.containsRight(user._Rank, "fuse_see_all_roomowners"))
                    details.Append("-");
                else
                    details.Append(roomDetails.Owner);
                
                details.AppendDelimiter()
                    .Append("model_").Append(roomDetails.Model).AppendDelimiter()
                    .Append(roomDetails.Name).AppendDelimiter()
                    .Append(roomDetails.Description).AppendDelimiter()
                    .AppendVL64(roomDetails.ShowName);
                
                if (_roomCategoryDataAccess.CategoryAllowsTrading(roomDetails.Category))
                    details.Append("I");
                else
                    details.Append("H");
                
                details.AppendVL64(roomDetails.VisitorsNow).AppendVL64(roomDetails.VisitorsMax);
                user.sendData(details);
            }
        }

        private static void HandleInitializeFavorites(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            List<int> roomIDs = _favoriteRoomDataAccess.GetFavoriteRoomIds(user.userID, Config.Navigator_Favourites_maxRooms);
            if (roomIDs.Count > 0)
            {
                int deletedAmount = 0;
                int guestRoomAmount = 0;
                bool seeHiddenRoomOwners = rankManager.containsRight(user._Rank, "fuse_enter_locked_rooms");
                StringBuilder roomsBuilder = new StringBuilder();
                
                for (int i = 0; i < roomIDs.Count; i++)
                {
                    var roomData = _roomDataAccess.GetRoomFavoriteInfo(roomIDs[i]);
                    if (roomData == null)
                    {
                        if (guestRoomAmount > 0)
                            deletedAmount++;
                        _favoriteRoomDataAccess.RemoveFavoriteRoom(user.userID, roomIDs[i]);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(roomData.Owner)) // Publicroom
                        {
                            int categoryID = _roomDataAccess.GetRoomCategory(roomIDs[i]);
                            List<string> cctsList = _roomDataAccess.GetRoomCCTsByCategory(categoryID);
                            string ccts = cctsList.Count > 0 ? cctsList[0] : string.Empty;
                            
                            roomsBuilder.Append(Encoding.encodeVL64(roomIDs[i]))
                                .Append("I")
                                .Append(roomData.Name).Append((char)2)
                                .Append(Encoding.encodeVL64(roomData.VisitorsNow))
                                .Append(Encoding.encodeVL64(roomData.VisitorsMax))
                                .Append(Encoding.encodeVL64(categoryID))
                                .Append(roomData.Description).Append((char)2)
                                .Append(Encoding.encodeVL64(roomIDs[i]))
                                .Append("H")
                                .Append(ccts).Append((char)2)
                                .Append("HI");
                        }
                        else // Guestroom
                        {
                            string ownerName = roomData.Owner;
                            if (roomData.ShowName == 0 && user._Username != ownerName && !seeHiddenRoomOwners)
                                ownerName = "-";
                            
                            roomsBuilder.Append(Encoding.encodeVL64(roomIDs[i]))
                                .Append(roomData.Name).Append((char)2)
                                .Append(ownerName).Append((char)2)
                                .Append(roomManager.getRoomState(roomData.State)).Append((char)2)
                                .Append(Encoding.encodeVL64(roomData.VisitorsNow))
                                .Append(Encoding.encodeVL64(roomData.VisitorsMax))
                                .Append(roomData.Description).Append((char)2);
                            guestRoomAmount++;
                        }
                    }
                }
                
                user.sendData("@}" + "HHJ" + (char)2 + "HHH" + Encoding.encodeVL64(guestRoomAmount - deletedAmount) + roomsBuilder.ToString());
            }
        }

        private static void HandleAddFavorite(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            reader.Skip(1); // Skip character after header
            int roomID = reader.PopVL64();
            
            if (_roomDataAccess.RoomExists(roomID) && !_favoriteRoomDataAccess.IsRoomInFavorites(user.userID, roomID))
            {
                if (_favoriteRoomDataAccess.GetFavoriteRoomCount(user.userID) < Config.Navigator_Favourites_maxRooms)
                    _favoriteRoomDataAccess.AddFavoriteRoom(user.userID, roomID);
                else
                    user.sendData("@a" + "nav_error_toomanyfavrooms");
            }
        }

        private static void HandleRemoveFavorite(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            reader.Skip(1); // Skip character after header
            int roomID = reader.PopVL64();
            _favoriteRoomDataAccess.RemoveFavoriteRoom(user.userID, roomID);
        }

        private static void HandleGetEventSetup(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.sendData(PacketBuilder.Create("Ep").AppendVL64(eventManager.categoryAmount));
        }

        private static void HandleToggleEventButton(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            if (user._inPublicroom || user.roomUser == null || user._hostsEvent)
                user.sendData("Eo" + "H");
            else
                user.sendData("Eo" + "I");
        }

        private static void HandleCheckEventCategory(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int categoryID = reader.PopVL64();
            if (eventManager.categoryOK(categoryID))
                user.sendData(PacketBuilder.Create("Eb").AppendVL64(categoryID));
        }

        private static void HandleOpenEventCategory(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int categoryID = reader.PopVL64();
            if (categoryID >= 1 && categoryID <= 11)
                user.sendData(PacketBuilder.Create("Eq").AppendVL64(categoryID).Append(eventManager.getEvents(categoryID)));
        }
        private static void HandleCreateEvent(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user._hostsEvent || user._inPublicroom || user.roomUser == null)
                return;
            
            int categoryID = reader.PopVL64();
            if (!eventManager.categoryOK(categoryID))
                return;
            
            string name = reader.PopB64String();
            string description = reader.PopString();
            
            user._hostsEvent = true;
            eventManager.createEvent(categoryID, user.userID, user._roomID, name, description);
            if (user.Room != null)
                user.Room.sendData("Er" + eventManager.getEvent(user._roomID));
        }

        private static void HandleEndEvent(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._hostsEvent || !user._isOwner || user._inPublicroom || user.roomUser == null)
                return;
            
            user._hostsEvent = false;
            eventManager.removeEvent(user._roomID);
            if (user.Room != null)
                user.Room.sendData("Er" + "-1");
        }
        private static void HandleCreateRoomPhase1(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            string remaining = reader.GetRemaining();
            string[] roomSettings = remaining.Split('/');
            
            if (_userDataAccess.GetUserRoomCount(user._Username) < Config.Navigator_createRoom_maxRooms)
            {
                roomSettings[2] = stringManager.filterSwearwords(roomSettings[2]);
                roomSettings[3] = roomSettings[3].Substring(6, 1);
                roomSettings[4] = roomManager.getRoomState(roomSettings[4]).ToString();
                if (roomSettings[5] != "0" && roomSettings[5] != "1")
                    return;
                
                _roomDataAccess.CreateRoom(DB.Stripslash(roomSettings[2]), user._Username, roomSettings[3], int.Parse(roomSettings[4]), int.Parse(roomSettings[5]));
                int roomID = _userDataAccess.GetMaxRoomIdByOwner(user._Username);
                user.sendData("@{" + roomID + (char)13 + roomSettings[2]);
            }
            else
            {
                user.sendData("@a" + "Error creating a private room");
            }
        }

        private static void HandleCreateRoomPhase2(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            string remaining = reader.GetRemaining();
            int roomID = 0;
            
            if (remaining.StartsWith("/"))
                roomID = int.Parse(remaining.Split('/')[1]);
            else
                roomID = int.Parse(remaining.Split('/')[0]);
            
            int superUsers = 0;
            int maxVisitors = 25;
            string roomDescription = "";
            string roomPassword = "";
            
            string[] packetContent = remaining.Split((char)13);
            for (int i = 1; i < packetContent.Length; i++)
            {
                string[] parts = packetContent[i].Split('=');
                if (parts.Length != 2) continue;
                
                string updHeader = parts[0];
                string updValue = parts[1];
                
                switch (updHeader)
                {
                    case "description":
                        roomDescription = stringManager.filterSwearwords(updValue);
                        roomDescription = DB.Stripslash(roomDescription);
                        break;
                    case "allsuperuser":
                        superUsers = int.Parse(updValue);
                        if (superUsers != 0 && superUsers != 1)
                            superUsers = 0;
                        break;
                    case "maxvisitors":
                        maxVisitors = int.Parse(updValue);
                        if (maxVisitors < 10 || maxVisitors > 25)
                            maxVisitors = 25;
                        break;
                    case "password":
                        roomPassword = DB.Stripslash(updValue);
                        break;
                    default:
                        return;
                }
            }
            
            _roomDataAccess.UpdateRoomSettings(roomID, roomDescription, superUsers.ToString(), maxVisitors, roomPassword, user._Username);
        }

        private static void HandleModifyRoom(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            string remaining = reader.GetRemaining();
            string[] packetContent = remaining.Split('/');
            if (packetContent.Length < 3) return;
            
            int roomID = int.Parse(packetContent[0]);
            string roomName = DB.Stripslash(stringManager.filterSwearwords(packetContent[1]));
            string showName = packetContent[2];
            if (showName != "1" && showName != "0")
                showName = "1";
            int roomState = roomManager.getRoomState(packetContent[2]);
            
            _roomDataAccess.UpdateRoomBasicInfo(roomID, roomName, roomState, int.Parse(showName), user._Username);
        }

        private static void HandleTriggerRoomModify(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int roomID = reader.PopVL64();
            int roomCategory = _roomDataAccess.GetRoomCategoryByOwner(roomID, user._Username);
            if (roomCategory > 0)
                user.sendData(PacketBuilder.Create("C^").AppendVL64(roomID).AppendVL64(roomCategory));
        }

        private static void HandleEditRoomCategory(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int roomID = reader.PopVL64();
            int cataID = reader.PopVL64();
            
            if (_roomCategoryDataAccess.IsCategoryValidForRank(cataID, 2, user._Rank))
                _roomDataAccess.UpdateRoomCategory(roomID, cataID, user._Username);
        }

        private static void HandleDeleteRoom(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int roomID = reader.PopInt();
            if (_roomDataAccess.UserOwnsRoom(roomID, user._Username))
            {
                _roomDataAccess.DeleteRoom(roomID, user._Username);
            }
            if (roomManager.containsRoom(roomID))
            {
                roomManager.getRoom(roomID).kickUsers(9, "This room has been deleted");
            }
        }

        private static void HandleWhoIsInRoom(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int roomID = reader.PopVL64();
            if (roomManager.containsRoom(roomID))
                user.sendData("C_" + roomManager.getRoom(roomID).Userlist);
            else
                user.sendData("C_");
        }
        private static void HandleLeaveRoom(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room != null && user.roomUser != null)
            {
                user.Room.removeUser(user.roomUser.roomUID, false, "");
            }
        }
        private static void HandleRoomAdvertisement(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            Config.Rooms_LoadAvertisement_img = "";
            if (string.IsNullOrEmpty(Config.Rooms_LoadAvertisement_img))
                user.sendData("DB0");
            else
                user.sendData(PacketBuilder.Create("DB")
                    .Append(Config.Rooms_LoadAvertisement_img)
                    .AppendChar((char)9)
                    .Append(Config.Rooms_LoadAvertisement_uri));
        }

        private static void HandleEnterRoom(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            char roomType = reader.PopChar();
            int roomID = reader.PopVL64();
            bool isPublicroom = (roomType == 'A');
            
            user.sendData("@S");
            user.sendData("Bf" + "http://wwww.vista4life.com/bf.php?p=emu");
            
            if (user.gamePlayer != null && user.gamePlayer.Game != null)
            {
                if (user.gamePlayer.enteringGame)
                {
                    if (user.Room != null && user.roomUser != null)
                        user.Room.removeUser(user.roomUser.roomUID, false, "");
                    user.sendData("AE" + user.gamePlayer.Game.Lobby.Type + "_arena_" + user.gamePlayer.Game.mapID + " " + roomID);
                    user.sendData("Cs" + user.gamePlayer.Game.getMap());
                }
                else
                {
                    user.leaveGame();
                }
            }
            else
            {
                if (user.Room != null && user.roomUser != null)
                    user.Room.removeUser(user.roomUser.roomUID, false, "");
                
                if (user._teleporterID == 0)
                {
                    bool allowEnterLockedRooms = rankManager.containsRight(user._Rank, "fuse_enter_locked_rooms");
                    int accessLevel = _roomDataAccess.GetRoomState(roomID);
                    if (accessLevel == 3 && !user._clubMember && !allowEnterLockedRooms)
                    {
                        user.sendData("C`" + "Kc");
                        return;
                    }
                    else if (accessLevel == 4 && !allowEnterLockedRooms)
                    {
                        user.sendData("BK" + stringManager.getString("room_stafflocked"));
                        return;
                    }
                    
                    int nowVisitors = _roomDataAccess.GetRoomVisitorsNow(roomID);
                    if (nowVisitors > 0)
                    {
                        int maxVisitors = _roomDataAccess.GetRoomVisitorsMax(roomID);
                        if (nowVisitors >= maxVisitors && !rankManager.containsRight(user._Rank, "fuse_enter_full_rooms"))
                        {
                            if (!isPublicroom)
                                user.sendData("C`" + "I");
                            else
                                user.sendData("BK" + stringManager.getString("room_full"));
                            return;
                        }
                    }
                }
                
                user._roomID = roomID;
                user._inPublicroom = isPublicroom;
                user._ROOMACCESS_PRIMARY_OK = true;
                
                if (isPublicroom)
                {
                    string roomModel = _roomDataAccess.GetRoomModel(roomID);
                    user.sendData("AE" + roomModel + " " + roomID);
                    user._ROOMACCESS_SECONDARY_OK = true;
                }
            }
        }

        private static void HandleEnterRoomTeleporter(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.sendData("@S");
        }
        private static void HandleCheckRoomAccess(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user._inPublicroom)
                return;
            
            user._isOwner = _roomDataAccess.UserOwnsRoom(user._roomID, user._Username);
            if (!user._isOwner)
                user._hasRights = _roomRightsDataAccess.UserHasRights(user._roomID, user.userID);
            if (!user._hasRights)
            {
                var roomDetails = _roomDataAccess.GetRoomDetails(user._roomID);
                user._hasRights = roomDetails != null && roomDetails.SuperUsers == "1";
            }
            
            if (user._teleporterID == 0 && !user._isOwner && !rankManager.containsRight(user._Rank, "fuse_enter_locked_rooms"))
            {
                int accessFlag = _roomDataAccess.GetRoomState(user._roomID);
                if (!user._ROOMACCESS_PRIMARY_OK && accessFlag != 2)
                    return;
                
                // Check for roombans
                if (_roomBanDataAccess.IsUserBannedFromRoom(user.userID, user._roomID))
                {
                    string banExpireDate = _roomBanDataAccess.GetBanExpirationDate(user.userID, user._roomID);
                    if (!string.IsNullOrEmpty(banExpireDate))
                    {
                        DateTime banExpireMoment = DateTime.Parse(banExpireDate);
                        if (DateTime.Compare(banExpireMoment, DateTime.Now) > 0)
                        {
                            user.sendData("C`" + "PA");
                            user.sendData("@R");
                            return;
                        }
                        else
                        {
                            _roomBanDataAccess.RemoveRoomBan(user._roomID, user.userID);
                        }
                    }
                }
                
                if (accessFlag == 1) // Doorbell
                {
                    if (!roomManager.containsRoom(user._roomID))
                    {
                        user.sendData("BC");
                        return;
                    }
                    else
                    {
                        roomManager.getRoom(user._roomID).sendDataToRights("A[" + user._Username);
                        user.sendData("A[");
                        return;
                    }
                }
                else if (accessFlag == 5) // Password
                {
                    string password = reader.PopString();
                    string roomPassword = _roomDataAccess.GetRoomPassword(user._roomID);
                    if (password != roomPassword)
                    {
                        user.sendData("C`" + "E");
                        user.sendData("@R");
                        return;
                    }
                }
            }
            
            user._ROOMACCESS_SECONDARY_OK = true;
        }

        private static void HandleAnswerDoorbell(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._hasRights && !rankManager.containsRight(user.roomUser.User._Rank, "fuse_enter_locked_rooms"))
                return;
            
            string ringer = reader.PopB64String();
            bool letIn = reader.PopBool();
            
            virtualUser ringerData = userManager.getUser(ringer);
            if (ringerData == null || ringerData._roomID != user._roomID)
                return;
            
            if (letIn)
            {
                ringerData._ROOMACCESS_SECONDARY_OK = true;
                user.Room.sendDataToRights(PacketBuilder.Create("@i").AppendString(ringer));
                ringerData.sendData("@i");
            }
            else
            {
                ringerData.sendData("BC");
                ringerData._roomID = 0;
                ringerData._inPublicroom = false;
                ringerData._ROOMACCESS_PRIMARY_OK = false;
                ringerData._ROOMACCESS_SECONDARY_OK = false;
                ringerData._isOwner = false;
                ringerData._hasRights = false;
                ringerData.Room = null;
                ringerData.roomUser = null;
            }
        }

        private static void HandleEnterRoomData(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._ROOMACCESS_SECONDARY_OK || user._inPublicroom)
                return;
            
            string model = "model_" + _roomDataAccess.GetRoomModel(user._roomID);
            user.sendData("AE" + model + " " + user._roomID);
            
            int wallpaper = _roomDataAccess.GetRoomWallpaper(user._roomID);
            int floor = _roomDataAccess.GetRoomFloor(user._roomID);
            string landscape = _roomDataAccess.GetRoomLandscape(user._roomID);
            
            user.sendData("@n" + "landscape/" + landscape);
            if (wallpaper > 0)
                user.sendData("@n" + "wallpaper/" + wallpaper);
            if (floor > 0)
                user.sendData("@n" + "floor/" + floor);
            
            if (!user._isOwner)
                user._isOwner = rankManager.containsRight(user._Rank, "fuse_any_room_controller");
            if (user._isOwner)
            {
                user._hasRights = true;
                user.sendData("@o");
            }
            if (user._hasRights)
                user.sendData("@j");
            
            int voteAmount = -1;
            if (_roomVoteDataAccess.HasUserVoted(user.userID, user._roomID))
            {
                voteAmount = _roomVoteDataAccess.GetRoomVoteSum(user._roomID);
                if (voteAmount < 0)
                    voteAmount = 0;
            }
            user.sendData(PacketBuilder.Create("EY").AppendVL64(voteAmount));
            user.sendData("Er" + eventManager.getEvent(user._roomID));
        }

        private static void HandleGetRoomAd(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            if (user._inPublicroom && _roomDataAccess.RoomHasAdvertisement(user._roomID))
            {
                var ad = _roomDataAccess.GetRoomAdvertisement(user._roomID);
                if (ad != null)
                    user.sendData(PacketBuilder.Create("CP")
                        .Append(ad.ImageUrl).AppendChar((char)9)
                        .Append(ad.Uri));
                else
                    user.sendData("CP0");
            }
            else
            {
                user.sendData("CP0");
            }
        }

        private static void HandleGetRoomClass(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._ROOMACCESS_SECONDARY_OK)
            {
                if (user.gamePlayer != null && user.gamePlayer.enteringGame && user.gamePlayer.teamID != -1 && user.gamePlayer.Game != null)
                {
                    user.sendData("@_" + user.gamePlayer.Game.Heightmap);
                    user.sendData("Cs" + user.gamePlayer.Game.getPlayers());
                }
                if (user.gamePlayer != null)
                    user.gamePlayer.enteringGame = false;
                return;
            }
            
            if (roomManager.containsRoom(user._roomID))
                user.Room = roomManager.getRoom(user._roomID);
            else
            {
                user.Room = new virtualRoom(user._roomID, user._inPublicroom);
                roomManager.addRoom(user._roomID, user.Room);
            }
            
            user.sendData("@_" + user.Room.Heightmap);
            user.sendData(@"@\" + user.Room.dynamicUnits);
        }

        private static void HandleGetRoomItems(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._ROOMACCESS_SECONDARY_OK || user.Room == null)
                return;
            
            user.sendData("@^" + user.Room.PublicroomItems);
            user.sendData("@`" + user.Room.Flooritems);
        }

        private static void HandleGetRoomGroupBadges(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._ROOMACCESS_SECONDARY_OK || user.Room == null)
                return;
            
            user.sendData("Du" + user.Room.Groups);
            if (user.Room.Lobby != null)
            {
                user.sendData(PacketBuilder.Create("Cg")
                    .Append("H")
                    .AppendString(user.Room.Lobby.Rank.Title)
                    .AppendVL64(user.Room.Lobby.Rank.minPoints)
                    .AppendVL64(user.Room.Lobby.Rank.maxPoints));
                user.sendData("Cz" + user.Room.Lobby.playerRanks);
            }
            user.sendData("DiH");
            
            if (!user._receivedSpriteIndex)
            {
                user._receivedSpriteIndex = true;
                // Send sprite index - this is a large hardcoded string from the legacy code
                user.sendData("Dg" + @"[SEshelves_norja" + (char)2 + "X~Dshelves_polyfon" + (char)2 + "YmAshelves_silo" + (char)2 + "XQHtable_polyfon_small" + (char)2 + "YmAchair_polyfon" + (char)2 + "ZbBtable_norja_med" + (char)2 + "Y_Itable_silo_med" + (char)2 + "X~Dtable_plasto_4leg" + (char)2 + "Y_Itable_plasto_round" + (char)2 + "Y_Itable_plasto_bigsquare" + (char)2 + "Y_Istand_polyfon_z" + (char)2 + "ZbBchair_silo" + (char)2 + "X~Dsofa_silo" + (char)2 + "X~Dcouch_norja" + (char)2 + "X~Dchair_norja" + (char)2 + "X~Dtable_polyfon_med" + (char)2 + "YmAdoormat_love" + (char)2 + "ZbBdoormat_plain" + (char)2 + "Z[Msofachair_polyfon" + (char)2 + "X~Dsofa_polyfon" + (char)2 + "Z[Msofachair_silo" + (char)2 + "X~Dchair_plasty" + (char)2 + "X~Dchair_plasto" + (char)2 + "YmAtable_plasto_square" + (char)2 + "Y_Ibed_polyfon" + (char)2 + "X~Dbed_polyfon_one" + (char)2 + "[dObed_trad_one" + (char)2 + "YmAbed_trad" + (char)2 + "YmAbed_silo_one" + (char)2 + "YmAbed_silo_two" + (char)2 + "YmAtable_silo_small" + (char)2 + "X~Dbed_armas_two" + (char)2 + "YmAbed_budget_one" + (char)2 + "XQHbed_budget" + (char)2 + "XQHshelves_armas" + (char)2 + "YmAbench_armas" + (char)2 + "YmAtable_armas" + (char)2 + "YmAsmall_table_armas" + (char)2 + "ZbBsmall_chair_armas" + (char)2 + "YmAfireplace_armas" + (char)2 + "YmAlamp_armas" + (char)2 + "YmAbed_armas_one" + (char)2 + "YmAcarpet_standard" + (char)2 + "Y_Icarpet_armas" + (char)2 + "YmAcarpet_polar" + (char)2 + "Y_Ifireplace_polyfon" + (char)2 + "Y_I");
            }
        }

        private static void HandleGetWallItems(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._ROOMACCESS_SECONDARY_OK || user.Room == null)
                return;
            
            user.sendData("@|" + user.Room.Wallitems);
        }

        private static void HandleAddUserToRoom(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._ROOMACCESS_SECONDARY_OK || user.Room == null || user.roomUser != null)
                return;
            
            user.sendData("@b" + user.Room.dynamicStatuses);
            user.Room.addUser(user);
        }
        private static void HandleModTool(virtualUser user, PacketReader reader) { if (!user._isLoggedIn) return; /* TODO: Implement */ }
        private static void HandleSendCFHMessage(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            var cfhStats = _helpDataAccess.GetPendingHelpRequest(user._Username);
            if (cfhStats == null)
                user.sendData("D\x7F" + "H");
            else
            {
                user.sendData(PacketBuilder.Create("D\x7F")
                    .Append("I")
                    .AppendInt(cfhStats.Id).AppendDelimiter()
                    .AppendString(cfhStats.Date)
                    .AppendString(cfhStats.Message));
            }
        }

        private static void HandleDeleteCFHMessage(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int cfhID = _helpDataAccess.GetPendingHelpRequestId(user._Username);
            _helpDataAccess.DeletePendingHelpRequest(user._Username);
            user.sendData("D\x7F" + "H");
            userManager.sendToRank(Config.Minimum_CFH_Rank, true, PacketBuilder.Create("BT")
                .AppendVL64(cfhID).AppendDelimiter()
                .Append("I").AppendString("User Deleted!").AppendString("User Deleted!").AppendString("User Deleted!")
                .AppendVL64(0).AppendDelimiter()
                .Append("").AppendDelimiter()
                .Append("H").AppendDelimiter()
                .AppendVL64(0));
        }

        private static void HandleSubmitCFHMessage(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (_helpDataAccess.HasPendingHelpRequest(user._Username))
                return;
            
            string cfhMessage = reader.PopB64String();
            if (string.IsNullOrEmpty(cfhMessage))
                return;
            
            string ipAddress = user.connectionSocket.RemoteEndPoint.ToString().Split(':')[0];
            _helpDataAccess.CreateHelpRequest(user._Username, ipAddress, DB.Stripslash(cfhMessage), DateTime.Now.ToString(), user._roomID);
            int cfhID = _helpDataAccess.GetPendingHelpRequestId(user._Username);
            string roomName = _roomDataAccess.GetRoomName(user._roomID);
            
            user.sendData("EAH");
            userManager.sendToRank(Config.Minimum_CFH_Rank, true, PacketBuilder.Create("BT")
                .AppendVL64(cfhID).AppendDelimiter()
                .Append("I").AppendString("Sent: " + DateTime.Now).AppendString(user._Username).AppendString(cfhMessage)
                .AppendVL64(user._roomID).AppendDelimiter()
                .Append(roomName).AppendDelimiter()
                .Append("I").AppendDelimiter()
                .AppendVL64(user._roomID));
        }

        private static void HandleReplyCFH(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!rankManager.containsRight(user._Rank, "fuse_receive_calls_for_help"))
                return;
            
            // Packet format: B64 length of ID, then VL64 ID, then reply message
            string cfhIDStr = reader.PopB64String();
            if (!int.TryParse(cfhIDStr, out int cfhID))
            {
                // Alternative format: skip B64 length, read VL64 directly
                reader.Reset().Skip(4);
                cfhID = reader.PopVL64();
            }
            
            string cfhReply = reader.PopString();
            
            string toUserName = _helpDataAccess.GetHelpRequestUsername(cfhID);
            if (string.IsNullOrEmpty(toUserName))
            {
                user.sendData("BK" + stringManager.getString("cfh_fail"));
                return;
            }
            
            int toUserID = userManager.getUserID(toUserName);
            virtualUser toVirtualUser = userManager.getUser(toUserID);
            if (toVirtualUser != null && toVirtualUser._isLoggedIn)
            {
                toVirtualUser.sendData(PacketBuilder.Create("DR").AppendString(cfhReply));
                _helpDataAccess.PickupHelpRequest(cfhID, user._Username);
            }
        }

        private static void HandleDeleteCFH(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!rankManager.containsRight(user._Rank, "fuse_receive_calls_for_help"))
                return;
            
            // Packet format: B64 length of ID, then VL64 ID
            reader.Skip(2); // Skip first B64 length indicator
            int cfhID = reader.PopVL64();
            
            var cfhStats = _helpDataAccess.GetHelpRequestDetails(cfhID);
            if (cfhStats == null)
                return;
            
            if (cfhStats.PickedUp == "1")
            {
                user.sendData("BK" + stringManager.getString("cfh_picked_up"));
            }
            else
            {
                _helpDataAccess.DeleteHelpRequest(cfhID);
                userManager.sendToRank(Config.Minimum_CFH_Rank, true, PacketBuilder.Create("BT")
                    .AppendVL64(cfhID).AppendDelimiter()
                    .Append("H").AppendString("Staff Deleted!").AppendString("Staff Deleted!").AppendString("Staff Deleted!")
                    .AppendVL64(0).AppendDelimiter()
                    .Append("").AppendDelimiter()
                    .Append("H").AppendDelimiter()
                    .AppendVL64(0));
            }
        }

        private static void HandlePickupCFH(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            reader.Skip(2); // Skip characters after header
            int cfhID = reader.PopVL64();
            
            if (!_helpDataAccess.HelpRequestExists(cfhID))
            {
                user.sendData(stringManager.getString("cfh_deleted"));
                return;
            }
            
            var cfhData = _helpDataAccess.GetHelpRequestDetails(cfhID);
            if (cfhData == null)
                return;
            
            string roomName = _roomDataAccess.GetRoomName(cfhData.RoomId);
            if (cfhData.PickedUp == "1")
            {
                user.sendData("BK" + stringManager.getString("cfh_picked_up"));
            }
            else
            {
                userManager.sendToRank(Config.Minimum_CFH_Rank, true, PacketBuilder.Create("BT")
                    .AppendVL64(cfhID).AppendDelimiter()
                    .Append("I").AppendString("Picked up: " + DateTime.Now).AppendString(cfhData.Username).AppendString(cfhData.Message)
                    .AppendVL64(cfhData.RoomId).AppendDelimiter()
                    .Append(roomName).AppendDelimiter()
                    .Append("I").AppendDelimiter()
                    .AppendVL64(cfhData.RoomId));
                _helpDataAccess.MarkHelpRequestAsPickedUp(cfhID);
            }
        }

        private static void HandleGoToCFHRoom(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!rankManager.containsRight(user._Rank, "fuse_receive_calls_for_help"))
                return;
            
            // Packet format: B64 length of ID, then VL64 ID
            reader.Skip(2); // Skip B64 length indicator
            int cfhID = reader.PopVL64();
            int roomID = _helpDataAccess.GetHelpRequestRoomId(cfhID);
            if (roomID == 0)
                return;
            
            virtualRoom room = roomManager.getRoom(roomID);
            if (room != null)
            {
                user.sendData(PacketBuilder.Create("D^")
                    .Append(room.isPublicroom ? "I" : "H")
                    .AppendVL64(roomID));
            }
        }
        private static void HandleIgnoreUser(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            try
            {
                reader.Skip(2); // Skip characters after header
                string username = DB.Stripslash(reader.PopString());
                virtualUser target = userManager.getUser(username);
                if (target != null && target._Rank <= 3)
                {
                    _ignoreDataAccess.AddIgnoredUser(user.userID, target.userID);
                    user.ignoreList.Add(target.userID);
                    user.sendData("FcI");
                }
            }
            catch { }
        }

        private static void HandleUnignoreUser(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            try
            {
                string username = DB.Stripslash(reader.PopString());
                virtualUser target = userManager.getUser(username);
                if (target != null && target._Rank <= 3)
                {
                    _ignoreDataAccess.RemoveIgnoredUser(user.userID, target.userID);
                    user.ignoreList.Remove(target.userID);
                    user.sendData("FcK");
                }
            }
            catch { }
        }
        private static void HandleRotateUser(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;

            int rotation = reader.PopInt();
            if (rotation >= 0 && rotation <= 7)
            {
                user.roomUser.Z2 = (byte)rotation;
                user.Room.sendData(PacketBuilder.Create("@\\").Append(user.roomUser.detailsString));
            }
        }

        private static void HandleWalkTo(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user.roomUser.walkLock) return;

            int x = reader.PopVL64();
            int y = reader.PopVL64();
            
            // Calculate max dimensions from heightmap
            if (user.Room != null && user.Room.Heightmap != null)
            {
                string[] heightmapLines = user.Room.Heightmap.Split((char)13);
                int maxX = heightmapLines[0].Length;
                int maxY = heightmapLines.Length - 1;
                
                if (x >= 0 && y >= 0 && x < maxX && y < maxY)
                {
                    user.roomUser.goalX = x;
                    user.roomUser.goalY = y;
                }
            }
        }
        private static void HandleClickDoor(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            
            user.roomUser.walkDoor = true;
            user.roomUser.goalX = user.Room.doorX;
            user.roomUser.goalY = user.Room.doorY;
        }

        private static void HandleSelectSwimmingOutfit(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || !user.Room.hasSwimmingPool) return;
            
            virtualRoom.squareTrigger trigger = user.Room.getTrigger(user.roomUser.X, user.roomUser.Y);
            if (trigger.Object == "curtains1" || trigger.Object == "curtains2")
            {
                string outfit = DB.Stripslash(reader.PopString());
                user.roomUser.SwimOutfit = outfit;
                user.Room.sendData(@"@\" + user.roomUser.detailsString);
                user.Room.sendSpecialCast(trigger.Object, "open");
                user.roomUser.walkLock = false;
                user.roomUser.goalX = trigger.goalX;
                user.roomUser.goalY = trigger.goalY;
                _userDataAccess.UpdateUserSwimOutfit(user.userID, outfit);
                virtualUser userObj = userManager.getUser(user.userID);
                if (userObj != null)
                    userObj.refreshAppearance(true, true, true);
            }
        }

        private static void HandleSwitchBadge(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            
            _badgeDataAccess.ResetUserBadgeSlots(user.userID);
            int enabledBadgeAmount = 0;
            
            while (reader.HasMore)
            {
                int slotID = reader.PopVL64();
                string badge = reader.PopB64String();
                
                if (!string.IsNullOrEmpty(badge))
                {
                    _badgeDataAccess.UpdateBadgeSlot(user.userID, badge, slotID);
                    enabledBadgeAmount++;
                }
            }
            
            user.refreshBadges();
            
            PacketBuilder notify = PacketBuilder.Create("Cd")
                .AppendInt(user.userID)
                .AppendDelimiter()
                .AppendVL64(enabledBadgeAmount);
            
            for (int x = 0; x < user._Badges.Count; x++)
            {
                if (user._badgeSlotIDs[x] > 0)
                {
                    notify.AppendVL64(user._badgeSlotIDs[x])
                        .Append(user._Badges[x])
                        .AppendDelimiter();
                }
            }
            
            user.Room.sendData(notify);
        }

        private static void HandleGetTags(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            int ownerID = reader.PopVL64();
            var tags = _tagDataAccess.GetUserTags(ownerID, 20);
            
            PacketBuilder list = PacketBuilder.Create("E^")
                .AppendVL64(ownerID)
                .AppendVL64(tags.Count);
            
            for (int i = 0; i < tags.Count; i++)
                list.AppendString(tags[i]);
            
            user.sendData(list);
        }

        private static void HandleGetGroupBadgeDetails(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            
            int groupID = reader.PopVL64();
            if (!_groupDataAccess.GroupExists(groupID))
                return;
            
            var groupDetails = _groupDataAccess.GetGroupDetails(groupID);
            if (groupDetails == null)
                return;
            
            int roomID = groupDetails.RoomId;
            string roomName = "";
            if (roomID > 0)
                roomName = _roomDataAccess.GetRoomName(roomID);
            else
                roomID = -1;
            
            user.sendData(PacketBuilder.Create("Dw")
                .AppendVL64(groupID)
                .AppendString(groupDetails.Name)
                .AppendString(groupDetails.Description)
                .AppendVL64(roomID)
                .AppendString(roomName));
        }
        private static void HandleIgnoreUser2(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            try
            {
                string username = DB.Stripslash(reader.PopString());
                virtualUser target = userManager.getUser(username);
                if (target != null && target._Rank <= 3)
                {
                    _ignoreDataAccess.AddIgnoredUser(user.userID, target.userID);
                    user.ignoreList.Add(target.userID);
                    user.sendData("FcI");
                }
            }
            catch { }
        }

        private static void HandleUnignoreUser2(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            try
            {
                string username = DB.Stripslash(reader.PopString());
                virtualUser target = userManager.getUser(username);
                if (target != null && target._Rank <= 3)
                {
                    _ignoreDataAccess.RemoveIgnoredUser(user.userID, target.userID);
                    user.ignoreList.Remove(target.userID);
                    user.sendData("FcK");
                }
            }
            catch { }
        }
        private static void HandleStopStatus(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.statusManager == null) return;

            string status = reader.PopString();
            if (status == "CarryItem")
                user.statusManager.dropCarrydItem();
            else if (status == "Dance")
            {
                user.statusManager.removeStatus("dance");
                user.statusManager.Refresh();
            }
        }

        private static void HandleWave(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            if (user.statusManager == null || user.statusManager.containsStatus("wave")) return;

            user.statusManager.removeStatus("dance");
            user.statusManager.handleStatus("wave", "", Config.Statuses_Wave_waveDuration);
        }

        private static void HandleDance(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            if (user.statusManager == null) return;

            if (user.statusManager.containsStatus("sit") || user.statusManager.containsStatus("lay"))
                return;

            user.statusManager.dropCarrydItem();
            
            if (!reader.HasMore)
            {
                user.statusManager.addStatus("dance", "");
            }
            else
            {
                if (!rankManager.containsRight(user._Rank, "fuse_use_club_dance"))
                    return;
                int danceID = reader.PopVL64();
                if (danceID < 0 || danceID > 4)
                    return;
                user.statusManager.addStatus("dance", danceID.ToString());
            }
            user.statusManager.Refresh();
        }

        private static void HandleCarryItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user.statusManager == null) return;

            string item = reader.PopString();
            if (user.statusManager.containsStatus("lay") || item.Contains("/"))
                return;

            try
            {
                int nItem = int.Parse(item);
                if (nItem < 1 || nItem > 26)
                    return;
            }
            catch
            {
                if (!user._inPublicroom && item != "Water" && item != "Milk" && item != "Juice")
                    return;
            }
            user.statusManager.carryItem(item);
        }
        private static void HandleAnswerPoll(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            
            int pollID = reader.PopVL64();
            if (_pollDataAccess.UserHasAnsweredPoll(user.userID, pollID))
                return;
            
            int questionID = reader.PopVL64();
            bool typeThree = _pollDataAccess.IsPollQuestionTypeThree(questionID);
            
            if (typeThree)
            {
                string answer = DB.Stripslash(reader.PopB64String());
                _pollDataAccess.CreatePollResultText(pollID, questionID, answer, user.userID);
            }
            else
            {
                int countAnswers = reader.PopVL64();
                for (int i = 0; i < countAnswers; i++)
                {
                    int answer = reader.PopVL64();
                    _pollDataAccess.CreatePollResultChoice(pollID, questionID, answer, user.userID);
                }
            }
        }
        private static void HandleSay(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            string message = reader.PopString();
            if (user._isMuted || (user.Room == null || user.roomUser == null) && (user.Room == null && user.gamePlayer == null))
                return;

            userManager.addChatMessage(user._Username, user._roomID, message);
            message = stringManager.filterSwearwords(message);

            if (message.Length > 0 && message[0] == ':' && user.isSpeechCommand(message.Substring(1)))
            {
                if (user.roomUser != null)
                {
                    if (user.roomUser.isTyping)
                    {
                        user.Room.sendData(PacketBuilder.Create("FO").AppendVL64(user.roomUser.roomUID).Append("H"));
                        user.roomUser.isTyping = false;
                    }
                }
                else if (user.gamePlayer != null)
                {
                    user.gamePlayer.Game.sendData(PacketBuilder.Create("FO").AppendVL64(user.gamePlayer.roomUID).Append("H"));
                }
            }
            else
            {
                if (user.gamePlayer == null && user.Room != null && user.roomUser != null)
                    user.Room.sendSaying(user.roomUser, message);
            }
        }

        private static void HandleShout(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            string message = reader.PopString();
            if (user._isMuted || (user.Room == null || user.roomUser == null) && (user.Room == null && user.gamePlayer == null))
                return;

            userManager.addChatMessage(user._Username, user._roomID, message);
            message = stringManager.filterSwearwords(message);

            if (message.Length > 0 && message[0] == ':' && user.isSpeechCommand(message.Substring(1)))
            {
                if (user.roomUser != null)
                {
                    if (user.roomUser.isTyping)
                    {
                        user.Room.sendData(PacketBuilder.Create("FO").AppendVL64(user.roomUser.roomUID).Append("H"));
                        user.roomUser.isTyping = false;
                    }
                }
                else if (user.gamePlayer != null)
                {
                    user.gamePlayer.Game.sendData(PacketBuilder.Create("FO").AppendVL64(user.gamePlayer.roomUID).Append("H"));
                }
            }
            else
            {
                if (user.gamePlayer == null && user.Room != null && user.roomUser != null)
                    user.Room.sendShout(user.roomUser, message);
                else if (user.gamePlayer != null)
                {
                    user.gamePlayer.Game.sendData(PacketBuilder.Create("Ei")
                        .AppendVL64(user.gamePlayer.roomUID)
                        .Append("H")
                        .AppendChar((char)1)
                        .Append("@Z")
                        .AppendVL64(user.gamePlayer.roomUID)
                        .AppendString(message));
                }
            }
        }

        private static void HandleWhisper(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._isMuted) return;

            string remaining = reader.GetRemaining();
            string[] parts = remaining.Split(' ');
            if (parts.Length < 2) return;

            string receiver = parts[0];
            string message = remaining.Substring(receiver.Length + 1);

            if (receiver == "" && message.Length > 0 && message[0] == ':' && user.isSpeechCommand(message.Substring(1)))
            {
                if (user.roomUser.isTyping)
                {
                    user.Room.sendData(PacketBuilder.Create("FO").AppendVL64(user.roomUser.roomUID).Append("H"));
                    user.roomUser.isTyping = false;
                }
            }
            else
            {
                userManager.addChatMessage(user._Username, user._roomID, message);
                message = stringManager.filterSwearwords(message);
                user.Room.sendWhisper(user.roomUser, receiver, message);
            }
        }

        private static void HandleShowSpeechBubble(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            if (!user.roomUser.isTyping)
            {
                user.roomUser.isTyping = true;
                user.Room.sendData(PacketBuilder.Create("FO").AppendVL64(user.roomUser.roomUID).Append("I"));
            }
        }

        private static void HandleHideSpeechBubble(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            if (user.roomUser.isTyping)
            {
                user.roomUser.isTyping = false;
                user.Room.sendData(PacketBuilder.Create("FO").AppendVL64(user.roomUser.roomUID).Append("H"));
            }
        }
        private static void HandleGiveRights(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._inPublicroom || !user._isOwner)
                return;
            
            string target = reader.PopString();
            if (!userManager.containsUser(target))
                return;
            
            virtualUser targetUser = userManager.getUser(target);
            if (targetUser._roomID != user._roomID || targetUser._hasRights || targetUser._isOwner)
                return;
            
            _roomRightsDataAccess.GrantRoomRights(user._roomID, targetUser.userID);
            targetUser._hasRights = true;
            targetUser.statusManager.addStatus("flatctrl", "onlyfurniture");
            targetUser.roomUser.Refresh();
            targetUser.sendData("@j");
        }

        private static void HandleTakeRights(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._inPublicroom || !user._isOwner)
                return;
            
            string target = reader.PopString();
            if (!userManager.containsUser(target))
                return;
            
            virtualUser targetUser = userManager.getUser(target);
            if (targetUser._roomID != user._roomID || !targetUser._hasRights || targetUser._isOwner)
                return;
            
            _roomRightsDataAccess.RevokeRoomRights(user._roomID, targetUser.userID);
            targetUser._hasRights = false;
            targetUser.statusManager.removeStatus("flatctrl");
            targetUser.roomUser.Refresh();
            targetUser.sendData("@k");
        }

        private static void HandleKickUser(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._inPublicroom || !user._hasRights)
                return;
            
            string target = reader.PopString();
            if (!userManager.containsUser(target))
                return;
            
            virtualUser targetUser = userManager.getUser(target);
            if (targetUser._roomID != user._roomID)
                return;
            
            if (targetUser._isOwner && (targetUser._Rank > user._Rank || rankManager.containsRight(targetUser._Rank, "fuse_any_room_controller")))
                return;
            
            targetUser.roomUser.walkLock = true;
            targetUser.roomUser.walkDoor = true;
            targetUser.roomUser.goalX = user.Room.doorX;
            targetUser.roomUser.goalY = user.Room.doorY;
        }

        private static void HandleKickAndBan(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._hasRights || user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            string target = reader.PopString();
            if (!userManager.containsUser(target))
                return;
            
            virtualUser targetUser = userManager.getUser(target);
            if (targetUser._roomID != user._roomID)
                return;
            
            if (targetUser._isOwner && (targetUser._Rank > user._Rank || rankManager.containsRight(targetUser._Rank, "fuse_any_room_controller")))
                return;
            
            string banExpireMoment = DateTime.Now.AddMinutes(Config.Rooms_roomBan_banDuration).ToString();
            _roomBanDataAccess.CreateRoomBan(user._roomID, targetUser.userID, banExpireMoment);
            
            targetUser.roomUser.walkLock = true;
            targetUser.roomUser.walkDoor = true;
            targetUser.roomUser.goalX = user.Room.doorX;
            targetUser.roomUser.goalY = user.Room.doorY;
        }

        private static void HandleVoteRoom(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            int vote = reader.PopVL64();
            if ((vote == 1 || vote == -1) && !_roomVoteDataAccess.HasUserVoted(user.userID, user._roomID))
            {
                _roomVoteDataAccess.CreateRoomVote(user.userID, user._roomID, vote);
                int voteAmount = _roomVoteDataAccess.GetRoomVoteSum(user._roomID);
                if (voteAmount < 0)
                    voteAmount = 0;
                user.roomUser.hasVoted = true;
                if (user._isOwner)
                    user.Room.sendNewVoteAmount(voteAmount);
                user.sendData(PacketBuilder.Create("EY").AppendVL64(voteAmount));
                user.sendData("Er" + eventManager.getEvent(user._roomID));
            }
        }
        private static void HandleOpenCatalogue(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.sendData("A~" + catalogueManager.getPageIndex(user._Rank));
        }

        private static void HandleOpenCataloguePage(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            string remaining = reader.GetRemaining();
            string pageIndexName = remaining.Split('/')[0];
            user.sendData("A\x7F" + catalogueManager.getPage(pageIndexName, user._Rank));
        }
        private static void HandlePurchaseItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            
            string remaining = reader.GetRemaining();
            string[] packetContent = remaining.Split((char)13);
            if (packetContent.Length < 4) return;
            
            string page = packetContent[1];
            string item = packetContent[3];
            int pageID = _catalogueDataAccess.GetPageIdByIndexName(DB.Stripslash(page), user._Rank);
            int templateID = _catalogueDataAccess.GetTemplateIdByItemName(DB.Stripslash(item));
            int cost = _catalogueDataAccess.GetCatalogueCost(pageID, templateID);
            
            if (cost == 0 || cost > user._Credits)
            {
                user.sendData("AD");
                return;
            }
            
            int receiverID = user.userID;
            int presentBoxID = 0;
            int roomID = 0;
            
            if (packetContent.Length > 5 && packetContent[5] == "1") // Purchased as present
            {
                string receiverName = packetContent[6];
                if (receiverName != user._Username)
                {
                    int receiverUserId = _userDataAccess.GetUserIdByUsername(DB.Stripslash(receiverName));
                    if (receiverUserId > 0)
                        receiverID = receiverUserId;
                    else
                    {
                        user.sendData("AL" + receiverName);
                        return;
                    }
                }
                
                string boxSprite = "present_gen" + new Random().Next(1, 7);
                int boxTemplateID = _catalogueDataAccess.GetTemplateIdByItemName(boxSprite);
                string boxNote = DB.Stripslash(stringManager.filterSwearwords(packetContent[7]));
                _furnitureDataAccess.CreateFurnitureItem(boxTemplateID, receiverID, null, "!" + boxNote);
                presentBoxID = catalogueManager.lastItemID;
                roomID = -1;
            }
            
            user._Credits -= cost;
            user.sendData(PacketBuilder.Create("@F").AppendInt(user._Credits));
            _userDataAccess.UpdateUserCredits(user.userID, user._Credits);
            
            if (stringManager.getStringPart(item, 0, 4) == "deal")
            {
                int dealID = int.Parse(item.Substring(4));
                List<int> itemIDs = _catalogueDataAccess.GetDealTemplateIds(dealID);
                List<int> itemAmounts = _catalogueDataAccess.GetDealItemAmounts(dealID);
                
                for (int i = 0; i < itemIDs.Count; i++)
                    for (int j = 1; j <= itemAmounts[i]; j++)
                    {
                        _furnitureDataAccess.CreateFurnitureItem(itemIDs[i], receiverID, roomID);
                        catalogueManager.handlePurchase(itemIDs[i], receiverID, roomID, 0, presentBoxID);
                    }
            }
            else
            {
                _furnitureDataAccess.CreateFurnitureItem(templateID, receiverID, roomID);
                var template = catalogueManager.getTemplate(templateID);
                if (template.Sprite == "wallpaper" || template.Sprite == "floor" || template.Sprite.Contains("landscape"))
                {
                    string decorID = packetContent[4];
                    catalogueManager.handlePurchase(templateID, receiverID, 0, decorID, presentBoxID);
                }
                else if (stringManager.getStringPart(item, 0, 11) == "prizetrophy" || stringManager.getStringPart(item, 0, 11) == "greektrophy")
                {
                    string inscription = DB.Stripslash(stringManager.filterSwearwords(packetContent[4]));
                    string itemVariable = user._Username + "\t" + DateTime.Today.ToShortDateString().Replace('/', '-') + "\t" + packetContent[4];
                    _furnitureDataAccess.UpdateFurnitureVariable(catalogueManager.lastItemID, itemVariable);
                    catalogueManager.handlePurchase(templateID, receiverID, 0, "0", presentBoxID);
                }
                else
                {
                    catalogueManager.handlePurchase(templateID, receiverID, roomID, "0", presentBoxID);
                }
            }
            
            if (receiverID == user.userID)
                user.refreshHand("last");
            else if (userManager.containsUser(receiverID))
            {
                virtualUser receiver = userManager.getUser(receiverID);
                if (receiver != null)
                    receiver.refreshHand("last");
            }
        }
        private static void HandleRecyclerProceed(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!Config.enableRecycler || user.Room == null || recyclerManager.sessionExists(user.userID))
                return;
            
            int itemCount = reader.PopVL64();
            if (!recyclerManager.rewardExists(itemCount))
                return;
            
            recyclerManager.createSession(user.userID, itemCount);
            
            for (int i = 0; i < itemCount; i++)
            {
                if (!reader.HasMore) break;
                
                int itemID = reader.PopVL64();
                if (_furnitureDataAccess.UserHasInventoryItems(user.userID))
                {
                    _furnitureDataAccess.UpdateFurnitureRoomId(itemID, -2);
                }
                else
                {
                    recyclerManager.dropSession(user.userID, true);
                    user.sendData("DpH");
                    return;
                }
            }
            
            user.sendData("Dp" + recyclerManager.sessionString(user.userID));
            user.refreshHand("update");
        }

        private static void HandleRecyclerRedeem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!Config.enableRecycler || (user.Room != null && !recyclerManager.sessionExists(user.userID)))
                return;
            
            bool redeem = reader.PopBool();
            if (redeem && recyclerManager.sessionReady(user.userID))
                recyclerManager.rewardSession(user.userID);
            
            recyclerManager.dropSession(user.userID, redeem);
            user.sendData("Dp" + recyclerManager.sessionString(user.userID));
            
            if (redeem)
                user.refreshHand("last");
            else
                user.refreshHand("new");
        }

        private static void HandleHand(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            
            string mode = reader.PopString();
            user.refreshHand(mode);
        }

        private static void HandleHand2(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            
            string mode = reader.PopString();
            user.refreshHand(mode);
        }

        private static void HandleApplyWallpaper(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._hasRights || user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            string remaining = reader.GetRemaining();
            string[] parts = remaining.Split('/');
            if (parts.Length < 2) return;
            
            string decorType = parts[0];
            int itemID = int.Parse(parts[1]);
            
            if (decorType != "wallpaper" && decorType != "floor" && decorType != "landscape")
                return;
            
            int templateID = _furnitureDataAccess.GetInventoryItemTemplateId(itemID, user.userID);
            if (templateID == 0 || catalogueManager.getTemplate(templateID).Sprite != decorType)
                return;
            
            string decorVal = _furnitureDataAccess.GetFurnitureVariable(itemID);
            if (decorType == "wallpaper")
                _roomDataAccess.UpdateRoomWallpaper(user._roomID, decorVal);
            else if (decorType == "floor")
                _roomDataAccess.UpdateRoomFloor(user._roomID, decorVal);
            else if (decorType == "landscape")
                _roomDataAccess.UpdateRoomLandscape(user._roomID, decorVal);
            
            user.Room.sendData("@n" + decorType + "/" + decorVal);
            _furnitureDataAccess.DeleteFurnitureItem(itemID);
        }

        private static void HandlePlaceItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._hasRights || user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            string remaining = reader.GetRemaining();
            string[] parts = remaining.Split(' ');
            if (parts.Length < 1) return;
            
            int itemID = int.Parse(parts[0]);
            int templateID = _furnitureDataAccess.GetInventoryItemTemplateId(itemID, user.userID);
            if (templateID == 0)
                return;
            
            var template = catalogueManager.getTemplate(templateID);
            if (template.typeID == 0) // Wall item
            {
                string inputPos = remaining.Substring(itemID.ToString().Length + 3);
                string checkedPos = catalogueManager.wallPositionOK(inputPos);
                if (checkedPos != inputPos)
                    return;
                
                string var = _furnitureDataAccess.GetFurnitureVariable(itemID);
                if (stringManager.getStringPart(template.Sprite, 0, 7) == "post.it")
                {
                    if (int.Parse(var) > 1)
                        _furnitureDataAccess.DecrementFurnitureVariable(itemID);
                    else
                        _furnitureDataAccess.DeleteFurnitureItem(itemID);
                    
                    _furnitureDataAccess.CreateFurnitureItem(templateID, user.userID);
                    itemID = catalogueManager.lastItemID;
                    _furnitureDataAccess.CreateStickyNote(itemID);
                    var = "FFFF33";
                    _furnitureDataAccess.UpdateFurnitureVariable(itemID, var);
                }
                user.Room.wallItemManager.addItem(itemID, templateID, checkedPos, var, true);
            }
            else // Floor item
            {
                if (parts.Length < 4) return;
                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);
                byte z = byte.Parse(parts[3]);
                byte typeID = template.typeID;
                user.Room.floorItemManager.placeItem(itemID, templateID, x, y, typeID, z);
            }
        }

        private static void HandlePickupItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            string remaining = reader.GetRemaining();
            int itemID = int.Parse(remaining.Contains(" ") ? remaining.Split(' ')[0] : remaining);
            
            if (user.Room.floorItemManager.containsItem(itemID))
            {
                user.Room.floorItemManager.removeItem(itemID, user.userID);
                _furnitureDataAccess.UpdateFurnitureRoomId(itemID, 0);
                user.refreshHand("new");
            }
            else if (user.Room.wallItemManager.containsItem(itemID))
            {
                user.Room.wallItemManager.removeItem(itemID, user.userID);
                _furnitureDataAccess.UpdateFurnitureRoomId(itemID, 0);
                user.refreshHand("new");
            }
        }
        private static void HandleMoveItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._hasRights || user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            string remaining = reader.GetRemaining();
            string[] parts = remaining.Split(' ');
            if (parts.Length < 4) return;
            
            int itemID = int.Parse(parts[0]);
            if (user.Room.floorItemManager.containsItem(itemID))
            {
                int x = int.Parse(parts[1]);
                int y = int.Parse(parts[2]);
                byte z = byte.Parse(parts[3]);
                user.Room.floorItemManager.relocateItem(itemID, x, y, z);
            }
        }

        private static void HandleToggleWallItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            // Read B64 length, then itemID as string and parse
            string itemIDStr = reader.PopB64String();
            if (!int.TryParse(itemIDStr, out int itemID))
                return;
            
            int toStatus = reader.PopVL64();
            user.Room.wallItemManager.toggleItemStatus(itemID, toStatus);
        }

        private static void HandleToggleFloorItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            try
            {
                // Read B64 length, then itemID as string and parse
                string itemIDStr = reader.PopB64String();
                if (!int.TryParse(itemIDStr, out int itemID))
                    return;
                
                string toStatus = DB.Stripslash(reader.PopString());
                if (user.Room != null)
                    user.Room.floorItemManager.toggleItemStatus(itemID, toStatus, user._hasRights);
            }
            catch { }
        }

        private static void HandleOpenPresent(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            int itemID = reader.PopInt();
            if (!user.Room.floorItemManager.containsItem(itemID))
                return;
            
            List<int> itemIDs = _furnitureDataAccess.GetPresentItemIds(itemID);
            if (itemIDs.Count > 0)
            {
                _furnitureDataAccess.UpdatePresentItemsToInventory(itemIDs);
                user.Room.floorItemManager.removeItem(itemID, 0);
                
                int lastItemTID = _furnitureDataAccess.GetFurnitureTemplateId(itemIDs[itemIDs.Count - 1]);
                catalogueManager.itemTemplate template = catalogueManager.getTemplate(lastItemTID);
                
                if (template.typeID > 0)
                {
                    user.sendData(PacketBuilder.Create("BA")
                        .Append(template.Sprite).AppendChar((char)13)
                        .Append(template.Sprite).AppendChar((char)13)
                        .AppendInt(template.Length).AppendChar((char)30)
                        .AppendInt(template.Width).AppendChar((char)30)
                        .Append(template.Colour));
                }
                else
                {
                    user.sendData(PacketBuilder.Create("BA")
                        .Append(template.Sprite).AppendChar((char)13)
                        .Append(template.Sprite).Append(" ").Append(template.Colour).AppendChar((char)13));
                }
            }
            
            _furnitureDataAccess.DeleteFurniturePresent(itemID, itemIDs.Count);
            _furnitureDataAccess.DeleteFurnitureItem(itemID);
            user.refreshHand("last");
        }

        private static void HandleRedeemCreditItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            int itemID = reader.PopVL64();
            if (!user.Room.floorItemManager.containsItem(itemID))
                return;
            
            string sprite = user.Room.floorItemManager.getItem(itemID).Sprite;
            if ((sprite.Length < 3 || sprite.Substring(0, 3).ToLower() != "cf_") && 
                (sprite.Length < 4 || sprite.Substring(0, 4).ToLower() != "cfc_"))
                return;
            
            int redeemValue = 0;
            try
            {
                string[] parts = sprite.Split('_');
                if (parts.Length >= 2)
                    redeemValue = int.Parse(parts[1]);
            }
            catch { return; }
            
            if (redeemValue > 0)
            {
                user._Credits += redeemValue;
                user.sendData(PacketBuilder.Create("@F").AppendInt(user._Credits));
                _userDataAccess.UpdateUserCredits(user.userID, user._Credits);
                user.Room.floorItemManager.removeItem(itemID, 0);
                _furnitureDataAccess.DeleteFurnitureItem(itemID);
            }
        }
        private static void HandleEnterTeleporter(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user._inPublicroom || user.Room == null || user.roomUser == null)
                return;
            
            int itemID = reader.PopInt();
            if (!user.Room.floorItemManager.containsItem(itemID))
                return;
            
            Rooms.Items.floorItem teleporter = user.Room.floorItemManager.getItem(itemID);
            // Prevent clientside 'jumps' to teleporter
            if (teleporter.Z == 2 && user.roomUser.X != teleporter.X + 1 && user.roomUser.Y != teleporter.Y)
                return;
            else if (teleporter.Z == 4 && user.roomUser.X != teleporter.X && user.roomUser.Y != teleporter.Y + 1)
                return;
            
            user.roomUser.goalX = -1;
            user.Room.moveUser(user.roomUser, teleporter.X, teleporter.Y, true);
        }

        private static void HandleCloseDice(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._inPublicroom)
                return;
            
            int itemID = reader.PopInt();
            if (!user.Room.floorItemManager.containsItem(itemID))
                return;
            
            Rooms.Items.floorItem item = user.Room.floorItemManager.getItem(itemID);
            string sprite = item.Sprite;
            if (sprite != "edice" && sprite != "edicehc")
                return;
            
            if (!(Math.Abs(user.roomUser.X - item.X) > 1 || Math.Abs(user.roomUser.Y - item.Y) > 1))
            {
                item.Var = "0";
                user.Room.sendData("AZ" + itemID + " " + (itemID * 38));
                _furnitureDataAccess.UpdateFurnitureVariable(itemID, "0");
            }
        }

        private static void HandleSpinDice(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._inPublicroom)
                return;
            
            int itemID = reader.PopInt();
            if (!user.Room.floorItemManager.containsItem(itemID))
                return;
            
            Rooms.Items.floorItem item = user.Room.floorItemManager.getItem(itemID);
            string sprite = item.Sprite;
            if (sprite != "edice" && sprite != "edicehc")
                return;
            
            if (Math.Abs(user.roomUser.X - item.X) > 1 || Math.Abs(user.roomUser.Y - item.Y) > 1)
                return;
            
            Random random = new Random();
            int result = random.Next(1, 7);
            string var = result.ToString();
            item.Var = var;
            user.Room.sendData("AZ" + itemID + " " + (itemID * 38) + " " + var);
            _furnitureDataAccess.UpdateFurnitureVariable(itemID, var);
        }

        private static void HandleSpinWheel(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._hasRights || user.Room == null || user.roomUser == null || user._inPublicroom)
                return;
            
            int itemID = reader.PopVL64();
            if (!user.Room.wallItemManager.containsItem(itemID))
                return;
            
            Rooms.Items.wallItem item = user.Room.wallItemManager.getItem(itemID);
            if (item.Sprite == "habbowheel")
            {
                int rndNum = new Random(DateTime.Now.Millisecond).Next(0, 10);
                user.Room.sendData(PacketBuilder.Create("AU")
                    .AppendInt(itemID).AppendChar((char)9)
                    .Append("habbowheel").AppendChar((char)9)
                    .Append(" ").Append(item.wallPosition).AppendChar((char)9)
                    .Append("-1"));
                user.Room.sendData(PacketBuilder.Create("AU")
                    .AppendInt(itemID).AppendChar((char)9)
                    .Append("habbowheel").AppendChar((char)9)
                    .Append(" ").Append(item.wallPosition).AppendChar((char)9)
                    .AppendInt(rndNum), 4250);
                _furnitureDataAccess.UpdateFurnitureVariable(itemID, rndNum.ToString());
            }
        }
        private static void HandleActivateLoveShuffler(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._inPublicroom)
                return;
            
            int itemID = reader.PopVL64();
            string message = reader.PopString();
            
            if (user.Room.floorItemManager.containsItem(itemID))
            {
                Rooms.Items.floorItem item = user.Room.floorItemManager.getItem(itemID);
                if (item.Sprite == "val_randomizer")
                {
                    int rndNum = new Random(DateTime.Now.Millisecond).Next(1, 5);
                    user.Room.sendData(PacketBuilder.Create("AX")
                        .AppendInt(itemID).AppendDelimiter()
                        .Append("123456789").AppendDelimiter());
                    user.Room.sendData(PacketBuilder.Create("AX")
                        .AppendInt(itemID).AppendDelimiter()
                        .AppendInt(rndNum).AppendDelimiter(), 5000);
                    _furnitureDataAccess.UpdateFurnitureVariable(itemID, rndNum.ToString());
                }
            }
        }

        private static void HandleOpenStickie(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._inPublicroom)
                return;
            
            int itemID = reader.PopInt();
            string message = reader.PopString();
            
            if (user.Room.wallItemManager.containsItem(itemID))
            {
                message = _furnitureDataAccess.GetStickyNoteText(itemID);
                string colour = _furnitureDataAccess.GetFurnitureVariable(itemID);
                user.sendData(PacketBuilder.Create("@p")
                    .AppendInt(itemID).AppendChar((char)9)
                    .Append(colour).Append(" ")
                    .Append(message));
            }
        }

        private static void HandleEditStickie(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._hasRights || user.Room == null || user.roomUser == null || user._inPublicroom)
                return;
            
            string remaining = reader.GetRemaining();
            int slashIndex = remaining.IndexOf('/');
            if (slashIndex == -1) return;
            
            int itemID = int.Parse(remaining.Substring(0, slashIndex));
            if (!user.Room.wallItemManager.containsItem(itemID))
                return;
            
            Rooms.Items.wallItem item = user.Room.wallItemManager.getItem(itemID);
            string sprite = item.Sprite;
            if (sprite != "post.it" && sprite != "post.it.vd")
                return;
            
            string colour = "FFFFFF"; // Valentine stickie default colour
            if (sprite == "post.it") // Normal stickie
            {
                if (remaining.Length < slashIndex + 7) return;
                colour = remaining.Substring(slashIndex + 1, 6);
                if (colour != "FFFF33" && colour != "FF9CFF" && colour != "9CFF9C" && colour != "9CCEFF")
                    return;
            }
            
            string message = remaining.Substring(slashIndex + 7);
            if (message.Length > 684)
                return;
            
            if (colour != item.Var)
                _furnitureDataAccess.UpdateFurnitureVariable(itemID, colour);
            item.Var = colour;
            
            user.Room.sendData(PacketBuilder.Create("AU")
                .AppendInt(itemID).AppendChar((char)9)
                .Append(sprite).AppendChar((char)9)
                .Append(" ").Append(item.wallPosition).AppendChar((char)9)
                .Append(colour));
            
            message = DB.Stripslash(stringManager.filterSwearwords(message)).Replace("/r", ((char)13).ToString());
            _furnitureDataAccess.UpdateStickyNoteText(itemID, message);
        }

        private static void HandleDeleteStickie(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user.Room == null || user.roomUser == null || user._inPublicroom)
                return;
            
            int itemID = reader.PopInt();
            if (user.Room.wallItemManager.containsItem(itemID))
            {
                Rooms.Items.wallItem item = user.Room.wallItemManager.getItem(itemID);
                if (stringManager.getStringPart(item.Sprite, 0, 7) == "post.it")
                {
                    user.Room.wallItemManager.removeItem(itemID, 0);
                    _furnitureDataAccess.DeleteStickyNote(itemID);
                }
            }
        }

        private static void HandleLidoVoting(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null) return;
            if (user.statusManager == null) return;
            
            if (user.statusManager.containsStatus("sit") || user.statusManager.containsStatus("lay"))
                return;
            
            if (!reader.HasMore)
            {
                user.statusManager.addStatus("sign", "");
            }
            else
            {
                string signID = reader.PopString();
                user.statusManager.handleStatus("sign", signID, Config.Statuses_Wave_waveDuration);
            }
            user.statusManager.Refresh();
        }
        private static void HandleInitializeSoundMachine(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            user.sendData("EB" + soundMachineManager.getMachineSongList(user.Room.floorItemManager.soundMachineID));
        }

        private static void HandleEnterRoomPlaylist(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            user.sendData("EC" + soundMachineManager.getMachinePlaylist(user.Room.floorItemManager.soundMachineID));
        }

        private static void HandleGetSongData(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            int songID = reader.PopVL64();
            user.sendData("Dl" + soundMachineManager.getSong(songID));
        }
        private static void HandleSavePlaylist(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            int amount = reader.PopVL64();
            if (amount >= 6) return; // Max playlist size
            
            _soundMachineDataAccess.DeleteAllPlaylistsForMachine(user.Room.floorItemManager.soundMachineID);
            for (int i = 0; i < amount; i++)
            {
                if (!reader.HasMore) break;
                int songID = reader.PopVL64();
                _soundMachineDataAccess.AddSongToPlaylist(user.Room.floorItemManager.soundMachineID, songID, i);
            }
            
            // Refresh playlist
            user.Room.sendData("EC" + soundMachineManager.getMachinePlaylist(user.Room.floorItemManager.soundMachineID));
        }

        private static void HandleBurnSong(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            int songID = reader.PopVL64();
            if (user._Credits <= 0 || !_soundMachineDataAccess.SongExists(songID, user.userID, user.Room.floorItemManager.soundMachineID))
            {
                user.sendData("AD");
                return;
            }
            
            var songData = _soundMachineDataAccess.GetSongDetails(songID);
            if (songData != null)
            {
                int length = songData.Length;
                string status = Encoding.encodeVL64(songID) + user._Username + (char)10 + DateTime.Today.Day + (char)10 + 
                               DateTime.Today.Month + (char)10 + DateTime.Today.Year + (char)10 + length + (char)10 + songData.Title;
                
                _furnitureDataAccess.CreateFurnitureItem(Config.Soundmachine_burnToDisk_diskTemplateID, user.userID, null, status);
                _soundMachineDataAccess.MarkSongAsBurnt(songID);
                _userDataAccess.DeductUserCredits(user.userID, 1);
                
                user._Credits--;
                user.sendData(PacketBuilder.Create("@F").AppendInt(user._Credits));
                user.sendData("EB" + soundMachineManager.getMachineSongList(user.Room.floorItemManager.soundMachineID));
                user.refreshHand("last");
            }
            else
            {
                user.sendData("AD");
            }
        }

        private static void HandleDeleteSong(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            int songID = reader.PopVL64();
            if (_soundMachineDataAccess.SongExistsForMachine(songID, user.Room.floorItemManager.soundMachineID))
            {
                _soundMachineDataAccess.RemoveSongFromMachineIfBurnt(songID);
                _soundMachineDataAccess.DeleteUnburntSong(songID);
                _soundMachineDataAccess.DeleteSongFromPlaylist(user.Room.floorItemManager.soundMachineID, songID);
                user.Room.sendData("EC" + soundMachineManager.getMachinePlaylist(user.Room.floorItemManager.soundMachineID));
            }
        }

        private static void HandleInitializeSongEditor(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            user.songEditor = new virtualSongEditor(user.Room.floorItemManager.soundMachineID, user.userID);
            user.songEditor.loadSoundsets();
            user.sendData("Dm" + user.songEditor.getSoundsets());
            user.sendData("Dn" + soundMachineManager.getHandSoundsets(user.userID));
        }

        private static void HandleAddSoundSet(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.songEditor == null || !user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            int soundSetID = reader.PopVL64();
            int slotID = reader.PopVL64();
            
            if (slotID > 0 && slotID < 5 && user.songEditor.slotFree(slotID))
            {
                user.songEditor.addSoundset(soundSetID, slotID);
                user.sendData("Dn" + soundMachineManager.getHandSoundsets(user.userID));
                user.sendData("Dm" + user.songEditor.getSoundsets());
            }
        }
        private static void HandleSaveNewSong(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.songEditor == null || !user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            string title = reader.PopB64String();
            string data = reader.PopString();
            int length = soundMachineManager.calculateSongLength(data);
            
            if (length != -1)
            {
                title = DB.Stripslash(stringManager.filterSwearwords(title));
                data = DB.Stripslash(data);
                _soundMachineDataAccess.CreateSong(user.userID, user.Room.floorItemManager.soundMachineID, title, length, DB.Stripslash(data));
                
                user.sendData("EB" + soundMachineManager.getMachineSongList(user.Room.floorItemManager.soundMachineID));
                user.sendData(PacketBuilder.Create("EK")
                    .AppendVL64(user.Room.floorItemManager.soundMachineID)
                    .AppendString(title));
            }
        }

        private static void HandleRequestEditSong(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            int songID = reader.PopVL64();
            user.sendData("Dl" + soundMachineManager.getSong(songID));
            
            user.songEditor = new virtualSongEditor(user.Room.floorItemManager.soundMachineID, user.userID);
            user.songEditor.loadSoundsets();
            user.sendData("Dm" + user.songEditor.getSoundsets());
            user.sendData("Dn" + soundMachineManager.getHandSoundsets(user.userID));
        }

        private static void HandleSaveEditedSong(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.songEditor == null || !user._isOwner || user.Room == null || user.Room.floorItemManager.soundMachineID <= 0)
                return;
            
            int songID = reader.PopVL64();
            if (_soundMachineDataAccess.SongExists(songID, user.userID, user.Room.floorItemManager.soundMachineID))
            {
                string title = reader.PopB64String();
                string data = reader.PopString();
                int length = soundMachineManager.calculateSongLength(data);
                
                if (length != -1)
                {
                    title = DB.Stripslash(stringManager.filterSwearwords(title));
                    data = DB.Stripslash(data);
                    _soundMachineDataAccess.UpdateSong(songID, title, data, length);
                    
                    user.sendData("ES");
                    user.sendData("EB" + soundMachineManager.getMachineSongList(user.Room.floorItemManager.soundMachineID));
                    user.Room.sendData("EC" + soundMachineManager.getMachinePlaylist(user.Room.floorItemManager.soundMachineID));
                }
            }
        }
        private static void HandleStartTrading(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._tradePartnerRoomUID != -1)
                return;
            
            if (!Config.enableTrading)
            {
                user.sendData("BK" + stringManager.getString("trading_disabled"));
                return;
            }
            
            int partnerUID = reader.PopInt();
            if (!user.Room.containsUser(partnerUID))
                return;
            
            virtualUser partner = user.Room.getUser(partnerUID);
            if (partner.statusManager.containsStatus("trd"))
                return;
            
            user._tradePartnerRoomUID = partnerUID;
            user.statusManager.addStatus("trd", "");
            user.roomUser.Refresh();
            
            partner._tradePartnerRoomUID = user.roomUser.roomUID;
            partner.statusManager.addStatus("trd", "");
            partner.roomUser.Refresh();
            
            user.refreshTradeBoxes();
            partner.refreshTradeBoxes();
        }

        private static void HandleOfferTradeItem(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._tradePartnerRoomUID == -1)
                return;
            
            if (!user.Room.containsUser(user._tradePartnerRoomUID))
                return;
            
            int itemID = reader.PopInt();
            int templateID = _furnitureDataAccess.GetInventoryItemTemplateId(itemID, user.userID);
            if (templateID == 0)
                return;
            
            user._tradeItems[user._tradeItemCount] = itemID;
            user._tradeItemCount++;
            virtualUser partner = user.Room.getUser(user._tradePartnerRoomUID);
            
            user._tradeAccept = false;
            partner._tradeAccept = false;
            
            user.refreshTradeBoxes();
            partner.refreshTradeBoxes();
        }

        private static void HandleDeclineTrade(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._tradePartnerRoomUID == -1)
                return;
            
            if (!user.Room.containsUser(user._tradePartnerRoomUID))
                return;
            
            virtualUser partner = user.Room.getUser(user._tradePartnerRoomUID);
            user._tradeAccept = false;
            partner._tradeAccept = false;
            user.refreshTradeBoxes();
            partner.refreshTradeBoxes();
        }

        private static void HandleAcceptTrade(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._tradePartnerRoomUID == -1)
                return;
            
            if (!user.Room.containsUser(user._tradePartnerRoomUID))
                return;
            
            virtualUser partner = user.Room.getUser(user._tradePartnerRoomUID);
            user._tradeAccept = true;
            user.refreshTradeBoxes();
            partner.refreshTradeBoxes();
            
            if (partner._tradeAccept)
            {
                // Swap items
                for (int i = 0; i < user._tradeItemCount; i++)
                {
                    if (user._tradeItems[i] > 0)
                        _furnitureDataAccess.UpdateFurnitureOwnerToInventory(user._tradeItems[i], partner.userID);
                }
                
                for (int i = 0; i < partner._tradeItemCount; i++)
                {
                    if (partner._tradeItems[i] > 0)
                        _furnitureDataAccess.UpdateFurnitureOwnerToInventory(partner._tradeItems[i], user.userID);
                }
                
                user.abortTrade();
            }
        }

        private static void HandleAbortTrade(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user._tradePartnerRoomUID == -1)
                return;
            
            if (user.Room.containsUser(user._tradePartnerRoomUID))
            {
                user.abortTrade();
                user.refreshHand("update");
            }
        }
        private static void HandleRefreshGameList(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.Room.Lobby == null)
                return;
            user.sendData("Ch" + user.Room.Lobby.gameList());
        }

        private static void HandleCheckoutGame(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user.Room.Lobby == null || user.gamePlayer != null)
                return;
            
            int gameID = reader.PopVL64();
            if (user.Room.Lobby.Games.ContainsKey(gameID))
            {
                user.gamePlayer = new gamePlayer(user, user.roomUser.roomUID, (Game)user.Room.Lobby.Games[gameID]);
                user.gamePlayer.Game.Subviewers.Add(user.gamePlayer);
                user.sendData("Ci" + user.gamePlayer.Game.Sub);
            }
        }

        private static void HandleRequestNewGame(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user.Room.Lobby == null || user.gamePlayer != null)
                return;
            
            if (user._Tickets <= 1)
            {
                user.sendData("Cl" + "J"); // Error [2] = Not enough tickets
                return;
            }
            
            if (!user.Room.Lobby.validGamerank(user.roomUser.gamePoints))
            {
                user.sendData("Cl" + "K"); // Error [3] = Skillevel not valid in this lobby
                return;
            }
            
            if (user.Room.Lobby.isBattleBall)
                user.sendData("Ck" + user.Room.Lobby.getCreateGameSettings());
            else
                user.sendData(PacketBuilder.Create("Ck")
                    .Append("RA")
                    .AppendString("secondsUntilRestart")
                    .Append("HIRGIHH")
                    .AppendString("fieldType")
                    .Append("HKIIIISA")
                    .AppendString("numTeams")
                    .Append("HJJIII")
                    .Append("PA")
                    .AppendString("gameLengthChoice")
                    .Append("HJIIIIK")
                    .AppendString("name")
                    .Append("IJ").AppendDelimiter().Append("H")
                    .AppendString("secondsUntilStart")
                    .Append("HIRBIHH"));
        }
        private static void HandleProcessNewGame(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.roomUser == null || user.Room.Lobby == null || user.gamePlayer != null)
                return;
            
            if (user._Tickets <= 1)
            {
                user.sendData("Cl" + "J"); // Error [2] = Not enough tickets
                return;
            }
            
            if (!user.Room.Lobby.validGamerank(user.roomUser.gamePoints))
            {
                user.sendData("Cl" + "K"); // Error [3] = Skillevel not valid in this lobby
                return;
            }
            
            try
            {
                int mapID = -1;
                int teamAmount = -1;
                int[] powerups = null;
                string name = "";
                
                int keyAmount = reader.PopVL64();
                for (int i = 0; i < keyAmount; i++)
                {
                    if (!reader.HasMore) break;
                    
                    string key = reader.PopB64String();
                    char valueType = reader.PopChar();
                    
                    if (valueType == 'H') // VL64 value
                    {
                        int value = reader.PopVL64();
                        switch (key)
                        {
                            case "fieldType":
                                mapID = value;
                                break;
                            case "numTeams":
                                teamAmount = value;
                                break;
                        }
                    }
                    else // B64 value (string)
                    {
                        string value = reader.PopB64String();
                        switch (key)
                        {
                            case "allowedPowerups":
                                string[] ps = value.Split(',');
                                powerups = new int[ps.Length];
                                for (int p = 0; p < ps.Length; p++)
                                {
                                    int powerupID = int.Parse(ps[p]);
                                    if (user.Room.Lobby.allowsPowerup(powerupID))
                                        powerups[p] = powerupID;
                                    else
                                        return;
                                }
                                break;
                            case "name":
                                name = stringManager.filterSwearwords(value);
                                break;
                        }
                    }
                }
                
                if (mapID == -1 || teamAmount == -1 || name == "")
                    return;
                
                user.gamePlayer = new gamePlayer(user, user.roomUser.roomUID, null);
                user.Room.Lobby.createGame(user.gamePlayer, name, mapID, teamAmount, powerups);
            }
            catch { }
        }

        private static void HandleSwitchTeam(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.Room.Lobby == null || user.gamePlayer == null || 
                user.gamePlayer.Game.State != Game.gameState.Waiting)
                return;
            
            if (user._Tickets <= 1)
            {
                user.sendData("Cl" + "J"); // Error [2] = Not enough tickets
                return;
            }
            
            if (!user.Room.Lobby.validGamerank(user.roomUser.gamePoints))
            {
                user.sendData("Cl" + "K"); // Error [3] = Skillevel not valid in this lobby
                return;
            }
            
            reader.Skip(1); // Skip character after header
            int teamID = reader.PopVL64();
            
            if (teamID != user.gamePlayer.teamID && user.gamePlayer.Game.teamHasSpace(teamID))
            {
                if (user.gamePlayer.teamID == -1) // User was a subviewer
                    user.gamePlayer.Game.Subviewers.Remove(user.gamePlayer);
                user.gamePlayer.Game.movePlayer(user.gamePlayer, user.gamePlayer.teamID, teamID);
            }
            else
            {
                user.sendData("Cl" + "H"); // Error [0] = Team full
            }
        }

        private static void HandleLeaveGame(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            user.leaveGame();
        }

        private static void HandleKickPlayer(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.Room.Lobby == null || user.gamePlayer == null || 
                user.gamePlayer.Game == null || user.gamePlayer != user.gamePlayer.Game.Owner)
                return;
            
            int roomUID = reader.PopVL64();
            for (int i = 0; i < user.gamePlayer.Game.Teams.Length; i++)
            {
                foreach (gamePlayer member in user.gamePlayer.Game.Teams[i])
                {
                    if (member.roomUID == roomUID)
                    {
                        member.sendData("Cl" + "RA"); // Error [6] = kicked from game
                        user.gamePlayer.Game.movePlayer(member, i, -1);
                        return;
                    }
                }
            }
        }

        private static void HandleStartGame(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.Room == null || user.Room.Lobby == null || user.gamePlayer == null || 
                user.gamePlayer != user.gamePlayer.Game.Owner)
                return;
            
            user.gamePlayer.Game.startGame();
        }

        private static void HandleGameMove(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.gamePlayer == null || user.gamePlayer.Game.State != Game.gameState.Started || 
                user.gamePlayer.teamID == -1)
                return;
            
            reader.Skip(1); // Skip character after header
            user.gamePlayer.goalX = reader.PopVL64();
            user.gamePlayer.goalY = reader.PopVL64();
        }

        private static void HandleGameRestart(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (user.gamePlayer == null || user.gamePlayer.Game.State != Game.gameState.Ended || 
                user.gamePlayer.teamID == -1)
                return;
            
            user.gamePlayer.Game.sendData("BK" + "" + user._Username + " wants to replay!");
        }

        private static void HandleToggleMoodlight(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner)
                return;
            roomManager.moodlight.setSettings(user._roomID, false, 0, 0, null, 0);
        }

        private static void HandleLoadMoodlight(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner)
                return;
            string settingData = roomManager.moodlight.getSettings(user._roomID);
            if (settingData != null)
                user.sendData("Em" + settingData);
        }

        private static void HandleUpdateDimmer(virtualUser user, PacketReader reader)
        {
            if (!user._isLoggedIn) return;
            if (!user._isOwner || user.Room == null)
                return;
            
            string remaining = reader.GetRemaining();
            string[] settings = remaining.Split(',');
            if (settings.Length < 5) return;
            
            bool enabled = settings[0] == "2";
            int presetID = int.Parse(settings[1]);
            int backgroundID = int.Parse(settings[2]);
            string colorCode = settings[3];
            int intensity = int.Parse(settings[4]);
            
            roomManager.moodlight.setSettings(user._roomID, enabled, presetID, backgroundID, colorCode, intensity);
            
            if (user.Room != null)
                user.Room.sendData("Em" + roomManager.moodlight.getSettings(user._roomID));
        }
        #endregion

        private void Refresh()
        {
            roomUser.Refresh();
        }

        #region Update voids
        /// <summary>
        /// Refreshes
        /// </summary>
        /// <param name="Reload">Specifies if the details have to be reloaded from database, or to use current _</param>
        /// <param name="refreshSettings">Specifies if the @E packet (which contains username etc) has to be resent.</param>
        ///<param name="refreshRoom">Specifies if the user has to be refreshed in room by using the 'poof' animation.</param>
        internal void refreshAppearance(bool Reload, bool refreshSettings, bool refreshRoom)
        {
            if (Reload)
            {
                var userAppearance = _userDataAccess.GetUserAppearance(userID);
                if (userAppearance != null)
                {
                    _Figure = userAppearance.Figure;
                    _Sex = userAppearance.Sex;
                    _Mission = userAppearance.Mission;
                }
            }

            if (refreshSettings)
                //sendData("@E" + connectionID + Convert.ToChar(2) + _Username + Convert.ToChar(2) + _Figure + Convert.ToChar(2) + _Sex + Convert.ToChar(2) + _Mission + Convert.ToChar(2) + Convert.ToChar(2) + "PCch=s02/53,51,44" + Convert.ToChar(2) + "HI");
                sendData("@E" + userID + Convert.ToChar(2) + _Username + Convert.ToChar(2) + _Figure + Convert.ToChar(2) + _Sex + Convert.ToChar(2) + _Mission + Convert.ToChar(2) + "H" + Convert.ToChar(2) + "HH");
            
            if (refreshRoom && Room != null && roomUser != null)
                Room.sendData("DJ" + Encoding.encodeVL64(roomUser.roomUID) + _Figure + Convert.ToChar(2) + _Sex + Convert.ToChar(2) + _Mission + Convert.ToChar(2));
                }
        /// <summary>
        /// Reloads the valueables (tickets and credits) from database and updates them for client.
        /// </summary>
        /// <param name="Credits">Specifies if to reload and update the Credit count.</param>
        /// <param name="Tickets">Specifies if to reload and update the Ticket count.</param>
        internal void refreshValueables(bool Credits, bool Tickets)
        {
            if (Credits)
            {
                _Credits = _userDataAccess.GetUserCredits(userID);
                sendData("@F" + _Credits);
            }

            if (Tickets)
            {
                _Tickets = _userDataAccess.GetUserTickets(userID);
                sendData("A|" + _Tickets);
            }
        }
        /// <summary>
        /// Refreshes the users Club subscription status.
        /// </summary>
        internal void refreshClub()
        {
            int restingDays = 0;
            int passedMonths = 0;
            int restingMonths = 0;
            var subscrDetails = _clubDataAccess.GetClubSubscription(userID);
            if (subscrDetails != null)
            {
                passedMonths = subscrDetails.MonthsExpired;
                restingMonths = subscrDetails.MonthsLeft - 1;
                restingDays = (int)(DateTime.Parse(subscrDetails.DateMonthStarted, new System.Globalization.CultureInfo("en-GB"))).Subtract(DateTime.Now).TotalDays + 32;
                _clubMember = true;  
            }
            else
                _clubMember = false;
            sendData("@Gclub_habbo" + Convert.ToChar(2) + Encoding.encodeVL64(restingDays) + Encoding.encodeVL64(passedMonths) + Encoding.encodeVL64(restingMonths) + Encoding.encodeVL64(1));
        }
        /// <summary>
        /// Refreshes the user's badges.
        /// </summary>
        internal void refreshBadges()
        {
            _Badges.Clear(); // Clear old badges
            _badgeSlotIDs.Clear(); // Clear old badge IDs

            var myBadges = _badgeDataAccess.GetUserBadgeNames(userID);
            var myBadgeSlotIDs = _badgeDataAccess.GetUserBadgeSlotIds(userID);

            StringBuilder sbMessage = new StringBuilder();
            sbMessage.Append(Encoding.encodeVL64(myBadges.Count)); // Total amount of badges
            for (int i = 0; i < myBadges.Count; i++)
            {
                sbMessage.Append(myBadges[i]);
                sbMessage.Append(Convert.ToChar(2));

                _Badges.Add(myBadges[i]);
            }

            for (int i = 0; i < myBadges.Count; i++)
            {
                if (i < myBadgeSlotIDs.Count && myBadgeSlotIDs[i] > 0) // Badge enabled!
                {
                    sbMessage.Append(Encoding.encodeVL64(myBadgeSlotIDs[i]));
                    sbMessage.Append(myBadges[i]);
                    sbMessage.Append(Convert.ToChar(2));

                    _badgeSlotIDs.Add(myBadgeSlotIDs[i]);
                }
                else
                    _badgeSlotIDs.Add(0); // :(
            }

            sendData("Ce" + sbMessage.ToString());
            sendData("Ft" + "SHJIACH_Graduate1" + Convert.ToChar(2) + "PAIACH_Login1" + Convert.ToChar(2) + "PAJACH_Login2" + Convert.ToChar(2) + "PAKACH_Login3" + Convert.ToChar(2) + "PAPAACH_Login4" + Convert.ToChar(2) + "PAQAACH_Login5" + Convert.ToChar(2) + "PBIACH_RoomEntry1" + Convert.ToChar(2) + "PBJACH_RoomEntry2" + Convert.ToChar(2) + "PBKACH_RoomEntry3" + Convert.ToChar(2) + "SBRAACH_RegistrationDuration6" + Convert.ToChar(2) + "SBSAACH_RegistrationDuration7" + Convert.ToChar(2) + "SBPBACH_RegistrationDuration8" + Convert.ToChar(2) + "SBQBACH_RegistrationDuration9" + Convert.ToChar(2) + "SBRBACH_RegistrationDuration10" + Convert.ToChar(2) + "RAIACH_AvatarLooks1" + Convert.ToChar(2) + "IJGLB" + Convert.ToChar(2) + "IKGLC" + Convert.ToChar(2) + "IPAGLD" + Convert.ToChar(2) + "IQAGLE" + Convert.ToChar(2) + "IRAGLF" + Convert.ToChar(2) + "ISAGLG" + Convert.ToChar(2) + "IPBGLH" + Convert.ToChar(2) + "IQBGLI" + Convert.ToChar(2) + "IRBGLJ" + Convert.ToChar(2) + "SAIACH_Student1" + Convert.ToChar(2) + "PCIHC1" + Convert.ToChar(2) + "PCJHC2" + Convert.ToChar(2) + "PCKHC3" + Convert.ToChar(2) + "PCPAHC4" + Convert.ToChar(2) + "PCQAHC5" + Convert.ToChar(2) + "QAIACH_GamePlayed1" + Convert.ToChar(2) + "QAJACH_GamePlayed2" + Convert.ToChar(2) + "QAKACH_GamePlayed3" + Convert.ToChar(2) + "QAPAACH_GamePlayed4" + Convert.ToChar(2) + "QAQAACH_GamePlayed5" + Convert.ToChar(2));
            sendData("Dt" + "IH" + Convert.ToChar(1) + "FCH");
        }
        /// <summary>
        /// Refreshes the user's group status.
        /// </summary>
        internal void refreshGroupStatus()
        {
            _groupID = _groupDataAccess.GetUserCurrentGroupId(userID);
            if (_groupID > 0) // User is member of a group
                _groupMemberRank = _groupDataAccess.GetUserGroupMemberRank(userID, _groupID);
        }
        /// <summary>
        /// Refreshes the Hand, which contains virtual items, with a specified mode.
        /// </summary>
        /// <param name="Mode">The refresh mode, available: 'next', 'prev', 'update', 'last' and 'new'.</param>
        internal void refreshHand(string Mode)
        {
            List<int> itemIDs = _furnitureDataAccess.GetInventoryItemIds(userID);
            StringBuilder Hand = new StringBuilder("BL");
            int startID = 0;
            int stopID = itemIDs.Count;

            switch (Mode)
            {
                case "next":
                    _handPage++;
                    break;
                case "prev":
                    _handPage--;
                    break;
                case "last":
                    _handPage = (stopID - 1) / 9;
                    break;
                case "update": // Nothing, keep handpage the same
                    break;
                default: // Probably, "new"
                    _handPage = 0;
                    break;
            }

            try
            {
                if (itemIDs.Count > 0)
                {
                reCount:
                    startID = _handPage * 9;
                    if (stopID > (startID + 9)) { stopID = startID + 9; }
                    if (startID > stopID || startID == stopID) { _handPage--; goto reCount; }

                    for (int i = startID; i < stopID; i++)
                    {
                        int templateID = _furnitureDataAccess.GetFurnitureTemplateId(itemIDs[i]);
                        catalogueManager.itemTemplate Template = catalogueManager.getTemplate(templateID);
                        char Recycleable = '1';
                        if (Template.isRecycleable == false)
                            Recycleable = '0';

                        if (Template.typeID == 0) // Wallitem
                        {
                            string Colour = Template.Colour;
                            if (Template.Sprite == "post.it" || Template.Sprite == "post.it.vd") // Stickies - pad size
                                Colour = _furnitureDataAccess.GetFurnitureVariable(itemIDs[i]);
                            Hand.Append("SI" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + i + Convert.ToChar(30).ToString() + "I" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + Template.Sprite + Convert.ToChar(30).ToString() + Colour + Convert.ToChar(30).ToString() + Recycleable + "/");
                        }
                        else // Flooritem
                            Hand.Append("SI" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + i + Convert.ToChar(30).ToString() + "S" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + Template.Sprite + Convert.ToChar(30).ToString() + Template.Length + Convert.ToChar(30).ToString() + Template.Width + Convert.ToChar(30).ToString() + _furnitureDataAccess.GetFurnitureVariable(itemIDs[i]) + Convert.ToChar(30).ToString() + Template.Colour + Convert.ToChar(30).ToString() + Recycleable + Convert.ToChar(30).ToString() + Template.Sprite + Convert.ToChar(30).ToString() + "/");
                    }
                }
                Hand.Append(Convert.ToChar(13).ToString() + itemIDs.Count);
                sendData(Hand.ToString());
            }
            catch
            {
                sendData("BL" + Convert.ToChar(13) + "0");
            }
        }
        /// <summary>
        /// Refreshes the trade window for the user.
        /// </summary>
        internal void refreshTradeBoxes()
        {
            if (Room != null && Room.containsUser(_tradePartnerRoomUID) && roomUser != null)
            {
                virtualUser Partner = Room.getUser(_tradePartnerRoomUID);
                StringBuilder tradeBoxes = new StringBuilder("Al" + _Username + Convert.ToChar(9) + _tradeAccept.ToString().ToLower() + Convert.ToChar(9));
                if (_tradeItemCount > 0)
                    tradeBoxes.Append(catalogueManager.tradeItemList(_tradeItems));

                tradeBoxes.Append(Convert.ToChar(13) + Partner._Username + Convert.ToChar(9) + Partner._tradeAccept.ToString().ToLower() + Convert.ToChar(9));

                if (Partner._tradeItemCount > 0)
                    tradeBoxes.Append(catalogueManager.tradeItemList(Partner._tradeItems));

                sendData(tradeBoxes.ToString());
            }
        }
        /// <summary>
        /// Aborts the trade between this user and his/her partner.
        /// </summary>
        internal void abortTrade()
        {
            if (Room != null && Room.containsUser(_tradePartnerRoomUID) && roomUser != null)
            {
                virtualUser Partner = Room.getUser(_tradePartnerRoomUID);
                this.sendData("An");
                this.refreshHand("update");
                Partner.sendData("An");
                Partner.refreshHand("update");

                this._tradePartnerRoomUID = -1;
                this._tradeAccept = false;
                this._tradeItems = new int[65];
                this._tradeItemCount = 0;
                this.statusManager.removeStatus("trd");
                this.roomUser.Refresh();

                Partner._tradePartnerRoomUID = -1;
                Partner._tradeAccept = false;
                Partner._tradeItems = new int[65];
                Partner._tradeItemCount = 0;
                Partner.statusManager.removeStatus("trd");
                Partner.roomUser.Refresh();
            }
        }
        #endregion

        #region Misc voids
        /// <summary>
        /// Checks if a certain chat message was a 'speech command', if so, then the action for this command is processed and a 'true' boolean is returned. Otherwise, 'false' is returned.
        /// </summary>
        /// <param name="Text">The chat message that was used.</param>
        private bool isSpeechCommand(string Text)
        {
            string[] args = Text.Split(' ');
            try // Try/catch, on error (eg, target user offline, parameters incorrect etc) then failure message will be sent
            {
                switch (args[0]) // arg[0] = command itself
                {
                    #region Public commands


                    case "emptyhand":
                        {
                            _furnitureDataAccess.DeleteUserInventoryFurniture(userID);
                            refreshHand("new");
                        }
                        break;

                    #endregion


                    #region Moderacy commands
                    case "alert": // Alert a virtual user
                        {
                            if (rankManager.containsRight(_Rank, "fuse_alert") == false)
                                return false;
                            else
                            {
                                virtualUser Target = userManager.getUser(DB.Stripslash(args[1]));
                                string Message = stringManager.wrapParameters(args, 2);

                                Target.sendData("B!" + Message + Convert.ToChar(2));
                                sendData("BK" + stringManager.getString("scommand_success"));
                                staffManager.addStaffMessage("alert", userID, Target.userID, args[2], "");
                            }
                            break;
                        }

                    case "roomalert": // Alert all virtual users in current virtual room
                        {
                            if (rankManager.containsRight(_Rank, "fuse_room_alert") == false)
                                return false;
                            else
                            {
                                string Message = Text.Substring(10);
                                Room.sendData("B!" + Message + Convert.ToChar(2));
                                staffManager.addStaffMessage("ralert", userID, Room.roomID, Message, "");
                            }
                            break;
                        }

                    case "kick": // Kicks a virtual user from room
                        {
                            if (rankManager.containsRight(_Rank, "fuse_kick") == false)
                                return false;
                            else
                            {
                                virtualUser Target = userManager.getUser(DB.Stripslash(args[1]));
                                if (Target._Rank < this._Rank)
                                {
                                    string Message = "";
                                    if (args.Length > 2) // Reason supplied
                                        Message = stringManager.wrapParameters(args, 2);

                                    Target.Room.removeUser(Target.roomUser.roomUID, true, Message);
                                    sendData("BK" + stringManager.getString("scommand_success"));
                                    staffManager.addStaffMessage("kick", userID, Target.userID, Message, "");
                                }
                                else
                                    sendData("BK" + stringManager.getString("scommand_failed"));
                            }
                            break;
                        }

                    case "roomkick": // Kicks all virtual users below rank from virtual room
                        {
                            if (rankManager.containsRight(_Rank, "fuse_room_kick") == false)
                                return false;
                            else
                            {
                                string Message = stringManager.wrapParameters(args, 1);
                                Room.kickUsers(_Rank, Message);
                                sendData("BK" + stringManager.getString("scommand_success"));
                                staffManager.addStaffMessage("rkick", userID, Room.roomID, Message, "");
                            }
                            break;
                        }

                    case "shutup": // Mutes a virtual user (disabling it from chat)
                        {
                            if (rankManager.containsRight(_Rank, "fuse_mute") == false)
                                return false;
                            else
                            {
                                virtualUser Target = userManager.getUser(DB.Stripslash(args[1]));
                                if (Target._Rank < _Rank && Target._isMuted == false)
                                {
                                    string Message = stringManager.wrapParameters(args, 2);
                                    Target._isMuted = true;
                                    Target.sendData("BK" + stringManager.getString("scommand_muted") + "\r" + Message);
                                    sendData("BK" + stringManager.getString("scommand_success"));
                                    staffManager.addStaffMessage("mute", userID, Target.userID, Message, "");
                                }
                                else
                                    sendData("BK" + stringManager.getString("scommand_failed"));
                            }
                            break;
                        }

                    case "unmute": // Unmutes a virtual user (enabling it to chat again)
                        {
                            if (rankManager.containsRight(_Rank, "fuse_mute") == false)
                                return false;
                            else
                            {
                                virtualUser Target = userManager.getUser(DB.Stripslash(args[1]));
                                if (Target._Rank < _Rank && Target._isMuted)
                                {
                                    Target._isMuted = false;
                                    Target.sendData("BK" + stringManager.getString("scommand_unmuted"));
                                    sendData("BK" + stringManager.getString("scommand_success"));
                                    staffManager.addStaffMessage("unmute", userID, Target.userID, "", "");
                                }
                                else
                                    sendData("BK" + stringManager.getString("scommand_failed"));
                            }
                            break;
                        }

                    case "roomshutup": // Mutes all virtual users in the current room from chat. Only user's that have a lower rank than this user are affected.
                        {
                            if (rankManager.containsRight(_Rank, "fuse_room_mute") == false)
                                return false;
                            else
                            {
                                string Message = stringManager.wrapParameters(args, 1);
                                Room.muteUsers(_Rank, Message);
                                sendData("BK" + stringManager.getString("scommand_success"));
                                staffManager.addStaffMessage("rmute", userID, Room.roomID, Message, "");
                            }
                            break;
                        }

                    case "roomunmute": // Unmutes all the muted virtual users in this room (who's rank is lower than this user's rank), making them able to chat again
                        {
                            if (rankManager.containsRight(_Rank, "fuse_room_mute") == false)
                                return false;
                            else
                            {
                                Room.unmuteUsers(_Rank);
                                sendData("BK" + stringManager.getString("scommand_success"));
                                staffManager.addStaffMessage("runmute", userID, Room.roomID, "", "");
                            }
                            break;
                        }

                    case "ban": // Bans a virtual user from server (no IP ban)
                        {
                            if (rankManager.containsRight(_Rank, "fuse_ban") == false)
                                return false;
                            else
                            {
                                UserModerationDetails userDetails = _userDataAccess.GetUserModerationDetails(DB.Stripslash(args[1]));
                                if (userDetails == null)
                                    sendData("BK" + stringManager.getString("modtool_actionfailed") + "\r" + stringManager.getString("modtool_usernotfound"));
                                else if (userDetails.Rank > _Rank)
                                    sendData("BK" + stringManager.getString("modtool_actionfailed") + "\r" + stringManager.getString("modtool_rankerror"));
                                else
                                {
                                    int banHours = int.Parse(args[2]);
                                    string Reason = stringManager.wrapParameters(args, 3);
                                    if (banHours == 0 || Reason == "")
                                        sendData("BK" + stringManager.getString("scommand_failed"));
                                    else
                                    {
                                        staffManager.addStaffMessage("ban", userID, userDetails.UserId, Reason, "");
                                        userManager.setBan(userDetails.UserId, banHours, Reason);
                                        sendData("BK" + userManager.generateBanReport(userDetails.UserId));
                                    }
                                }
                            }
                            break;
                        }

                    case "superban": // Bans an IP address and all virtual user's that used this IP address for their last access from the system
                        {
                            if (rankManager.containsRight(_Rank, "fuse_superban") == false)
                                return false;
                            else
                            {
                                UserModerationDetails userDetails = _userDataAccess.GetUserModerationDetails(DB.Stripslash(args[1]));
                                if (userDetails == null)
                                    sendData("BK" + stringManager.getString("modtool_actionfailed") + "\r" + stringManager.getString("modtool_usernotfound"));
                                else if (userDetails.Rank > _Rank)
                                    sendData("BK" + stringManager.getString("modtool_actionfailed") + "\r" + stringManager.getString("modtool_rankerror"));
                                else
                                {
                                    int banHours = int.Parse(args[2]);
                                    string Reason = stringManager.wrapParameters(args, 3);
                                    if (banHours == 0 || Reason == "")
                                        sendData("BK" + stringManager.getString("scommand_failed"));
                                    else
                                    {
                                        string IP = userDetails.LastIpAddress;
                                        staffManager.addStaffMessage("ban", userID, userDetails.UserId, Reason, "");
                                        userManager.setBan(IP, banHours, Reason);
                                        sendData("BK" + userManager.generateBanReport(IP));
                                    }
                                }
                            }
                            break;
                        }

                    case "info": // Generates a list of information about a certain virtual user
                        {
                            if (rankManager.containsRight(_Rank, "fuse_moderator_access") == false)
                                return false;
                            else
                                sendData("BK" + userManager.generateUserInfo(userManager.getUserID(DB.Stripslash(args[1])), _Rank));
                            break;
                        }

                    case "find": // Teleports user to the room the receiver is in
                        {
                            if (rankManager.containsRight(_Rank, "fuse_moderator_access") == false)
                            {
                                return false;
                            }
                            else
                            {

                                virtualUser Target = userManager.getUser(DB.Stripslash(args[1]));

                                if (Target.Room != null && Target._inPublicroom)
                                {
                                    sendData("D^" + "I" + Encoding.encodeVL64(Target._roomID));
                                }
                                else if (Target.Room != null)
                                {
                                    sendData("D^" + "H" + Encoding.encodeVL64(Target._roomID));
                                }
                                else
                                {
                                    sendData("BK" + "Unable to teleport, " + Target._Username + " is not in a room.");
                                }

                            }
                            break;
                        }

                    case "teletome": // Teleports user to the room the sender is in
                        {
                            if (rankManager.containsRight(_Rank, "fuse_moderator_access") == false)
                            {
                                return false;
                            }
                            else
                            {

                                virtualUser Target = userManager.getUser(DB.Stripslash(args[1]));
                                Target.sendData("BK" + "You are being teleported to where " + _Username + " is.");
                                if (_inPublicroom)
                                {
                                    Target.sendData("D^" + "I" + Encoding.encodeVL64(_roomID));
                                }
                                else
                                {
                                    Target.sendData("D^" + "H" + Encoding.encodeVL64(_roomID));
                                }

                            }
                            break;
                        }

                    case "offline": // Broadcoasts a message that the server will shutdown in xx minutes
                        {
                            if (rankManager.containsRight(_Rank, "fuse_administrator_access") == false)
                                return false;
                            else
                            {
                                int Minutes = int.Parse(args[1]);
                                userManager.sendData("Dc" + Encoding.encodeVL64(Minutes));
                                staffManager.addStaffMessage("offline", userID, 0, "mm=" + Minutes, "");
                            }
                            break;
                        }

                    case "position": // Sends an alert with the virtual room user's current coordinates
                        {
                            if (rankManager.containsRight(_Rank, "fuse_administrator_access") == false)
                                return false;
                            else
                                sendData("BK" + "X: " + roomUser.X + "\r" + "Y: " + roomUser.Y + "\r" + "Z: " + roomUser.Z1 + "\r" + "Height: " + roomUser.H);
                            break;
                        }


                    #endregion

                    #region Message broadcoasting
                    case "ha": // Broadcoasts a message to all virtual users (hotel alert)
                        {
                            if (rankManager.containsRight(_Rank, "fuse_administrator_access") == false)
                                return false;
                            else
                            {
                                string Message = Text.Substring(3);
                                userManager.sendData("BK" + stringManager.getString("scommand_hotelalert") + "\r" + Message);
                                staffManager.addStaffMessage("halert", userID, 0, Message, "");
                            }
                        }
                        break;

                    case "ra": // Broadcoasts a message to all users with the same rank (rank alert)
                        {
                            if (rankManager.containsRight(_Rank, "fuse_alert") == false)
                                return false;
                            else
                            {
                                string Message = Text.Substring(3);
                                userManager.sendToRank(_Rank, false, "BK" + stringManager.getString("scommand_rankalert") + "\r" + Message);
                                staffManager.addStaffMessage("rankalert", userID, _Rank, Message, "");
                            }
                            break;
                        }
                    #endregion

                    #region Special staff commands
                    case "refreshrooms": // Refreshes the Hotel Public and Private Rooms
                        {
                            if (rankManager.containsRight(_Rank, "fuse_administrator_access") == false)
                                return false;
                            else
                            {
                                string Message = Text.Substring(3);
                                userManager.sendData("DBO" + "\r");
                            }
                            break;
                        }

                    case "refreshhotel": // Refreshes the Hotel Public and Private Rooms
                        {
                            if (rankManager.containsRight(_Rank, "fuse_administrator_access") == false)
                                return false;
                            else
                            {
                                string Message = Text.Substring(3);
                                userManager.sendData("DBO" + "\r");
                            }
                            break;
                        }

                    case "refreshcatalogue": // refresh cata
                        {
                            sendData("HKRC");
                            catalogueManager.Init();
                            break;
                        }

                    case "coins": // Credits command
                        {
                            virtualUser Target = userManager.getUser(DB.Stripslash(args[1]));
                            int Credits = int.Parse(args[2]);
                            if (rankManager.containsRight(_Rank, "fuse_administrator_access") == false)
                                sendData("BK" + "You don't have the rights to give coins to someone!");
                            else
                            {
                                _userDataAccess.UpdateUserCredits(Target.userID, Target._Credits + Credits);
                                Target.sendData("BK" + "You have recieved " + Credits + " credits from a staff member!");
                                sendData("BK" + "You've succesfully sent " + Credits + " coins to " + Target._Username + ".");
                                Room.Refresh(roomUser);
                                Target.refreshAppearance(true, true, true);
                                Target.refreshValueables(true, false);
                            }
                            break;
                        }

                    case "commands": // commands list
                        {
                            if (rankManager.containsRight(_Rank, "fuse_moderator_access") == false)
                            {
                                return false;
                            }
                            sendData("BK" +
                            ":alert <user> <message>\r" +
                            ":roomalert <message>\r" +
                            ":kick <user> <message>\r" +
                            ":roomkick <message>\r" +
                            ":shutup <user> <message>\r" +
                            ":unmute <user>\r" +
                            ":roomshutup <message>\r" +
                            ":roomunmute\r" +
                            ":ban <user> <hours> <message>\r" +
                            ":superban <user> <hours> <message>\r" +
                            ":ha <message>\r" +
                            ":ra <message>\r" +
                            ":teleport\r" +
                            ":warp X Y\r" +
                            ":position \r" +
                            ":transfer \r" +
                            ":refresh  \r" +
                            ":coins <howmuch> \r" +
                                //":masscredits <howmuch> \r" +
                            ":info <username>\r");
                            break;
                        }

                    #endregion
                    #region Public Commands
                    #region About

                        


                    #endregion
                    #region Dive
                    case "dive-off":
                        {
                            sendData("A}");
                            break;
                        }
                    #endregion
                    #endregion


                    default:
                        return false;
                }
            }
            catch { sendData("BK" + stringManager.getString("scommand_failed")); }
            return true;
        }
        
        
        /// <summary>
        /// Checks if the user is involved with a 'BattleBall' or 'SnowStorm' game. If so, then the removal procedure is invoked. If the user is the owner of the game, then the game will be aborted.
        /// </summary>
        internal void leaveGame()
        {
            if (gamePlayer != null && gamePlayer.Game != null)
            {
                if (gamePlayer.Game.Owner == gamePlayer) // Owner leaves game
                {
                    try { gamePlayer.Game.Lobby.Games.Remove(gamePlayer.Game.ID); }
                    catch {}
                    gamePlayer.Game.Abort(); 
                    sendData("FS");
                }
                else if (gamePlayer.teamID != -1) // Team member leaves game
                    gamePlayer.Game.movePlayer(gamePlayer, gamePlayer.teamID, -1);
                else
                {
                    gamePlayer.Game.Subviewers.Remove(gamePlayer);
                    sendData("Cm" + "H");
                    sendData("FS");
                }
            }
            this.gamePlayer = null;
        }

        private delegate void TeleporterUsageSleep(Rooms.Items.floorItem Teleporter1, int idTeleporter2, int roomIDTeleporter2);
        private void useTeleporter(Rooms.Items.floorItem Teleporter1, int idTeleporter2, int roomIDTeleporter2)
        {
            roomUser.walkLock = true;
            string Sprite = Teleporter1.Sprite;
            if (roomIDTeleporter2 == _roomID) // Partner teleporter is in same room, don't leave room
            {
                Rooms.Items.floorItem Teleporter2 = Room.floorItemManager.getItem(idTeleporter2);
                Thread.Sleep(500);
                Room.sendData("AY" + Teleporter1.ID + "/" + _Username + "/" + Sprite);
                Room.sendData(@"A\" + Teleporter2.ID + "/" + _Username + "/" + Sprite);
                roomUser.X = Teleporter2.X;
                roomUser.Y = Teleporter2.Y;
                roomUser.H = Teleporter2.H;
                roomUser.Z1 = Teleporter2.Z;
                roomUser.Z2 = Teleporter2.Z;
                roomUser.Refresh();
                roomUser.walkLock = false;
            }
            else // Partner teleporter is in different room
            {
                _teleporterID = idTeleporter2;
                sendData("@~" + Encoding.encodeVL64(idTeleporter2) + Encoding.encodeVL64(roomIDTeleporter2));
                Room.sendData("AY" + Teleporter1.ID + "/" + _Username + "/" + Sprite);
            }
        }
        
    }
}
#endregion