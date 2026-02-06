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
    /// Represents a virtual user - Helpers partial class containing update and misc helper methods.
    /// </summary>
    public partial class virtualUser
    {
        #region Update voids
        /// <summary>
        /// Refreshes the user's appearance.
        /// </summary>
        /// <param name="Reload">Specifies if the details have to be reloaded from database.</param>
        /// <param name="refreshSettings">Specifies if the @E packet (which contains username etc) has to be resent.</param>
        /// <param name="refreshRoom">Specifies if the user has to be refreshed in room by using the 'poof' animation.</param>
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
            {
                var packet = new HabboPacketBuilder(HabboPackets.USER_APPEARANCE)
                    .Append(userID)
                    .Separator()
                    .Append(_Username)
                    .Separator()
                    .Append(_Figure)
                    .Separator()
                    .Append(_Sex)
                    .Separator()
                    .Append(_Mission)
                    .Separator()
                    .Append("H")
                    .Separator()
                    .Append("HH");
                sendData(packet.Build());
            }

            if (refreshRoom && Room != null && roomUser != null)
            {
                var roomPacket = new HabboPacketBuilder(HabboPackets.USER_APPEARANCE_ROOM)
                    .AppendVL64(roomUser.roomUID)
                    .Append(_Figure)
                    .Separator()
                    .Append(_Sex)
                    .Separator()
                    .Append(_Mission)
                    .Separator();
                Room.sendData(roomPacket.Build());
            }
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
                sendData(HabboPacketBuilder.CreditsUpdate(_Credits));
            }

            if (Tickets)
            {
                _Tickets = DB.runRead("SELECT tickets FROM users WHERE id = '" + userID + "'", null);
                sendData(HabboPacketBuilder.TicketsUpdate(_Tickets));
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

            var packet = new HabboPacketBuilder(HabboPackets.CLUB_STATUS)
                .Append("club_habbo")
                .Separator()
                .AppendVL64(restingDays)
                .AppendVL64(passedMonths)
                .AppendVL64(restingMonths)
                .AppendVL64(1);
            sendData(packet.Build());
        }

        /// <summary>
        /// Refreshes the user's badges.
        /// </summary>
        internal void refreshBadges()
        {
            _Badges.Clear();
            _badgeSlotIDs.Clear();

            string[] myBadges = DB.runReadColumn("SELECT badge FROM users_badges WHERE userid = '" + userID + "' ORDER BY slotid ASC", 0);
            int[] myBadgeSlotIDs = DB.runReadColumn("SELECT slotid FROM users_badges WHERE userid = '" + userID + "' ORDER BY slotid ASC", 0, null);

            var sbMessage = new HabboPacketBuilder()
                .AppendVL64(myBadges.Length);

            for (int i = 0; i < myBadges.Length; i++)
            {
                sbMessage.AppendField(myBadges[i]);
                _Badges.Add(myBadges[i]);
            }

            for (int i = 0; i < myBadges.Length; i++)
            {
                if (myBadgeSlotIDs[i] > 0)
                {
                    sbMessage.AppendVL64(myBadgeSlotIDs[i])
                        .AppendField(myBadges[i]);
                    _badgeSlotIDs.Add(myBadgeSlotIDs[i]);
                }
                else
                    _badgeSlotIDs.Add(0);
            }

            sendData(new HabboPacketBuilder(HabboPackets.BADGE_LIST).Append(sbMessage.Build()).Build());

            // Achievement tokens packet - static data
            var achievementPacket = new HabboPacketBuilder(HabboPackets.ACHIEVEMENT_TOKENS)
                .AppendField("SHJIACH_Graduate1")
                .AppendField("PAIACH_Login1")
                .AppendField("PAJACH_Login2")
                .AppendField("PAKACH_Login3")
                .AppendField("PAPAACH_Login4")
                .AppendField("PAQAACH_Login5")
                .AppendField("PBIACH_RoomEntry1")
                .AppendField("PBJACH_RoomEntry2")
                .AppendField("PBKACH_RoomEntry3")
                .AppendField("SBRAACH_RegistrationDuration6")
                .AppendField("SBSAACH_RegistrationDuration7")
                .AppendField("SBPBACH_RegistrationDuration8")
                .AppendField("SBQBACH_RegistrationDuration9")
                .AppendField("SBRBACH_RegistrationDuration10")
                .AppendField("RAIACH_AvatarLooks1")
                .AppendField("IJGLB")
                .AppendField("IKGLC")
                .AppendField("IPAGLD")
                .AppendField("IQAGLE")
                .AppendField("IRAGLF")
                .AppendField("ISAGLG")
                .AppendField("IPBGLH")
                .AppendField("IQBGLI")
                .AppendField("IRBGLJ")
                .AppendField("SAIACH_Student1")
                .AppendField("PCIHC1")
                .AppendField("PCJHC2")
                .AppendField("PCKHC3")
                .AppendField("PCPAHC4")
                .AppendField("PCQAHC5")
                .AppendField("QAIACH_GamePlayed1")
                .AppendField("QAJACH_GamePlayed2")
                .AppendField("QAKACH_GamePlayed3")
                .AppendField("QAPAACH_GamePlayed4")
                .AppendField("QAQAACH_GamePlayed5");
            sendData(achievementPacket.Build());

            sendData(new HabboPacketBuilder(HabboPackets.ACHIEVEMENT_DISPLAY)
                .Append("IH")
                .Append(HabboProtocol.PACKET_TERMINATOR)
                .Append("FCH")
                .Build());
        }

        /// <summary>
        /// Refreshes the user's group status.
        /// </summary>
        internal void refreshGroupStatus()
        {
            _groupID = DB.runReadUnsafe("SELECT groupid FROM groups_memberships WHERE userid = '" + userID + "' AND is_current = '1'", null);
            if (_groupID > 0)
                _groupMemberRank = Holo.DB.runRead("SELECT member_rank FROM groups_memberships WHERE userid = '" + userID + "' AND groupID = '" + _groupID + "'", null);
        }

        /// <summary>
        /// Refreshes the Hand, which contains virtual items, with a specified mode.
        /// </summary>
        /// <param name="Mode">The refresh mode, available: 'next', 'prev', 'update', 'last' and 'new'.</param>
        internal void refreshHand(string Mode)
        {
            int[] itemIDs = DB.runReadColumn("SELECT id FROM furniture WHERE ownerid = '" + userID + "' AND roomid = '0' ORDER BY id ASC", 0, null);
            var Hand = new HabboPacketBuilder(HabboPackets.INVENTORY_HAND);
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
                    _handPage = (stopID - 1) / HabboProtocol.HAND_PAGE_SIZE;
                    break;
                case "update":
                    break;
                default:
                    _handPage = 0;
                    break;
            }

            try
            {
                if (itemIDs.Length > 0)
                {
                reCount:
                    startID = _handPage * HabboProtocol.HAND_PAGE_SIZE;
                    if (stopID > (startID + HabboProtocol.HAND_PAGE_SIZE)) { stopID = startID + HabboProtocol.HAND_PAGE_SIZE; }
                    if (startID > stopID || startID == stopID) { _handPage--; goto reCount; }

                    for (int i = startID; i < stopID; i++)
                    {
                        int templateID = DB.runRead("SELECT tid FROM furniture WHERE id = '" + itemIDs[i] + "'", null);
                        catalogueManager.itemTemplate Template = catalogueManager.getTemplate(templateID);
                        char Recycleable = Template.isRecycleable ? '1' : '0';

                        if (Template.typeID == 0) // Wallitem
                        {
                            string Colour = Template.Colour;
                            if (Template.Sprite == "post.it" || Template.Sprite == "post.it.vd")
                                Colour = DB.runRead("SELECT var FROM furniture WHERE id = '" + itemIDs[i] + "'");

                            Hand.StartInventoryItem()
                                .AppendItemField(itemIDs[i])
                                .AppendItemField(i)
                                .AppendItemField("I")
                                .AppendItemField(itemIDs[i])
                                .AppendItemField(Template.Sprite)
                                .AppendItemField(Colour)
                                .Append(Recycleable)
                                .AppendSlashTerminated("");
                        }
                        else // Flooritem
                        {
                            string itemVar = DB.runRead("SELECT var FROM furniture WHERE id = '" + itemIDs[i] + "'");
                            Hand.StartInventoryItem()
                                .AppendItemField(itemIDs[i])
                                .AppendItemField(i)
                                .AppendItemField("S")
                                .AppendItemField(itemIDs[i])
                                .AppendItemField(Template.Sprite)
                                .AppendItemField(Template.Length)
                                .AppendItemField(Template.Width)
                                .AppendItemField(itemVar)
                                .AppendItemField(Template.Colour)
                                .AppendItemField(Recycleable.ToString())
                                .Append(Template.Sprite)
                                .ItemSeparator()
                                .AppendSlashTerminated("");
                        }
                    }
                }
                Hand.RecordSeparator().Append(itemIDs.Length);
                sendData(Hand.Build());
            }
            catch
            {
                sendData(new HabboPacketBuilder(HabboPackets.INVENTORY_HAND)
                    .RecordSeparator()
                    .Append("0")
                    .Build());
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

                var tradePacket = new HabboPacketBuilder(HabboPackets.TRADE_COMPLETE)
                    .Append(_Username)
                    .TabSeparator()
                    .Append(_tradeAccept.ToString().ToLower())
                    .TabSeparator();

                if (_tradeItemCount > 0)
                    tradePacket.Append(catalogueManager.tradeItemList(_tradeItems));

                tradePacket.RecordSeparator()
                    .Append(Partner._Username)
                    .TabSeparator()
                    .Append(Partner._tradeAccept.ToString().ToLower())
                    .TabSeparator();

                if (Partner._tradeItemCount > 0)
                    tradePacket.Append(catalogueManager.tradeItemList(Partner._tradeItems));

                sendData(tradePacket.Build());
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
                this.sendData(HabboPackets.TRADE_ABORT);
                this.refreshHand("update");
                Partner.sendData(HabboPackets.TRADE_ABORT);
                Partner.refreshHand("update");

                this._tradePartnerRoomUID = -1;
                this._tradeAccept = false;
                this._tradeItems = new int[HabboProtocol.TRADE_ITEMS_MAX];
                this._tradeItemCount = 0;
                this.statusManager.removeStatus("trd");
                this.roomUser.Refresh();

                Partner._tradePartnerRoomUID = -1;
                Partner._tradeAccept = false;
                Partner._tradeItems = new int[HabboProtocol.TRADE_ITEMS_MAX];
                Partner._tradeItemCount = 0;
                Partner.statusManager.removeStatus("trd");
                Partner.roomUser.Refresh();
            }
        }
        #endregion

        #region Misc voids - Game and Teleporter
        /// <summary>
        /// Checks if the user is involved with a 'BattleBall' or 'SnowStorm' game. If so, then the removal procedure is invoked.
        /// </summary>
        internal void leaveGame()
        {
            if (gamePlayer != null && gamePlayer.Game != null)
            {
                if (gamePlayer.Game.Owner == gamePlayer)
                {
                    try { gamePlayer.Game.Lobby.Games.Remove(gamePlayer.Game.ID); }
                    catch { }
                    gamePlayer.Game.Abort();
                    sendData(HabboPackets.GAME_PLAYER_EXIT);
                }
                else if (gamePlayer.teamID != -1)
                    gamePlayer.Game.movePlayer(gamePlayer, gamePlayer.teamID, -1);
                else
                {
                    gamePlayer.Game.Subviewers.Remove(gamePlayer);
                    sendData(new HabboPacketBuilder(HabboPackets.GAME_ENDED).Append(HabboProtocol.BOOL_FALSE).Build());
                    sendData(HabboPackets.GAME_PLAYER_EXIT);
                }
            }
            this.gamePlayer = null;
        }

        private delegate void TeleporterUsageSleep(Rooms.Items.floorItem Teleporter1, int idTeleporter2, int roomIDTeleporter2);
        private void useTeleporter(Rooms.Items.floorItem Teleporter1, int idTeleporter2, int roomIDTeleporter2)
        {
            roomUser.walkLock = true;
            string Sprite = Teleporter1.Sprite;
            if (roomIDTeleporter2 == _roomID)
            {
                Rooms.Items.floorItem Teleporter2 = Room.floorItemManager.getItem(idTeleporter2);
                Thread.Sleep(500);
                Room.sendData(new HabboPacketBuilder(HabboPackets.TELEPORTER_ENTER)
                    .Append(Teleporter1.ID).Append("/").Append(_Username).Append("/").Append(Sprite).Build());
                Room.sendData(new HabboPacketBuilder(HabboPackets.TELEPORTER_EXIT)
                    .Append(Teleporter2.ID).Append("/").Append(_Username).Append("/").Append(Sprite).Build());
                roomUser.X = Teleporter2.X;
                roomUser.Y = Teleporter2.Y;
                roomUser.H = Teleporter2.H;
                roomUser.Z1 = Teleporter2.Z;
                roomUser.Z2 = Teleporter2.Z;
                roomUser.Refresh();
                roomUser.walkLock = false;
            }
            else
            {
                _teleporterID = idTeleporter2;
                var packet = new HabboPacketBuilder(HabboPackets.TELEPORTER_DEST)
                    .AppendVL64(idTeleporter2)
                    .AppendVL64(roomIDTeleporter2);
                sendData(packet.Build());
                Room.sendData(new HabboPacketBuilder(HabboPackets.TELEPORTER_ENTER)
                    .Append(Teleporter1.ID).Append("/").Append(_Username).Append("/").Append(Sprite).Build());
            }
        }
        #endregion
    }
}
