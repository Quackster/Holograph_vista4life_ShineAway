using System.Text;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Users;
using Holo.Virtual.Rooms.Bots;
using Holo.Virtual.Rooms.Items;

namespace Holo.Virtual.Rooms;


    /// <summary>
    /// Partial class for virtualRoom containing user and bot management methods.
    /// </summary>
    public partial class virtualRoom
    {
        #region Virtual user management
        #region User and bot adding/removing
        internal void addUser(virtualUser User)
        {
            if (User._teleporterID == 0 && (User._ROOMACCESS_PRIMARY_OK == false || User._ROOMACCESS_SECONDARY_OK == false))
                return;

            User.statusManager = new virtualRoomUserStatusManager(User.userID, this.roomID);
            User.roomUser = new virtualRoomUser(User.userID, roomID, getFreeRoomIdentifier(), User, User.statusManager);

            if (User._teleporterID == 0)
            {
                User.roomUser.X = this.doorX;
                User.roomUser.Y = this.doorY;
                User.roomUser.Z1 = this.doorZ;
                User.roomUser.Z2 = this.doorZ;
                User.roomUser.H = this.doorH;
            }
            else
            {
                floorItem Teleporter = floorItemManager.getItem(User._teleporterID);
                User.roomUser.X = Teleporter.X;
                User.roomUser.Y = Teleporter.Y;
                User.roomUser.H = Teleporter.H;
                User.roomUser.Z1 = Teleporter.Z;
                User.roomUser.Z2 = Teleporter.Z;
                User._teleporterID = 0;
                sendData(@"A\" + Teleporter.ID + "/" + User._Username + "/" + Teleporter.Sprite);
            }
            User.roomUser.goalX = -1;
            _Users.Add(User.roomUser.roomUID, User.roomUser);

            if (this.isPublicroom == false)
            {
                if (User._hasRights)
                    if (User._isOwner == false) { User.statusManager.addStatus("flatctrl", "onlyfurniture"); }
                if (User._isOwner)
                    User.statusManager.addStatus("flatctrl", "useradmin");
                User.roomUser.hasVoted = DB.checkExists("SELECT userid FROM room_votes WHERE userid = '" + User.userID + "' AND roomid = '" + this.roomID + "'");
            }
            else
                if (this.hasSwimmingPool)
                    User.roomUser.SwimOutfit = DB.runRead("SELECT figure_swim FROM users WHERE id = '" + User.userID + "'");
            if (this.Lobby != null) // Game lobby here
            {
                User.roomUser.gamePoints = DB.runReadUnsafe("SELECT " + Lobby.Type + "_totalpoints FROM users WHERE id = '" + User.userID + "'", null);
                sendData(new HabboPacketBuilder("Cz").Append("I").AppendVL64(User.roomUser.roomUID).Append(User.roomUser.gamePoints).Separator().Append(rankManager.getGameRankTitle(Lobby.isBattleBall, User.roomUser.gamePoints)).Separator().Build());
            }
            sendData(@"@\" + User.roomUser.detailsString);
            if (User._groupID > 0 && _activeGroups.Contains(User._groupID) == false)
            {
                string groupBadge = DB.runRead("SELECT badge FROM groups_details WHERE id = '" + User._groupID + "'");
                sendData(new HabboPacketBuilder("Du").Append("I").AppendVL64(User._groupID).Append(groupBadge).Separator().Build());
                _activeGroups.Add(User._groupID);
            }
            roomManager.updateRoomVisitorCount(this.roomID, this._Users.Count);
        }
        /// <summary>
        /// Removes a room user from the virtual room.
        /// </summary>
        /// <param name="roomUID">The room identifier of the room user to remove.</param>
        /// <param name="sendKick">Specifies if the user must be kicked with the @R packet.</param>
        /// <param name="moderatorMessage">Specifies a moderator message [B!] packet to be used at kick.</param>
        internal void removeUser(int roomUID, bool sendKick, string moderatorMessage)
        {
            if (!_Users.TryGetValue(roomUID, out var roomUser))
                return;

            if (sendKick)
            {
                roomUser.User.sendData("@R");
                if (moderatorMessage != "")
                    roomUser.User.sendData(new HabboPacketBuilder("B!").Append(moderatorMessage).Separator().Append("holo.cast.modkick").Build());
            }

            sqUNIT[roomUser.X, roomUser.Y] = false;
            roomUser.User.statusManager.Clear();
            roomUser.User.statusManager = null;
            roomUser.User._roomID = 0;
            roomUser.User._inPublicroom = false;
            roomUser.User._ROOMACCESS_PRIMARY_OK = false;
            roomUser.User._ROOMACCESS_SECONDARY_OK = false;
            roomUser.User._isOwner = false;
            roomUser.User._hasRights = false;
            roomUser.User.Room = null;
            roomUser.User.roomUser = null;

            _Users.Remove(roomUID);
            if (_Users.Count > 0) // Still users in room
            {
                if (roomUser.User._groupID > 0)
                {
                    bool removeBadge = true;
                    foreach (virtualRoomUser rUser in _Users.Values)
                    {
                        if (rUser.roomUID != roomUser.roomUID && rUser.User._groupID == roomUser.User._groupID)
                        {
                            removeBadge = false;
                            break;
                        }
                    }
                    if (removeBadge)
                        _activeGroups.Remove(roomUser.User._groupID);
                }

                sendData(new HabboPacketBuilder("@]").Append(roomUID).Build());
                //men
                roomManager.updateRoomVisitorCount(this.roomID, _Users.Count);
            }
            else
            {
                _Users.Clear();
                _Bots.Clear();
                _statusUpdates = null;
                if (isPublicroom == false)
                {
                    floorItemManager.Clear();
                    wallItemManager.Clear();
                }
                try { specialCastHandler.Abort(); }
                catch { }

                roomManager.removeRoom(this.roomID);
                _statusHandler.Abort();
            }
        }
        /// <summary>
        /// Returns a bool that indicates if the room contains a certain room user.
        /// </summary>
        /// <param name="roomUID">The ID that identifies the user in the virtual room.</param>
        internal bool containsUser(int roomUID)
        {
            return _Users.ContainsKey(roomUID);
        }
        /// <summary>
        /// The values of the _Users Dictionary as a collection.
        /// </summary>
        internal IEnumerable<virtualRoomUser> Users
        {
            get
            {
                return _Users.Values;
            }
        }
        #endregion
        #endregion
    }
