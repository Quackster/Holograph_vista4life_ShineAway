using System;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;
using Holo.Virtual.Rooms.Games;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Partial class containing game-related packet processing.
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes game-related packets.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        /// <returns>True if the packet was handled, false otherwise.</returns>
        private bool processGamePackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                #region Games
                case "B_": // Gamelobby - refresh gamelist
                    {
                        if (Room != null && Room.Lobby != null)
                            sendData(new HabboPacketBuilder("Ch").Append(Room.Lobby.gameList()).Build());
                        break;
                    }

                case "B`": // Gamelobby - checkout single game sub
                    {
                        if (Room != null && roomUser != null && Room.Lobby != null && gamePlayer == null)
                        {
                            int gameID = Encoding.decodeVL64(currentPacket.Substring(2));
                            if (Room.Lobby.Games.ContainsKey(gameID))
                            {
                                this.gamePlayer = new gamePlayer(this, roomUser.roomUID, (Game)Room.Lobby.Games[gameID]);
                                gamePlayer.Game.Subviewers.Add(gamePlayer);
                                sendData(new HabboPacketBuilder("Ci").Append(gamePlayer.Game.Sub).Build());
                            }
                        }
                        break;
                    }

                case "Bb": // Gamelobby - request new game create
                    {
                        if (Room != null && roomUser != null && Room.Lobby != null && gamePlayer == null)
                        {
                            if (_Tickets > 1) // Atleast two tickets in inventory
                            {
                                if (Room.Lobby.validGamerank(roomUser.gamePoints))
                                {
                                    if (Room.Lobby.isBattleBall)
                                        sendData(new HabboPacketBuilder("Ck").Append(Room.Lobby.getCreateGameSettings()).Build());
                                    else
                                        sendData(new HabboPacketBuilder("Ck").Append("RA").Append("secondsUntilRestart").Append(Convert.ToChar(2)).Append("HIRGIHHfieldType").Append(Convert.ToChar(2)).Append("HKIIIISAnumTeams").Append(Convert.ToChar(2)).Append("HJJIII").Append("PA").Append("gameLengthChoice").Append(Convert.ToChar(2)).Append("HJIIIIK").Append("name").Append(Convert.ToChar(2)).Append("IJ").Append(Convert.ToChar(2)).Append("H").Append("secondsUntilStart").Append(Convert.ToChar(2)).Append("HIRBIHH").Build());
                                }
                                else
                                    sendData(new HabboPacketBuilder("Cl").Append("K").Build()); // Error [3] = Skillevel not valid in this lobby
                            }
                            else
                                sendData(new HabboPacketBuilder("Cl").Append("J").Build()); // Error [2] = Not enough tickets
                        }
                        break;
                    }

                case "Bc": // Gamelobby - process new created game
                    {
                        if (Room != null && roomUser != null && Room.Lobby != null && gamePlayer == null)
                        {
                            if (_Tickets > 1) // Atleast two tickets in inventory
                            {
                                if (Room.Lobby.validGamerank(roomUser.gamePoints))
                                {
                                    try
                                    {
                                        int mapID = -1;
                                        int teamAmount = -1;
                                        int[] Powerups = null;
                                        string Name = "";

                                        #region Game settings decoding
                                        int keyAmount = Encoding.decodeVL64(currentPacket.Substring(2));
                                        currentPacket = currentPacket.Substring(Encoding.encodeVL64(keyAmount).Length + 2);
                                        for (int i = 0; i < keyAmount; i++)
                                        {
                                            int j = Encoding.decodeB64(currentPacket.Substring(0, 2));
                                            string Key = currentPacket.Substring(2, j);
                                            if (currentPacket.Substring(j + 2, 1) == "H") // VL64 value
                                            {
                                                int Value = Encoding.decodeVL64(currentPacket.Substring(j + 3));
                                                switch (Key)
                                                {
                                                    case "fieldType":
                                                        //if (Value != 5)
                                                        //{
                                                        //    sendData("BK" + "Soz but only the maps for Oldskool are added to db yet kthx.");
                                                        //    return;
                                                        //}
                                                        mapID = Value;
                                                        break;

                                                    case "numTeams":
                                                        teamAmount = Value;
                                                        break;
                                                }
                                                int k = Encoding.encodeVL64(Value).Length;
                                                currentPacket = currentPacket.Substring(j + k + 3);
                                            }
                                            else // B64 value
                                            {

                                                int valLen = Encoding.decodeB64(currentPacket.Substring(j + 3, 2));
                                                string Value = currentPacket.Substring(j + 5, valLen);

                                                switch (Key)
                                                {
                                                    case "allowedPowerups":
                                                        string[] ps = Value.Split(',');
                                                        Powerups = new int[ps.Length];
                                                        for (int p = 0; p < ps.Length; p++)
                                                        {
                                                            int P = int.Parse(ps[p]);
                                                            if (Room.Lobby.allowsPowerup(P))
                                                                Powerups[p] = P;
                                                            else // Powerup not allowed in this lobby
                                                                return true;
                                                        }
                                                        break;

                                                    case "name":
                                                        Name = stringManager.filterSwearwords(Value);
                                                        break;
                                                }
                                                currentPacket = currentPacket.Substring(j + valLen + 5);
                                            }
                                        }
                                        #endregion

                                        if (mapID == -1 || teamAmount == -1 || Name == "") // Incorrect keys supplied by client
                                            return true;
                                        this.gamePlayer = new gamePlayer(this, roomUser.roomUID, null);
                                        Room.Lobby.createGame(this.gamePlayer, Name, mapID, teamAmount, Powerups);
                                    }
                                    catch { }
                                }
                                else
                                    sendData(new HabboPacketBuilder("Cl").Append("K").Build()); // Error [3] = Skillevel not valid in this lobby
                            }
                            else
                                sendData(new HabboPacketBuilder("Cl").Append("J").Build()); // Error [2] = Not enough tickets
                        }
                        break;
                    }

                case "Be": // Gamelobby - switch team in game
                    {
                        if (Room != null && Room.Lobby != null && gamePlayer != null && gamePlayer.Game.State == Game.gameState.Waiting)
                        {
                            if (_Tickets > 1) // Atleast two tickets in inventory
                            {
                                if (Room.Lobby.validGamerank(roomUser.gamePoints))
                                {
                                    int j = Encoding.decodeVL64(currentPacket.Substring(2));
                                    int teamID = Encoding.decodeVL64(currentPacket.Substring(Encoding.encodeVL64(j).Length + 2));

                                    if (teamID != gamePlayer.teamID && gamePlayer.Game.teamHasSpace(teamID))
                                    {
                                        if (gamePlayer.teamID == -1) // User was a subviewer
                                            gamePlayer.Game.Subviewers.Remove(gamePlayer);
                                        gamePlayer.Game.movePlayer(gamePlayer, gamePlayer.teamID, teamID);
                                    }
                                    else
                                        sendData(new HabboPacketBuilder("Cl").Append("H").Build()); // Error [0] = Team full
                                }
                                else
                                    sendData(new HabboPacketBuilder("Cl").Append("K").Build()); // Error [3] = Skillevel not valid in this lobby
                            }
                            else
                                sendData(new HabboPacketBuilder("Cl").Append("J").Build()); // Error [2] = Not enough tickets
                        }
                        break;
                    }

                case "Bg": // Gamelobby - leave single game sub
                    {
                        leaveGame();
                        break;
                    }

                case "Bh": // Gamelobby - kick player from game
                    {
                        if (Room != null && Room.Lobby != null && gamePlayer != null && gamePlayer.Game != null && gamePlayer == gamePlayer.Game.Owner)
                        {
                            int roomUID = Encoding.decodeVL64(currentPacket.Substring(2));
                            for (int i = 0; i < gamePlayer.Game.Teams.Length; i++)
                            {
                                foreach (gamePlayer Member in gamePlayer.Game.Teams[i])
                                {
                                    if (Member.roomUID == roomUID)
                                    {
                                        Member.sendData(new HabboPacketBuilder("Cl").Append("RA").Build()); // Error [6] = kicked from game
                                        gamePlayer.Game.movePlayer(Member, i, -1);
                                        return true;
                                    }
                                }
                            }
                        }
                        break;
                    }

                case "Bj": // Gamelobby - start game
                    {
                        if (Room != null && Room.Lobby != null && gamePlayer != null && gamePlayer == gamePlayer.Game.Owner)
                        {
                            //if(Game.Launchable)
                            //{
                            gamePlayer.Game.startGame();
                            //}
                            //else
                            //    sendData("Cl" + "I");
                        }
                        break;
                    }

                case "Bk": // Game - ingame - move unit
                    {
                        if (gamePlayer != null && gamePlayer.Game.State == Game.gameState.Started && gamePlayer.teamID != -1)
                        {
                            gamePlayer.goalX = Encoding.decodeVL64(currentPacket.Substring(3));
                            gamePlayer.goalY = Encoding.decodeVL64(currentPacket.Substring(Encoding.encodeVL64(gamePlayer.goalX).Length + 3));
                            Out.WriteLine(_Username + ": " + gamePlayer.goalX + "," + gamePlayer.goalY);
                        }
                        break;
                    }

                case "Bl": // Game - ingame - proceed with restart of game
                    {
                        if (gamePlayer != null && gamePlayer.Game.State == Game.gameState.Ended && gamePlayer.teamID != -1)
                        {
                            //sendData(new HabboPacketBuilder("Ck").Append("RA").Append("secondsUntilRestart").Append(Convert.ToChar(2)).Append("HIRGIHHfieldType").Append(Convert.ToChar(2)).Append("HKIIIISAnumTeams").Append(Convert.ToChar(2)).Append("HJJIII").Append("PA").Append("gameLengthChoice").Append(Convert.ToChar(2)).Append("HJIIIIK").Append("name").Append(Convert.ToChar(2)).Append("IJ").Append(Convert.ToChar(2)).Append("H").Append("secondsUntilStart").Append(Convert.ToChar(2)).Append("HIRBIHH").Build());
                            gamePlayer.Game.sendData(new HabboPacketBuilder("BK").Append(_Username).Append(" wants to replay!").Build());
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
