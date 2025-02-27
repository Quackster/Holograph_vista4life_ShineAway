using System;
using System.Threading;
using System.Collections;

using Holo.Managers;
using Holo.Virtual.Rooms;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Represents a virtual user in a virtual room.
    /// </summary>
    public class virtualRoomUser
    {
        /// <summary>
        /// The database ID of the user.
        /// </summary>
        internal int userID;
        /// <summary>
        /// The ID of the virtual room where the user is in.
        /// </summary>
        internal int roomID;
        /// <summary>
        /// The ID of the user in the virtual room.
        /// </summary>
        internal int roomUID;
        /// <summary>
        /// The X position of the user in the virtual room.
        /// </summary>
        internal int X;
        /// <summary>
        /// The Y position of the user in the virtual room.
        /// </summary>
        internal int Y;
        /// <summary>
        /// The height of the user in the virtual room.
        /// </summary>
        internal double H;
        /// <summary>
        /// The rotation of the user's head in the virtual room.
        /// </summary>
        internal byte Z1;
        /// <summary>
        /// The rotation of the user's body in the virtual room.
        /// </summary>
        internal byte Z2;
        /// <summary>
        /// The X position where the user wants to walk to. If not walking, -1.
        /// </summary>
        internal int goalX;
        /// <summary>
        /// The Y position where the user wants to walk to. If not walking, -1.
        /// </summary>
        internal int goalY;
        /// <summary>
        /// Specifies if the user can walk.
        /// </summary>
        internal bool walkLock;
        /// <summary>
        /// Specifies if the user is on his way of leaving the room after clicking the door.
        /// </summary>
        internal bool walkDoor;
        /// <summary>
        /// Guestroom only. Specifies if the user has voted on the guestroom already.
        /// </summary>
        internal bool hasVoted;
        /// <summary>
        /// Swimming pools only. Specifies the swimming pool outfit.
        /// </summary>
        internal string SwimOutfit;
        /// <summary>
        /// Gamelobbies only. Specifies the amount of gamepoints that this user has for the game in the current gamelobby.
        /// </summary>
        internal int gamePoints;
        /// <summary>
        /// The parent virtual user of this room user.
        /// </summary>
        internal virtualUser User;
        /// <summary>
        /// The status manager of this room user.
        /// </summary>
        internal virtualRoomUserStatusManager statusManager;
        /// <summary>
        /// Indicates if the room user is currently typing a chat message.
        /// </summary>
        internal bool isTyping;
        /// <summary>
        /// Turn the pettransform on or off.
        /// </summary>
        internal bool catTransform;
        internal bool crocTransform;
        internal bool dogTransform;  
        internal bool SPECIAL_TELEPORTABLE;
        private delegate void walkSleepPointer();
        /// <summary>
        /// Initializes a virtual room user.
        /// </summary>
        /// <param name="userID">The database ID of the virtual user of this room user object.</param>
        /// <param name="roomID">The database ID of the room where the room user is in.</param>
        /// <param name="roomUID">The ID that identifies the room user in the virtual room.</param>
        /// <param name="User">The parent vittual user of this room user.</param>
        /// <param name="statusManager">The status manager of this room user.</param>
        internal virtualRoomUser(int userID, int roomID, int roomUID, virtualUser User, virtualRoomUserStatusManager statusManager)
        {
            this.userID = userID;
            this.roomID = roomID;
            this.roomUID = roomUID;
            this.User = User;
            this.statusManager = statusManager;
        }
        /// <summary>
        /// The virtualRoom object that represents the room where this room user is in.
        /// </summary>
        internal virtualRoom Room
        {
            get
            {
                return roomManager.getRoom(roomID);
            }
        }
        /// <summary>
        /// Refreshes the status of this room user in the virtual room.
        /// </summary>
        internal void Refresh()
        {
            Room.Refresh(this);
        }
        /// <summary>
        /// Returns the details string for this room user, containing username etc.
        /// </summary>
        internal string detailsString
        {
            get
            {

                string petTransformAdd = "";
                string petTransformFigure = User._Figure;

                if (catTransform)
                {
                    petTransformAdd = "7ui1";
                    petTransformFigure = "1 006 D98961";
                }

                if (crocTransform)
                {
                    petTransformAdd = "7ui1";
                    petTransformFigure = "2 006 c8d71d";
                }

                if (dogTransform)
                {
                    petTransformAdd = "7ui1";
                    petTransformFigure = "0 005 B0C993";
                }

                string userName = DB.runRead("SELECT name FROM users WHERE id = '" + userID + "'");
                string userMotto = DB.runRead("SELECT mission FROM users WHERE id = '" + userID + "'");
                string userSex = DB.runRead("SELECT sex FROM users WHERE id = '" + userID + "'");
                if (DB.checkExists("SELECT * FROM users_badgescur WHERE userid = '" + userID + "'") == false)
                {
                    DB.runQuery("INSERT INTO users_badgescur(userid,badge1,badge2,badge3,badge4,badge5) VALUES ('" + userID + "','','','','','')");
                }
                string Badge1 = DB.runRead("SELECT badge1 FROM users_badgescur WHERE userid = '" + userID + "'");
                string Badge2 = DB.runRead("SELECT badge2 FROM users_badgescur WHERE userid = '" + userID + "'");
                string Badge3 = DB.runRead("SELECT badge3 FROM users_badgescur WHERE userid = '" + userID + "'");
                string Badge4 = DB.runRead("SELECT badge4 FROM users_badgescur WHERE userid = '" + userID + "'");
                string Badge5 = DB.runRead("SELECT badge5 FROM users_badgescur WHERE userid = '" + userID + "'");

                string s = "i:" + roomUID + Convert.ToChar(13) + "a:" + userID + Convert.ToChar(13) + "n:" + userName + Convert.ToChar(13) + "f:" + petTransformFigure + Convert.ToChar(13) + "l:" + X + " " + Y + " " + H + Convert.ToChar(13) + "c:" + userMotto + Convert.ToChar(13) + "s:" + userSex + Convert.ToChar(13) + "b:";
                if (Badge1 != "") { s += "1:" + Badge1; }
                if (Badge2 != "") { s += ",2:" + Badge2; }
                if (Badge3 != "") { s += ",3:" + Badge3; }
                if (Badge4 != "") { s += ",4:" + Badge4; }
                if (Badge5 != "") { s += ",5:" + Badge5; }
                if (SwimOutfit != "") { s += Convert.ToChar(13) + "p:" + SwimOutfit; }
                if (User._groupID > 0) { s += Convert.ToChar(13) + "g:" + User._groupID + Convert.ToChar(13) + "t:" + (int)User._groupMemberRank; }
                return s;
            }
        }

        /// <summary>
        /// Returns the action string for this room user, containing position, movements, statuses etc.
        /// </summary>
        internal string statusString
        {
            get
            {
               return roomUID + " " + X + "," + Y + "," + H.ToString().Replace(",",".") + "," + Z1 + "," + Z2 + "/" + statusManager.ToString();
            }
        }
    }
}
