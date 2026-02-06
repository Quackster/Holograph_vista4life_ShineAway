using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Holo.Data.Repositories.Furniture;
using Holo.Data.Repositories.Users;
using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;
using Holo.Virtual.Users.Items;
using Holo.Virtual.Users.Messenger;
using Holo.Virtual.Rooms.Games;
using Holo.Virtual;
using System.Collections.Generic;

namespace Holo.Virtual.Users;

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
                        FurnitureRepository.Instance.DeleteAllHandItems(userID);
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

                            Target.sendData(new HabboPacketBuilder("B!").Append(Message).Separator().Build());
                            sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_success")).Build());
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
                            Room.sendData(new HabboPacketBuilder("B!").Append(Message).Separator().Build());
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
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_success")).Build());
                                staffManager.addStaffMessage("kick", userID, Target.userID, Message, "");
                            }
                            else
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_failed")).Build());
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
                            sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_success")).Build());
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
                                Target.sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_muted")).Append("\r").Append(Message).Build());
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_success")).Build());
                                staffManager.addStaffMessage("mute", userID, Target.userID, Message, "");
                            }
                            else
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_failed")).Build());
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
                                Target.sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_unmuted")).Build());
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_success")).Build());
                                staffManager.addStaffMessage("unmute", userID, Target.userID, "", "");
                            }
                            else
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_failed")).Build());
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
                            sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_success")).Build());
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
                            sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_success")).Build());
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
                            int[] userDetails = UserRepository.Instance.GetUserIdAndRankByName(DB.Stripslash(args[1]));
                            if (userDetails.Length == 0)
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfailed")).Append("\r").Append(stringManager.getString("modtool_usernotfound")).Build());
                            else if ((byte)userDetails[1] > _Rank)
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfailed")).Append("\r").Append(stringManager.getString("modtool_rankerror")).Build());
                            else
                            {
                                int banHours = int.Parse(args[2]);
                                string Reason = stringManager.wrapParameters(args, 3);
                                if (banHours == 0 || Reason == "")
                                    sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_failed")).Build());
                                else
                                {
                                    staffManager.addStaffMessage("ban", userID, userDetails[0], Reason, "");
                                    userManager.setBan(userDetails[0], banHours, Reason);
                                    sendData(new HabboPacketBuilder("BK").Append(userManager.generateBanReport(userDetails[0])).Build());
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
                            int[] userDetails = UserRepository.Instance.GetUserIdAndRankByName(DB.Stripslash(args[1]));
                            if (userDetails.Length == 0)
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfailed")).Append("\r").Append(stringManager.getString("modtool_usernotfound")).Build());
                            else if ((byte)userDetails[1] > _Rank)
                                sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("modtool_actionfailed")).Append("\r").Append(stringManager.getString("modtool_rankerror")).Build());
                            else
                            {
                                int banHours = int.Parse(args[2]);
                                string Reason = stringManager.wrapParameters(args, 3);
                                if (banHours == 0 || Reason == "")
                                    sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_failed")).Build());
                                else
                                {
                                    string IP = UserRepository.Instance.GetIpAddressById(userDetails[0]) ?? "";
                                    staffManager.addStaffMessage("ban", userID, userDetails[0], Reason, "");
                                    userManager.setBan(IP, banHours, Reason);
                                    sendData(new HabboPacketBuilder("BK").Append(userManager.generateBanReport(IP)).Build());
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
                            sendData(new HabboPacketBuilder("BK").Append(userManager.generateUserInfo(userManager.getUserID(DB.Stripslash(args[1])), _Rank)).Build());
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
                                sendData(new HabboPacketBuilder("D^").Append("I").AppendVL64(Target._roomID).Build());
                            }
                            else if (Target.Room != null)
                            {
                                sendData(new HabboPacketBuilder("D^").Append("H").AppendVL64(Target._roomID).Build());
                            }
                            else
                            {
                                sendData(new HabboPacketBuilder("BK").Append("Unable to teleport, ").Append(Target._Username).Append(" is not in a room.").Build());
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
                            Target.sendData(new HabboPacketBuilder("BK").Append("You are being teleported to where ").Append(_Username).Append(" is.").Build());
                            if (_inPublicroom)
                            {
                                Target.sendData(new HabboPacketBuilder("D^").Append("I").AppendVL64(_roomID).Build());
                            }
                            else
                            {
                                Target.sendData(new HabboPacketBuilder("D^").Append("H").AppendVL64(_roomID).Build());
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
                            userManager.sendData(new HabboPacketBuilder("Dc").AppendVL64(Minutes).Build());
                            staffManager.addStaffMessage("offline", userID, 0, "mm=" + Minutes, "");
                        }
                        break;
                    }

                case "position": // Sends an alert with the virtual room user's current coordinates
                    {
                        if (rankManager.containsRight(_Rank, "fuse_administrator_access") == false)
                            return false;
                        else
                            sendData(new HabboPacketBuilder("BK").Append("X: ").Append(roomUser.X.ToString()).Append("\r").Append("Y: ").Append(roomUser.Y.ToString()).Append("\r").Append("Z: ").Append(roomUser.Z1.ToString()).Append("\r").Append("Height: ").Append(roomUser.H.ToString()).Build());
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
                            userManager.sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_hotelalert")).Append("\r").Append(Message).Build());
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
                            userManager.sendToRank(_Rank, false, new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_rankalert")).Append("\r").Append(Message).Build());
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
                            userManager.sendData(new HabboPacketBuilder("DBO").Append("\r").Build());
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
                            userManager.sendData(new HabboPacketBuilder("DBO").Append("\r").Build());
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
                            sendData(new HabboPacketBuilder("BK").Append("You don't have the rights to give coins to someone!").Build());
                        else
                        {
                            UserRepository.Instance.AddCredits(Target.userID, Credits);
                            Target.sendData(new HabboPacketBuilder("BK").Append("You have recieved ").Append(Credits.ToString()).Append(" credits from a staff member!").Build());
                            sendData(new HabboPacketBuilder("BK").Append("You've succesfully sent ").Append(Credits.ToString()).Append(" coins to ").Append(Target._Username).Append(".").Build());
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
                        sendData(new HabboPacketBuilder("BK")
                            .Append(":alert <user> <message>\r")
                            .Append(":roomalert <message>\r")
                            .Append(":kick <user> <message>\r")
                            .Append(":roomkick <message>\r")
                            .Append(":shutup <user> <message>\r")
                            .Append(":unmute <user>\r")
                            .Append(":roomshutup <message>\r")
                            .Append(":roomunmute\r")
                            .Append(":ban <user> <hours> <message>\r")
                            .Append(":superban <user> <hours> <message>\r")
                            .Append(":ha <message>\r")
                            .Append(":ra <message>\r")
                            .Append(":teleport\r")
                            .Append(":warp X Y\r")
                            .Append(":position \r")
                            .Append(":transfer \r")
                            .Append(":refresh  \r")
                            .Append(":coins <howmuch> \r")
                            //":masscredits <howmuch> \r" +
                            .Append(":info <username>\r")
                            .Build());
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
        catch { sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("scommand_failed")).Build()); }
        return true;
    }
    #endregion
}
