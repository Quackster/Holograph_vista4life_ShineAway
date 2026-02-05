using System;
using System.Text;
using System.Threading;
using System.Collections;

using Holo.Managers;
using Holo.Virtual.Users;
using Holo.Virtual.Rooms.Bots;

namespace Holo.Virtual.Rooms
{
    /// <summary>
    /// Partial class for virtualRoom containing status management and walking handler methods.
    /// </summary>
    public partial class virtualRoom
    {
        #region User and bot status management
        /// <summary>
        /// Ran on a thread and handles walking and pathfinding. All status updates are sent to all room users.
        /// </summary>
        private void cycleStatuses()
        {
            try
            {
                while (true)
                {
                    foreach (virtualRoomUser roomUser in ((Hashtable)_Users.Clone()).Values)
                    #region Virtual user status handling
                    {
                        if (roomUser.goalX == -1) // No destination set, user is not walking/doesn't start to walk, advance to next user
                            continue;

                        // If the goal is a seat, then allow to 'walk' on the seat, so seat the user
                        squareState[,] stateMap = (squareState[,])sqSTATE.Clone();
                        try
                        {
                            if (stateMap[roomUser.goalX, roomUser.goalY] == squareState.Seat || stateMap[roomUser.goalX, roomUser.goalY] == squareState.Bed)
                                stateMap[roomUser.goalX, roomUser.goalY] = squareState.Open;
                            if (sqUNIT[roomUser.goalX, roomUser.goalY])
                                stateMap[roomUser.goalX, roomUser.goalY] = squareState.Blocked;
                        }
                        catch { }
                        // Use AStar pathfinding to get the next step to the goal
                        int[] nextCoords = new Pathfinding.Pathfinder(stateMap, sqFLOORHEIGHT, sqUNIT).getNext(roomUser.X, roomUser.Y, roomUser.goalX, roomUser.goalY);

                        roomUser.statusManager.removeStatus("mv");
                        if (nextCoords == null) // No next steps found, destination reached/stuck
                        {
                            roomUser.goalX = -1; // Next time the thread cycles this user, it won't attempt to walk since destination has been reached
                            if (sqTRIGGER[roomUser.X, roomUser.Y] != null)
                            {
                                squareTrigger Trigger = sqTRIGGER[roomUser.X, roomUser.Y];
                                if (this.hasSwimmingPool) // This virtual room has a swimming pool
                                #region Swimming pool triggers
                                {
                                    if (Trigger.Object == "curtains1" || Trigger.Object == "curtains2") // User has entered a swimming pool clothing booth
                                    {
                                        roomUser.walkLock = true;
                                        roomUser.User.sendData("A`");
                                        sendSpecialCast(Trigger.Object, "close");
                                    }
                                    else if (roomUser.SwimOutfit != "") // User wears a swim outfit and hasn't entered a swimming pool clothing booth
                                    {
                                        if (Trigger.Object == "door" && roomUser.User._Tickets != 33333) // User has entered the diving board elevator
                                        {
                                            roomUser.walkLock = true;
                                            roomUser.goalX = -1;
                                            roomUser.goalY = 0;
                                            moveUser(roomUser, Trigger.stepX, Trigger.stepY, true);
                                            sendSpecialCast("door", "close");
                                            sendData("A}");
                                            roomUser.User._Tickets--;
                                            roomUser.User.sendData("A|" + roomUser.User._Tickets);
                                            DB.runQuery("UPDATE users SET tickets = tickets - 1 WHERE id = '" + roomUser.userID + "' LIMIT 1");
                                        }
                                        else if (Trigger.Object.Substring(0, 6) == "Splash") // User has entered/left a swimming pool
                                        {
                                            sendData("AG" + Trigger.Object);
                                            if (Trigger.Object.Substring(8) == "enter")
                                            {
                                                roomUser.statusManager.dropCarrydItem();
                                                roomUser.statusManager.addStatus("swim", "");
                                            }
                                            else
                                                roomUser.statusManager.removeStatus("swim");
                                            moveUser(roomUser, Trigger.stepX, Trigger.stepY, false);

                                            roomUser.goalX = Trigger.goalX;
                                            roomUser.goalY = Trigger.goalY;
                                        }
                                    }
                                }
                                #endregion
                                else
                                {
                                    // Different trigger species here
                                }
                            }
                            else if (roomUser.walkDoor) // User has clicked the door to leave the room (and got stuck while walking or reached destination)
                            {
                                if (_Users.Count > 1)
                                {
                                    removeUser(roomUser.roomUID, true, "");
                                    continue;
                                }
                                else
                                {
                                    removeUser(roomUser.roomUID, true, "");
                                    return;
                                }
                            }
                            _statusUpdates.Append(roomUser.statusString + Convert.ToChar(13));
                        }
                        else // Next steps found by pathfinder
                        {
                            int nextX = nextCoords[0];
                            int nextY = nextCoords[1];
                            squareState nextState = sqSTATE[nextX, nextY];

                            sqUNIT[roomUser.X, roomUser.Y] = false; // Free last position, allow other users to use that spot again
                            sqUNIT[nextX, nextY] = true; // Block the spot of the next steps
                            roomUser.Z1 = Pathfinding.Rotation.Calculate(roomUser.X, roomUser.Y, nextX, nextY); // Calculate the users new rotation
                            roomUser.Z2 = roomUser.Z1;
                            roomUser.statusManager.removeStatus("sit");
                            roomUser.statusManager.removeStatus("lay");

                            double nextHeight = 0;
                            if (nextState == squareState.Rug) // If next step is on a rug, then set user's height to that of the rug [floating stacked rugs in mid-air, petals etc]
                                nextHeight = sqITEMHEIGHT[nextX, nextY];
                            else
                                nextHeight = (double)sqFLOORHEIGHT[nextX, nextY];

                            // Add the walk status to users status manager + add users whole status string to stringbuilder
                            roomUser.statusManager.addStatus("mv", nextX + "," + nextY + "," + nextHeight.ToString().Replace(',', '.'));
                            _statusUpdates.Append(roomUser.statusString + Convert.ToChar(13));

                            // Set new coords for virtual room user
                            roomUser.X = nextX;
                            roomUser.Y = nextY;
                            roomUser.H = nextHeight;
                            if (nextState == squareState.Seat) // The next steps are on a seat, seat the user, prepare the sit status for next cycle of thread
                            {
                                roomUser.statusManager.removeStatus("dance"); // Remove dance status
                                roomUser.Z1 = sqITEMROT[nextX, nextY]; //
                                roomUser.Z2 = roomUser.Z1;
                                roomUser.statusManager.addStatus("sit", sqITEMHEIGHT[nextX, nextY].ToString().Replace(',', '.'));
                            }
                            else if (nextState == squareState.Bed)
                            {
                                roomUser.statusManager.removeStatus("dance"); // Remove dance status
                                roomUser.Z1 = sqITEMROT[nextX, nextY]; //
                                roomUser.Z2 = roomUser.Z1;
                                roomUser.statusManager.addStatus("lay", sqITEMHEIGHT[nextX, nextY].ToString().Replace(',', '.'));
                            }
                        }
                    }
                    #endregion

                    #region Roombot walking
                    foreach (virtualBot roomBot in _Bots.Values)
                    {
                        if (roomBot.goalX == -1)
                            continue;

                        // If the goal is a seat, then allow to 'walk' on the seat, so seat the user
                        squareState[,] stateMap = (squareState[,])sqSTATE.Clone();
                        try
                        {
                            if (stateMap[roomBot.goalX, roomBot.goalY] == squareState.Seat)
                                stateMap[roomBot.goalX, roomBot.goalY] = squareState.Open;
                            if (sqUNIT[roomBot.goalX, roomBot.goalY])
                                stateMap[roomBot.goalX, roomBot.goalY] = squareState.Blocked;
                        }
                        catch { }

                        int[] nextCoords = new Pathfinding.Pathfinder(stateMap, sqFLOORHEIGHT, sqUNIT).getNext(roomBot.X, roomBot.Y, roomBot.goalX, roomBot.goalY);

                        roomBot.removeStatus("mv");
                        if (nextCoords == null) // No next steps found, destination reached/stuck
                        {
                            if (roomBot.X == roomBot.goalX && roomBot.Y == roomBot.goalY)
                            {
                                roomBot.checkOrders();
                            }
                            roomBot.goalX = -1;
                            _statusUpdates.Append(roomBot.statusString + Convert.ToChar(13));
                        }
                        else
                        {
                            int nextX = nextCoords[0];
                            int nextY = nextCoords[1];

                            sqUNIT[roomBot.X, roomBot.Y] = false; // Free last position, allow other users to use that spot again
                            sqUNIT[nextX, nextY] = true; // Block the spot of the next steps
                            roomBot.Z1 = Pathfinding.Rotation.Calculate(roomBot.X, roomBot.Y, nextX, nextY); // Calculate the bot's new rotation
                            roomBot.Z2 = roomBot.Z1;
                            roomBot.removeStatus("sit");

                            double nextHeight = (double)sqFLOORHEIGHT[nextX, nextY];
                            if (sqSTATE[nextX, nextY] == squareState.Rug) // If next step is on a rug, then set bot's height to that of the rug [floating stacked rugs in mid-air, petals etc]
                                nextHeight = sqITEMHEIGHT[nextX, nextY];
                            else
                                nextHeight = (double)sqFLOORHEIGHT[nextX, nextY];

                            roomBot.addStatus("mv", nextX + "," + nextY + "," + nextHeight.ToString().Replace(',', '.'));
                            _statusUpdates.Append(roomBot.statusString + Convert.ToChar(13));

                            // Set new coords for the bot
                            roomBot.X = nextX;
                            roomBot.Y = nextY;
                            roomBot.H = nextHeight;
                            if (sqSTATE[nextX, nextY] == squareState.Seat) // The next steps are on a seat, seat the bot, prepare the sit status for next cycle of thread
                            {
                                roomBot.removeStatus("dance"); // Remove dance status
                                roomBot.Z1 = sqITEMROT[nextX, nextY]; //
                                roomBot.Z2 = roomBot.Z1;
                                roomBot.addStatus("sit", sqITEMHEIGHT[nextX, nextY].ToString().Replace(',', '.'));
                            }
                        }
                    }
                    #endregion

                    // Send statuses to all room users [if in stringbuilder]
                    if (_statusUpdates.Length > 0)
                    {
                        sendData("@b" + _statusUpdates.ToString());
                        _statusUpdates = new StringBuilder();
                    }
                    Thread.Sleep(410);
                    Out.WriteTrace("Status update loop");
                } // Repeat (infinite loop on thread)
            }
            catch (Exception e) { Out.WriteError(e.Message); } // thread aborted
        }

