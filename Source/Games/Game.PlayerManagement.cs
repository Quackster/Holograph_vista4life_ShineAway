using System;
using System.Text;
using System.Collections.ObjectModel;

using Holo.Protocol;
using Holo.Virtual.Users;

namespace Holo.Virtual.Rooms.Games
{
    /// <summary>
    /// Partial class for Game containing player management and properties.
    /// </summary>
    internal partial class Game
    {
        #region Player management
        /// <summary>
        /// Moves a player to a certain team, or removes a player from the game.
        /// </summary>
        /// <param name="Player">The gamePlayer object of the player to move.</param>
        /// <param name="fromTeamID">The ID of the team where the player was in before. If the player is new to the game, then this parameter should be -1.</param>
        /// <param name="toTeamID">The ID of the team where the player is moving to. If the player is leaving the game, then this parameter should be -1.</param>
        internal void movePlayer(gamePlayer Player, int fromTeamID, int toTeamID)
        {
            try
            {
                if (fromTeamID != -1) // Remove player from previous team
                    Teams[fromTeamID].Remove(Player);

                if (toTeamID != -1) // User added to team
                {
                    if (Teams[toTeamID].Contains(Player) == false)
                        Teams[toTeamID].Add(Player);
                    Player.teamID = toTeamID;
                }
                else
                {
                    Teams[Player.teamID].Remove(Player);
                    Player.User.gamePlayer = null;
                }

                sendData(new HabboPacketBuilder("Ci").Append(this.Sub).Build());
            }
            catch { }
        }
        /// <summary>
        /// Indicates if the game can be launched. Games are 'launchable' when there are atleast two teams with players.
        /// </summary>
        internal bool Launchable
        {
            get
            {
                int activeTeamCount = 0;
                for (int i = 0; i < Teams.Length; i++)
                    if (Teams[i].Count != 0) // This team has got players
                        activeTeamCount++;
                return (activeTeamCount > 1); // Atleast two teams with players
            }
        }
        /// <summary>
        /// Returns a boolean that indicates if a certain team has space left for new team members. On error, false is returned.
        /// </summary>
        /// <param name="teamID">The ID of the team to check.</param>
        internal bool teamHasSpace(int teamID)
        {
            try
            {
                int m = 0;
                int l = Teams.Length;
                if (l == 2)
                    m = 6;
                else if (l == 3)
                    m = 4;
                else if (l == 4)
                    m = 3;

                return (Teams[teamID].Count < m);
            }
            catch { return false; }
        }
        /// <summary>
        /// Sends a single packet to all players, spectators and subviewers of this game.
        /// </summary>
        /// <param name="Data">The packet to send.</param>
        internal void sendData(string Data)
        {
            for (int i = 0; i < Teams.Length; i++)
                foreach (gamePlayer Member in Teams[i])
                    Member.sendData(Data);

            if (State == gameState.Waiting) // Game hasn't started yet
            {
                foreach (gamePlayer subViewer in Subviewers)
                    subViewer.sendData(Data);
            }
        }
        #endregion

        #region Properties
        internal string Sub
        {
            get
            {
                StringBuilder Entry = new StringBuilder(Encoding.encodeVL64((int)State));
                if (Launchable) // Atleast two teams with waiting players
                    Entry.Append("I"); // 1 = Game can be started
                else
                    Entry.Append("H"); // 0 = Game can't be started yet

                Entry.Append(Name + Convert.ToChar(2) + Encoding.encodeVL64(Owner.roomUID) + Owner.User._Username + Convert.ToChar(2));
                if (isBattleBall == false) // SnowStorm game
                    Entry.Append(Encoding.encodeVL64(totalTime));
                Entry.Append(Encoding.encodeVL64(mapID) + Encoding.encodeVL64(0) + Encoding.encodeVL64(Teams.Length));

                for (int i = 0; i < Teams.Length; i++)
                {
                    Entry.Append(Encoding.encodeVL64(Teams[i].Count));
                    foreach (gamePlayer Member in Teams[i])
                        Entry.Append(Encoding.encodeVL64(Member.roomUID) + Member.User._Username + Convert.ToChar(2));
                }

                if (isBattleBall) // BattleBall game
                {
                    for (int i = 0; i < enabledPowerups.Length; i++)
                        Entry.Append(enabledPowerups[i] + ",");
                    Entry.Append("9" + Convert.ToChar(2));
                }
                else // SnowStorm game
                    Entry.Append(Encoding.encodeVL64(leftTime) + Encoding.encodeVL64(mapID));

                return Entry.ToString();
            }
        }
        #endregion
    }
}
