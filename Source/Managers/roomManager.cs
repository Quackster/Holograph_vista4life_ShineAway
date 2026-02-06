using System;
using System.Text;
using System.Collections;

using Holo.Protocol;
using Holo.Virtual.Rooms;

namespace Holo.Managers
{
    /// <summary>
    /// Provides management for virtual rooms, aswell as some misc tasks for rooms.
    /// </summary>
    public static class roomManager
    {
        #region Declares
        /// <summary>
        /// Contains the hooked virtual room objects.
        /// </summary>
        private static Hashtable _Rooms = new Hashtable();
        /// <summary>
        /// The peak amount of rooms that has been in the room manager since start of the emulator.
        /// </summary>
        private static int _peakRoomCount;
        #endregion

        #region Virtual room management
        /// <summary>
        /// Adds a virtualRoom class together with the roomID to the roomManager.
        /// </summary>
        /// <param name="roomID">The ID of the room to add..</param>
        /// <param name="Room">The virtualRoom class of this room.</param>
        public static void addRoom(int roomID, virtualRoom Room)
        {
            if (_Rooms.ContainsKey(roomID) == false)
            {
                _Rooms.Add(roomID, Room);
                Out.WriteLine("Room [" + roomID + ", publicroom: " + Room.isPublicroom.ToString().ToLower() + "] loaded.", Out.logFlags.StandardAction);
                if (_Rooms.Count > _peakRoomCount)
                    _peakRoomCount = _Rooms.Count;
            }

        }
        /// <summary>
        /// Removes a room from the roomManager. [if it exists]
        /// </summary>
        /// <param name="roomID">The ID of the room to remove.</param>
        public static void removeRoom(int roomID)
        {
            if (_Rooms.ContainsKey(roomID))
            {
                bool boolPublicroom = ((virtualRoom)_Rooms[roomID]).isPublicroom;
                _Rooms.Remove(roomID);
                updateRoomVisitorCount(roomID, 0);
                Out.WriteLine("Room [" + roomID + ", publicroom: " + boolPublicroom.ToString().ToLower() + "] destroyed.", Out.logFlags.StandardAction);
                GC.Collect();
            }
        }
        /// <summary>
        /// Returns a bool that indicates if the roomManager contains a certain room.
        /// </summary>
        /// <param name="roomID">The ID of the room.</param>
        public static bool containsRoom(int roomID)
        {
            return _Rooms.ContainsKey(roomID);
        }

        /// <summary>
        /// Returns the current amount of rooms in the roomManager.
        /// </summary>
        public static int roomCount
        {
            get
            {
                return _Rooms.Count;
            }
        }
        /// <summary>
        /// Returns the peak amount of rooms in the roomManager since boot.
        /// </summary>
        public static int peakRoomCount
        {
            get
            {
                return _peakRoomCount;
            }
        }

