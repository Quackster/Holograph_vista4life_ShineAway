using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using HolographEmulator.Infrastructure.DataAccess;

using HolographEmulator.Infrastructure.Managers;
using HolographEmulator.Domain;
using HolographEmulator.Domain.Users;
using HolographEmulator.Domain.Rooms;

namespace HolographEmulator.Domain.Users.Messenger
{
    /// <summary>
    /// Represents the messenger for a virtual user, which provides keeping buddy lists, instant messaging, inviting friends to a user's virtual room and various other features. The messenger object provides voids for updating status of friends, instant messaging and more.
    /// </summary>
    class Messenger
    {
        private static readonly MessengerDataAccess _messengerDataAccess = new MessengerDataAccess();
        private static readonly UserDataAccess _userDataAccess = new UserDataAccess();
        #region Declares
        /// <summary>
        /// The database ID of the parent virtual user.
        /// </summary>
        private int userID;
        private Hashtable Buddies;
        #endregion

        #region Constructors/destructors
        /// <summary>
        /// Initializes the virtual messenger for the parent virtual user, generating friendlist, friendrequests etc.
        /// </summary>
        /// <param name="userID">The database ID of the parent virtual user.</param>
        internal Messenger(int userID)
        {
            this.userID = userID;
            this.Buddies = new Hashtable();
        }
        internal string friendList()
        {
            int[] userIDs = userManager.getUserFriendIDs(userID);
            StringBuilder Buddylist = new StringBuilder(Encoding.encodeVL64(200) + Encoding.encodeVL64(200) + Encoding.encodeVL64(600) + "H" + Encoding.encodeVL64(userIDs.Length));
                Buddy Me = new Buddy(userID);
            for (int i = 0; i < userIDs.Length; i++)
            {
                Buddy Buddy = new Buddy(userIDs[i]);
                try
                {
                    if (Buddy.Online)
                        userManager.getUser(userIDs[i]).Messenger.addBuddy(Me, true);
                }
                catch { }
                Buddies.Add(userIDs[i], Buddy);
                Buddylist.Append(Buddy.ToString(true));
            }
            Buddylist.Append(Encoding.encodeVL64(200) + "H");
            return Buddylist.ToString();
        }
        internal string friendRequests()
        {
            var userIDs = _messengerDataAccess.GetFriendRequestUserIds(this.userID);
            StringBuilder Requests = new StringBuilder(Encoding.encodeVL64(userIDs.Count) + Encoding.encodeVL64(userIDs.Count));
            if(userIDs.Count > 0)
            {
                var requestIDs = _messengerDataAccess.GetFriendRequestIds(this.userID);
                for(int i = 0; i < userIDs.Count; i++)
                    Requests.Append(Encoding.encodeVL64(requestIDs[i]) + _userDataAccess.GetUsername(userIDs[i]) + Convert.ToChar(2) + userIDs[i] + Convert.ToChar(2));
            }
            return Requests.ToString();
        }
        internal void Clear()
        {
            
        }

        internal void addBuddy(virtualBuddy Buddy, bool Update)
        {
            if (Buddies.ContainsKey(Buddy.userID) == false)
                Buddies.Add(Buddy.userID, Buddy);
            if(Update)
                User.sendData("@MHII" + Buddy.ToString(true));
        }
        /// <summary>
        /// Deletes a buddy from the friendlist and virtual messenger of this user, but leaves the database row untouched.
        /// </summary>
        /// <param name="ID">The database ID of the buddy to delete from the friendlist.</param>
        internal void removeBuddy(int ID)
        {
            User.sendData("@MHI" + "M" + Encoding.encodeVL64(ID));
            if (Buddies.Contains(ID))
                Buddies.Remove(ID);
        }
        internal string getUpdates()
        {
            int updateAmount = 0;
            StringBuilder Updates = new StringBuilder();
            try
            {
                foreach (virtualBuddy Buddy in Buddies.Values)
                {
                    if (Buddy.Updated)
                    {
                        updateAmount++;
                        Updates.Append("H" + Buddy.ToString(false));
                    }
                }
                return "H" + Encoding.encodeVL64(updateAmount) + Updates.ToString();
            }
            catch { return "HH"; }
        }
        #endregion
        /// <summary>
        /// Returns a boolean that indicates if the messenger contains a certain buddy, and this buddy is online.
        /// </summary>
        /// <param name="userID">The database ID of the buddy to check.</param>
        internal bool containsOnlineBuddy(int userID)
        {
            if (Buddies.ContainsKey(userID) == false)
                return false;
            else
                return userManager.containsUser(userID);
        }
        /// <summary>
        /// Returns a bool that indicates if there is a friendship between the parent virtual user and a certain user.
        /// </summary>
        /// <param name="userID">The database ID of the user to check.</param>
        internal bool hasFriendship(int userID)
        {
            return Buddies.ContainsKey(userID);
        }
        /// <summary>
        /// Returns a bool that indicates if there are friend requests hinth and forth between the the parent virtual user and a certain user.
        /// </summary>
        /// <param name="userID">The database ID of the user to check.</param>
        internal bool hasFriendRequests(int userID)
        {
            return _messengerDataAccess.HasFriendRequests(this.userID, userID);
        }

        #region Object management
        /// <summary>
        /// Returns the parent virtual user instance of this virtual messenger.
        /// </summary>
        internal virtualUser User
        {
            get
            {
                return userManager.getUser(this.userID);
            }
        }
        #endregion
    }
}
