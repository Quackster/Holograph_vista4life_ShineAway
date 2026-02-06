using System;
using System.Text;

using Holo.Data.Repositories.Rooms;
using Holo.Data.Repositories.System;
using Holo.Data.Repositories.Users;
using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Contains moderation-related packet handlers for the virtualUser class.
    /// Handles MOD-Tool region (case CH with sub-cases HH, HI, HJ, IH, II) and
    /// Call For Help region (cases Cm, Cn, AV for user side, and CG, CF, @p, EC for staff side).
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes moderation-related packets including MOD-Tool operations (alerts, kicks, bans)
        /// and Call For Help operations (submit, delete, reply, pickup, goto room).
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        /// <returns>True if the packet was handled, false otherwise.</returns>
        private bool processModerationPackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                #region Moderation
                #region MOD-Tool
                case "CH": // MOD-Tool
                    {
                        int messageLength = 0;
                        string Message = "";
                        int staffNoteLength = 0;
                        string staffNote = "";
                        string targetUser = "";

                        switch (currentPacket.Substring(2, 2)) // Select the action
                        {
                            #region Alert single user
                            case "HH": // Alert single user
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_alert") == false) { sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_accesserror")).Build()); return true; }

                                    messageLength = Encoding.decodeB64(currentPacket.Substring(4, 2));
                                    Message = currentPacket.Substring(6, messageLength).Replace(Convert.ToChar(1).ToString(), " ");
                                    staffNoteLength = Encoding.decodeB64(currentPacket.Substring(messageLength + 6, 2));
                                    staffNote = currentPacket.Substring(messageLength + 8, staffNoteLength);
                                    targetUser = currentPacket.Substring(messageLength + staffNoteLength + 10);

                                    if (Message == "" || targetUser == "")
                                        return true;

                                    virtualUser _targetUser = userManager.getUser(targetUser);
                                    if (_targetUser == null)
                                        sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfail")).Append("\r").Append(stringManager.getString("modtool_usernotfound")).Build());
                                    else
                                    {
                                        _targetUser.sendData(new HabboPacketBuilder("B!").Append(Message).Separator().Build());
                                        staffManager.addStaffMessage("alert", userID, _targetUser.userID, Message, staffNote);
                                    }
                                    break;
                                }
                            #endregion

                            #region Kick single user from room
                            case "HI": // Kick single user from room
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_kick") == false) { sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_accesserror")).Build()); return true; }

                                    messageLength = Encoding.decodeB64(currentPacket.Substring(4, 2));
                                    Message = currentPacket.Substring(6, messageLength).Replace(Convert.ToChar(1).ToString(), " ");
                                    staffNoteLength = Encoding.decodeB64(currentPacket.Substring(messageLength + 6, 2));
                                    staffNote = currentPacket.Substring(messageLength + 8, staffNoteLength);
                                    targetUser = currentPacket.Substring(messageLength + staffNoteLength + 10);

                                    if (Message == "" || targetUser == "")
                                        return true;

                                    virtualUser _targetUser = userManager.getUser(targetUser);
                                    if (_targetUser == null)
                                        sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfail")).Append("\r").Append(stringManager.getString("modtool_usernotfound")).Build());
                                    else
                                    {
                                        if (_targetUser.Room != null && _targetUser.roomUser != null)
                                        {
                                            if (_targetUser._Rank < _Rank)
                                            {
                                                _targetUser.Room.removeUser(_targetUser.roomUser.roomUID, true, Message);
                                                staffManager.addStaffMessage("kick", userID, _targetUser.userID, Message, staffNote);
                                            }
                                            else
                                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfail")).Append("\r").Append(stringManager.getString("modtool_rankerror")).Build());
                                        }
                                    }
                                    break;
                                }
                            #endregion

                            #region Ban single user
                            case "HJ": // Ban single user / IP
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_ban") == false) { sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_accesserror")).Build()); return true; }

                                    int targetUserLength = 0;
                                    int banHours = 0;
                                    bool banIP = (currentPacket.Substring(currentPacket.Length - 1, 1) == "I");

                                    messageLength = Encoding.decodeB64(currentPacket.Substring(4, 2));
                                    Message = currentPacket.Substring(6, messageLength).Replace(Convert.ToChar(1).ToString(), " ");
                                    staffNoteLength = Encoding.decodeB64(currentPacket.Substring(messageLength + 6, 2));
                                    staffNote = currentPacket.Substring(messageLength + 8, staffNoteLength);
                                    targetUserLength = Encoding.decodeB64(currentPacket.Substring(messageLength + staffNoteLength + 8, 2));
                                    targetUser = currentPacket.Substring(messageLength + staffNoteLength + 10, targetUserLength);
                                    banHours = Encoding.decodeVL64(currentPacket.Substring(messageLength + staffNoteLength + targetUserLength + 10));

                                    if (Message == "" || targetUser == "" || banHours == 0)
                                        return true;
                                    else
                                    {
                                        string[] userDetails = UserRepository.Instance.GetUserDetailsForBanReport(UserRepository.Instance.GetUserId(targetUser));
                                        if (userDetails.Length == 0)
                                        {
                                            sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfail")).Append("\r").Append(stringManager.getString("modtool_usernotfound")).Build());
                                            return true;
                                        }
                                        else if (byte.Parse(userDetails[1]) >= _Rank)
                                        {
                                            sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfail")).Append("\r").Append(stringManager.getString("modtool_rankerror")).Build());
                                            return true;
                                        }

                                        int targetID = UserRepository.Instance.GetUserId(targetUser);
                                        string Report = "";
                                        staffManager.addStaffMessage("ban", userID, targetID, Message, staffNote);
                                        if (banIP && rankManager.containsRight(_Rank, "fuse_superban")) // IP ban is chosen and allowed for this staff member
                                        {
                                            userManager.setBan(userDetails[2], banHours, Message);
                                            Report = userManager.generateBanReport(userDetails[2]);
                                        }
                                        else
                                        {
                                            userManager.setBan(targetID, banHours, Message);
                                            Report = userManager.generateBanReport(targetID);
                                        }

                                        sendData(new HabboPacketBuilder("BK").Append(Report).Build());
                                    }
                                    break;
                                }
                            #endregion

                            #region Room alert
                            case "IH": // Alert all users in current room
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_room_alert") == false) { sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_accesserror")).Build()); return true; }
                                    if (Room == null || roomUser == null) { return true; }

                                    messageLength = Encoding.decodeB64(currentPacket.Substring(4, 2));
                                    Message = currentPacket.Substring(6, messageLength).Replace(Convert.ToChar(1).ToString(), " ");
                                    staffNoteLength = Encoding.decodeB64(currentPacket.Substring(messageLength + 6, 2));
                                    staffNote = currentPacket.Substring(messageLength + 8, staffNoteLength);

                                    if (Message != "")
                                    {
                                        Room.sendData(new HabboPacketBuilder("B!").Append(Message).Separator().Build());
                                        staffManager.addStaffMessage("ralert", userID, _roomID, Message, staffNote);
                                    }
                                    break;
                                }
                            #endregion

                            #region Room kick
                            case "II": // Kick all users below users rank from room
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_room_kick") == false) { sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_accesserror")).Build()); return true; }
                                    if (Room == null || roomUser == null) { return true; }

                                    messageLength = Encoding.decodeB64(currentPacket.Substring(4, 2));
                                    Message = currentPacket.Substring(6, messageLength).Replace(Convert.ToChar(1).ToString(), " ");
                                    staffNoteLength = Encoding.decodeB64(currentPacket.Substring(messageLength + 6, 2));
                                    staffNote = currentPacket.Substring(messageLength + 8, staffNoteLength);

                                    if (Message != "")
                                    {
                                        Room.kickUsers(_Rank, Message);
                                        staffManager.addStaffMessage("rkick", userID, _roomID, Message, staffNote);
                                    }
                                    break;
                                }
                            #endregion
                        }
                        break;
                    }
                #endregion

                #region Call For Help

                #region User Side
                case "Cm": // User wants to send a CFH message
                    {
                        string[] cfhStats = MiscRepository.Instance.GetPendingCfhByUsername(_Username);
                        if (cfhStats.Length == 0)
                            sendData(new HabboPacketBuilder("D").Append("H").Build());
                        else
                            sendData(new HabboPacketBuilder("D").Append("I").Append(cfhStats[0]).Separator().Append(cfhStats[1]).Separator().Append(cfhStats[2]).Separator().Build());
                        break;
                    }

                case "Cn": // User deletes his pending CFH message
                    {
                        int cfhID = MiscRepository.Instance.GetPendingCfhIdByUsername(_Username);
                        MiscRepository.Instance.DeletePendingCfhByUsername(_Username);
                        sendData("DH");
                        userManager.sendToRank(Config.Minimum_CFH_Rank, true, new HabboPacketBuilder("BT").AppendVL64(cfhID).Separator().Append("I").Append("User Deleted!").Separator().Append("User Deleted!").Separator().Append("User Deleted!").Separator().AppendVL64(0).Separator().Append("").Separator().Append("H").Separator().AppendVL64(0).Build());
                        break;
                    }

                case "AV": // User sends CFH message
                    {
                        if (MiscRepository.Instance.HasPendingCfh(_Username))
                            return true;
                        int messageLength = Encoding.decodeB64(currentPacket.Substring(2, 2));
                        if (messageLength == 0)
                            return true;
                        string cfhMessage = currentPacket.Substring(4, messageLength);
                        MiscRepository.Instance.CreateCfh(_Username, connectionRemoteIP, cfhMessage, _roomID);
                        int cfhID = MiscRepository.Instance.GetPendingCfhIdByUsername(_Username);
                        string? roomName = RoomRepository.Instance.GetRoomName(_roomID); //          H = Automated / I = Manual                                                                                                                                                                          H = Hide Room ID / I = Show Room ID
                        sendData("EAH"); //                                                                                           \_/                                                                                                                                                                                                    \_/
                        userManager.sendToRank(Config.Minimum_CFH_Rank, true, new HabboPacketBuilder("BT").AppendVL64(cfhID).Separator().Append("I").Append("Sent: " + DateTime.Now).Separator().Append(_Username).Separator().Append(cfhMessage).Separator().AppendVL64(_roomID).Separator().Append(roomName ?? "").Separator().Append("I").Separator().AppendVL64(_roomID).Build());
                        break;
                    }
                #endregion

                #region Staff Side
                case "CG": // CFH center - reply call
                    {
                        if (rankManager.containsRight(_Rank, "fuse_receive_calls_for_help") == false)
                            return true;
                        int cfhID = Encoding.decodeVL64(currentPacket.Substring(4, Encoding.decodeB64(currentPacket.Substring(2, 2))));
                        string cfhReply = currentPacket.Substring(Encoding.decodeB64(currentPacket.Substring(2, 2)) + 6);

                        string? toUserName = MiscRepository.Instance.GetCfhUsername(cfhID);
                        if (toUserName == null)
                            sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("cfh_fail")).Build());
                        else
                        {
                            int toUserID = userManager.getUserID(toUserName);
                            virtualUser toVirtualUser = userManager.getUser(toUserID);
                            if (toVirtualUser._isLoggedIn)
                            {
                                toVirtualUser.sendData(new HabboPacketBuilder("DR").Append(cfhReply).Separator().Build());
                                MiscRepository.Instance.MarkCfhPickedUp(cfhID, _Username);
                            }
                        }
                        break;
                    }

                case "CF": // CFH center - Delete (Downgrade)
                    {
                        if (rankManager.containsRight(_Rank, "fuse_receive_calls_for_help") == false)
                            return true;
                        int cfhID = Encoding.decodeVL64(currentPacket.Substring(4, Encoding.decodeB64(currentPacket.Substring(2, 2))));
                        string[] cfhStats = MiscRepository.Instance.GetCfhDetails(cfhID);
                        if (cfhStats.Length == 0)
                            return true;
                        else
                        {
                            if (cfhStats[3] == "1")
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("cfh_picked_up")).Build());
                            else
                            {
                                MiscRepository.Instance.DeleteCfh(cfhID);
                                userManager.sendToRank(Config.Minimum_CFH_Rank, true, new HabboPacketBuilder("BT").AppendVL64(cfhID).Separator().Append("H").Append("Staff Deleted!").Separator().Append("Staff Deleted!").Separator().Append("Staff Deleted!").Separator().AppendVL64(0).Separator().Append("").Separator().Append("H").Separator().AppendVL64(0).Build());
                            }
                        }
                        break;
                    }

                case "@p": // CFH center - Pickup
                    {
                        int cfhID = Encoding.decodeVL64(currentPacket.Substring(4));
                        if (MiscRepository.Instance.CfhExists(cfhID) == false)
                        {
                            sendData(stringManager.getString("cfh_deleted"));
                            return true;
                        }
                        string[] cfhData = MiscRepository.Instance.GetCfhDetails(cfhID);
                        string? roomName = RoomRepository.Instance.GetRoomName(int.Parse(cfhData[4]));
                        if (cfhData[3] == "1")
                            sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("cfh_picked_up")).Build());
                        else
                            userManager.sendToRank(Config.Minimum_CFH_Rank, true, new HabboPacketBuilder("BT").AppendVL64(cfhID).Separator().Append("I").Append("Picked up: " + DateTime.Now).Separator().Append(cfhData[0]).Separator().Append(cfhData[1]).Separator().AppendVL64(int.Parse(cfhData[4])).Separator().Append(roomName ?? "").Separator().Append("I").Separator().AppendVL64(int.Parse(cfhData[4])).Build());
                        MiscRepository.Instance.MarkCfhPickedUp(cfhID, "1");
                        break;
                    }

                case "EC": // Go to the room that the call for help was sent from
                    {
                        if (rankManager.containsRight(_Rank, "fuse_receive_calls_for_help") == false)
                            return true;
                        int idLength = Encoding.decodeB64(currentPacket.Substring(2, 2));
                        int cfhID = Encoding.decodeVL64(currentPacket.Substring(4, idLength));
                        int roomID = MiscRepository.Instance.GetCfhRoomId(cfhID);
                        if (roomID == 0)
                            return true;
                        virtualRoom room = roomManager.getRoom(roomID);
                        if (room.isPublicroom)
                            sendData(new HabboPacketBuilder("D^").Append("I").AppendVL64(roomID).Build());
                        else
                            sendData(new HabboPacketBuilder("D^").Append("H").AppendVL64(roomID).Build());

                        break;
                    }
                #endregion
                #endregion
                #endregion

                default:
                    return false; // Packet was not handled
            }
            return true; // Packet was handled
        }
    }
}