        /// <summary>
        /// Returns a virtualRoom class for a certain room.
        /// </summary>
        /// <param name="roomID">The ID of the room.</param>
        public static virtualRoom getRoom(int roomID)
        {
            return (virtualRoom)_Rooms[roomID];
        }
        /// <summary>
        /// Returns a the poll packet for a certain room.
        /// </summary>
        /// <param name="roomID">The ID of the room.</param>
        public static string getPoll(int roomID)
        {
            string[] poll_poll = DB.runReadRow("SELECT pid,title,thanks FROM poll WHERE rid = '" + roomID + "'");
            int pollID = int.Parse(poll_poll[0]);
            string pollTitle = poll_poll[1];
            string pollThanks = poll_poll[2];
            int[] pollQIDs = DB.runReadColumn("SELECT qid FROM poll_questions WHERE pid = '" + pollID + "'", 0, null);
            string[] pollQuestion = DB.runReadColumn("SELECT question FROM poll_questions WHERE pid = '" + pollID + "'", 0);
            int pollQuestions = pollQuestion.Length;
            int[] pollTypes = DB.runReadColumn("SELECT type FROM poll_questions WHERE pid = '" + pollID + "'", 0, null);
            int[] pollMin = DB.runReadColumn("SELECT min FROM poll_questions WHERE pid = '" + pollID + "'", 0, null);
            int[] pollMax = DB.runReadColumn("SELECT max FROM poll_questions WHERE pid = '" + pollID + "'", 0, null);
            string[][] pollAnswer = new string[pollQIDs.Length][];
            int[] pollAnswers = new int[pollQIDs.Length];
            int qCount = 0;
            foreach (int q in pollQIDs)
            {
                int[] aid = DB.runReadColumn("SELECT aid FROM poll_answers WHERE qid = '" + q + "'", 0, null);
                pollAnswer[qCount] = new string[aid.Length];
                int iCount = 0;
                foreach (int a in aid)
                {
                    pollAnswer[qCount][iCount] = DB.runRead("SELECT answer FROM poll_answers WHERE qid = '" + q + "' AND aid = '" + a + "'");
                    iCount++;
                }
                pollAnswers[qCount] = pollAnswer[qCount].Length;
                qCount++;
            }
            StringBuilder pollPacket = new StringBuilder("D}");
            pollPacket.Append(Encoding.encodeVL64(pollID));
            pollPacket.Append(pollTitle);
            pollPacket.Append(Convert.ToChar(2));
            pollPacket.Append(pollThanks);
            pollPacket.Append(Convert.ToChar(2));
            pollPacket.Append(Encoding.encodeVL64(pollQuestions));
            qCount = 0;
            foreach (int i in pollQIDs)
            {
                pollPacket.Append(Encoding.encodeVL64(i));
                pollPacket.Append(Encoding.encodeVL64(qCount + 1));
                pollPacket.Append(Encoding.encodeVL64(pollTypes[qCount]));
                pollPacket.Append(pollQuestion[qCount]);
                pollPacket.Append(Convert.ToChar(2));
                pollPacket.Append(Encoding.encodeVL64(pollAnswers[qCount]));
                pollPacket.Append(Encoding.encodeVL64(pollMin[qCount]));
                pollPacket.Append(Encoding.encodeVL64(pollMax[qCount]));
                foreach (string a in pollAnswer[qCount])
                {
                    pollPacket.Append(a);
                    pollPacket.Append(Convert.ToChar(2));
                }
                qCount++;
            }
            return pollPacket.ToString();
        }
        #endregion

        #region Misc room related functions
        /// <summary>
        /// Updates the inside visitors count in the database for a certain room.
        /// </summary>
        /// <param name="roomID">The ID of the room to update.</param>
        /// <param name="visitorCount">The new visitors count.</param>
        public static void updateRoomVisitorCount(int roomID, int visitorCount)
        {
            DB.runQuery("UPDATE rooms SET visitors_now = '" + visitorCount + "' WHERE id = '" + roomID + "' LIMIT 1");
        }
        /// <summary>
        /// Returns the int ID for a certain room state.
        /// </summary>
        /// <param name="State">The room state ID.</param>
        public static int getRoomState(string State)
        {
            if (State == "closed")
                return 1;
            else if (State == "password")
                return 2;
            else
                return 0;
        }
        /// <summary>
        /// Returns the string state for a certain room state byte.
        /// </summary>
        /// <param name="State">The room state ID.</param>
        public static string getRoomState(int State)
        {
            if (State == 1)
                return "closed";
            else if (State == 2)
                return "password";
            else
                return "open";
        }
        #endregion