        /// <summary>
        /// Updates the status of a virtualRoomUser object in room. If the user is walking, then the user isn't refreshed immediately but processed at the next cycle of the status thread, to prevent double status strings in @b.
        /// </summary>
        /// <param name="roomUser">The virtualRoomUser object to update.</param>
        internal void Refresh(virtualRoomUser roomUser)
        {
            if (roomUser.goalX == -1)
                _statusUpdates.Append(roomUser.statusString + Convert.ToChar(13));
        }
        /// <summary>
        /// Updates the status of a virtualBot object in room. If the bot is walking, then the bot isn't refreshed immediately but processed at the next cycle of the status thread, to prevent double status strings in @b.
        /// </summary>
        /// <param name="roomBot">The virtualBot object to update.</param>
        internal void Refresh(virtualBot roomBot)
        {
            try
            {
                if (roomBot.goalX == -1)
                    _statusUpdates.Append(roomBot.statusString + Convert.ToChar(13));
            }
            catch { }
        }
        internal void refreshCoord(int X, int Y)
        {
            foreach (virtualRoomUser roomUser in _Users.Values)
            {
                if (roomUser.X == X && roomUser.Y == Y)
                {
                    if (sqSTATE[X, Y] == squareState.Seat) // Seat here
                    {
                        if (roomUser.statusManager.containsStatus("sit") == false) // User is not sitting yet
                        {
                            roomUser.Z1 = sqITEMROT[X, Y];
                            roomUser.Z2 = roomUser.Z1;
                            roomUser.statusManager.addStatus("sit", sqITEMHEIGHT[X, Y].ToString().Replace(',', '.'));
                            Refresh(roomUser);
                        }
                    }
                    else if (sqSTATE[X, Y] == squareState.Bed) // Bed here
                    {
                        if (roomUser.statusManager.containsStatus("bed") == false) // User is not laying yet
                        {
                            roomUser.Z1 = sqITEMROT[X, Y];
                            roomUser.Z2 = roomUser.Z1;
                            roomUser.statusManager.addStatus("lay", sqITEMHEIGHT[X, Y].ToString().Replace(',', '.'));
                            Refresh(roomUser);
                        }
                    }
                    else // No seat/bed here
                    {
                        roomUser.statusManager.removeStatus("sit");
                        roomUser.statusManager.removeStatus("lay");
                        roomUser.H = sqFLOORHEIGHT[X, Y];
                        Refresh(roomUser);
                    }
                    return; // One user per coord
                }
            }
        }

