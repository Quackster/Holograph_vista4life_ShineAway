using System;
using System.Text;

using Holo.Managers;
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
                                    if (rankManager.containsRight(_Rank, "fuse_alert") == false) { sendData("BK" + stringManager.getString("modtool_accesserror")); return true; }

                                    messageLength = Encoding.decodeB64(currentPacket.Substring(4, 2));
                                    Message = currentPacket.Substring(6, messageLength).Replace(Convert.ToChar(1).ToString(), " ");
                                    staffNoteLength = Encoding.decodeB64(currentPacket.Substring(messageLength + 6, 2));
                                    staffNote = currentPacket.Substring(messageLength + 8, staffNoteLength);
                                    targetUser = currentPacket.Substring(messageLength + staffNoteLength + 10);

                                    if (Message == "" || targetUser == "")
                                        return true;

                                    virtualUser _targetUser = userManager.getUser(targetUser);
                                    if (_targetUser == null)
                                        sendData("BK" + stringManager.getString("modtool_actionfail") + "\r" + stringManager.getString("modtool_usernotfound"));
                                    else
                                    {
                                        _targetUser.sendData("B!" + Message + Convert.ToChar(2));
                                        staffManager.addStaffMessage("alert", userID, _targetUser.userID, Message, staffNote);
                                    }
                                    break;
                                }
                            #endregion

                            #region Kick single user from room
                            case "HI": // Kick single user from room
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_kick") == false) { sendData("BK" + stringManager.getString("modtool_accesserror")); return true; }

                                    messageLength = Encoding.decodeB64(currentPacket.Substring(4, 2));
                                    Message = currentPacket.Substring(6, messageLength).Replace(Convert.ToChar(1).ToString(), " ");
                                    staffNoteLength = Encoding.decodeB64(currentPacket.Substring(messageLength + 6, 2));
                                    staffNote = currentPacket.Substring(messageLength + 8, staffNoteLength);
                                    targetUser = currentPacket.Substring(messageLength + staffNoteLength + 10);

                                    if (Message == "" || targetUser == "")
                                        return true;

                                    virtualUser _targetUser = userManager.getUser(targetUser);
                                    if (_targetUser == null)
                                        sendData("BK" + stringManager.getString("modtool_actionfail") + "\r" + stringManager.getString("modtool_usernotfound"));
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
                                                sendData("BK" + stringManager.getString("modtool_actionfail") + "\r" + stringManager.getString("modtool_rankerror"));
                                        }
                                    }
                                    break;
                                }
                            #endregion

                            #region Ban single user
                            case "HJ": // Ban single user / IP
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_ban") == false) { sendData("BK" + stringManager.getString("modtool_accesserror")); return true; }

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
                                        string[] userDetails = DB.runReadRow("SELECT id,rank,ipaddress_last FROM users WHERE name = '" + DB.Stripslash(targetUser) + "'");
                                        if (userDetails.Length == 0)
                                        {
                                            sendData("BK" + stringManager.getString("modtool_actionfail") + "\r" + stringManager.getString("modtool_usernotfound"));
                                            return true;
                                        }
                                        else if (byte.Parse(userDetails[1]) >= _Rank)
                                        {
                                            sendData("BK" + stringManager.getString("modtool_actionfail") + "\r" + stringManager.getString("modtool_rankerror"));
                                            return true;
                                        }

                                        int targetID = int.Parse(userDetails[0]);
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

                                        sendData("BK" + Report);
                                    }
                                    break;
                                }
                            #endregion

                            #region Room alert
                            case "IH": // Alert all users in current room
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_room_alert") == false) { sendData("BK" + stringManager.getString("modtool_accesserror")); return true; }
                                    if (Room == null || roomUser == null) { return true; }

                                    messageLength = Encoding.decodeB64(currentPacket.Substring(4, 2));
                                    Message = currentPacket.Substring(6, messageLength).Replace(Convert.ToChar(1).ToString(), " ");
                                    staffNoteLength = Encoding.decodeB64(currentPacket.Substring(messageLength + 6, 2));
                                    staffNote = currentPacket.Substring(messageLength + 8, staffNoteLength);

                                    if (Message != "")
                                    {
                                        Room.sendData("B!" + Message + Convert.ToChar(2));
                                        staffManager.addStaffMessage("ralert", userID, _roomID, Message, staffNote);
                                    }
                                    break;
                                }
                            #endregion

                            #region Room kick
                            case "II": // Kick all users below users rank from room
                                {
                                    if (rankManager.containsRight(_Rank, "fuse_room_kick") == false) { sendData("BK" + stringManager.getString("modtool_accesserror")); return true; }
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
                        string[] cfhStats = DB.runReadRow("SELECT id, date, message, picked_up FROM cms_help WHERE username = '" + _Username + "' AND picked_up = '0'");
                        if (cfhStats.Length == 0)
                            sendData("D" + "H");
                        else
                            sendData("D" + "I" + cfhStats[0] + Convert.ToChar(2) + cfhStats[1] + Convert.ToChar(2) + cfhStats[2] + Convert.ToChar(2));
                        break;
                    }

                case "Cn": // User deletes his pending CFH message
                    {
                        int cfhID = DB.runRead("SELECT id FROM cms_help WHERE username = '" + _Username + "' AND picked_up = '0'", null);
                        DB.runQuery("DELETE FROM cms_help WHERE picked_up = '0' AND username = '" + _Username + "' LIMIT 1");
                        sendData("DH");
                        userManager.sendToRank(Config.Minimum_CFH_Rank, true, "BT" + Encoding.encodeVL64(cfhID) + Convert.ToChar(2) + "I" + "User Deleted!" + Convert.ToChar(2) + "User Deleted!" + Convert.ToChar(2) + "User Deleted!" + Convert.ToChar(2) + Encoding.encodeVL64(0) + Convert.ToChar(2) + "" + Convert.ToChar(2) + "H" + Convert.ToChar(2) + Encoding.encodeVL64(0));
                        break;
                    }

                case "AV": // User sends CFH message
                    {
                        if (DB.checkExists("SELECT id FROM cms_help WHERE username = '" + _Username + "' AND picked_up = '0'"))
                            return true;
                        int messageLength = Encoding.decodeB64(currentPacket.Substring(2, 2));
                        if (messageLength == 0)
                            return true;
                        string cfhMessage = currentPacket.Substring(4, messageLength);
                        DB.runQuery("INSERT INTO cms_help (username,ip,message,date,picked_up,subject,roomid) VALUES ('" + _Username + "','" + connectionRemoteIP + "','" + DB.Stripslash(cfhMessage) + "','" + DateTime.Now + "','0','CFH message [hotel]','" + _roomID.ToString() + "')");
                        int cfhID = DB.runRead("SELECT id FROM cms_help WHERE username = '" + _Username + "' AND picked_up = '0'", null);
                        string roomName = DB.runRead("SELECT name FROM rooms WHERE id = '" + _roomID + "'"); //          H = Automated / I = Manual                                                                                                                                                                          H = Hide Room ID / I = Show Room ID
                        sendData("EAH"); //                                                                                           \_/                                                                                                                                                                                                    \_/
                        userManager.sendToRank(Config.Minimum_CFH_Rank, true, "BT" + Encoding.encodeVL64(cfhID) + Convert.ToChar(2) + "I" + "Sent: " + DateTime.Now + Convert.ToChar(2) + _Username + Convert.ToChar(2) + cfhMessage + Convert.ToChar(2) + Encoding.encodeVL64(_roomID) + Convert.ToChar(2) + roomName + Convert.ToChar(2) + "I" + Convert.ToChar(2) + Encoding.encodeVL64(_roomID));
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

                        string toUserName = DB.runRead("SELECT username FROM cms_help WHERE id = '" + cfhID + "'");
                        if (toUserName == null)
                            sendData("BK" + stringManager.getString("cfh_fail"));
                        else
                        {
                            int toUserID = userManager.getUserID(toUserName);
                            virtualUser toVirtualUser = userManager.getUser(toUserID);
                            if (toVirtualUser._isLoggedIn)
                            {
                                toVirtualUser.sendData("DR" + cfhReply + Convert.ToChar(2));
                                DB.runQuery("UPDATE cms_help SET picked_up = '" + _Username + "' WHERE id = '" + cfhID + "' LIMIT 1");
                            }
                        }
                        break;
                    }

                case "CF": // CFH center - Delete (Downgrade)
                    {
                        if (rankManager.containsRight(_Rank, "fuse_receive_calls_for_help") == false)
                            return true;
                        int cfhID = Encoding.decodeVL64(currentPacket.Substring(4, Encoding.decodeB64(currentPacket.Substring(2, 2))));
                        string[] cfhStats = DB.runReadRow("SELECT username,message,date,picked_up,roomid FROM cms_help WHERE id = '" + cfhID + "'");
                        if (cfhStats.Length == 0)
                            return true;
                        else
                        {
                            if (cfhStats[3] == "1")
                                sendData("BK" + stringManager.getString("cfh_picked_up"));
                            else
                            {
                                DB.runQuery("DELETE FROM cms_help WHERE id = '" + cfhID + "' LIMIT 1");
                                userManager.sendToRank(Config.Minimum_CFH_Rank, true, "BT" + Encoding.encodeVL64(cfhID) + Convert.ToChar(2) + "H" + "Staff Deleted!" + Convert.ToChar(2) + "Staff Deleted!" + Convert.ToChar(2) + "Staff Deleted!" + Convert.ToChar(2) + Encoding.encodeVL64(0) + Convert.ToChar(2) + "" + Convert.ToChar(2) + "H" + Convert.ToChar(2) + Encoding.encodeVL64(0));
                            }
                        }
                        break;
                    }

                case "@p": // CFH center - Pickup
                    {
                        int cfhID = Encoding.decodeVL64(currentPacket.Substring(4));
                        if (DB.checkExists("SELECT id FROM cms_help WHERE id = '" + cfhID + "'") == false)
                        {
                            sendData(stringManager.getString("cfh_deleted"));
                            return true;
                        }
                        string[] cfhData = DB.runReadRow("SELECT picked_up,username,message,roomid FROM cms_help WHERE id = '" + cfhID + "'");
                        string roomName = DB.runRead("SELECT name FROM rooms WHERE id = '" + cfhData[3] + "'");
                        if (cfhData[0] == "1")
                            sendData("BK" + stringManager.getString("cfh_picked_up"));
                        else
                            userManager.sendToRank(Config.Minimum_CFH_Rank, true, "BT" + Encoding.encodeVL64(cfhID) + Convert.ToChar(2) + "I" + "Picked up: " + DateTime.Now + Convert.ToChar(2) + cfhData[1] + Convert.ToChar(2) + cfhData[2] + Convert.ToChar(2) + Encoding.encodeVL64(int.Parse(cfhData[3])) + Convert.ToChar(2) + roomName + Convert.ToChar(2) + "I" + Convert.ToChar(2) + Encoding.encodeVL64(int.Parse(cfhData[3])));
                        DB.runQuery("UPDATE cms_help SET picked_up = '1' WHERE id = '" + cfhID + "' LIMIT 1");
                        break;
                    }

                case "EC": // Go to the room that the call for help was sent from
                    {
                        if (rankManager.containsRight(_Rank, "fuse_receive_calls_for_help") == false)
                            return true;
                        int idLength = Encoding.decodeB64(currentPacket.Substring(2, 2));
                        int cfhID = Encoding.decodeVL64(currentPacket.Substring(4, idLength));
                        int roomID = DB.runRead("SELECT roomid FROM cms_help WHERE id = '" + cfhID + "'", null);
                        if (roomID == 0)
                            return true;
                        virtualRoom room = roomManager.getRoom(roomID);
                        if (room.isPublicroom)
                            sendData("D^" + "I" + Encoding.encodeVL64(roomID));
                        else
                            sendData("D^" + "H" + Encoding.encodeVL64(roomID));

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