        #region Furni related functions
        /// <summary>
        /// Updates the inside visitors count in the database for a certain room.
        /// </summary>
        /// <param name="roomID">The ID of the room the item is in.</param>
        /// <param name="itemID">The ID of the item to refresh.</param>
        /// <param name="cctName">The cct name of the item to refresh.</param>
        /// <param name="wallPosition">The wall position of the item to refresh.</param>
        /// <param name="itemVariable">The variable of the item to refresh.</param>
        public static void refreshWallitem(int roomID, int itemID, string cctName, string wallPosition, string itemVariable)
        {
            getRoom(roomID).sendData(new HabboPacketBuilder("AU").Append(itemID).TabSeparator().Append(cctName).TabSeparator().Append(" ").Append(wallPosition).TabSeparator().Append(itemVariable).Build());
        }

        /// <summary>
        /// Contains functions for the moodlight.
        /// </summary>
        public static class moodlight
        {
            /// <summary>
            /// Returns a string containing the setting data for the moodlight in the room.
            /// </summary>
            /// <param name="roomID">The roomID to get the moodlight for.</param>
            public static string getSettings(int roomID)
            {
                try
                {
                    string[] itemSettings = DB.runReadRow("SELECT preset_cur,preset_1,preset_2,preset_3 FROM furniture_moodlight WHERE roomid = '" + roomID + "'");
                    string settingPack = Encoding.encodeVL64(3) + Encoding.encodeVL64(int.Parse(itemSettings[0]));

                    for (int i = 1; i <= 3; i++)
                    {
                        string[] curPresetData = itemSettings[i].Split(Char.Parse(","));
                        settingPack += Encoding.encodeVL64(i) + Encoding.encodeVL64(int.Parse(curPresetData[0])) + curPresetData[1] + Convert.ToChar(2) + Encoding.encodeVL64(int.Parse(curPresetData[2]));
                    }
                    return settingPack;
                }
                catch
                {
                    return null;
                }
            }
            /// <summary>
            /// Saves the setting data for the moodlight in the room.
            /// </summary>
            /// <param name="roomID">The roomID to get the moodlight for.</param>
            /// <param name="isEnabled">The status of the moodlight (on/off).</param>
            /// <param name="presetID">The preset slot that is being used.</param>
            /// <param name="bgState">The status of the background only tick.</param>
            /// <param name="presetColour">The colour that is being saved.</param>
            /// <param name="alphaDarkF">The alpha value of the darkness level.</param>
            public static void setSettings(int roomID, bool isEnabled, int presetID, int bgState, string presetColour, int alphaDarkF)
            {
                int itemID = DB.runRead("SELECT id FROM furniture_moodlight WHERE roomid = '" + roomID + "'", null);
                string newPresetValue;

                if (isEnabled == false)
                {
                    string curPresetValue = DB.runRead("SELECT var FROM furniture WHERE id = '" + itemID + "'");
                    if (curPresetValue.Substring(0, 1) == "2")
                    {
                        newPresetValue = "1" + curPresetValue.Substring(1);
                    }
                    else
                    {
                        newPresetValue = "2" + curPresetValue.Substring(1);
                    }
                    DB.runQuery("UPDATE furniture SET var = '" + newPresetValue + "' WHERE id = '" + itemID.ToString() + "' LIMIT 1");
                }
                else
                {
                    newPresetValue = "2" + "," + presetID.ToString() + "," + bgState.ToString() + "," + presetColour + "," + alphaDarkF.ToString();
                    DB.runQuery("UPDATE furniture SET var = '" + newPresetValue + "' WHERE id = '" + itemID.ToString() + "' LIMIT 1");
                    DB.runQuery("UPDATE furniture_moodlight SET preset_cur = '" + presetID.ToString() + "',preset_" + presetID.ToString() + "= '" + bgState.ToString() + "," + presetColour + "," + alphaDarkF.ToString() + "' WHERE id = '" + itemID.ToString() + "' LIMIT 1");
                }
                string wallPosition = DB.runRead("SELECT wallpos FROM furniture WHERE id = '" + itemID + "'");
                refreshWallitem(roomID, itemID, "roomdimmer", wallPosition, newPresetValue);
            }
        }
        #endregion
    }
}
