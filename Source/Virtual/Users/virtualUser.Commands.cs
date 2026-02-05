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
    /// Represents a virtual user - Commands partial class containing speech command handling.
    /// </summary>
    public partial class virtualUser
    {
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
                            DB.runQuery("DELETE FROM furniture WHERE ownerid = '" + userID + "' AND roomid = '0'");
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
                                int[] userDetails = DB.runReadRow("SELECT id,rank FROM users WHERE name = '" + DB.Stripslash(args[1]) + "'", null);
                                if (userDetails.Length == 0)
                                    sendData("BK" + stringManager.getString("modtool_actionfailed") + "\r" + stringManager.getString("modtool_usernotfound"));
                                else if ((byte)userDetails[1] > _Rank)
                                    sendData("BK" + stringManager.getString("modtool_actionfailed") + "\r" + stringManager.getString("modtool_rankerror"));
                                else
                                {
                                    int banHours = int.Parse(args[2]);
                                    string Reason = stringManager.wrapParameters(args, 3);
                                    if (banHours == 0 || Reason == "")
                                        sendData("BK" + stringManager.getString("scommand_failed"));
                                    else
                                    {
                                        staffManager.addStaffMessage("ban", userID, userDetails[0], Reason, "");
                                        userManager.setBan(userDetails[0], banHours, Reason);
                                        sendData("BK" + userManager.generateBanReport(userDetails[0]));
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
                                int[] userDetails = DB.runReadRow("SELECT id,rank FROM users WHERE name = '" + DB.Stripslash(args[1]) + "'", null);
                                if (userDetails.Length == 0)
                                    sendData("BK" + stringManager.getString("modtool_actionfailed") + "\r" + stringManager.getString("modtool_usernotfound"));
                                else if ((byte)userDetails[1] > _Rank)
                                    sendData("BK" + stringManager.getString("modtool_actionfailed") + "\r" + stringManager.getString("modtool_rankerror"));
                                else
                                {
                                    int banHours = int.Parse(args[2]);
                                    string Reason = stringManager.wrapParameters(args, 3);
                                    if (banHours == 0 || Reason == "")
                                        sendData("BK" + stringManager.getString("scommand_failed"));
                                    else
                                    {
                                        string IP = DB.runRead("SELECT ipaddress_last FROM users WHERE id = '" + userDetails[0] + "'");
                                        staffManager.addStaffMessage("ban", userID, userDetails[0], Reason, "");
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
                                DB.runQuery("UPDATE users SET credits = " + Target._Credits + " + " + Credits + " WHERE id = " + Target.userID + "");
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
        #endregion
    }
}
