using System;
using System.Text;
using System.Threading;
using System.Collections.ObjectModel;

using Holo.Virtual.Users;
using Holo.Virtual.Rooms.Pathfinding;
namespace Holo.Virtual.Rooms.Games
{
    /// <summary>
    /// Represents a game instance for BattleBall or SnowStorm games.
    /// </summary>
    internal partial class Game
    {
        #region Declares
        /// <summary>
        /// The gameLobby object where this game is hosted in.
        /// </summary>
        internal gameLobby Lobby;
        /// <summary>
        /// Indicates if this game is of the type 'BattleBall'. If false, then the game is of the type 'SnowStorm'.
        /// </summary>
        internal bool isBattleBall;
        /// <summary>
        /// The ID of this game.
        /// </summary>
        internal int ID;
        /// <summary>
        /// The display name of this game.
        /// </summary>
        internal string Name;
        /// <summary>
        /// The ID of the map that is played with this game.
        /// </summary>
        internal int mapID;
        /// <summary>
        /// The total amount of gametime in seconds.
        /// </summary>
        internal int totalTime;
        /// <summary>
        /// The amount of gametime is seconds that is still left on this game.
        /// </summary>
        internal int leftTime;
        internal gameState State;
        /// <summary>
        /// BattleBall only. The numbers of the enabled powerups for this game.
        /// </summary>
        internal int[] enabledPowerups;
        /// <summary>
        /// The gamePlayer object (bbPlayer or ssPlayer) of the virtual user that has created this game.
        /// </summary>
        internal gamePlayer Owner;
        /// <summary>
        /// Collection with the gamePlayer objects of the virtual users that have 'checked in' to this game sub and should receive updates about team status etc.
        /// </summary>
        internal Collection<gamePlayer> Subviewers = new Collection<gamePlayer>();
        internal Collection<gamePlayer>[] Teams;
        #region Map declares
        #region Enumerators etc
        internal enum gameState { Waiting = 0, Started = 1, Ended = 2 };
        /// <summary>
        /// Represents the state of a tile on the gamemap in a game of 'BattleBall'.
        /// </summary>
        private enum bbTileState { Default = 0, Touched = 1, Clicked = 2, Pressed = 3, Sealed = 4 }
        /// <summary>
        /// Represents the color of the team that owns a tile on the gamemap in a game of 'BattleBall'.
        /// </summary>
        private enum bbTileColor { Disabled = -2, Default = -1, Red = 0, Blue = 1, Yellow = 2, Green = 3 }
        #endregion
        /// <summary>
        /// The amount of seconds that the 'loading game' bar still has to be displayed.
        /// </summary>
        private int leftCountdownSeconds = Config.Game_Countdown_Seconds;
        /// <summary>
        /// The length of the arena.
        private int bX;
        /// <summary>
        /// The max width of the arena.
        /// </summary>
        private int bY;
        /// <summary>
        /// The heightmap of the arena.
        /// </summary>
        internal string Heightmap;
        private byte[,] Height;
        private bool[,] Blocked;
        private bbTileState[,] bbTState;
        private bbTileColor[,] bbTColor;
        private Thread updateHandler;
        #endregion
        #endregion

        #region Constructors/destructor
        /// <summary>
        /// Initializes a 'BattleBall' game.
        /// </summary>
        /// <param name="Lobby">The gameLobby object of the lobby where this game is hosted in.</param>
        /// <param name="ID">The ID of this game.</param>
        /// <param name="Name">The display name of this game.</param>
        /// <param name="mapID">The ID of the map that is played with this game.</param>
        /// <param name="teamAmount">The amount of teams that is used with the game.</param>
        /// <param name="enabledPowerups">The numbers of the enabled powerups for this game.</param>
        /// <param name="Owner">The gamePlayer instance of the virtual user that has created this game.</param>
        internal Game(gameLobby Lobby, int ID, string Name, int mapID, int teamAmount, int[] enabledPowerups, gamePlayer Owner)
        {
            this.Lobby = Lobby;
            this.ID = ID;
            this.Name = Name;
            this.mapID = mapID;
            this.Owner = Owner;
            this.enabledPowerups = enabledPowerups;
            this.totalTime = Config.Game_BattleBall_gameLength_Seconds;
            this.leftTime = Config.Game_BattleBall_gameLength_Seconds;
            this.isBattleBall = true;

            // Dimension teams
            this.Teams = new Collection<gamePlayer>[teamAmount];
            for (int i = 0; i < teamAmount; i++)
                Teams[i] = new Collection<gamePlayer>();
        }
        /// <summary>
        /// Initializes a 'SnowStorm' game.
        /// </summary>
        /// <param name="Lobby">The gameLobby object of the lobby where this game is hosted in.</param>
        /// <param name="ID">The ID of this game.</param>
        /// <param name="Name">The display name of this game.</param>
        /// <param name="mapID">The ID of the map that is played with this game.</param>
        /// <param name="teamAmount">The amount of teams that is used with the game.</param>
        /// <param name="totalTime">The total amount of gametime in seconds.</param>
        /// <param name="Owner">The gamePlayer instance of the virtual user that has created this game.</param>
        internal Game(gameLobby Lobby, int ID, string Name, int mapID, int teamAmount, int totalTime, gamePlayer Owner)
        {
            this.Lobby = Lobby;
            this.ID = ID;
            this.Name = Name;
            this.mapID = mapID;
            this.Owner = Owner;
            this.totalTime = totalTime;
            this.leftTime = totalTime;
            this.isBattleBall = false;

            // Dimension teams
            this.Teams = new Collection<gamePlayer>[teamAmount];
            for (int i = 0; i < teamAmount; i++)
                Teams[i] = new Collection<gamePlayer>();
        }
        /// <summary>
        /// Stops the game, and removes all players and subviewers from the game.
        /// </summary>
        internal void Abort()
        {
            try { updateHandler.Abort(); }
            catch { }

            string Data = "Cm" + "H";
            for (int i = 0; i < Teams.Length; i++)
                foreach (gamePlayer Member in Teams[i])
                {
                    if (Member.User != null)
                    {
                        if (State == gameState.Waiting)
                            Member.User.sendData(Data);
                        Member.User.gamePlayer = null;
                    }
                }

            foreach (gamePlayer subViewer in Subviewers)
            {
                if (subViewer.User != null)
                {
                    if (State == gameState.Waiting)
                        subViewer.User.sendData(Data);
                    subViewer.User.gamePlayer = null;
                }
            }
        }
        #endregion
    }
}
