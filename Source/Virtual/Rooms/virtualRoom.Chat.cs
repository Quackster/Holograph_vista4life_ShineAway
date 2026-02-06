using System;
using System.Text;
using System.Collections;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Users;

namespace Holo.Virtual.Rooms
{
    /// <summary>
    /// Partial class for virtualRoom containing chat-related methods.
    /// </summary>
    public partial class virtualRoom
    {
        #region Chat
        /// <summary>
        /// Filter the emotions in a chat message, and displays it at the face of the virtual user
        /// </summary>
        /// <param name="sourceUser">The virtualRoomUser object of the sender.</param>
        /// <param name="Message">The message being sent.</param>
        internal void checkEmotion(virtualRoomUser sourceUser, string Message)
        {

            // Define all gestures
            string[] GestureList = { "sml", "agr", "sad", "srp" };
            string[] Gestures_sml = { ":)", ":D" };
            string[] Gestures_agr = { ">:(", ":@", ":/" };
            string[] Gestures_sad = { ":(", ":'(" };
            string[] Gestures_srp = { ":o", ":O", ":0" };
            string[][] Gestures = { Gestures_sml, Gestures_agr, Gestures_sad, Gestures_srp };

            // Look for them
            for (int i = 0; i < Gestures.Length; i++)
            {
                string[] currentGestures = Gestures[i];
                string[] splitMessage = Message.Split(currentGestures, StringSplitOptions.None);

                if (splitMessage.Length > 1) // Result?
                {
                    string Gesture = GestureList[i];
                    sourceUser.statusManager.handleStatus("gest", Gesture, 5000);
                    return;
                }
            }
        }

