using System;
using System.Text;
using System.Collections;

using Holo.Managers;
using Holo.Virtual.Users;

namespace Holo.Virtual.Rooms
{
    /// <summary>
    /// Partial class for virtualRoom containing data distribution and property provider methods.
    /// </summary>
    public partial class virtualRoom
    {
        #region Properties and providers
        /// <summary>
        /// Returns a room identifier ID that isn't used by a virtual unit in this virtual room yet.
        /// </summary>
        /// <returns></returns>
        private int getFreeRoomIdentifier()
        {
            int i = 0;
            while (true)
            {
                if (_Bots.ContainsKey(i) == false && _Users.ContainsKey(i) == false)
                    return i;
                i++;
                Out.WriteTrace("Get free room identifier");
            }
        }
        /// <summary>
        /// Returns a room identifier of a virtual unit in this room, by picking a unit at random. If there are no units in the room, then -1 is returned.
        /// </summary>
        /// <returns></returns>
        private int getRandomRoomIdentifier()
        {
            if (_Users.Count > 0)
            {
                while (true)
                {
                    int rndID = new Random(DateTime.Now.Millisecond).Next(0, _Users.Count);
                    if (_Bots.ContainsKey(rndID) || _Users.ContainsKey(rndID))
                        return rndID;
                    Out.WriteTrace("Get random room identifier");
                }
            }
            else
                return -1;
        }
        /// <summary>
        /// Returns the virtualUser object of a room user.
        /// </summary>
        /// <param name="roomUID">The room identifier of the user.</param>
        internal virtualUser getUser(int roomUID)
        {
            return ((virtualRoomUser)_Users[roomUID]).User;
        }
        /// <summary>
        /// Returns the virtualRoomUser object of a room user.
        /// </summary>
        /// <param name="roomUID">The room identifier of the user.</param>
        internal virtualRoomUser getRoomUser(int roomUID)
        {
            return ((virtualRoomUser)_Users[roomUID]);
        }

        /// <summary>
        /// The details string for all the virtual units in this room.
        /// </summary>
        internal string dynamicUnits
        {
            get
            {
                StringBuilder userList = new StringBuilder();
                foreach (virtualBot roomBot in _Bots.Values)
                    userList.Append(roomBot.detailsString);
                foreach (virtualRoomUser roomUser in _Users.Values)
                    userList.Append(roomUser.detailsString);

                return userList.ToString();
            }
        }
        /// <summary>
        /// The status string of all the virtual units in this room.
        /// </summary>
        internal string dynamicStatuses
        {
            get
            {
                StringBuilder Statuses = new StringBuilder();
                foreach (virtualBot roomBot in _Bots.Values)
                    Statuses.Append(roomBot.statusString + Convert.ToChar(13));
                foreach (virtualRoomUser roomUser in _Users.Values)
                    Statuses.Append(roomUser.statusString + Convert.ToChar(13));

                return Statuses.ToString();
            }
        }
        /// <summary>
        /// The usernames of all the virtual users in this room.
        /// </summary>
        internal string Userlist
        {
            get
            {
                StringBuilder listBuilder = new StringBuilder(Encoding.encodeVL64(this.roomID) + Encoding.encodeVL64(_Users.Count));
                foreach (virtualRoomUser roomUser in _Users.Values)
                    listBuilder.Append(roomUser.User._Username + Convert.ToChar(2));

                return listBuilder.ToString();
            }
        }
        /// <summary>
        /// The IDs and badge strings of all the active user groups in this room.
        /// </summary>
        internal string Groups
        {
            get
            {
                StringBuilder listBuilder = new StringBuilder(Encoding.encodeVL64(_activeGroups.Count));
                foreach (int groupID in _activeGroups)
                    listBuilder.Append(Encoding.encodeVL64(groupID) + DB.runRead("SELECT badge FROM groups_details WHERE id = '" + groupID + "'") + Convert.ToChar(2));

                return listBuilder.ToString();
            }
        }
        #endregion

        #region User data distribution
        /// <summary>
        /// Sends a single packet to all users inside the user manager.
        /// </summary>
        /// <param name="Data">The packet to send.</param>
        internal void sendData(string Data)
        {
            try
            {
                foreach (virtualRoomUser roomUser in _Users.Values)
                    roomUser.User.sendData(Data);
            }
            catch { }
        }
        /// <summary>
        /// Sends a single packet to all users inside the user manager.
        /// </summary>
        /// <param name="Data">The packet to send.</param>
        internal void sendDataToRights(string Data)
        {
            try
            {
                foreach (virtualRoomUser roomUser in _Users.Values)
                    if (roomUser.User._hasRights || rankManager.containsRight(roomUser.User._Rank, "fuse_pick_up_any_furni"))
                        roomUser.User.sendData(Data);

            }
            catch { }
        }
        /// <summary>
        /// Sends a single packet to all users inside the user manager, after sleeping (on different thread) for a specified amount of milliseconds.
        /// </summary>
        /// <param name="Data">The packet to send.</param>
        /// <param name="msSleep">The amount of milliseconds to sleep before sending.</param>
        internal void sendData(string Data, int msSleep)
        {
            new sendDataSleep(SENDDATASLEEP).BeginInvoke(Data, msSleep, null, null);
        }
        private delegate void sendDataSleep(string Data, int msSleep);
        private void SENDDATASLEEP(string Data, int msSleep)
        {
            Thread.Sleep(msSleep);
            foreach (virtualRoomUser roomUser in _Users.Values)
                roomUser.User.sendData(Data);
        }
        /// <summary>
        /// Sends a single packet to a user in the usermanager.
        /// </summary>
        /// <param name="userID">The ID of the user.</param>
        /// <param name="Data">The packet to send.</param>
        internal void sendData(int userID, string Data)
        {
            foreach (virtualRoomUser roomUser in _Users.Values)
            {
                if (roomUser.userID == userID)
                {
                    roomUser.User.sendData(Data);
                    return;
                }
            }
        }
        /// <summary>
        /// Sends a single packet to a user in the usermanager.
        /// </summary>
        /// <param name="Username">The username of the user.</param>
        /// <param name="Data">The packet to send.</param>
        internal void sendData(string Username, string Data)
        {
            foreach (virtualRoomUser roomUser in _Users.Values)
            {
                if (roomUser.User._Username == Username)
                {
                    roomUser.User.sendData(Data);
                    return;
                }
            }
        }
        /// <summary>
        /// Sends a special cast to all users in the usermanager.
        /// </summary>
        /// <param name="Emitter">The objects that emits the cast.</param>
        /// <param name="Cast">The cast to emit.</param>
        internal void sendSpecialCast(string Emitter, string Cast)
        {
            sendData("AG" + Emitter + " " + Cast);
        }
        /// <summary>
        /// Updates the room votes amount for all users that have voted yet. User's that haven't voted yet are skipped so their vote buttons stay visible.
        /// </summary>
        /// <param name="voteAmount">The new amount of votes.</param>
        internal void sendNewVoteAmount(int voteAmount)
        {
            string Data = "EY" + Encoding.encodeVL64(voteAmount);
            foreach (virtualRoomUser roomUser in _Users.Values)
            {
                if (roomUser.hasVoted)
                    roomUser.User.sendData(Data);
            }
        }
        #endregion
    }
}
