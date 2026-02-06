using System;
using System.Text;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;

namespace Holo.Virtual.Users;

/// <summary>
/// Partial class for virtualUser containing in-room action packet handlers.
/// </summary>
public partial class virtualUser
{
    /// <summary>
    /// Processes in-room action packets including movement, chat, rights management, and user interactions.
    /// </summary>
    /// <param name="currentPacket">The packet string to process.</param>
    private bool processInRoomPackets(string currentPacket)
    {
        switch (currentPacket.Substring(0, 2))
        {
            #region In-room actions

            case "cI": // Ignore Habbo
                {
                    if (Room != null && roomUser != null && statusManager.containsStatus("sit") == true && statusManager.containsStatus("lay") == true)
                    {
                        statusManager.dropCarrydItem();
                        if (currentPacket.Length == 2)
                            statusManager.addStatus("ignore", "");
                        statusManager.Refresh();
                    }
                    break;
                }

            case "cK": // lising habbo
                {
                    break;
                }

            case "AO": // Room - rotate user
                {
                    if (Room != null && roomUser != null && statusManager.containsStatus("sit") == false && statusManager.containsStatus("lay") == false)
                    {
                        int X = int.Parse(currentPacket.Substring(2).Split(' ')[0]);
                        int Y = int.Parse(currentPacket.Split(' ')[1]);
                        roomUser.Z1 = Rooms.Pathfinding.Rotation.Calculate(roomUser.X, roomUser.Y, X, Y);
                        roomUser.Z2 = roomUser.Z1;
                        roomUser.Refresh();
                    }
                    break;
                }

            case "AK": // Room - walk to a new square
                {
                    if (Room != null && roomUser != null && roomUser.walkLock == false)
                    {
                        int goalX = Encoding.decodeB64(currentPacket.Substring(2, 2));
                        int goalY = Encoding.decodeB64(currentPacket.Substring(4, 2));
                        {
                            roomUser.goalX = goalX;
                            roomUser.goalY = goalY;
                        }
                    }
                    break;
                }


            case "As": // Room - click door to exit room
                {
                    if (Room != null && roomUser != null && roomUser.walkDoor == false)
                    {
                        roomUser.walkDoor = true;
                        roomUser.goalX = Room.doorX;
                        roomUser.goalY = Room.doorY;
                    }
                    break;
                }

            case "At": // Room - select swimming outfit
                {
                    if (Room != null || roomUser != null && Room.hasSwimmingPool)
                    {
                        virtualRoom.squareTrigger Trigger = Room.getTrigger(roomUser.X, roomUser.Y);
                        if (Trigger.Object == "curtains1" || Trigger.Object == "curtains2")
                        {
                            string Outfit = DB.Stripslash(currentPacket.Substring(2));
                            roomUser.SwimOutfit = Outfit;
                            Room.sendData(new HabboPacketBuilder(@"@\").Append(roomUser.detailsString).Build());
                            Room.sendSpecialCast(Trigger.Object, "open");
                            roomUser.walkLock = false;
                            roomUser.goalX = Trigger.goalX;
                            roomUser.goalY = Trigger.goalY;
                            DB.runQuery("UPDATE users SET figure_swim = '" + Outfit + "' WHERE id = '" + userID + "' LIMIT 1");
                            //Refresh();
                            userManager.getUser(userID).refreshAppearance(true, true, true);
                        }
                    }
                    break;
                }
            case "B^": // Badges - switch or toggle on/off badge
                {
                    if (Room != null && roomUser != null)
                    {
                        // Reset slots
                        DB.runQuery("UPDATE users_badges SET slotid = '0' WHERE userid = '" + this.userID + "'");

                        int enabledBadgeAmount = 0;
                        string szWorkData = currentPacket.Substring(2);
                        while (szWorkData != "")
                        {
                            int slotID = Encoding.decodeVL64(szWorkData);
                            szWorkData = szWorkData.Substring(Encoding.encodeVL64(slotID).Length);

                            int badgeNameLength = Encoding.decodeB64(szWorkData.Substring(0, 2));

                            if (badgeNameLength > 0)
                            {
                                string Badge = szWorkData.Substring(2, badgeNameLength);
                                DB.runQuery("UPDATE users_badges SET slotid = '" + slotID + "' WHERE userid = '" + this.userID + "' AND badge = '" + Badge + "' LIMIT 1"); // update slot
                                enabledBadgeAmount++;
                            }

                            szWorkData = szWorkData.Substring(badgeNameLength + 2);
                        }
                        // Active badges have their badge slot set now, other ones have '0'

                        this.refreshBadges();

                        var badgeNotify = new HabboPacketBuilder("Cd")
                            .Append(this.userID)
                            .Separator()
                            .AppendVL64(enabledBadgeAmount);
                        for (int x = 0; x < _Badges.Count; x++)
                        {
                            if (_badgeSlotIDs[x] > 0) // Badge enabled
                            {
                                badgeNotify.AppendVL64(_badgeSlotIDs[x])
                                    .Append(_Badges[x])
                                    .Separator();
                            }
                        }

                        this.Room.sendData(badgeNotify.Build());
                    }
                    break;
                }


            case "DG": // Tags - get tags of virtual user
                {
                    int ownerID = Encoding.decodeVL64(currentPacket.Substring(2));
                    string[] Tags = DB.runReadColumn("SELECT tag FROM cms_tags WHERE ownerid = '" + ownerID + "'", 20);
                    var tagPacket = new HabboPacketBuilder("E^")
                        .AppendVL64(ownerID)
                        .AppendVL64(Tags.Length);
                    for (int i = 0; i < Tags.Length; i++)
                        tagPacket.Append(Tags[i]).Separator();
                    sendData(tagPacket.Build());
                    break;
                }

            case "Cg": // Group badges - get details about a group [click badge]
                {
                    if (Room != null && roomUser != null)
                    {
                        int groupID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (DB.checkExists("SELECT id FROM groups_details WHERE id = '" + groupID + "'"))
                        {
                            string Name = DB.runRead("SELECT name FROM groups_details WHERE id = '" + groupID + "'");
                            string Description = DB.runRead("SELECT description FROM groups_details WHERE id = '" + groupID + "'");

                            int roomID = DB.runRead("SELECT roomid FROM groups_details WHERE id = '" + groupID + "'", null);
                            string roomName = "";
                            if (roomID > 0)
                                roomName = DB.runRead("SELECT name FROM rooms WHERE id = '" + roomID + "'");
                            else
                                roomID = -1;

                            sendData(new HabboPacketBuilder("Dw")
                                .AppendVL64(groupID)
                                .Append(Name).Separator()
                                .Append(Description).Separator()
                                .AppendVL64(roomID)
                                .Append(roomName).Separator()
                                .Build());
                        }
                    }
                    break;
                }

            case "D": // Ignore User
                {
                    try
                    {
                        virtualUser Target = userManager.getUser(DB.Stripslash(currentPacket.Substring(4)));
                        if (Target._Rank > 3)
                            return true;
                        DB.runQuery("INSERT INTO user_ignores(userid,targetid) VALUES ('" + userID + "','" + Target.userID + "')");
                        ignoreList.Add(Target.userID);
                        sendData("FcI");
                    }
                    catch { }
                    break;
                }

            case "EB": // Unignore User
                {
                    try
                    {
                        virtualUser Target = userManager.getUser(DB.Stripslash(currentPacket.Substring(4)));
                        if (Target._Rank > 3)
                            return true;
                        DB.runQuery("DELETE FROM user_ignores WHERE userid = '" + userID + "' AND targetid = '" + Target.userID + "'");
                        ignoreList.Remove(Target.userID);
                        sendData("FcK");
                    }
                    catch { }
                    break;
                }

            case "AX": // Statuses - stop status
                {
                    if (statusManager != null)
                    {
                        string Status = currentPacket.Substring(2);
                        if (Status == "CarryItem")
                            statusManager.dropCarrydItem();
                        else if (Status == "Dance")
                        {
                            statusManager.removeStatus("dance");
                            statusManager.Refresh();
                        }
                    }
                    break;
                }

            case "A^": // Statuses - wave
                {
                    if (Room != null && roomUser != null && statusManager.containsStatus("wave") == false)
                    {
                        statusManager.removeStatus("dance");
                        statusManager.handleStatus("wave", "", Config.Statuses_Wave_waveDuration);
                    }
                    break;
                }

            case "A]": // Statuses - dance
                {
                    if (Room != null && roomUser != null && statusManager.containsStatus("sit") == false && statusManager.containsStatus("lay") == false)
                    {
                        statusManager.dropCarrydItem();
                        if (currentPacket.Length == 2)
                            statusManager.addStatus("dance", "");
                        else
                        {
                            if (rankManager.containsRight(_Rank, "fuse_use_club_dance") == false) { return true; }
                            int danceID = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (danceID < 0 || danceID > 4) { return true; }
                            statusManager.addStatus("dance", danceID.ToString());
                        }

                        statusManager.Refresh();
                    }
                    break;
                }

            case "AP": // Statuses - carry item
                {
                    if (Room != null && roomUser != null)
                    {
                        string Item = currentPacket.Substring(2);
                        if (statusManager.containsStatus("lay") || Item.Contains("/"))
                            return true; // THE HAX! \o/

                        try
                        {
                            int nItem = int.Parse(Item);
                            if (nItem < 1 || nItem > 26)
                                return true;
                        }
                        catch
                        {
                            if (_inPublicroom == false && Item != "Water" && Item != "Milk" && Item != "Juice") // Not a drink that can be retrieved from the infobus minibar
                                return true;
                        }
                        statusManager.carryItem(Item);
                    }
                    break;
                }

            case "Cl": // Room Poll - answer
                {
                    if (Room == null || roomUser == null)
                        return true;
                    int subStringSkip = 2;
                    int pollID = Encoding.decodeVL64(currentPacket.Substring(subStringSkip));
                    if (DB.checkExists("SELECT aid FROM poll_results WHERE uid = '" + userID + "' AND pid = '" + pollID + "'"))
                        return true;
                    subStringSkip += Encoding.encodeVL64(pollID).Length;
                    int questionID = Encoding.decodeVL64(currentPacket.Substring(subStringSkip));
                    subStringSkip += Encoding.encodeVL64(questionID).Length;
                    bool typeThree = DB.checkExists("SELECT type FROM poll_questions WHERE qid = '" + questionID + "' AND type = '3'");
                    if (typeThree)
                    {
                        int countAnswers = Encoding.decodeB64(currentPacket.Substring(subStringSkip, 2));
                        subStringSkip += 2;
                        string Answer = DB.Stripslash(currentPacket.Substring(subStringSkip, countAnswers));
                        DB.runQuery("INSERT INTO poll_results (pid,qid,aid,answers,uid) VALUES ('" + pollID + "','" + questionID + "','0','" + Answer + "','" + userID + "')");
                    }
                    else
                    {
                        int countAnswers = Encoding.decodeVL64(currentPacket.Substring(subStringSkip));
                        subStringSkip += Encoding.encodeVL64(countAnswers).Length;
                        int[] Answer = new int[countAnswers];
                        for (int i = 0; i < countAnswers; i++)
                        {
                            Answer[i] = Encoding.decodeVL64(currentPacket.Substring(subStringSkip));
                            subStringSkip += Encoding.encodeVL64(Answer[i]).Length;
                        }
                        foreach (int a in Answer)
                        {
                            DB.runQuery("INSERT INTO poll_results (pid,qid,aid,answers,uid) VALUES ('" + pollID + "','" + questionID + "','" + a + "',' ','" + userID + "')");
                        }
                    }
                    break;
                }

            #endregion

            #region Chat
            case "@t": // Chat - say
            case "@w": // Chat - shout
                {
                    string persnlGesture = "";
                    string Message = currentPacket.Substring(4);
                    //statusManager.showTalkAnimation((Message.Length + 50) * 30, persnlGesture);
                    if (_isMuted == false && (Room != null && roomUser != null) || (Room == null && gamePlayer != null))
                    {
                        userManager.addChatMessage(_Username, _roomID, Message);
                        Message = stringManager.filterSwearwords(Message);
                        if (Message.Substring(0, 1) == ":" && isSpeechCommand(Message.Substring(1))) // Speechcommand invoked!
                        {
                            if (roomUser != null)
                            {
                                if (roomUser.isTyping)
                                {
                                    Room.sendData(new HabboPacketBuilder("FO").AppendVL64(roomUser.roomUID).Append("H").Build());
                                    roomUser.isTyping = false;
                                }
                            }
                            else
                                gamePlayer.Game.sendData(new HabboPacketBuilder("FO").AppendVL64(gamePlayer.roomUID).Append("H").Build());
                        }
                        else
                        {
                            if (currentPacket.Substring(1, 1) == "w") // Shout
                            {
                                if (gamePlayer == null)
                                    Room.sendShout(roomUser, Message);
                                else
                                    gamePlayer.Game.sendData(new HabboPacketBuilder("Ei")
                                        .AppendVL64(gamePlayer.roomUID)
                                        .Append("H")
                                        .Append(Convert.ToChar(1))
                                        .Append("@Z")
                                        .AppendVL64(gamePlayer.roomUID)
                                        .Append(Message)
                                        .Separator()
                                        .Build());
                            }
                            else
                            {
                                if (gamePlayer == null)
                                    Room.sendSaying(roomUser, Message);
                                else
                                    gamePlayer.Game.sendData(new HabboPacketBuilder("Ei")
                                        .AppendVL64(gamePlayer.roomUID)
                                        .Append("H")
                                        .Append(Convert.ToChar(1))
                                        .Append("@X")
                                        .AppendVL64(gamePlayer.roomUID)
                                        .Append(Message)
                                        .Separator()
                                        .Build());
                            }
                        }
                    }
                    break;
                }

            case "@x": // Chat - whisper
                {
                    // * muted
                    string Receiver = currentPacket.Substring(4).Split(' ')[0];
                    string Message = currentPacket.Substring(Receiver.Length + 5);
                    string persnlGesture = "";
                    //statusManager.showTalkAnimation((Message.Length + 50) * 30, persnlGesture);
                    if (_isMuted == false && Room != null && roomUser != null)
                    {
                        //string Receiver = currentPacket.Substring(4).Split(' ')[0];
                        //string Message = currentPacket.Substring(Receiver.Length + 5);
                        if (Receiver == "" && Message.Substring(0, 1) == ":" && isSpeechCommand(Message.Substring(1))) // Speechcommand invoked!
                        {
                            if (roomUser.isTyping)
                            {
                                Room.sendData(new HabboPacketBuilder("FO").AppendVL64(roomUser.roomUID).Append("H").Build());
                                roomUser.isTyping = false;
                            }
                        }
                        else
                        {
                            userManager.addChatMessage(_Username, _roomID, Message);

                            Message = stringManager.filterSwearwords(Message);
                            Room.sendWhisper(roomUser, Receiver, Message);
                        }
                    }
                    break;
                }

            case "D}": // Chat - show speech bubble
                {
                    if (_isMuted == false && Room != null && roomUser != null)
                    {
                        Room.sendData(new HabboPacketBuilder("Ei").AppendVL64(roomUser.roomUID).Append("I").Build());
                        roomUser.isTyping = true;
                    }
                    break;
                }

            case "D~": // Chat - hide speech bubble
                {
                    if (Room != null && roomUser != null)
                    {
                        Room.sendData(new HabboPacketBuilder("Ei").AppendVL64(roomUser.roomUID).Append("H").Build());
                        roomUser.isTyping = false;
                    }
                    break;
                }
            #endregion

            #region Guestroom - rights, kicking, roombans and room voting
            case "A`": // Give rights
                {
                    if (Room == null || roomUser == null || _inPublicroom || _isOwner == false)
                        return true;

                    string Target = currentPacket.Substring(2);
                    if (userManager.containsUser(Target) == false)
                        return true;

                    virtualUser _Target = userManager.getUser(Target);
                    if (_Target._roomID != _roomID || _Target._hasRights || _Target._isOwner)
                        return true;

                    DB.runQuery("INSERT INTO room_rights(roomid,userid) VALUES ('" + _roomID + "','" + _Target.userID + "')");
                    _Target._hasRights = true;
                    _Target.statusManager.addStatus("flatctrl", "onlyfurniture");
                    _Target.roomUser.Refresh();
                    _Target.sendData("@j");
                    break;
                }

            case "Aa": // Take rights
                {
                    if (Room == null || roomUser == null || _inPublicroom || _isOwner == false)
                        return true;

                    string Target = currentPacket.Substring(2);
                    if (userManager.containsUser(Target) == false)
                        return true;

                    virtualUser _Target = userManager.getUser(Target);
                    if (_Target._roomID != _roomID || _Target._hasRights == false || _Target._isOwner)
                        return true;

                    DB.runQuery("DELETE FROM room_rights WHERE roomid = '" + _roomID + "' AND userid = '" + _Target.userID + "' LIMIT 1");
                    _Target._hasRights = false;
                    _Target.statusManager.removeStatus("flatctrl");
                    _Target.roomUser.Refresh();
                    _Target.sendData("@k");
                    break;
                }

            case "A_": // Kick user
                {
                    if (Room == null || roomUser == null || _inPublicroom || _hasRights == false)
                        return true;

                    string Target = currentPacket.Substring(2);
                    if (userManager.containsUser(Target) == false)
                        return true;

                    virtualUser _Target = userManager.getUser(Target);
                    if (_Target._roomID != _roomID)
                        return true;

                    if (_Target._isOwner && (_Target._Rank > _Rank || rankManager.containsRight(_Target._Rank, "fuse_any_room_controller")))
                        return true;

                    _Target.roomUser.walkLock = true;
                    _Target.roomUser.walkDoor = true;
                    _Target.roomUser.goalX = Room.doorX;
                    _Target.roomUser.goalY = Room.doorY;
                    break;
                }

            case "E@": // Kick and apply roomban
                {
                    if (_hasRights == false || _inPublicroom || Room == null || roomUser == null)
                        return true;

                    string Target = currentPacket.Substring(2);
                    if (userManager.containsUser(Target) == false)
                        return true;

                    virtualUser _Target = userManager.getUser(Target);
                    if (_Target._roomID != _roomID)
                        return true;

                    if (_Target._isOwner && (_Target._Rank > _Rank || rankManager.containsRight(_Target._Rank, "fuse_any_room_controller")))
                        return true;

                    string banExpireMoment = DateTime.Now.AddMinutes(Config.Rooms_roomBan_banDuration).ToString();
                    DB.runQuery("INSERT INTO room_bans (roomid,userid,ban_expire) VALUES ('" + _roomID + "','" + _Target.userID + "','" + banExpireMoment + "')");

                    _Target.roomUser.walkLock = true;
                    _Target.roomUser.walkDoor = true;
                    _Target.roomUser.goalX = Room.doorX;
                    _Target.roomUser.goalY = Room.doorY;
                    break;
                }

            case "DE": // Vote -1 or +1 on room
                {
                    if (_inPublicroom || Room == null || roomUser == null)
                        return true;

                    int Vote = Encoding.decodeVL64(currentPacket.Substring(2));
                    if ((Vote == 1 || Vote == -1) && DB.checkExists("SELECT userid FROM room_votes WHERE userid = '" + userID + "' AND roomid = '" + _roomID + "'") == false)
                    {
                        DB.runQuery("INSERT INTO room_votes (userid,roomid,vote) VALUES ('" + userID + "','" + _roomID + "','" + Vote + "')");
                        int voteAmount = DB.runRead("SELECT SUM(vote) FROM room_votes WHERE roomid = '" + _roomID + "'", null);
                        if (voteAmount < 0)
                            voteAmount = 0;
                        roomUser.hasVoted = true;
                        if (_isOwner == true)
                        Room.sendNewVoteAmount(voteAmount);
                        sendData(new HabboPacketBuilder("EY").AppendVL64(voteAmount).Build());
                        sendData(new HabboPacketBuilder("Er").Append(eventManager.getEvent(_roomID)).Build());
                    }
                    break;
                }

            #endregion

            default:
                return false;
        }
        return true;
    }
}
