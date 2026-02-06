using System;
using System.Text;
using System.Threading;

using Holo.Protocol;
using Holo.Virtual.Users;
using Holo.Virtual.Rooms.Pathfinding;

namespace Holo.Virtual.Rooms.Games
{
    /// <summary>
    /// Partial class for Game containing arena and gameplay methods.
    /// </summary>
    internal partial class Game
    {
        #region Arena
        internal void startGame()
        {
            State = gameState.Started;
            foreach (gamePlayer subViewer in Subviewers)
                subViewer.sendData(this.Sub);

            this.Heightmap = DB.runRead("SELECT heightmap FROM games_maps WHERE type = '" + Lobby.Type + "' AND id = '" + mapID + "'");
            string[] _H = Heightmap.Split(Convert.ToChar(13));
            this.bX = _H[0].Length;
            this.bY = _H.Length - 1;
            string[] _T = null;

            if (isBattleBall)
            {
                this.bbTState = new bbTileState[bX, bY];
                this.bbTColor = new bbTileColor[bX, bY];
                _T = DB.runRead("SELECT bb_tilemap FROM games_maps WHERE type = 'bb' AND id = '" + mapID + "'").Split(Convert.ToChar(13));
            }
            this.Height = new byte[bX, bY];
            this.Blocked = new bool[bX, bY];

            for (int y = 0; y < bY; y++)
            {
                for (int x = 0; x < bX; x++)
                {
                    string q = _H[y].Substring(x, 1);
                    if (q == "x")
                    {
                        Blocked[x, y] = true;
                        if (isBattleBall)
                        {
                            bbTColor[x, y] = bbTileColor.Disabled;
                            bbTState[x, y] = bbTileState.Sealed;
                        }
                    }
                    else
                    {
                        Height[x, y] = Convert.ToByte(q);
                        if (isBattleBall)
                        {
                            if (_T[y].Substring(x, 1) == "1")
                                bbTColor[x, y] = bbTileColor.Default;
                            else
                            {
                                bbTColor[x, y] = bbTileColor.Disabled;
                                bbTState[x, y] = bbTileState.Sealed;
                            }
                        }
                    }
                }
            }

            int j = 0;
            for (int i = 0; i < Teams.Length; i++)
            {
                if (Teams[i].Count == 0) // Empty team
                    continue;

                int[] spawnData = DB.runReadRow("SELECT x,y,z FROM games_maps_playerspawns WHERE type = '" + Lobby.Type + "' AND mapid = '" + mapID + "' AND teamid = '" + i + "'", null);
                int spawnX = spawnData[0];
                int spawnY = spawnData[1];
                byte spawnZ = (byte)spawnData[2];
                bool Flip = false;

                foreach (gamePlayer Player in Teams[i])
                {
                    Player.roomUID = j;
                rs:
                    try
                    {
                        while (Blocked[spawnX, spawnY])
                        {
                            if (spawnZ == 0 || spawnZ == 2)
                            {
                                if (Flip)
                                    spawnX -= 1;
                                else
                                    spawnX += 1;
                            }
                            else if (spawnZ == 4 || spawnZ == 6)
                            {
                                if (Flip)
                                    spawnY -= 1;
                                else
                                    spawnY += 1;
                            }
                        }
                        Flip = (Flip != true);
                    }
                    catch { Flip = (Flip != true); goto rs; } // Out of range of map, reverse spawn

                    Blocked[spawnX, spawnY] = true;
                    Player.X = spawnX;
                    Player.Y = spawnY;
                    Player.Z = spawnZ;
                    Player.H = Height[spawnX, spawnY];
                    Player.goalX = -1;
                    Player.enteringGame = true;
                    j++;

                    Out.WriteLine("Assigned spawnpoint [" + spawnX + "," + spawnY + "] to player [" + j + ", team id: " + Player.teamID + "]");
                }
            }

            sendData(new HabboPacketBuilder("Cq").AppendVL64(-1).Build()); // Send players to arena
            new Eucalypt.commonDelegate(countDownTicker).BeginInvoke(null, null); // Start countdown bar
        }
        /// <summary>
        /// Counts
        /// </summary>
        private void countDownTicker()
        {
            while (leftCountdownSeconds > 0)
            {
                leftCountdownSeconds--;
                Thread.Sleep(1000);
            }
            for (int i = 0; i < Teams.Length; i++)
                foreach (gamePlayer Player in Teams[i])
                {
                    Player.User._Tickets -= 2;
                    Player.User.sendData(new HabboPacketBuilder("A|").Append(Player.User._Tickets).Build());
                    DB.runQuery("UPDATE users SET tickets = '" + Player.User._Tickets + "' WHERE id = '" + Player.User.userID + "' LIMIT 1");
                }

            // Start game
            updateHandler = new Thread(new ThreadStart(updateLoop));
            updateHandler.Start();
            sendData(new HabboPacketBuilder("Cw").AppendVL64(totalTime).Build());
        }
        internal string getMap()
        {
            StringBuilder Setup = new StringBuilder();
            if (isBattleBall)
            {
                Setup.Append("I" + Encoding.encodeVL64(leftCountdownSeconds) + Encoding.encodeVL64(Config.Game_Countdown_Seconds) + "H" + Encoding.encodeVL64(bX) + Encoding.encodeVL64(bY));
                for (int y = 0; y < bY; y++)
                    for (int x = 0; x < bX; x++)
                        Setup.Append(Encoding.encodeVL64((int)bbTColor[x, y]) + Encoding.encodeVL64((int)bbTState[x, y]));

                Setup.Append("IH");
            }
            else
                Setup.Append("holo.cast.not_finished");

            return Setup.ToString();
        }
        internal string getPlayers()
        {
            StringBuilder Setup = null;
            StringBuilder Helper = new StringBuilder();
            Random RND = new Random(DateTime.Now.Millisecond * totalTime);
            if (isBattleBall)
            {
                int p = 0;
                for (int i = 0; i < Teams.Length; i++)
                    foreach (gamePlayer Player in Teams[i])
                    {
                        Out.WriteLine(Player.roomUID + "!");
                        Helper.Append("H" + Encoding.encodeVL64(p) + Encoding.encodeVL64(Player.X) + Encoding.encodeVL64(Player.Y) + Encoding.encodeVL64(Player.H) + Encoding.encodeVL64(Player.Z) + "HM" + Player.User._Username + Convert.ToChar(2) + Player.User._Mission + Convert.ToChar(2) + Player.User._Figure + Convert.ToChar(2) + Player.User._Sex + Convert.ToChar(2) + Encoding.encodeVL64(Player.teamID) + Encoding.encodeVL64(Player.roomUID));
                        p++;
                    }

                Setup = new StringBuilder("I" + Encoding.encodeVL64(leftCountdownSeconds) + Encoding.encodeVL64(Config.Game_Countdown_Seconds) + Encoding.encodeVL64(p));
                Setup.Append(Helper.ToString());
                Helper = null;

                Setup.Append(Encoding.encodeVL64(bX) + Encoding.encodeVL64(bY));
                for (int y = 0; y < bY; y++)
                    for (int x = 0; x < bX; x++)
                        Setup.Append(Encoding.encodeVL64((int)bbTColor[x, y]) + Encoding.encodeVL64((int)bbTState[x, y]));

                Setup.Append("IH");
            }
            else
                Setup.Append("holo.cast.not_finished");

            return Setup.ToString();
        }
        private void updateLoop()
        {
            if (isBattleBall)
            {
                int teamAmount = Teams.Length;
                int[] teamScores = new int[teamAmount];
                bool subtrSecond = false;
                int activeTeamAmount = 0;
                for (int i = 0; i < teamAmount; i++)
                {
                    if (Teams[i].Count > 0)
                        activeTeamAmount++;
                    else
                        teamScores[i] = -1;
                }

                Random RND = new Random(DateTime.Now.Millisecond * teamAmount);

                while (leftTime > 0)
                {
                    int[] Amounts = new int[3]; // [0] = unit amount, [1] = updated tile amount, [2] = moving units amount
                    StringBuilder Players = new StringBuilder();
                    StringBuilder updatedTiles = new StringBuilder();
                    StringBuilder Movements = new StringBuilder();

                    for (int i = 0; i < teamAmount; i++)
                        foreach (gamePlayer Player in Teams[i])
                        {
                            Players.Append("H" + Encoding.encodeVL64(Player.roomUID) + Encoding.encodeVL64(Player.X) + Encoding.encodeVL64(Player.Y) + Encoding.encodeVL64(Player.H) + Encoding.encodeVL64(Player.Z) + "HM");
                            if (Player.bbColorTile)
                            {
                                Player.bbColorTile = false;
                                updatedTiles.Append(Encoding.encodeVL64(Player.X) + Encoding.encodeVL64(Player.Y) + Encoding.encodeVL64((int)bbTColor[Player.X, Player.Y]) + Encoding.encodeVL64((int)bbTState[Player.X, Player.Y]));
                                Amounts[1]++;
                            }
                            if (Player.goalX != -1)
                            {
                                Coord Next = gamePathfinder.getNextStep(Player.X, Player.Y, Player.goalX, Player.goalY);
                                if (Next.X != -1 && Blocked[Next.X, Next.Y] == false)
                                {
                                    Amounts[2]++;
                                    Movements.Append("J" + Encoding.encodeVL64(Player.roomUID) + Encoding.encodeVL64(Next.X) + Encoding.encodeVL64(Next.Y));
                                    if (Next.X == Player.goalX && Next.Y == Player.goalY)
                                        Player.goalX = -1;

                                    Blocked[Player.X, Player.Y] = false;
                                    Blocked[Next.X, Next.Y] = true;
                                    Player.Z = Rotation.Calculate(Player.X, Player.Y, Next.X, Next.Y);

                                    Player.X = Next.X;
                                    Player.Y = Next.Y;
                                    Player.H = Height[Next.X, Next.Y];

                                    if (bbTState[Next.X, Next.Y] != bbTileState.Sealed)
                                    {
                                        if (bbTColor[Next.X, Next.Y] == (bbTileColor)Player.teamID) // Already property of this team, increment the state
                                            bbTState[Next.X, Next.Y]++;
                                        else
                                        {
                                            bbTState[Next.X, Next.Y] = bbTileState.Touched;
                                            bbTColor[Next.X, Next.Y] = (bbTileColor)Player.teamID;
                                        }

                                        int extraPoints = 0;
                                        switch ((int)bbTState[Next.X, Next.Y]) // Check the new status and calculate extra points
                                        {
                                            case 1:
                                                extraPoints = activeTeamAmount;
                                                break;

                                            case 2:
                                                extraPoints = activeTeamAmount * 3;
                                                break;

                                            case 3:
                                                extraPoints = activeTeamAmount * 5;
                                                break;

                                            case 4:
                                                extraPoints = activeTeamAmount * 7;
                                                break;
                                        }
                                        teamScores[Player.teamID] += extraPoints;
                                        Player.Score += extraPoints;
                                        Player.bbColorTile = true;
                                    }
                                }
                                else
                                    Player.goalX = -1; // Stop moving
                            }
                            Amounts[0]++;
                        }

                    string scoreString = "H" + Encoding.encodeVL64(teamAmount);
                    for (int i = 0; i < teamAmount; i++)
                        scoreString += Encoding.encodeVL64(teamScores[i]);

                    sendData(new HabboPacketBuilder("Ct").AppendVL64(Amounts[0]).Append(Players.ToString()).AppendVL64(Amounts[1]).Append(updatedTiles.ToString()).Append(scoreString).Append("I").AppendVL64(Amounts[2]).Append(Movements.ToString()).Build());

                    if (subtrSecond)
                        leftTime--;
                    subtrSecond = (subtrSecond != true);
                    Thread.Sleep(470);
                }
                State = gameState.Ended;

                StringBuilder Scores = new StringBuilder("Cx" + Encoding.encodeVL64(Config.Game_scoreWindow_restartGame_Seconds) + Encoding.encodeVL64(teamAmount));
                for (int i = 0; i < teamAmount; i++)
                {
                    int memberAmount = Teams[i].Count;
                    if (memberAmount > 0)
                    {
                        Scores.Append(Encoding.encodeVL64(memberAmount));
                        foreach (gamePlayer Player in Teams[i])
                        {
                            Scores.Append(Encoding.encodeVL64(Player.roomUID));
                            if (Player.User != null)
                            {
                                DB.runQuery("UPDATE users SET bb_playedgames = bb_playedgames + 1,bb_totalpoints = bb_totalpoints + " + Player.Score + " WHERE id = '" + Player.User.userID + "' LIMIT 1");
                                Scores.Append(Player.User._Username + Convert.ToChar(2));
                            }
                            else
                                Scores.Append("M");
                            Scores.Append(Encoding.encodeVL64(Player.Score));
                        }
                        Scores.Append(Encoding.encodeVL64(teamScores[i]));
                    }
                    else
                        Scores.Append("M");
                }
                sendData(Scores.ToString());
            }
        }
        private Coord[] getSurfaceTiles(Coord Start)
        {
            return null;
        }
        #endregion
    }
}
