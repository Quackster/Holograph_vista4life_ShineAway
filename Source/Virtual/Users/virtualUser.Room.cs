using System;
using System.Text;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Contains room entry/exit packet handlers for the virtualUser class.
    /// Handles Enter/leave room region packets (@u, Bv, @B, @v, @y, Ab, @{, A~, @|, @}, @~, @, A@).
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes room entry/exit related packets including leave room, loading screen,
        /// room determination, teleporter entry, doorbell/password checks, and room initialization.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        /// <returns>True if the packet was handled, false otherwise.</returns>
        private bool processRoomPackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                #region Enter/leave room
                case "@u": // Rooms - leave room
                    {
                        if (Room != null && roomUser != null)
                            Room.removeUser(roomUser.roomUID, false, "");
                        else
                        {
                            if (gamePlayer != null)
                                leaveGame();
                        }
                        break;
                    }

                case "Bv": // Enter room - loading screen advertisement
                    {
                        Config.Rooms_LoadAvertisement_img = "";
                        if (Config.Rooms_LoadAvertisement_img == "")
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_ADVERTISEMENT).Append("0").Build());
                        else
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_ADVERTISEMENT).Append(Config.Rooms_LoadAvertisement_img).TabSeparator().Append(Config.Rooms_LoadAvertisement_uri).Build());
                    }
                    break;

                case "@B": // Enter room - determine room and check state + max visitors override
                    {
                        int roomID = Encoding.decodeVL64(currentPacket.Substring(3));
                        bool isPublicroom = (currentPacket.Substring(2, 1) == "A");

                        sendData("@S");
                        sendData(new HabboPacketBuilder("Bf").Append("http://wwww.vista4life.com/bf.php?p=emu").Build());

                        if (gamePlayer != null && gamePlayer.Game != null)
                        {
                            if (gamePlayer.enteringGame)
                            {
                                Room.removeUser(roomUser.roomUID, false, "");
                                sendData(new HabboPacketBuilder("AE").Append(gamePlayer.Game.Lobby.Type).Append("_arena_").Append(gamePlayer.Game.mapID).Append(" ").Append(roomID).Build());
                                sendData(new HabboPacketBuilder("Cs").Append(gamePlayer.Game.getMap()).Build());
                                string s = gamePlayer.Game.getMap();
                            }
                            else
                                leaveGame();
                        }
                        else
                        {
                            if (Room != null && roomUser != null)
                                Room.removeUser(roomUser.roomUID, false, "");

                            if (_teleporterID == 0)
                            {
                                bool allowEnterLockedRooms = rankManager.containsRight(_Rank, "fuse_enter_locked_rooms");
                                int accessLevel = DB.runRead("SELECT state FROM rooms WHERE id = '" + roomID + "'", null);
                                if (accessLevel == 3 && _clubMember == false && allowEnterLockedRooms == false) // Room is only for club subscribers and the user isn't club and hasn't got the fuseright for entering all rooms nomatter the state
                                {
                                    sendData(new HabboPacketBuilder(HabboPackets.ROOM_FULL).Append("Kc").Build());
                                    return true;
                                }
                                else if (accessLevel == 4 && allowEnterLockedRooms == false) // The room is only for staff and the user hasn't got the fuseright for entering all rooms nomatter the state
                                {
                                    sendData(HabboPacketBuilder.SystemMessage(stringManager.getString("room_stafflocked")));
                                    return true;
                                }

                                int nowVisitors = DB.runRead("SELECT SUM(visitors_now) FROM rooms WHERE id = '" + roomID + "'", null);
                                if (nowVisitors > 0)
                                {
                                    int maxVisitors = DB.runRead("SELECT SUM(visitors_max) FROM rooms WHERE id = '" + roomID + "'", null);
                                    if (nowVisitors >= maxVisitors && rankManager.containsRight(_Rank, "fuse_enter_full_rooms") == false)
                                    {
                                        if (isPublicroom == false)
                                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_FULL).Append(HabboProtocol.BOOL_TRUE).Build());
                                        else
                                            sendData(HabboPacketBuilder.SystemMessage(stringManager.getString("room_full")));
                                        return true;
                                    }
                                }
                            }

                            _roomID = roomID;
                            _inPublicroom = isPublicroom;
                            _ROOMACCESS_PRIMARY_OK = true;

                            if (isPublicroom)
                            {
                                string roomModel = DB.runRead("SELECT model FROM rooms WHERE id = '" + roomID + "'");
                                sendData(new HabboPacketBuilder("AE").Append(roomModel).Append(" ").Append(roomID).Build());
                                _ROOMACCESS_SECONDARY_OK = true;
                            }
                        }
                        break;
                    }

                case "@v": // Enter room - guestroom - enter room by using a teleporter
                    {
                        sendData("@S");
                        break;
                    }

                case "@y": // Enter room - guestroom - check roomban/password/doorbell
                    {
                        if (_inPublicroom == false)
                        {
                            _isOwner = DB.checkExists("SELECT id FROM rooms WHERE id = '" + _roomID + "' AND owner = '" + _Username + "'");
                            if (_isOwner == false)
                                _hasRights = DB.checkExists("SELECT userid FROM room_rights WHERE roomid = '" + _roomID + "' AND userid = '" + userID + "'");
                            if (_hasRights == false)
                                _hasRights = DB.checkExists("SELECT id FROM rooms WHERE id = '" + _roomID + "' AND superusers = '1'");

                            if (_teleporterID == 0 && _isOwner == false && rankManager.containsRight(_Rank, "fuse_enter_locked_rooms") == false)
                            {
                                int accessFlag = DB.runRead("SELECT state FROM rooms WHERE id = '" + _roomID + "'", null);
                                if (_ROOMACCESS_PRIMARY_OK == false && accessFlag != 2)
                                    return true;

                                // Check for roombans
                                if (DB.checkExists("SELECT roomid FROM room_bans WHERE roomid = '" + _roomID + "' AND userid = '" + userID + "'"))
                                {
                                    DateTime banExpireMoment = DateTime.Parse(DB.runRead("SELECT ban_expire FROM room_bans WHERE roomid = '" + _roomID + "' AND userid = '" + userID + "'"));
                                    if (DateTime.Compare(banExpireMoment, DateTime.Now) > 0)
                                    {
                                        sendData(new HabboPacketBuilder(HabboPackets.ROOM_FULL).Append("PA").Build());
                                        sendData(HabboPackets.USER_KICK);
                                        return true;
                                    }
                                    else
                                        DB.runQuery("DELETE FROM room_bans WHERE roomid = '" + _roomID + "' AND userid = '" + userID + "' LIMIT 1");
                                }

                                if (accessFlag == 1) // Doorbell
                                {
                                    if (roomManager.containsRoom(_roomID) == false)
                                    {
                                        sendData(HabboPackets.DOORBELL_RING);
                                        return true;
                                    }
                                    else
                                    {
                                        roomManager.getRoom(_roomID).sendDataToRights("A[" + _Username);
                                        sendData("A[");
                                        return true;
                                    }
                                }
                                else if (accessFlag == 2) // Password
                                {
                                    string givenPassword = "";
                                    try { givenPassword = currentPacket.Split('/')[1]; }
                                    catch { }
                                    string roomPassword = DB.runRead("SELECT password FROM rooms WHERE id = '" + _roomID + "'");
                                    if (givenPassword != roomPassword) { sendData(new HabboPacketBuilder(HabboPackets.ROOM_PASSWORD_REQUIRED).Append("Incorrect flat password").Build()); return true; }
                                }
                            }
                            _ROOMACCESS_SECONDARY_OK = true;
                            sendData(HabboPackets.ROOM_CLOSED);
                        }
                        break;
                    }

                case "Ab": // Answer guestroom doorbell
                    {
                        if (_hasRights == false && rankManager.containsRight(roomUser.User._Rank, "fuse_enter_locked_rooms"))
                            return true;

                        string ringer = currentPacket.Substring(4, Encoding.decodeB64(currentPacket.Substring(2, 2)));
                        bool letIn = currentPacket.Substring(currentPacket.Length - 1) == "A";

                        virtualUser ringerData = userManager.getUser(ringer);
                        if (ringerData == null)
                            return true;
                        if (ringerData._roomID != _roomID)
                            return true;

                        if (letIn)
                        {
                            ringerData._ROOMACCESS_SECONDARY_OK = true;
                            Room.sendDataToRights(new HabboPacketBuilder(HabboPackets.ROOM_CLOSED).Append(ringer).Separator().Build());
                            ringerData.sendData(HabboPackets.ROOM_CLOSED);
                        }
                        else
                        {
                            ringerData.sendData(HabboPackets.DOORBELL_RING);
                            ringerData._roomID = 0;
                            ringerData._inPublicroom = false;
                            ringerData._ROOMACCESS_PRIMARY_OK = false;
                            ringerData._ROOMACCESS_SECONDARY_OK = false;
                            ringerData._isOwner = false;
                            ringerData._hasRights = false;
                            ringerData.Room = null;
                            ringerData.roomUser = null;
                            //ringerData.Room.removeUser(ringerData.roomUser.roomUID, true, "");
                        }
                        break;
                    }

                case "@{": // Enter room - guestroom - guestroom only data: model, wallpaper, floor, landscape, rights, room votes // Updated by Su-la-ke
                    {
                        if (_ROOMACCESS_SECONDARY_OK && _inPublicroom == false)
                        {
                            string Model = "model_" + DB.runRead("SELECT model FROM rooms WHERE id = '" + _roomID + "'");
                            sendData(new HabboPacketBuilder("AE").Append(Model).Append(" ").Append(_roomID).Build());

                            int Wallpaper = DB.runRead("SELECT wallpaper FROM rooms WHERE id = '" + _roomID + "'", null);
                            int Floor = DB.runRead("SELECT floor FROM rooms WHERE id = '" + _roomID + "'", null);
                            string Landscape = DB.runRead("SELECT landscape FROM rooms WHERE id = '" + _roomID + "'");
                            sendData(new HabboPacketBuilder(HabboPackets.USER_STATUS).Append("landscape/").Append(Landscape).Build());
                            if (Wallpaper > 0)
                                sendData(new HabboPacketBuilder(HabboPackets.USER_STATUS).Append("wallpaper/").Append(Wallpaper).Build());
                            if (Floor > 0)
                                sendData(new HabboPacketBuilder(HabboPackets.USER_STATUS).Append("floor/").Append(Floor).Build());



                            if (_isOwner == false)
                            {
                                _isOwner = rankManager.containsRight(_Rank, "fuse_any_room_controller");
                            }
                            if (_isOwner)
                            {
                                _hasRights = true;
                                sendData(HabboPackets.ROOM_RIGHTS);
                            }
                            if (_hasRights)
                                sendData("@j");

                            int voteAmount = -1;
                            if (DB.checkExists("SELECT userid FROM room_votes WHERE userid = '" + userID + "' AND roomid = '" + _roomID + "'"))
                            {
                                voteAmount = DB.runRead("SELECT SUM(vote) FROM room_votes WHERE roomid = '" + _roomID + "'", null);
                                if (voteAmount < 0) { voteAmount = 0; }
                            }
                            sendData(new HabboPacketBuilder(HabboPackets.VOTE_UPDATE).AppendVL64(voteAmount).Build());
                            sendData(new HabboPacketBuilder(HabboPackets.EVENT_INFO).Append(eventManager.getEvent(_roomID)).Build());
                        }
                        break;
                    }

                case "A~": // Enter room - get room advertisement
                    {
                        if (_inPublicroom && DB.checkExists("SELECT roomid FROM room_ads WHERE roomid = '" + _roomID + "'"))
                        {
                            string advImg = DB.runRead("SELECT img FROM room_ads WHERE roomid = '" + _roomID + "'");
                            string advUri = DB.runRead("SELECT uri FROM room_ads WHERE roomid = '" + _roomID + "'");
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_INTERSTITIAL).Append(advImg).TabSeparator().Append(advUri).Build());
                        }
                        else
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_INTERSTITIAL).Append("0").Build());
                        break;
                    }

                case "@|": // Enter room - get roomclass + get heightmap
                    {
                        if (_ROOMACCESS_SECONDARY_OK)
                        {
                            if (roomManager.containsRoom(_roomID))
                                Room = roomManager.getRoom(_roomID);
                            else
                            {
                                Room = new virtualRoom(_roomID, _inPublicroom);
                                roomManager.addRoom(_roomID, Room);
                            }

                            sendData(new HabboPacketBuilder(HabboPackets.FLAT_CATEGORY).Append(Room.Heightmap).Build());
                            sendData(new HabboPacketBuilder(@"@\").Append(Room.dynamicUnits).Build());
                        }
                        else
                        {
                            if (gamePlayer != null && gamePlayer.enteringGame && gamePlayer.teamID != -1 && gamePlayer.Game != null)
                            {
                                sendData(new HabboPacketBuilder(HabboPackets.FLAT_CATEGORY).Append(gamePlayer.Game.Heightmap).Build());
                                sendData(new HabboPacketBuilder("Cs").Append(gamePlayer.Game.getPlayers()).Build());
                                string s = gamePlayer.Game.getPlayers();
                            }
                            gamePlayer.enteringGame = false;
                        }
                        break;
                    }

                case "@}": // Enter room - get items
                    {
                        if (_ROOMACCESS_SECONDARY_OK && Room != null)
                        {
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_FLOOR_ITEMS).Append(Room.PublicroomItems).Build());
                            sendData(new HabboPacketBuilder(HabboPackets.ROOM_WALL_ITEMS).Append(Room.Flooritems).Build());
                        }
                        break;
                    }

                case "@~": // Enter room - get group badges, optional skill levels in game lobbies and sprite index
                    {
                        {
                            if (_ROOMACCESS_SECONDARY_OK && Room != null)
                            {
                                sendData(new HabboPacketBuilder(HabboPackets.FRIEND_UPDATE).Append(Room.Groups).Build());
                                if (Room.Lobby != null)
                                {
                                    var lobbyPacket = new HabboPacketBuilder(HabboPackets.GAME_LOBBY_INFO)
                                        .Append(HabboProtocol.BOOL_FALSE)
                                        .Append(Room.Lobby.Rank.Title).Separator()
                                        .AppendVL64(Room.Lobby.Rank.minPoints)
                                        .AppendVL64(Room.Lobby.Rank.maxPoints);
                                    sendData(lobbyPacket.Build());
                                    sendData(new HabboPacketBuilder("Cz").Append(Room.Lobby.playerRanks).Build());
                                }
                                sendData(new HabboPacketBuilder(HabboPackets.ROOM_INFO).Append(HabboProtocol.BOOL_FALSE).Build());
                                if (_receivedSpriteIndex == false)
                                {
                                    sendData(new HabboPacketBuilder("Dg").Append(@"[SEshelves_norjaX~Dshelves_polyfonYmAshelves_siloXQHtable_polyfon_smallYmAchair_polyfonZbBtable_norja_medY_Itable_silo_medX~Dtable_plasto_4legY_Itable_plasto_roundY_Itable_plasto_bigsquareY_Istand_polyfon_zZbBchair_siloX~Dsofa_siloX~Dcouch_norjaX~Dchair_norjaX~Dtable_polyfon_medYmAdoormat_loveZbBdoormat_plainZ[Msofachair_polyfonX~Dsofa_polyfonZ[Msofachair_siloX~Dchair_plastyX~Dchair_plastoYmAtable_plasto_squareY_Ibed_polyfonX~Dbed_polyfon_one[dObed_trad_oneYmAbed_tradYmAbed_silo_oneYmAbed_silo_twoYmAtable_silo_smallX~Dbed_armas_twoYmAbed_budget_oneXQHbed_budgetXQHshelves_armasYmAbench_armasYmAtable_armasYmAsmall_table_armasZbBsmall_chair_armasYmAfireplace_armasYmAlamp_armasYmAbed_armas_oneYmAcarpet_standardY_Icarpet_armasYmAcarpet_polarY_Ifireplace_polyfonY_Itable_plasto_4leg*1Y_Itable_plasto_bigsquare*1Y_Itable_plasto_round*1Y_Itable_plasto_square*1Y_Ichair_plasto*1YmAcarpet_standard*1Y_Idoormat_plain*1Z[Mtable_plasto_4leg*2Y_Itable_plasto_bigsquare*2Y_Itable_plasto_round*2Y_Itable_plasto_square*2Y_Ichair_plasto*2YmAdoormat_plain*2Z[Mcarpet_standard*2Y_Itable_plasto_4leg*3Y_Itable_plasto_bigsquare*3Y_Itable_plasto_round*3Y_Itable_plasto_square*3Y_Ichair_plasto*3YmAcarpet_standard*3Y_Idoormat_plain*3Z[Mtable_plasto_4leg*4Y_Itable_plasto_bigsquare*4Y_Itable_plasto_round*4Y_Itable_plasto_square*4Y_Ichair_plasto*4YmAcarpet_standard*4Y_Idoormat_plain*4Z[Mdoormat_plain*6Z[Mdoormat_plain*5Z[Mcarpet_standard*5Y_Itable_plasto_4leg*5Y_Itable_plasto_bigsquare*5Y_Itable_plasto_round*5Y_Itable_plasto_square*5Y_Ichair_plasto*5YmAtable_plasto_4leg*6Y_Itable_plasto_bigsquare*6Y_Itable_plasto_round*6Y_Itable_plasto_square*6Y_Ichair_plasto*6YmAtable_plasto_4leg*7Y_Itable_plasto_bigsquare*7Y_Itable_plasto_round*7Y_Itable_plasto_square*7Y_Ichair_plasto*7YmAtable_plasto_4leg*8Y_Itable_plasto_bigsquare*8Y_Itable_plasto_round*8Y_Itable_plasto_square*8Y_Ichair_plasto*8YmAtable_plasto_4leg*9Y_Itable_plasto_bigsquare*9Y_Itable_plasto_round*9Y_Itable_plasto_square*9Y_Ichair_plasto*9YmAcarpet_standard*6Y_Ichair_plasty*1X~DpizzaYmAdrinksYmAchair_plasty*2X~Dchair_plasty*3X~Dchair_plasty*4X~Dbar_polyfonY_Iplant_cruddyYmAbottleYmAbardesk_polyfonX~Dbardeskcorner_polyfonX~DfloortileHbar_armasY_Ibartable_armasYmAbar_chair_armasYmAcarpet_softZ@Kcarpet_soft*1Z@Kcarpet_soft*2Z@Kcarpet_soft*3Z@Kcarpet_soft*4Z@Kcarpet_soft*5Z@Kcarpet_soft*6Z@Kred_tvY_Iwood_tvYmAcarpet_polar*1Y_Ichair_plasty*5X~Dcarpet_polar*2Y_Icarpet_polar*3Y_Icarpet_polar*4Y_Ichair_plasty*6X~Dtable_polyfonYmAsmooth_table_polyfonYmAsofachair_polyfon_girlX~Dbed_polyfon_girl_one[dObed_polyfon_girlX~Dsofa_polyfon_girlZ[Mbed_budgetb_oneXQHbed_budgetbXQHplant_pineappleYmAplant_fruittreeY_Iplant_small_cactusY_Iplant_bonsaiY_Iplant_big_cactusY_Iplant_yukkaY_Icarpet_standard*7Y_Icarpet_standard*8Y_Icarpet_standard*9Y_Icarpet_standard*aY_Icarpet_standard*bY_Iplant_sunflowerY_Iplant_roseY_Itv_luxusY_IbathZ\BsinkY_ItoiletYmAduckYmAtileYmAtoilet_redYmAtoilet_yellYmAtile_redYmAtile_yellYmApresent_gen[~Npresent_gen1[~Npresent_gen2[~Npresent_gen3[~Npresent_gen4[~Npresent_gen5[~Npresent_gen6[~Nbar_basicY_Ishelves_basicXQHsoft_sofachair_norjaX~Dsoft_sofa_norjaX~Dlamp_basicXQHlamp2_armasYmAfridgeY_IdoorYc[doorBYc[doorCYc[pumpkinYmAskullcandleYmAdeadduckYmAdeadduck2YmAdeadduck3YmAmenorahYmApuddingYmAhamYmAturkeyYmAxmasduckY_IhouseYmAtriplecandleYmAtree3YmAtree4YmAtree5X~Dham2YmAwcandlesetYmArcandlesetYmAstatueYmAheartY_IvaleduckYmAheartsofaX~DthroneYmAsamovarY_IgiftflowersY_IhabbocakeYmAhologramYmAeasterduckY_IbunnyYmAbasketY_IbirdieYmAediceX~Dclub_sofaZ[Mprize1YmAprize2YmAprize3YmAdivider_poly3X~Ddivider_arm1YmAdivider_arm2YmAdivider_arm3YmAdivider_nor1X~Ddivider_silo1X~Ddivider_nor2X~Ddivider_silo2Z[Mdivider_nor3X~Ddivider_silo3X~DtypingmachineYmAspyroYmAredhologramYmAcameraHjoulutahtiYmAhyacinth1YmAhyacinth2YmAchair_plasto*10YmAchair_plasto*11YmAbardeskcorner_polyfon*12X~Dbardeskcorner_polyfon*13X~Dchair_plasto*12YmAchair_plasto*13YmAchair_plasto*14YmAtable_plasto_4leg*14Y_ImocchamasterY_Icarpet_legocourtYmAbench_legoYmAlegotrophyYmAvalentinescreenYmAedicehcYmArare_daffodil_rugYmArare_beehive_bulbY_IhcsohvaYmAhcammeYmArare_elephant_statueYmArare_fountainY_Irare_standYmArare_globeYmArare_hammockYmArare_elephant_statue*1YmArare_elephant_statue*2YmArare_fountain*1Y_Irare_fountain*2Y_Irare_fountain*3Y_Irare_beehive_bulb*1Y_Irare_beehive_bulb*2Y_Irare_xmas_screenY_Irare_parasol*1XMVrare_parasol*2XMVrare_parasol*3XMVtree1X~Dtree2ZmBwcandleYxBrcandleYxBsoft_jaggara_norjaYmAhouse2YmAdjesko_turntableYmAmd_sofaZ[Mmd_limukaappiY_Itable_plasto_4leg*10Y_Itable_plasto_4leg*15Y_Itable_plasto_bigsquare*14Y_Itable_plasto_bigsquare*15Y_Itable_plasto_round*14Y_Itable_plasto_round*15Y_Itable_plasto_square*14Y_Itable_plasto_square*15Y_Ichair_plasto*15YmAchair_plasty*7X~Dchair_plasty*8X~Dchair_plasty*9X~Dchair_plasty*10X~Dchair_plasty*11X~Dchair_plasto*16YmAtable_plasto_4leg*16Y_Ihockey_scoreY_Ihockey_lightYmAdoorDYc[prizetrophy2*3Yd[prizetrophy3*3Yd[prizetrophy4*3Yd[prizetrophy5*3Yd[prizetrophy6*3Yd[prizetrophy*1Yd[prizetrophy2*1Yd[prizetrophy3*1Yd[prizetrophy4*1Yd[prizetrophy5*1Yd[prizetrophy6*1Yd[prizetrophy*2Yd[prizetrophy2*2Yd[prizetrophy3*2Yd[prizetrophy4*2Yd[prizetrophy5*2Yd[prizetrophy6*2Yd[prizetrophy*3Yd[rare_parasol*0XMVhc_lmp[fBhc_tblYmAhc_chrYmAhc_dskXQHnestHpetfood1ZvCpetfood2ZvCpetfood3ZvCwaterbowl*4XICwaterbowl*5XICwaterbowl*2XICwaterbowl*1XICwaterbowl*3XICtoy1XICtoy1*1XICtoy1*2XICtoy1*3XICtoy1*4XICgoodie1Yc[goodie1*1Yc[goodie1*2Yc[goodie2Yc[prizetrophy7*3Yd[prizetrophy7*1Yd[prizetrophy7*2Yd[scifiport*0Y_Iscifiport*9Y_Iscifiport*8Y_Iscifiport*7Y_Iscifiport*6Y_Iscifiport*5Y_Iscifiport*4Y_Iscifiport*3Y_Iscifiport*2Y_Iscifiport*1Y_Iscifirocket*9Y_Iscifirocket*8Y_Iscifirocket*7Y_Iscifirocket*6Y_Iscifirocket*5Y_Iscifirocket*4Y_Iscifirocket*3Y_Iscifirocket*2Y_Iscifirocket*1Y_Iscifirocket*0Y_Iscifidoor*10Y_Iscifidoor*9Y_Iscifidoor*8Y_Iscifidoor*7Y_Iscifidoor*6Y_Iscifidoor*5Y_Iscifidoor*4Y_Iscifidoor*3Y_Iscifidoor*2Y_Iscifidoor*1Y_Ipillow*5YmApillow*8YmApillow*0YmApillow*1YmApillow*2YmApillow*7YmApillow*9YmApillow*4YmApillow*6YmApillow*3YmAmarquee*1Y_Imarquee*2Y_Imarquee*7Y_Imarquee*aY_Imarquee*8Y_Imarquee*9Y_Imarquee*5Y_Imarquee*4Y_Imarquee*6Y_Imarquee*3Y_Iwooden_screen*1Y_Iwooden_screen*2Y_Iwooden_screen*7Y_Iwooden_screen*0Y_Iwooden_screen*8Y_Iwooden_screen*5Y_Iwooden_screen*9Y_Iwooden_screen*4Y_Iwooden_screen*6Y_Iwooden_screen*3Y_Ipillar*6Y_Ipillar*1Y_Ipillar*9Y_Ipillar*0Y_Ipillar*8Y_Ipillar*2Y_Ipillar*5Y_Ipillar*4Y_Ipillar*7Y_Ipillar*3Y_Irare_dragonlamp*4Y_Irare_dragonlamp*0Y_Irare_dragonlamp*5Y_Irare_dragonlamp*2Y_Irare_dragonlamp*8Y_Irare_dragonlamp*9Y_Irare_dragonlamp*7Y_Irare_dragonlamp*6Y_Irare_dragonlamp*1Y_Irare_dragonlamp*3Y_Irare_icecream*1Y_Irare_icecream*7Y_Irare_icecream*8Y_Irare_icecream*2Y_Irare_icecream*6Y_Irare_icecream*9Y_Irare_icecream*3Y_Irare_icecream*0Y_Irare_icecream*4Y_Irare_icecream*5Y_Irare_fan*7YxBrare_fan*6YxBrare_fan*9YxBrare_fan*3YxBrare_fan*0YxBrare_fan*4YxBrare_fan*5YxBrare_fan*1YxBrare_fan*8YxBrare_fan*2YxBqueue_tile1*3X~Dqueue_tile1*6X~Dqueue_tile1*4X~Dqueue_tile1*9X~Dqueue_tile1*8X~Dqueue_tile1*5X~Dqueue_tile1*7X~Dqueue_tile1*2X~Dqueue_tile1*1X~Dqueue_tile1*0X~DticketHrare_snowrugX~Dcn_lampZxIcn_sofaYmAsporttrack1*1YmAsporttrack1*3YmAsporttrack1*2YmAsporttrack2*1[~Nsporttrack2*2[~Nsporttrack2*3[~Nsporttrack3*1YmAsporttrack3*2YmAsporttrack3*3YmAfootylampX~Dbarchair_siloX~Ddivider_nor4*4X~Dtraffic_light*1ZxItraffic_light*2ZxItraffic_light*3ZxItraffic_light*4ZxItraffic_light*6ZxIrubberchair*1X~Drubberchair*2X~Drubberchair*3X~Drubberchair*4X~Drubberchair*5X~Drubberchair*6X~Dbarrier*1X~Dbarrier*2X~Dbarrier*3X~Drubberchair*7X~Drubberchair*8X~Dtable_norja_med*2Y_Itable_norja_med*3Y_Itable_norja_med*4Y_Itable_norja_med*5Y_Itable_norja_med*6Y_Itable_norja_med*7Y_Itable_norja_med*8Y_Itable_norja_med*9Y_Icouch_norja*2X~Dcouch_norja*3X~Dcouch_norja*4X~Dcouch_norja*5X~Dcouch_norja*6X~Dcouch_norja*7X~Dcouch_norja*8X~Dcouch_norja*9X~Dshelves_norja*2X~Dshelves_norja*3X~Dshelves_norja*4X~Dshelves_norja*5X~Dshelves_norja*6X~Dshelves_norja*7X~Dshelves_norja*8X~Dshelves_norja*9X~Dchair_norja*2X~Dchair_norja*3X~Dchair_norja*4X~Dchair_norja*5X~Dchair_norja*6X~Dchair_norja*7X~Dchair_norja*8X~Dchair_norja*9X~Ddivider_nor1*2X~Ddivider_nor1*3X~Ddivider_nor1*4X~Ddivider_nor1*5X~Ddivider_nor1*6X~Ddivider_nor1*7X~Ddivider_nor1*8X~Ddivider_nor1*9X~Dsoft_sofa_norja*2X~Dsoft_sofa_norja*3X~Dsoft_sofa_norja*4X~Dsoft_sofa_norja*5X~Dsoft_sofa_norja*6X~Dsoft_sofa_norja*7X~Dsoft_sofa_norja*8X~Dsoft_sofa_norja*9X~Dsoft_sofachair_norja*2X~Dsoft_sofachair_norja*3X~Dsoft_sofachair_norja*4X~Dsoft_sofachair_norja*5X~Dsoft_sofachair_norja*6X~Dsoft_sofachair_norja*7X~Dsoft_sofachair_norja*8X~Dsoft_sofachair_norja*9X~Dsofachair_silo*2X~Dsofachair_silo*3X~Dsofachair_silo*4X~Dsofachair_silo*5X~Dsofachair_silo*6X~Dsofachair_silo*7X~Dsofachair_silo*8X~Dsofachair_silo*9X~Dtable_silo_small*2X~Dtable_silo_small*3X~Dtable_silo_small*4X~Dtable_silo_small*5X~Dtable_silo_small*6X~Dtable_silo_small*7X~Dtable_silo_small*8X~Dtable_silo_small*9X~Ddivider_silo1*2X~Ddivider_silo1*3X~Ddivider_silo1*4X~Ddivider_silo1*5X~Ddivider_silo1*6X~Ddivider_silo1*7X~Ddivider_silo1*8X~Ddivider_silo1*9X~Ddivider_silo3*2X~Ddivider_silo3*3X~Ddivider_silo3*4X~Ddivider_silo3*5X~Ddivider_silo3*6X~Ddivider_silo3*7X~Ddivider_silo3*8X~Ddivider_silo3*9X~Dtable_silo_med*2X~Dtable_silo_med*3X~Dtable_silo_med*4X~Dtable_silo_med*5X~Dtable_silo_med*6X~Dtable_silo_med*7X~Dtable_silo_med*8X~Dtable_silo_med*9X~Dsofa_silo*2X~Dsofa_silo*3X~Dsofa_silo*4X~Dsofa_silo*5X~Dsofa_silo*6X~Dsofa_silo*7X~Dsofa_silo*8X~Dsofa_silo*9X~Dsofachair_polyfon*2X~Dsofachair_polyfon*3X~Dsofachair_polyfon*4X~Dsofachair_polyfon*6X~Dsofachair_polyfon*7X~Dsofachair_polyfon*8X~Dsofachair_polyfon*9X~Dsofa_polyfon*2Z[Msofa_polyfon*3Z[Msofa_polyfon*4Z[Msofa_polyfon*6Z[Msofa_polyfon*7Z[Msofa_polyfon*8Z[Msofa_polyfon*9Z[Mbed_polyfon*2X~Dbed_polyfon*3X~Dbed_polyfon*4X~Dbed_polyfon*6X~Dbed_polyfon*7X~Dbed_polyfon*8X~Dbed_polyfon*9X~Dbed_polyfon_one*2[dObed_polyfon_one*3[dObed_polyfon_one*4[dObed_polyfon_one*6[dObed_polyfon_one*7[dObed_polyfon_one*8[dObed_polyfon_one*9[dObardesk_polyfon*2X~Dbardesk_polyfon*3X~Dbardesk_polyfon*4X~Dbardesk_polyfon*5X~Dbardesk_polyfon*6X~Dbardesk_polyfon*7X~Dbardesk_polyfon*8X~Dbardesk_polyfon*9X~Dbardeskcorner_polyfon*2X~Dbardeskcorner_polyfon*3X~Dbardeskcorner_polyfon*4X~Dbardeskcorner_polyfon*5X~Dbardeskcorner_polyfon*6X~Dbardeskcorner_polyfon*7X~Dbardeskcorner_polyfon*8X~Dbardeskcorner_polyfon*9X~Ddivider_poly3*2X~Ddivider_poly3*3X~Ddivider_poly3*4X~Ddivider_poly3*5X~Ddivider_poly3*6X~Ddivider_poly3*7X~Ddivider_poly3*8X~Ddivider_poly3*9X~Dchair_silo*2X~Dchair_silo*3X~Dchair_silo*4X~Dchair_silo*5X~Dchair_silo*6X~Dchair_silo*7X~Dchair_silo*8X~Dchair_silo*9X~Ddivider_nor3*2X~Ddivider_nor3*3X~Ddivider_nor3*4X~Ddivider_nor3*5X~Ddivider_nor3*6X~Ddivider_nor3*7X~Ddivider_nor3*8X~Ddivider_nor3*9X~Ddivider_nor2*2X~Ddivider_nor2*3X~Ddivider_nor2*4X~Ddivider_nor2*5X~Ddivider_nor2*6X~Ddivider_nor2*7X~Ddivider_nor2*8X~Ddivider_nor2*9X~Dsilo_studydeskX~Dsolarium_norjaY_Isolarium_norja*1Y_Isolarium_norja*2Y_Isolarium_norja*3Y_Isolarium_norja*5Y_Isolarium_norja*6Y_Isolarium_norja*7Y_Isolarium_norja*8Y_Isolarium_norja*9Y_IsandrugX~Drare_moonrugYmAchair_chinaYmAchina_tableYmAsleepingbag*1YmAsleepingbag*2YmAsleepingbag*3YmAsleepingbag*4YmAsafe_siloY_Isleepingbag*7YmAsleepingbag*9YmAsleepingbag*5YmAsleepingbag*10YmAsleepingbag*6YmAsleepingbag*8YmAchina_shelveX~Dtraffic_light*5ZxIdivider_nor4*2X~Ddivider_nor4*3X~Ddivider_nor4*5X~Ddivider_nor4*6X~Ddivider_nor4*7X~Ddivider_nor4*8X~Ddivider_nor4*9X~Ddivider_nor5*2X~Ddivider_nor5*3X~Ddivider_nor5*4X~Ddivider_nor5*5X~Ddivider_nor5*6X~Ddivider_nor5*7X~Ddivider_nor5*8X~Ddivider_nor5*9X~Ddivider_nor5X~Ddivider_nor4X~Dwall_chinaYmAcorner_chinaYmAbarchair_silo*2X~Dbarchair_silo*3X~Dbarchair_silo*4X~Dbarchair_silo*5X~Dbarchair_silo*6X~Dbarchair_silo*7X~Dbarchair_silo*8X~Dbarchair_silo*9X~Dsafe_silo*2Y_Isafe_silo*3Y_Isafe_silo*4Y_Isafe_silo*5Y_Isafe_silo*6Y_Isafe_silo*7Y_Isafe_silo*8Y_Isafe_silo*9Y_Iglass_shelfY_Iglass_chairY_Iglass_stoolY_Iglass_sofaY_Iglass_tableY_Iglass_table*2Y_Iglass_table*3Y_Iglass_table*4Y_Iglass_table*5Y_Iglass_table*6Y_Iglass_table*7Y_Iglass_table*8Y_Iglass_table*9Y_Iglass_chair*2Y_Iglass_chair*3Y_Iglass_chair*4Y_Iglass_chair*5Y_Iglass_chair*6Y_Iglass_chair*7Y_Iglass_chair*8Y_Iglass_chair*9Y_Iglass_sofa*2Y_Iglass_sofa*3Y_Iglass_sofa*4Y_Iglass_sofa*5Y_Iglass_sofa*6Y_Iglass_sofa*7Y_Iglass_sofa*8Y_Iglass_sofa*9Y_Iglass_stool*2Y_Iglass_stool*4Y_Iglass_stool*5Y_Iglass_stool*6Y_Iglass_stool*7Y_Iglass_stool*8Y_Iglass_stool*3Y_Iglass_stool*9Y_ICF_10_coin_goldZvCCF_1_coin_bronzeZvCCF_20_moneybagZvCCF_50_goldbarZvCCF_5_coin_silverZvChc_crptYmAhc_tvZ\BgothgateX~DgothiccandelabraYxBgothrailingX~Dgoth_tableYmAhc_bkshlfYmAhc_btlrY_Ihc_crtnYmAhc_djsetYmAhc_frplcZbBhc_lmpstYmAhc_machineYmAhc_rllrXQHhc_rntgnX~Dhc_trllYmAgothic_chair*1X~Dgothic_sofa*1X~Dgothic_stool*1X~Dgothic_chair*2X~Dgothic_sofa*2X~Dgothic_stool*2X~Dgothic_chair*3X~Dgothic_sofa*3X~Dgothic_stool*3X~Dgothic_chair*4X~Dgothic_sofa*4X~Dgothic_stool*4X~Dgothic_chair*5X~Dgothic_sofa*5X~Dgothic_stool*5X~Dgothic_chair*6X~Dgothic_sofa*6X~Dgothic_stool*6X~Dval_cauldronX~Dsound_machineX~Dromantique_pianochair*3Y_Iromantique_pianochair*5Y_Iromantique_pianochair*2Y_Iromantique_pianochair*4Y_Iromantique_pianochair*1Y_Iromantique_divan*3Y_Iromantique_divan*5Y_Iromantique_divan*2Y_Iromantique_divan*4Y_Iromantique_divan*1Y_Iromantique_chair*3Y_Iromantique_chair*5Y_Iromantique_chair*2Y_Iromantique_chair*4Y_Iromantique_chair*1Y_Irare_parasolY_Iplant_valentinerose*3XICplant_valentinerose*5XICplant_valentinerose*2XICplant_valentinerose*4XICplant_valentinerose*1XICplant_mazegateYeCplant_mazeZcCplant_bulrushXICpetfood4Y_Icarpet_valentineZ|Egothic_carpetXICgothic_carpet2Z|Egothic_chairX~Dgothic_sofaX~Dgothic_stoolX~Dgrand_piano*3Z|Egrand_piano*5Z|Egrand_piano*2Z|Egrand_piano*4Z|Egrand_piano*1Z|Etheatre_seatZ@Kromantique_tray2Y_Iromantique_tray1Y_Iromantique_smalltabl*3Y_Iromantique_smalltabl*5Y_Iromantique_smalltabl*2Y_Iromantique_smalltabl*4Y_Iromantique_smalltabl*1Y_Iromantique_mirrortablY_Iromantique_divider*3Z[Mromantique_divider*2Z[Mromantique_divider*4Z[Mromantique_divider*1Z[Mjp_tatami2[dWjp_tatamiYGGhabbowood_chairYGGjp_bambooYGGjp_iroriXQHjp_pillowYGGsound_set_1[dWsound_set_2[dWsound_set_3[dWsound_set_4[dWsound_set_5[dWsound_set_6[dWsound_set_7[dWsound_set_8[dWsound_set_9[dWsound_machine*1Yc[spotlightY_Isound_machine*2Yc[sound_machine*3Yc[sound_machine*4Yc[sound_machine*5Yc[sound_machine*6Yc[sound_machine*7Yc[rom_lampZ|Erclr_sofaXQHrclr_gardenXQHrclr_chairZ|Esound_set_28[dWsound_set_27[dWsound_set_26[dWsound_set_25[dWsound_set_24[dWsound_set_23[dWsound_set_22[dWsound_set_21[dWsound_set_20[dWsound_set_19[dWsound_set_18[dWsound_set_17[dWsound_set_16[dWsound_set_15[dWsound_set_14[dWsound_set_13[dWsound_set_12[dWsound_set_11[dWsound_set_10[dWrope_dividerXQHromantique_clockY_Irare_icecream_campaignY_Ipura_mdl5*1Yc[pura_mdl5*2Yc[pura_mdl5*3Yc[pura_mdl5*4Yc[pura_mdl5*5Yc[pura_mdl5*6Yc[pura_mdl5*7Yc[pura_mdl5*8Yc[pura_mdl5*9Yc[pura_mdl4*1XQHpura_mdl4*2XQHpura_mdl4*3XQHpura_mdl4*4XQHpura_mdl4*5XQHpura_mdl4*6XQHpura_mdl4*7XQHpura_mdl4*8XQHpura_mdl4*9XQHpura_mdl3*1XQHpura_mdl3*2XQHpura_mdl3*3XQHpura_mdl3*4XQHpura_mdl3*5XQHpura_mdl3*6XQHpura_mdl3*7XQHpura_mdl3*8XQHpura_mdl3*9XQHpura_mdl2*1XQHpura_mdl2*2XQHpura_mdl2*3XQHpura_mdl2*4XQHpura_mdl2*5XQHpura_mdl2*6XQHpura_mdl2*7XQHpura_mdl2*8XQHpura_mdl2*9XQHpura_mdl1*1XQHpura_mdl1*2XQHpura_mdl1*3XQHpura_mdl1*4XQHpura_mdl1*5XQHpura_mdl1*6XQHpura_mdl1*7XQHpura_mdl1*8XQHpura_mdl1*9XQHjp_lanternXQHchair_basic*1XQHchair_basic*2XQHchair_basic*3XQHchair_basic*4XQHchair_basic*5XQHchair_basic*6XQHchair_basic*7XQHchair_basic*8XQHchair_basic*9XQHbed_budget*1XQHbed_budget*2XQHbed_budget*3XQHbed_budget*4XQHbed_budget*5XQHbed_budget*6XQHbed_budget*7XQHbed_budget*8XQHbed_budget*9XQHbed_budget_one*1XQHbed_budget_one*2XQHbed_budget_one*3XQHbed_budget_one*4XQHbed_budget_one*5XQHbed_budget_one*6XQHbed_budget_one*7XQHbed_budget_one*8XQHbed_budget_one*9XQHjp_drawerXQHtile_stellaZ[Mtile_marbleZ[Mtile_brownZ[Msummer_grill*1Y_Isummer_grill*2Y_Isummer_grill*3Y_Isummer_grill*4Y_Isummer_chair*1Y_Isummer_chair*2Y_Isummer_chair*3Y_Isummer_chair*4Y_Isummer_chair*5Y_Isummer_chair*6Y_Isummer_chair*7Y_Isummer_chair*8Y_Isummer_chair*9Y_Isound_set_36[dWsound_set_35[dWsound_set_34[dWsound_set_33[dWsound_set_32[dWsound_set_31[dWsound_set_30[dWsound_set_29[dWsound_machine_proYc[rare_mnstrY_Ione_way_door*1XQHone_way_door*2XQHone_way_door*3XQHone_way_door*4XQHone_way_door*5XQHone_way_door*6XQHone_way_door*7XQHone_way_door*8XQHone_way_door*9XQHexe_rugZ[Mexe_s_tableZGRsound_set_37[dWsummer_pool*1ZlIsummer_pool*2ZlIsummer_pool*3ZlIsummer_pool*4ZlIsong_diskYc[jukebox*1Yc[carpet_soft_tut[~Nsound_set_44[dWsound_set_43[dWsound_set_42[dWsound_set_41[dWsound_set_40[dWsound_set_39[dWsound_set_38[dWgrunge_chairZ@Kgrunge_mattressZ@Kgrunge_radiatorZ@Kgrunge_shelfZ@Kgrunge_signZ@Kgrunge_tableZ@Khabboween_crypt[uKhabboween_grassZ@Khal_cauldronZ@Khal_graveZ@Ksound_set_52[dWsound_set_51[dWsound_set_50[dWsound_set_49[dWsound_set_48[dWsound_set_47[dWsound_set_46[dWsound_set_45[dWxmas_icelampZ[Mxmas_cstl_wallZ[Mxmas_cstl_twrZ[Mxmas_cstl_gate[~Ntree7Z[Mtree6Z[Msound_set_54[dWsound_set_53[dWsafe_silo_pb[dOplant_mazegate_snowZ[Mplant_maze_snowZ[Mchristmas_sleighZ[Mchristmas_reindeer[~Nchristmas_poopZ[Mexe_bardeskZ[Mexe_chairZ[Mexe_chair2Z[Mexe_cornerZ[Mexe_drinksZ[Mexe_sofaZ[Mexe_tableZ[Msound_set_59[dWsound_set_58[dWsound_set_57[dWsound_set_56[dWsound_set_55[dWnoob_table*1[~Nnoob_table*2[~Nnoob_table*3[~Nnoob_table*4[~Nnoob_table*5[~Nnoob_table*6[~Nnoob_stool*1[~Nnoob_stool*2[~Nnoob_stool*3[~Nnoob_stool*4[~Nnoob_stool*5[~Nnoob_stool*6[~Nnoob_rug*1[~Nnoob_rug*2[~Nnoob_rug*3[~Nnoob_rug*4[~Nnoob_rug*5[~Nnoob_rug*6[~Nnoob_lamp*1[dOnoob_lamp*2[dOnoob_lamp*3[dOnoob_lamp*4[dOnoob_lamp*5[dOnoob_lamp*6[dOnoob_chair*1[~Nnoob_chair*2[~Nnoob_chair*3[~Nnoob_chair*4[~Nnoob_chair*5[~Nnoob_chair*6[~Nexe_globe[~Nexe_plantZ[Mval_teddy*1[dOval_teddy*2[dOval_teddy*3[dOval_teddy*4[dOval_teddy*5[dOval_teddy*6[dOval_randomizer[dOval_choco[dOteleport_doorYc[sound_set_61[dWsound_set_60[dWfortune[dOsw_tableZIPsw_raven[cQsw_chestZIPsand_cstl_wallZIPsand_cstl_twrZIPsand_cstl_gateZIPgrunge_candleZIPgrunge_benchZIPgrunge_barrelZIPrclr_lampZGRprizetrophy9*1Yd[prizetrophy8*1Yd[nouvelle_traxYc[md_rugZGRjp_tray6ZGRjp_tray5ZGRjp_tray4ZGRjp_tray3ZGRjp_tray2ZGRjp_tray1ZGRarabian_teamkZGRarabian_snakeZGRarabian_rugZGRarabian_pllwZGRarabian_divdrZGRarabian_chairZGRarabian_bigtbZGRarabian_tetblZGRarabian_tray1ZGRarabian_tray2ZGRarabian_tray3ZGRarabian_tray4ZGRsound_set_64[dWsound_set_63[dWsound_set_62[dWjukebox_ptv*1Yc[calippoZAStraxsilverYc[traxgoldYc[traxbronzeYc[bench_puffetYATCFC_500_goldbarZvCCFC_200_moneybagZvCCFC_10_coin_bronzeZvCCFC_100_coin_goldZvCCFC_50_coin_silverZvCjp_tableXMVjp_rareXMVjp_katana3XMVjp_katana2XMVjp_katana1XMVfootylamp_campaignXMVtiki_waterfall[dWtiki_tray4[dWtiki_tray3[dWtiki_tray2[dWtiki_tray1[dWtiki_tray0[dWtiki_toucan[dWtiki_torch[dWtiki_statue[dWtiki_sand[dWtiki_parasol[dWtiki_junglerug[dWtiki_corner[dWtiki_bflies[dWtiki_bench[dWtiki_bardesk[dWtampax_rug[dWsound_set_70[dWsound_set_69[dWsound_set_68[dWsound_set_67[dWsound_set_66[dWsound_set_65[dWnoob_rug_tradeable*1[dWnoob_rug_tradeable*2[dWnoob_rug_tradeable*3[dWnoob_rug_tradeable*4[dWnoob_rug_tradeable*5[dWnoob_rug_tradeable*6[dWnoob_plant[dWnoob_lamp_tradeable*1[dWnoob_lamp_tradeable*2[dWnoob_lamp_tradeable*3[dWnoob_lamp_tradeable*4[dWnoob_lamp_tradeable*5[dWnoob_lamp_tradeable*6[dWnoob_chair_tradeable*1[dWnoob_chair_tradeable*2[dWnoob_chair_tradeable*3[dWnoob_chair_tradeable*4[dWnoob_chair_tradeable*5[dWnoob_chair_tradeable*6[dWjp_teamaker[dWsvnr_uk[`_svnr_nlXhXsvnr_itXhXsvnr_de[gXsvnr_aus[gXdiner_tray_7[gXdiner_tray_6[gXdiner_tray_5[gXdiner_tray_4[gXdiner_tray_3[gXdiner_tray_2[gXdiner_tray_1[gXdiner_tray_0[gXdiner_sofa_2*1[gXdiner_sofa_2*2[gXdiner_sofa_2*3[gXdiner_sofa_2*4[gXdiner_sofa_2*5[gXdiner_sofa_2*6[gXdiner_sofa_2*7[gXdiner_sofa_2*8[gXdiner_sofa_2*9[gXdiner_shaker[gXdiner_rug[gXdiner_gumvendor*1[gXdiner_gumvendor*2[gXdiner_gumvendor*3[gXdiner_gumvendor*4[gXdiner_gumvendor*5[gXdiner_gumvendor*6[gXdiner_gumvendor*7[gXdiner_gumvendor*8[gXdiner_gumvendor*9[gXdiner_cashreg*1[gXdiner_cashreg*2[gXdiner_cashreg*3[gXdiner_cashreg*4[gXdiner_cashreg*5[gXdiner_cashreg*6[gXdiner_cashreg*7[gXdiner_cashreg*8[gXdiner_cashreg*9[gXdiner_table_2*1XiZdiner_table_2*2diner_table_2*2XiZdiner_table_2*3XiZdiner_table_2*4XiZdiner_table_2*5XiZdiner_table_2*6XiZdiner_table_2*7XiZdiner_table_2*8XiZdiner_table_2*9XiZdiner_table_1*1XiZdiner_table_1*2XiZdiner_table_1*3XiZdiner_table_1*4XiZdiner_table_1*5XiZdiner_table_1*6XiZdiner_table_1*7XiZdiner_table_1*8XiZdiner_table_1*9XiZdiner_sofa_1*1XiZdiner_sofa_1*2XiZdiner_sofa_1*3XiZdiner_sofa_1*4XiZdiner_sofa_1*5XiZdiner_sofa_1*6XiZdiner_sofa_1*7XiZdiner_sofa_1*8XiZdiner_sofa_1*9XiZdiner_chair*1XiZdiner_chair*2XiZdiner_chair*3XiZdiner_chair*4XiZdiner_chair*5XiZdiner_chair*6XiZdiner_chair*7XiZdiner_chair*8XiZdiner_chair*9XiZdiner_bardesk_gate*1XiZdiner_bardesk_gate*2XiZdiner_bardesk_gate*3XiZdiner_bardesk_gate*4XiZdiner_bardesk_gate*5XiZdiner_bardesk_gate*6XiZdiner_bardesk_gate*7XiZdiner_bardesk_gate*8XiZdiner_bardesk_gate*9XiZdiner_bardesk_corner*1XiZdiner_bardesk_corner*2XiZdiner_bardesk_corner*3XiZdiner_bardesk_corner*4XiZdiner_bardesk_corner*5XiZdiner_bardesk_corner*6XiZdiner_bardesk_corner*7XiZdiner_bardesk_corner*8XiZdiner_bardesk_corner*9XiZdiner_bardesk*1XiZdiner_bardesk*2XiZdiner_bardesk*3XiZdiner_bardesk*4XiZdiner_bardesk*5XiZdiner_bardesk*6XiZdiner_bardesk*7XiZdiner_bardesk*8XiZdiner_bardesk*9XiZads_dave_cnsXiZeasy_carpetYc[easy_bowl2Yc[greek_cornerYc[greek_gateYc[greek_pillarsYc[greek_seatYc[greektrophy*1[P\greektrophy*2[P\greektrophy*3[P\greek_blockXt[hcc_tableY`]hcc_shelfY`]hcc_sofaY`]hcc_minibarY`]hcc_chairY`]det_dividerY`]netari_carpetY`]det_bodyY`]hcc_stoolY`]hcc_sofachairY`]hcc_crnrXw]hcc_dvdrXw]sob_carpet[`_igor_seat[`_ads_igorbrainY_aads_igorswitchY_aads_711*1Y_aads_711*2Y_aads_711*3Y_aads_711*4Y_aads_igorraygunY_ahween08_sinkY[chween08_curtainY[chween08_bathY[chween08_defibsY[chween08_bbagY[chween08_curtain2Y[chween08_defibs2Y[chween08_bedY[chween08_sink2Y[chween08_bed2Y[chween08_bath2Y[chween08_manholeY[chween08_trllY[cPRpost.itHpost.it.vdHphotoHChessHTicTacToeHBattleShipHPokerHwallpaperHfloorHposterZ@KgothicfountainYxBhc_wall_lampZbBindustrialfanZ`BtorchZ\Bval_heartXBCwallmirrorZ|Ejp_ninjastarsXQHhabw_mirrorXQHhabbowheelZ[Mguitar_skullZ@Kguitar_vZ@Kxmas_light[~Nhrella_poster_3[Nhrella_poster_2ZIPhrella_poster_1[Nsw_swordsZIPsw_stoneZIPsw_holeZIProomdimmerYc[md_logo_wallZGRmd_canZGRjp_sheet3ZGRjp_sheet2ZGRjp_sheet1ZGRarabian_swordsZGRarabian_wndwZGRtiki_wallplnt[dWtiki_surfboard[dWtampax_wall[dWwindow_single_default[gXwindow_double_default[gXnoob_window_double[dWwindow_triple[gXwindow_square[gXwindow_romantic_wide[gXwindow_romantic_narrow[gXwindow_grunge[gXwindow_golden[gXwindow_chinese_wide[gXwindow_chinese_narrowYA\window_basic[gXwindow_70s_wide[gXwindow_70s_narrow[gXads_sunnydYlXwindow_diner2XiZwindow_dinerXiZdiner_walltableXiZads_dave_wallXiZwindow_holeYc[easy_posterYc[ads_nokia_logoYc[ads_nokia_phoneYc[landscapeXV^window_skyscraper[j\netari_posterY`]det_bholeY`]ads_campguitarXw]hween08_radY[chween08_wndwbY[chween08_wndwY[chween08_bioY[chw_08_xrayY[c").Build());
                                    _receivedSpriteIndex = true;
                                }
                            }
                            break;
                        }
                    }

                case "@ ": // Enter room - guestroom - get wallitems
                    {
                        if (_ROOMACCESS_SECONDARY_OK && Room != null)
                            sendData(new HabboPacketBuilder("@m").Append(Room.Wallitems).Build());
                        break;
                    }

                case "A@": // Enter room - add this user to room
                    {
                        if (_ROOMACCESS_SECONDARY_OK && Room != null && roomUser == null)
                        {
                            sendData(new HabboPacketBuilder("@b").Append(Room.dynamicStatuses).Build());
                            Room.addUser(this);
                        }
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
