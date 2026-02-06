using System;
using System.Text;

using Holo.Managers;
using Holo.Protocol;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Contains navigator-related packet handlers for the virtualUser class.
    /// Handles Navigator actions region (BV, BW, DH, @P, @Q, @U, @R, @S, @T),
    /// Room event actions region (EA, EY, D{, E^, EZ, E\, E[),
    /// and Guestroom create and modify region (@], @Y, @X, BX, BY, @W, BZ).
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes navigator-related packets including room navigation, events, and guestroom management.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        /// <returns>True if the packet was handled, false otherwise.</returns>
        private bool processNavigatorPackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                #region Navigator actions
                case "BV": // Navigator - navigate through rooms and categories
                    {
                        int hideFull = Encoding.decodeVL64(currentPacket.Substring(2, 1));
                        int cataID = Encoding.decodeVL64(currentPacket.Substring(3));

                        string Name = DB.runReadUnsafe("SELECT name FROM room_categories WHERE id = '" + cataID + "' AND (access_rank_min <= " + _Rank + " OR access_rank_hideforlower = '0')");
                        if (Name == "") // User has no access to this category/it does not exist
                            return true;

                        int Type = DB.runRead("SELECT type FROM room_categories WHERE id = '" + cataID + "'", null);
                        int parentID = DB.runRead("SELECT parent FROM room_categories WHERE id = '" + cataID + "'", null);

                        var Navigator = new HabboPacketBuilder(HabboPackets.NAVIGATOR_MAIN)
                            .AppendVL64(hideFull)
                            .AppendVL64(cataID)
                            .AppendVL64(Type)
                            .Append(Name).Separator()
                            .AppendVL64(0)
                            .AppendVL64(HabboProtocol.NAVIGATOR_MAX_ROOMS)
                            .AppendVL64(parentID);
                        string _SQL_ORDER_HELPER = "";
                        if (Type == 0) // Publicrooms
                        {
                            if (hideFull == 1)
                                _SQL_ORDER_HELPER = "AND visitors_now < visitors_max ORDER BY id ASC";
                            else
                                _SQL_ORDER_HELPER = "ORDER BY id ASC";
                        }
                        else // Guestrooms
                        {
                            if (hideFull == 1)
                                _SQL_ORDER_HELPER = "AND visitors_now < visitors_max ORDER BY visitors_now DESC LIMIT 30";
                            else
                                _SQL_ORDER_HELPER = "ORDER BY visitors_now DESC LIMIT " + Config.Navigator_openCategory_maxResults;
                        }

                        int[] roomIDs = DB.runReadColumn("SELECT id FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0, null);
                        if (Type == 2) // Guestrooms
                            Navigator.AppendVL64(roomIDs.Length);
                        if (roomIDs.Length > 0)
                        {
                            bool canSeeHiddenNames = false;
                            int[] roomStates = DB.runReadColumn("SELECT state FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0, null);
                            int[] showNameFlags = DB.runReadColumn("SELECT showname FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0, null);
                            int[] nowVisitors = DB.runReadColumn("SELECT visitors_now FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0, null);
                            int[] maxVisitors = DB.runReadColumn("SELECT visitors_max FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0, null);
                            string[] roomNames = DB.runReadColumn("SELECT name FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0);
                            string[] roomDescriptions = DB.runReadColumn("SELECT description FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0);
                            string[] roomOwners = DB.runReadColumn("SELECT owner FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0);
                            string[] roomCCTs = null;

                            if (Type == 0) // Publicroom
                                roomCCTs = DB.runReadColumn("SELECT ccts FROM rooms WHERE category = '" + cataID + "' " + _SQL_ORDER_HELPER, 0);
                            else
                                canSeeHiddenNames = rankManager.containsRight(_Rank, "fuse_enter_locked_rooms");

                            for (int i = 0; i < roomIDs.Length; i++)
                            {
                                if (Type == 0) // Publicroom
                                    Navigator.AppendVL64(roomIDs[i])
                                        .AppendVL64(1)
                                        .Append(roomNames[i]).Separator()
                                        .AppendVL64(nowVisitors[i])
                                        .AppendVL64(maxVisitors[i])
                                        .AppendVL64(cataID)
                                        .Append(roomDescriptions[i]).Separator()
                                        .AppendVL64(roomIDs[i])
                                        .AppendVL64(0)
                                        .Append(roomCCTs[i]).Separator()
                                        .Append("HI");
                                else // Guestroom
                                {
                                    if (showNameFlags[i] == 0 && canSeeHiddenNames == false)
                                        continue;
                                    else
                                        Navigator.AppendVL64(roomIDs[i])
                                            .Append(roomNames[i]).Separator()
                                            .Append(roomOwners[i]).Separator()
                                            .Append(roomManager.getRoomState(roomStates[i])).Separator()
                                            .AppendVL64(nowVisitors[i])
                                            .AppendVL64(maxVisitors[i])
                                            .Append(roomDescriptions[i]).Separator();
                                }
                            }
                        }

                        int[] subCataIDs = DB.runReadColumn("SELECT id FROM room_categories WHERE parent = '" + cataID + "' AND (access_rank_min <= " + _Rank + " OR access_rank_hideforlower = '0') ORDER BY id ASC", 0, null);
                        if (subCataIDs.Length > 0) // Sub categories
                        {
                            for (int i = 0; i < subCataIDs.Length; i++)
                            {
                                int visitorCount = DB.runReadUnsafe("SELECT SUM(visitors_now) FROM rooms WHERE category = '" + subCataIDs[i] + "'", null);
                                int visitorMax = DB.runReadUnsafe("SELECT SUM(visitors_max) FROM rooms WHERE category = '" + subCataIDs[i] + "'", null);
                                if (visitorMax > 0 && hideFull == 1 && visitorCount >= visitorMax)
                                    continue;

                                string subName = DB.runRead("SELECT name FROM room_categories WHERE id = '" + subCataIDs[i] + "'");
                                Navigator.AppendVL64(subCataIDs[i])
                                    .AppendVL64(0)
                                    .Append(subName).Separator()
                                    .AppendVL64(visitorCount)
                                    .AppendVL64(visitorMax)
                                    .AppendVL64(cataID);
                            }
                        }

                        sendData(Navigator.Build());
                        break;
                    }

                case "BW": // Navigator - request index of categories to place guestroom on
                    {
                        var catBuilder = new HabboPacketBuilder(HabboPackets.NAVIGATOR_CATEGORY_INDEX);
                        int[] cataIDs = DB.runReadColumn("SELECT id FROM room_categories WHERE type = '2' AND parent > 0 AND access_rank_min <= " + _Rank + " ORDER BY id ASC", 0, null);
                        string[] cataNames = DB.runReadColumn("SELECT name FROM room_categories WHERE type = '2' AND parent > 0 AND access_rank_min <= " + _Rank + " ORDER BY id ASC", 0);
                        catBuilder.AppendVL64(cataIDs.Length);
                        for (int i = 0; i < cataIDs.Length; i++)
                            catBuilder.AppendVL64(cataIDs[i]).Append(cataNames[i]).Separator();

                        sendData(catBuilder.Build());
                        break;
                    }

                case "DH": // Navigator - refresh recommended rooms (random guestrooms)
                    {
                        var roomsBuilder = new HabboPacketBuilder(HabboPackets.NAVIGATOR_RECOMMENDED)
                            .AppendVL64(3);
                        for (int i = 0; i <= 3; i++)
                        {
                            string[] roomDetails = DB.runReadRow("SELECT id,name,owner,description,state,visitors_now,visitors_max FROM rooms WHERE NOT(owner IS NULL) ORDER BY RAND()");
                            if (roomDetails.Length == 0)
                                return true;
                            else
                                roomsBuilder.AppendVL64(int.Parse(roomDetails[0]))
                                    .Append(roomDetails[1]).Separator()
                                    .Append(roomDetails[2]).Separator()
                                    .Append(roomManager.getRoomState(int.Parse(roomDetails[4]))).Separator()
                                    .AppendVL64(int.Parse(roomDetails[5]))
                                    .AppendVL64(int.Parse(roomDetails[6]))
                                    .Append(roomDetails[3]).Separator();
                        }
                        sendData(roomsBuilder.Build());
                        break;
                    }

                case "@P": // Navigator - view user's own guestrooms
                    {
                        string[] roomIDs = DB.runReadColumn("SELECT id FROM rooms WHERE owner = '" + _Username + "' ORDER BY id ASC", 0);
                        if (roomIDs.Length > 0)
                        {
                            var roomsBuilder = new HabboPacketBuilder(HabboPackets.NAVIGATOR_CATEGORIES);
                            for (int i = 0; i < roomIDs.Length; i++)
                            {
                                string[] roomDetails = DB.runReadRow("SELECT name,description,state,showname,visitors_now,visitors_max FROM rooms WHERE id = '" + roomIDs[i] + "'");
                                roomsBuilder.Append(roomIDs[i]).TabSeparator()
                                    .Append(roomDetails[0]).TabSeparator()
                                    .Append(_Username).TabSeparator()
                                    .Append(roomManager.getRoomState(int.Parse(roomDetails[2]))).TabSeparator()
                                    .Append("x").TabSeparator()
                                    .Append(roomDetails[4]).TabSeparator()
                                    .Append(roomDetails[5]).TabSeparator()
                                    .Append("null").TabSeparator()
                                    .Append(roomDetails[1]).TabSeparator()
                                    .Append(roomDetails[1]).TabSeparator()
                                    .RecordSeparator();
                            }
                            sendData(roomsBuilder.Build());
                        }
                        else
                            sendData(new HabboPacketBuilder(HabboPackets.NAVIGATOR_NO_OWNED).Append(_Username).Build());
                        break;
                    }

                case "@Q": // Navigator - perform guestroom search on name/owner with a given criticeria
                    {
                        bool seeAllRoomOwners = rankManager.containsRight(_Rank, "fuse_see_all_roomowners");
                        string _SEARCH = DB.Stripslash(currentPacket.Substring(2));
                        string[] roomIDs = DB.runReadColumn("SELECT id FROM rooms WHERE NOT(owner IS NULL) AND (owner = '" + _SEARCH + "' OR name LIKE '%" + _SEARCH + "%') ORDER BY id ASC", Config.Navigator_roomSearch_maxResults);
                        if (roomIDs.Length > 0)
                        {
                            var roomsBuilder = new HabboPacketBuilder(HabboPackets.NAVIGATOR_SEARCH);
                            for (int i = 0; i < roomIDs.Length; i++)
                            {
                                string[] roomDetails = DB.runReadRow("SELECT name,owner,description,state,showname,visitors_now,visitors_max FROM rooms WHERE id = '" + roomIDs[i] + "'");
                                if (roomDetails[4] == "0" && roomDetails[1] != _Username && seeAllRoomOwners == false) // The room owner has hidden his name at the guestroom and this user hasn't got the fuseright to see all room owners
                                    roomDetails[1] = "-";
                                roomsBuilder.Append(roomIDs[i]).TabSeparator()
                                    .Append(roomDetails[0]).TabSeparator()
                                    .Append(roomDetails[1]).TabSeparator()
                                    .Append(roomManager.getRoomState(int.Parse(roomDetails[3]))).TabSeparator()
                                    .Append("x").TabSeparator()
                                    .Append(roomDetails[5]).TabSeparator()
                                    .Append(roomDetails[6]).TabSeparator()
                                    .Append("null").TabSeparator()
                                    .Append(roomDetails[2]).TabSeparator()
                                    .RecordSeparator();
                            }
                            sendData(roomsBuilder.Build());
                        }
                        else
                            sendData(HabboPackets.NAVIGATOR_NO_RESULTS);
                        break;
                    }

                case "@U": // Navigator - get guestroom details
                    {
                        int roomID = int.Parse(currentPacket.Substring(2));
                        string[] roomDetails = DB.runReadRow("SELECT name,owner,description,model,state,superusers,showname,category,visitors_now,visitors_max FROM rooms WHERE id = '" + roomID + "' AND NOT(owner IS NULL)");

                        if (roomDetails.Length > 0) // Guestroom does exist
                        {
                            var detailsBuilder = new HabboPacketBuilder()
                                .AppendVL64(int.Parse(roomDetails[5]))
                                .AppendVL64(int.Parse(roomDetails[4]))
                                .AppendVL64(roomID);

                            if (roomDetails[6] == "0" && rankManager.containsRight(_Rank, "fuse_see_all_roomowners")) // The room owner has decided to hide his name at this room, and this user hasn't got the fuseright to see all room owners, hide the name
                                detailsBuilder.Append("-");
                            else
                                detailsBuilder.Append(roomDetails[1]);

                            detailsBuilder.Separator()
                                .Append("model_").Append(roomDetails[3]).Separator()
                                .Append(roomDetails[0]).Separator()
                                .Append(roomDetails[2]).Separator()
                                .AppendVL64(int.Parse(roomDetails[6]));

                            if (DB.checkExists("SELECT id FROM room_categories WHERE id = '" + roomDetails[7] + "' AND trading = '1'"))
                                detailsBuilder.Append(HabboProtocol.BOOL_TRUE); // Allow trading
                            else
                                detailsBuilder.Append(HabboProtocol.BOOL_FALSE); // Disallow trading

                            detailsBuilder.AppendVL64(int.Parse(roomDetails[8])).AppendVL64(int.Parse(roomDetails[9]));
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_DETAILS).Append(detailsBuilder.Build()).Build());
                        }
                        break;
                    }

                case "@R": // Navigator - initialize user's favorite rooms
                    {
                        int[] roomIDs = DB.runReadColumn("SELECT roomid FROM users_favouriterooms WHERE userid = '" + userID + "' ORDER BY roomid DESC", Config.Navigator_Favourites_maxRooms, null);
                        if (roomIDs.Length > 0)
                        {
                            int deletedAmount = 0;
                            int guestRoomAmount = 0;
                            bool seeHiddenRoomOwners = rankManager.containsRight(_Rank, "fuse_enter_locked_rooms");
                            var roomsBuilder = new HabboPacketBuilder();
                            for (int i = 0; i < roomIDs.Length; i++)
                            {
                                string[] roomData = DB.runReadRow("SELECT name,owner,state,showname,visitors_now,visitors_max,description FROM rooms WHERE id = '" + roomIDs[i] + "'");
                                if (roomData.Length == 0)
                                {
                                    if (guestRoomAmount > 0)
                                        deletedAmount++;
                                    DB.runQuery("DELETE FROM users_favouriterooms WHERE userid = '" + userID + "' AND roomid = '" + roomIDs[i] + "' LIMIT 1");
                                }
                                else
                                {
                                    if (roomData[1] == "") // Publicroom
                                    {
                                        int categoryID = DB.runRead("SELECT category FROM rooms WHERE id = '" + roomIDs[i] + "'", null);
                                        string CCTs = DB.runRead("SELECT ccts FROM rooms WHERE id = '" + roomIDs[i] + "'");
                                        roomsBuilder.AppendVL64(roomIDs[i])
                                            .Append(HabboProtocol.BOOL_TRUE)
                                            .Append(roomData[0]).Separator()
                                            .AppendVL64(int.Parse(roomData[4]))
                                            .AppendVL64(int.Parse(roomData[5]))
                                            .AppendVL64(categoryID)
                                            .Append(roomData[6]).Separator()
                                            .AppendVL64(roomIDs[i])
                                            .Append(HabboProtocol.BOOL_FALSE)
                                            .Append(CCTs).Separator()
                                            .Append("HI");
                                    }
                                    else // Guestroom
                                    {
                                        if (roomData[3] == "0" && _Username != roomData[1] && seeHiddenRoomOwners == false) // Room owner doesn't wish to show his name, and this user isn't the room owner and this user doesn't has the right to see hidden room owners, change room owner to '-'
                                            roomData[1] = "-";
                                        roomsBuilder.AppendVL64(roomIDs[i])
                                            .Append(roomData[0]).Separator()
                                            .Append(roomData[1]).Separator()
                                            .Append(roomManager.getRoomState(int.Parse(roomData[2]))).Separator()
                                            .AppendVL64(int.Parse(roomData[4]))
                                            .AppendVL64(int.Parse(roomData[5]))
                                            .Append(roomData[6]).Separator();
                                        guestRoomAmount++;
                                    }
                                }
                            }
                            var favHeader = new HabboPacketBuilder(HabboPackets.NAVIGATOR_FAVORITES)
                                .Append(HabboProtocol.BOOL_FALSE)
                                .Append(HabboProtocol.BOOL_FALSE)
                                .Append("J").Separator()
                                .Append("HHH")
                                .AppendVL64(guestRoomAmount - deletedAmount)
                                .Append(roomsBuilder.Build());
                            sendData(favHeader.Build());
                        }
                        break;
                    }

                case "@S": // Navigator - add room to favourite rooms list
                    {
                        int roomID = Encoding.decodeVL64(currentPacket.Substring(3));
                        if (DB.checkExists("SELECT id FROM rooms WHERE id = '" + roomID + "'") == true && DB.checkExists("SELECT userid FROM users_favouriterooms WHERE userid = '" + userID + "' AND roomid = '" + roomID + "'") == false) // The virtual room does exist, and the virtual user hasn't got it in the list already
                        {
                            if (DB.runReadUnsafe("SELECT COUNT(userid) FROM users_favouriterooms WHERE userid = '" + userID + "'", null) < Config.Navigator_Favourites_maxRooms)
                                DB.runQuery("INSERT INTO users_favouriterooms(userid,roomid) VALUES ('" + userID + "','" + roomID + "')");
                            else
                                sendData(new HabboPacketBuilder(HabboPackets.ROOM_PASSWORD_REQUIRED).Append("nav_error_toomanyfavrooms").Build());
                        }
                        break;
                    }
                case "@T": // Navigator - remove room from favourite rooms list
                    {
                        int roomID = Encoding.decodeVL64(currentPacket.Substring(3));
                        DB.runQuery("DELETE FROM users_favouriterooms WHERE userid = '" + userID + "' AND roomid = '" + roomID + "' LIMIT 1");
                        break;
                    }

                #endregion

                #region Room event actions
                case "EA": // Events - get setup
                    sendData(new HabboPacketBuilder(HabboPackets.EVENT_SETUP).AppendVL64(eventManager.categoryAmount).Build());
                    break;

                case "EY": // Events - show/hide 'Host event' button
                    if (_inPublicroom || roomUser == null || _hostsEvent) // In publicroom, not in room at all or already hosting event
                        sendData(new HabboPacketBuilder(HabboPackets.EVENT_BUTTON).Append(HabboProtocol.BOOL_FALSE).Build()); // Hide
                    else
                        sendData(new HabboPacketBuilder(HabboPackets.EVENT_BUTTON).Append(HabboProtocol.BOOL_TRUE).Build()); // Show
                    break;

                case "D{": // Events - check if event category is OK
                    {
                        int categoryID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (eventManager.categoryOK(categoryID))
                            sendData(new HabboPacketBuilder(HabboPackets.EVENT_CATEGORY_OK).AppendVL64(categoryID).Build());
                        break;
                    }

                case "E^": // Events - open category
                    {
                        int categoryID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (categoryID >= 1 && categoryID <= 11)
                            sendData(new HabboPacketBuilder(HabboPackets.EVENT_CATEGORY).AppendVL64(categoryID).Append(eventManager.getEvents(categoryID)).Build());
                        break;
                    }

                case "EZ": // Events - create event
                    {
                        if (_isOwner && _hostsEvent == false && _inPublicroom == false && roomUser != null)
                        {
                            int categoryID = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (eventManager.categoryOK(categoryID))
                            {
                                int categoryLength = Encoding.encodeVL64(categoryID).Length;
                                int nameLength = Encoding.decodeB64(currentPacket.Substring(categoryLength + 2, 2));
                                string Name = currentPacket.Substring(categoryLength + 4, nameLength);
                                string Description = currentPacket.Substring(categoryLength + nameLength + 6);

                                _hostsEvent = true;
                                eventManager.createEvent(categoryID, userID, _roomID, Name, Description);
                                Room.sendData(new HabboPacketBuilder(HabboPackets.EVENT_INFO).Append(eventManager.getEvent(_roomID)).Build());
                            }
                        }
                        break;
                    }

                case @"E\": // Events - edit event
                    {
                        if (_hostsEvent && _isOwner && _inPublicroom == false && roomUser != null)
                        {
                            int categoryID = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (eventManager.categoryOK(categoryID))
                            {
                                int categoryLength = Encoding.encodeVL64(categoryID).Length;
                                int nameLength = Encoding.decodeB64(currentPacket.Substring(categoryLength + 2, 2));
                                string Name = currentPacket.Substring(categoryLength + 4, nameLength);
                                string Description = currentPacket.Substring(categoryLength + nameLength + 6);

                                eventManager.editEvent(categoryID, _roomID, Name, Description);
                                Room.sendData(new HabboPacketBuilder(HabboPackets.EVENT_INFO).Append(eventManager.getEvent(_roomID)).Build());
                            }
                        }
                        break;
                    }

                case "E[": // Events - end event
                    {
                        if (_hostsEvent && _isOwner && _inPublicroom == false && roomUser != null)
                        {
                            _hostsEvent = false;
                            eventManager.removeEvent(_roomID);
                            Room.sendData(new HabboPacketBuilder(HabboPackets.EVENT_INFO).Append("-1").Build());
                        }
                        break;
                    }

                #endregion

                #region Guestroom create and modify
                case "@]": // Create guestroom - phase 1
                    {
                        string[] roomSettings = currentPacket.Split('/');
                        if (DB.runRead("SELECT COUNT(id) FROM rooms WHERE owner = '" + _Username + "'", null) < Config.Navigator_createRoom_maxRooms)
                        {
                            roomSettings[2] = stringManager.filterSwearwords(roomSettings[2]);
                            roomSettings[3] = roomSettings[3].Substring(6, 1);
                            roomSettings[4] = roomManager.getRoomState(roomSettings[4]).ToString();
                            if (roomSettings[5] != "0" && roomSettings[5] != "1")
                                return true;

                            DB.runQuery("INSERT INTO rooms (name,owner,model,state,showname) VALUES ('" + DB.Stripslash(roomSettings[2]) + "','" + _Username + "','" + roomSettings[3] + "','" + roomSettings[4] + "','" + roomSettings[5] + "')");
                            string roomID = DB.runRead("SELECT MAX(id) FROM rooms WHERE owner = '" + _Username + "'");
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_CREATED).Append(roomID).RecordSeparator().Append(roomSettings[2]).Build());
                        }
                        else
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_PASSWORD_REQUIRED).Append("Error creating a private room").Build());
                        break;
                    }

                case "@Y": // Create guestroom - phase 2 / modify guestroom
                    {
                        int roomID = 0;
                        if (currentPacket.Substring(2, 1) == "/")
                            roomID = int.Parse(currentPacket.Split('/')[1]);
                        else
                            roomID = int.Parse(currentPacket.Substring(2).Split('/')[0]);

                        int superUsers = 0;
                        int maxVisitors = HabboProtocol.DEFAULT_MAX_VISITORS;
                        string[] packetContent = currentPacket.Split(HabboProtocol.RECORD_SEPARATOR);
                        string roomDescription = "";
                        string roomPassword = "";

                        for (int i = 1; i < packetContent.Length; i++) // More proper way, thanks Jeax
                        {
                            string updHeader = packetContent[i].Split('=')[0];
                            string updValue = packetContent[i].Substring(updHeader.Length + 1);
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
                                    if (maxVisitors < 10 || maxVisitors > HabboProtocol.DEFAULT_MAX_VISITORS)
                                        maxVisitors = HabboProtocol.DEFAULT_MAX_VISITORS;
                                    break;

                                case "password":
                                    roomPassword = DB.Stripslash(updValue);
                                    break;

                                default:
                                    return true;
                            }
                        }
                        DB.runQuery("UPDATE rooms SET description = '" + roomDescription + "',superusers = '" + superUsers + "',visitors_max = '" + maxVisitors + "',password = '" + roomPassword + "' WHERE id = '" + roomID + "' AND owner = '" + _Username + "' LIMIT 1");
                        break;
                    }

                case "@X": // Modify guestroom, save name, state and show/hide ownername
                    {
                        string[] packetContent = currentPacket.Substring(2).Split('/');
                        int roomID = int.Parse(packetContent[0]);
                        string roomName = DB.Stripslash(stringManager.filterSwearwords(packetContent[1]));
                        string showName = packetContent[2];
                        if (showName != "1" && showName != "0")
                            showName = "1";
                        int roomState = roomManager.getRoomState(packetContent[2]);
                        DB.runQuery("UPDATE rooms SET name = '" + roomName + "',state = '" + roomState + "',showname = '" + showName + "' WHERE id = '" + roomID + "' AND owner = '" + _Username + "' LIMIT 1");
                        break;
                    }

                case "BX": // Navigator - trigger guestroom modify
                    {
                        int roomID = Encoding.decodeVL64(currentPacket.Substring(2));
                        string roomCategory = DB.runRead("SELECT category FROM rooms WHERE id = '" + roomID + "' AND owner = '" + _Username + "'");
                        if (roomCategory != "")
                        {
                            var modifyPacket = new HabboPacketBuilder(HabboPackets.ROOM_MODIFY)
                                .AppendVL64(roomID)
                                .AppendVL64(int.Parse(roomCategory));
                            sendData(modifyPacket.Build());
                        }
                        break;
                    }

                case "BY": // Navigator - edit category of a guestroom
                    {
                        int roomID = Encoding.decodeVL64(currentPacket.Substring(2));
                        int cataID = Encoding.decodeVL64(currentPacket.Substring(Encoding.encodeVL64(roomID).Length + 2));
                        if (DB.checkExists("SELECT id FROM room_categories WHERE id = '" + cataID + "' AND type = '2' AND parent > 0 AND access_rank_min <= " + _Rank)) // Category is valid for this user
                            DB.runQuery("UPDATE rooms SET category = '" + cataID + "' WHERE id = '" + roomID + "' AND owner = '" + _Username + "' LIMIT 1");
                        break;
                    }

                case "@W": // Guestroom - Delete
                    {
                        int roomID = int.Parse(currentPacket.Substring(2));
                        if (DB.checkExists("SELECT id FROM rooms WHERE id = '" + roomID + "' AND owner = '" + _Username + "'") == true)
                        {
                            DB.runQuery("DELETE FROM room_rights WHERE roomid = '" + roomID + "'");
                            DB.runQuery("DELETE FROM rooms WHERE id = '" + roomID + "' LIMIT 1");
                            DB.runQuery("DELETE FROM room_votes WHERE roomid = '" + roomID + "'");
                            DB.runQuery("DELETE FROM room_bans WHERE roomid = '" + roomID + "' LIMIT 1");
                            DB.runQuery("DELETE FROM furniture WHERE roomid = '" + roomID + "'");
                            DB.runQuery("DELETE FROM furniture_moodlight WHERE roomid = '" + roomID + "'");
                        }
                        if (roomManager.containsRoom(roomID) == true)
                        {
                            roomManager.getRoom(roomID).kickUsers(HabboProtocol.RANK_SUPERADMIN + 2, "This room has been deleted");
                        }
                        break;
                    }

                case "BZ": // Navigator - 'Who's in here' feature for public rooms
                    {
                        int roomID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (roomManager.containsRoom(roomID))
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_USERLIST).Append(roomManager.getRoom(roomID).Userlist).Build());
                        else
                            sendData(HabboPackets.ROOM_USERLIST);
                        break;
                    }
                #endregion

                default:
                    return false; // Packet was not handled
            }
            return true; // Packet was handled
        }
    }
}
