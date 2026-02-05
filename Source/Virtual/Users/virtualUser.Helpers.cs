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

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Represents a virtual user - Helpers partial class containing update and misc helper methods.
    /// </summary>
    public partial class virtualUser
    {
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
                string[] userData = DB.runReadRow("SELECT figure,sex,mission FROM users WHERE id = '" + userID + "'");
                _Figure = userData[0];
                _Sex = char.Parse(userData[1]);
                _Mission = userData[2];
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
                _Credits = DB.runRead("SELECT credits FROM users WHERE id = '" + userID + "'", null);
                sendData("@F" + _Credits);
            }

            if (Tickets)
            {
                _Tickets = DB.runRead("SELECT tickets FROM users WHERE id = '" + userID + "'", null);
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
            string[] subscrDetails = DB.runReadRow("SELECT months_expired,months_left,date_monthstarted FROM users_club WHERE userid = '" + userID + "'");
            if (subscrDetails.Length > 0)
            {
                passedMonths = int.Parse(subscrDetails[0]);
                restingMonths = int.Parse(subscrDetails[1]) - 1;
                restingDays = (int)(DateTime.Parse(subscrDetails[2], new System.Globalization.CultureInfo("en-GB"))).Subtract(DateTime.Now).TotalDays + 32;
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

            string[] myBadges = DB.runReadColumn("SELECT badge FROM users_badges WHERE userid = '" + userID + "' ORDER BY slotid ASC", 0);
            int[] myBadgeSlotIDs = DB.runReadColumn("SELECT slotid FROM users_badges WHERE userid = '" + userID + "' ORDER BY slotid ASC", 0, null);

            StringBuilder sbMessage = new StringBuilder();
            sbMessage.Append(Encoding.encodeVL64(myBadges.Length)); // Total amount of badges
            for (int i = 0; i < myBadges.Length; i++)
            {
                sbMessage.Append(myBadges[i]);
                sbMessage.Append(Convert.ToChar(2));

                _Badges.Add(myBadges[i]);
            }

            for (int i = 0; i < myBadges.Length; i++)
            {
                if (myBadgeSlotIDs[i] > 0) // Badge enabled!
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
            _groupID = DB.runReadUnsafe("SELECT groupid FROM groups_memberships WHERE userid = '" + userID + "' AND is_current = '1'", null);
            if (_groupID > 0) // User is member of a group
                _groupMemberRank = Holo.DB.runRead("SELECT member_rank FROM groups_memberships WHERE userid = '" + userID + "' AND groupID = '" + _groupID + "'", null);
        }
        /// <summary>
        /// Refreshes the Hand, which contains virtual items, with a specified mode.
        /// </summary>
        /// <param name="Mode">The refresh mode, available: 'next', 'prev', 'update', 'last' and 'new'.</param>
        internal void refreshHand(string Mode)
        {
            int[] itemIDs = DB.runReadColumn("SELECT id FROM furniture WHERE ownerid = '" + userID + "' AND roomid = '0' ORDER BY id ASC", 0, null);
            StringBuilder Hand = new StringBuilder("BL");
            int startID = 0;
            int stopID = itemIDs.Length;

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
                if (itemIDs.Length > 0)
                {
                reCount:
                    startID = _handPage * 9;
                    if (stopID > (startID + 9)) { stopID = startID + 9; }
                    if (startID > stopID || startID == stopID) { _handPage--; goto reCount; }

                    for (int i = startID; i < stopID; i++)
                    {
                        int templateID = DB.runRead("SELECT tid FROM furniture WHERE id = '" + itemIDs[i] + "'", null);
                        catalogueManager.itemTemplate Template = catalogueManager.getTemplate(templateID);
                        char Recycleable = '1';
                        if (Template.isRecycleable == false)
                            Recycleable = '0';

                        if (Template.typeID == 0) // Wallitem
                        {
                            string Colour = Template.Colour;
                            if (Template.Sprite == "post.it" || Template.Sprite == "post.it.vd") // Stickies - pad size
                                Colour = DB.runRead("SELECT var FROM furniture WHERE id = '" + itemIDs[i] + "'");
                            Hand.Append("SI" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + i + Convert.ToChar(30).ToString() + "I" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + Template.Sprite + Convert.ToChar(30).ToString() + Colour + Convert.ToChar(30).ToString() + Recycleable + "/");
                        }
                        else // Flooritem
                            Hand.Append("SI" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + i + Convert.ToChar(30).ToString() + "S" + Convert.ToChar(30).ToString() + itemIDs[i] + Convert.ToChar(30).ToString() + Template.Sprite + Convert.ToChar(30).ToString() + Template.Length + Convert.ToChar(30).ToString() + Template.Width + Convert.ToChar(30).ToString() + DB.runRead("SELECT var FROM furniture WHERE id = '" + itemIDs[i] + "'") + Convert.ToChar(30).ToString() + Template.Colour + Convert.ToChar(30).ToString() + Recycleable + Convert.ToChar(30).ToString() + Template.Sprite + Convert.ToChar(30).ToString() + "/");
                    }
                }
                Hand.Append(Convert.ToChar(13).ToString() + itemIDs.Length);
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

        #region Misc voids - Game and Teleporter
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
        #endregion
    }
}
