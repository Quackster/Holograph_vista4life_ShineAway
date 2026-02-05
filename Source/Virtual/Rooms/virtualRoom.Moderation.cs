using System;
using System.Collections;

using Holo.Managers;
using Holo.Virtual.Users;
using Holo.Virtual.Rooms.Bots;

namespace Holo.Virtual.Rooms
{
    /// <summary>
    /// Partial class for virtualRoom containing moderation and bot loading methods.
    /// </summary>
    public partial class virtualRoom
    {
        #region Moderacy tasks
        /// <summary>
        /// Casts a 'roomkick' on the user manager, kicking all users from the room [with message] who have a lower rank than the caster of the roomkick.
        /// </summary>
        /// <param name="casterRank">The rank of the caster of the 'room kick'.</param>
        /// <param name="Message">The message that goes with the 'roomkick'.</param>
        internal void kickUsers(byte casterRank, string Message)
        {
            foreach (virtualRoomUser roomUser in ((Hashtable)_Users.Clone()).Values)
            {
                if (roomUser.User._Rank < casterRank)
                    removeUser(roomUser.roomUID, true, Message);
            }
        }
        /// <summary>
        /// Casts a 'room mute' on the user manager, muting all users in the room who aren't muted yet and have a lower rank than the caster of the room mute. The affected users receive a message with the reason of their muting, and they won't be able to chat anymore until another user unmutes them.
        /// </summary>
        /// <param name="casterRank">The rank of the caster of the 'room mute'.</param>
        /// <param name="Message">The message that goes with the 'room mute'.</param>
        internal void muteUsers(byte casterRank, string Message)
        {
            Message = "BK" + stringManager.getString("scommand_muted") + "\r" + Message;
            foreach (virtualRoomUser roomUser in ((Hashtable)_Users.Clone()).Values)
            {
                if (roomUser.User._isMuted == false && roomUser.User._Rank < casterRank)
                {
                    roomUser.User._isMuted = true;
                    roomUser.User.sendData(Message);
                }
            }
        }
        /// <summary>
        /// Casts a 'room unmute' on the user manager, unmuting all users in the room who are muted and have a lower rank than the caster of the room mute. The affected users are notified that they can chat again.
        /// </summary>
        /// <param name="casterRank">The rank of the caster of the 'room unmute'.</param>
        internal void unmuteUsers(byte casterRank)
        {
            string Message = "BK" + stringManager.getString("scommand_unmuted");
            foreach (virtualRoomUser roomUser in ((Hashtable)_Users.Clone()).Values)
            {
                if (roomUser.User._isMuted && roomUser.User._Rank < casterRank)
                {
                    roomUser.User._isMuted = false;
                    roomUser.User.sendData(Message);
                }
            }
        }
        #endregion

        #region Bot loading and deloading
        internal void loadBots()
        {
            int[] IDs = DB.runReadColumn("SELECT id FROM roombots WHERE roomid = '" + this.roomID + "'", 0, null);
            for (int i = 0; i < IDs.Length; i++)
            {
                virtualBot roomBot = new virtualBot(IDs[i], getFreeRoomIdentifier(), this);
                roomBot.H = sqFLOORHEIGHT[roomBot.X, roomBot.Y];
                sqUNIT[roomBot.X, roomBot.Y] = true;

                _Bots.Add(roomBot.roomUID, roomBot);
                sendData(@"@\" + roomBot.detailsString);
                sendData("@b" + roomBot.statusString);
            }
        }
        #endregion
    }
}