        #region Single step walking
        /// <summary>
        /// Moves a virtual room user one step to a certain coord [the coord has to be one step removed from the room user's current coords], with pauses and handling for seats and rugs.
        /// </summary>
        /// <param name="roomUser">The virtual room user to move.</param>
        /// <param name="toX">The X of the destination coord.</param>
        /// <param name="toY">The Y of the destination coord.</param>
        internal void moveUser(virtualRoomUser roomUser, int toX, int toY, bool secondRefresh)
        {
            new userMover(MOVEUSER).BeginInvoke(roomUser, toX, toY, secondRefresh, null, null);
        }
        private delegate void userMover(virtualRoomUser roomUser, int toX, int toY, bool secondRefresh);
        private void MOVEUSER(virtualRoomUser roomUser, int toX, int toY, bool secondRefresh)
        {
            try
            {
                sqUNIT[roomUser.X, roomUser.Y] = false;
                sqUNIT[toX, toY] = true;
                roomUser.Z1 = Pathfinding.Rotation.Calculate(roomUser.X, roomUser.Y, toX, toY);
                roomUser.Z2 = roomUser.Z1;
                roomUser.statusManager.removeStatus("sit");
                double nextHeight = 0;
                if (sqSTATE[toX, toY] == squareState.Rug)
                    nextHeight = sqITEMHEIGHT[toX, toY];
                else
                    nextHeight = (double)sqFLOORHEIGHT[toX, toY];
                roomUser.statusManager.addStatus("mv", toX + "," + toY + "," + nextHeight.ToString().Replace(',','.'));
                sendData("@b" + roomUser.statusString);

                Thread.Sleep(310);
                roomUser.X = toX;
                roomUser.Y = toY;
                roomUser.H = nextHeight;

                roomUser.statusManager.removeStatus("mv");
                if (secondRefresh)
                {
                    if (sqSTATE[toX, toY] == squareState.Seat) // The next steps are on a seat, seat the user, prepare the sit status for next cycle of thread
                    {
                        roomUser.statusManager.removeStatus("dance"); // Remove dance status
                        roomUser.Z1 = sqITEMROT[toX, toY];
                        roomUser.Z2 = roomUser.Z1;
                        roomUser.statusManager.addStatus("sit", sqITEMHEIGHT[toX, toY].ToString().Replace(',', '.'));
                        roomUser.statusManager.removeStatus("mv");
                    }
                    sendData("@b" + roomUser.statusString);
                }
            }
            catch { }
        }
        #endregion
        #endregion
    }
}