        /// <summary>
        /// Sends a 'say' chat message from a virtualRoomUser to the room. Users and bots in a range of 5 squares will receive the message and bob their heads. Roombots will check the message and optionally interact to it.
        /// </summary>
        /// <param name="sourceUser">The virtualRoomUser object that sent the message.</param>
        /// <param name="Message">The message that was sent.</param>
        internal void sendSaying(virtualRoomUser sourceUser, string Message)
        {
            checkEmotion(sourceUser, Message);
            if (sourceUser.isTyping)
            {
                sendData(new HabboPacketBuilder("Ei").AppendVL64(sourceUser.roomUID).Append("H").Build());
                sourceUser.isTyping = false;
            }

            string Data = "@X" + Encoding.encodeVL64(sourceUser.roomUID) + Message + Convert.ToChar(2);
            foreach (virtualRoomUser roomUser in _Users.Values)
            {
                if (Math.Abs(roomUser.X - sourceUser.X) < 6 && Math.Abs(roomUser.Y - sourceUser.Y) < 6)
                {
                    {
                    }
                    roomUser.User.sendData(Data);
                }
            }

            foreach (virtualBot roomBot in _Bots.Values)
            {
                if (Math.Abs(roomBot.X - sourceUser.X) < 6 && Math.Abs(roomBot.Y - sourceUser.Y) < 6)
                    roomBot.Interact(sourceUser, Message);
            }
        }
        /// <summary>
        /// Sends a 'say' chat message from a virtualBot to the room. Users in a range of 5 squares will receive the message and bob their heads.
        /// </summary>
        /// <param name="sourceBot">The virtualBot object that sent the message.</param>
        /// <param name="Message">The message that was sent.</param>
        internal void sendSaying(virtualBot sourceBot, string Message)
        {
            string Data = "@X" + Encoding.encodeVL64(sourceBot.roomUID) + Message + Convert.ToChar(2);
            //sourceBot.handleStatus("talk", "", Message.Length * 190);

            foreach (virtualRoomUser roomUser in _Users.Values)
            {
                if (Math.Abs(roomUser.X - sourceBot.X) < 6 && Math.Abs(roomUser.Y - sourceBot.Y) < 6)
                {
                    //if (roomUser.goalX == -1)
                    //{
                    //    byte newHeadRotation = Pathfinding.Rotation.headRotation(roomUser.Z2, roomUser.X, roomUser.Y, sourceBot.X, sourceBot.Y);
                    //    if (newHeadRotation < 10 && newHeadRotation != roomUser.Z1) // Rotation changed
                    //    {
                    //        roomUser.Z1 = newHeadRotation;
                    //        _statusUpdates.Append(roomUser.statusString + Convert.ToChar(13));
                    //    }
                    //}
                    roomUser.User.sendData(Data);
                }
            }
        }
        /// <summary>
        /// Sends a 'shout' chat message from a virtualRoomUser to the room. All users will receive the message and bob their heads. Roombots have a 1/10 chance to react with the 'please don't shout message' set for them.
        /// </summary>
        /// <param name="sourceUser">The virtualRoomUser object that sent the message.</param>
        /// <param name="Message">The message that was sent.</param>
        internal void sendShout(virtualRoomUser sourceUser, string Message)
        {
            checkEmotion(sourceUser, Message);
            if (sourceUser.isTyping)
            {
                sendData(new HabboPacketBuilder("Ei").AppendVL64(sourceUser.roomUID).Append("H").Build());
                sourceUser.isTyping = false;
            }
            //sourceUser.statusManager.handleStatus("talk", "", Message.Length * 190);

            string Data = "@Z" + Encoding.encodeVL64(sourceUser.roomUID) + Message + Convert.ToChar(2);
            foreach (virtualRoomUser roomUser in _Users.Values)
            {
                //if (roomUser.roomUID != sourceUser.roomUID && roomUser.goalX == -1)
                //{
                //byte newHeadRotation = Pathfinding.Rotation.headRotation(roomUser.Z2, roomUser.X, roomUser.Y, sourceUser.X, sourceUser.Y);
                //if (newHeadRotation < 10 && newHeadRotation != roomUser.Z1) // Rotation changed
                //{
                //    roomUser.Z1 = newHeadRotation;
                _statusUpdates.Append(roomUser.statusString + Convert.ToChar(13));
                //}
                //}
                roomUser.User.sendData(Data);
            }

            foreach (virtualBot roomBot in _Bots.Values)
            {
                if (Math.Abs(roomBot.X - sourceUser.X) < 6 && Math.Abs(roomBot.Y - sourceUser.Y) < 6)
                {
                    if (new Random(DateTime.Now.Millisecond).Next(0, 11) == 0)
                        sendSaying(roomBot, roomBot.noShoutingMessage);
                }
            }
        }
        /// <summary>
        /// Sends a 'shout' chat message from a virtualBot to the room. All users will receive the message and bob their heads.
        /// </summary>
        /// <param name="sourceBot">The virtualRoomBot object that sent the message.</param>
        /// <param name="Message">The message that was sent.</param>
        internal void sendShout(virtualBot sourceBot, string Message)
        {
            //sourceBot.handleStatus("talk", "", Message.Length * 190);
            string Data = "@Z" + Encoding.encodeVL64(sourceBot.roomUID) + Message + Convert.ToChar(2);

            foreach (virtualRoomUser roomUser in ((Hashtable)_Users.Clone()).Values)
            {
                //if (roomUser.roomUID != sourceBot.roomUID && roomUser.goalX == -1)
                //{
                //    byte newHeadRotation = Pathfinding.Rotation.headRotation(roomUser.Z2, roomUser.X, roomUser.Y, sourceBot.X, sourceBot.Y);
                //    if (newHeadRotation < 10 && newHeadRotation != roomUser.Z1) // Rotation changed
                //    {
                //        roomUser.Z1 = newHeadRotation;
                //        _statusUpdates.Append(roomUser.statusString + Convert.ToChar(13));
                //    }
                //}
                roomUser.User.sendData(Data);
            }
        }
        /// <summary>
        /// Sends a 'whisper' chat message, which is only visible for sender and receiver, from a certain user to a certain user in the virtual room.
        /// </summary>
        /// <param name="sourceUser">The virtualRoomUser object of the sender.</param>
        /// <param name="Receiver">The username of the receiver.</param>
        /// <param name="Message">The message being sent.</param>
        internal void sendWhisper(virtualRoomUser sourceUser, string Receiver, string Message)
        {
            if (sourceUser.isTyping)
            {
                sendData(new HabboPacketBuilder("Ei").AppendVL64(sourceUser.roomUID).Append("H").Build());
                sourceUser.isTyping = false;
            }

            string Data = "@Y" + Encoding.encodeVL64(sourceUser.roomUID) + Message + Convert.ToChar(2);
            foreach (virtualRoomUser roomUser in _Users.Values)
            {
                if (roomUser.User._Username == Receiver)
                {
                    sourceUser.User.sendData(Data);
                    roomUser.User.sendData(Data);
                    return;
                }
            }
        }
        #endregion
    }
}
