using System;
using System.Text;

using Holo.Managers;
using Holo.Virtual.Users.Messenger;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Partial class for virtualUser containing Messenger packet handling.
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes Messenger-related packets.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        private bool processMessengerPackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                case "Fs": //search console
                    sendData("HR" + "L");
                    break;

                case "@i": // Search in console
                    {

                        // Variables
                        string Search = DB.Stripslash(currentPacket.Substring(4));
                        string Packet = "Fs";
                        string PacketFriends = "";
                        string PacketOthers = "";
                        string PacketAdd = "";
                        int CountFriends = 0;
                        int CountOthers = 0;

                        // Database
                        string[] IDs = DB.runReadColumn("SELECT id FROM users WHERE name LIKE '%" + Search + "%'", 50);

                        // Loop through results
                        for (int i = 0; i < IDs.Length; i++)
                        {

                            int thisID = Convert.ToInt16(IDs[i]);
                            bool online = userManager.containsUser(thisID);
                            string onlineStr = online ? "I" : "H";

                            string[] row = DB.runReadRow("SELECT name, mission, lastvisit, figure FROM users WHERE id = " + thisID.ToString());
                            PacketAdd = Encoding.encodeVL64(thisID)
                                         + row[0] + ""
                                         + row[1] + ""
                                         + onlineStr + onlineStr + ""
                                         + onlineStr + (online ? row[3] : "") + ""
                                         + (online ? "" : row[2]) + "";

                            // Friend or not?
                            if (Messenger.hasFriendship(thisID))
                            {
                                CountFriends += 1;
                                PacketFriends += PacketAdd;
                            }
                            else
                            {
                                CountOthers += 1;
                                PacketOthers += PacketAdd;
                            }

                        }

                        // Add count headers
                        PacketFriends = Encoding.encodeVL64(CountFriends) + PacketFriends;
                        PacketOthers = Encoding.encodeVL64(CountOthers) + PacketOthers;

                        // Merge packets
                        Packet += PacketFriends + PacketOthers;

                        Out.WriteLine(Packet);
                        // Send packets
                        sendData(Packet);

                        break;
                    }


                case "@g": // Messenger - request user as friend
                    {
                        if (Messenger != null)
                        {
                            string Username = DB.Stripslash(currentPacket.Substring(4));
                            int toID = DB.runRead("SELECT id FROM users WHERE name = '" + Username + "'", null);
                            if (toID > 0 && Messenger.hasFriendRequests(toID) == false && Messenger.hasFriendship(toID) == false)
                            {
                                int requestID = DB.runReadUnsafe("SELECT MAX(requestid) FROM messenger_friendrequests WHERE userid_to = '" + toID + "'", null) + 1;
                                DB.runQuery("INSERT INTO messenger_friendrequests(userid_to,userid_from,requestid) VALUES ('" + toID + "','" + userID + "','" + requestID + "')");
                                userManager.getUser(toID).sendData("BD" + "I" + _Username + Convert.ToChar(2) + userID + Convert.ToChar(2));
                            }
                        }
                        break;
                    }

                case "@e": // Messenger - accept friendrequest(s)
                    {
                        if (Messenger != null)
                        {
                            int Amount = Encoding.decodeVL64(currentPacket.Substring(2));
                            currentPacket = currentPacket.Substring(Encoding.encodeVL64(Amount).Length + 2);

                            int updateAmount = 0;
                            StringBuilder Updates = new StringBuilder();
                            virtualBuddy Me = new virtualBuddy(userID);

                            for (int i = 0; i < Amount; i++)
                            {
                                if (currentPacket == "")
                                    return;
                                int requestID = Encoding.decodeVL64(currentPacket);
                                int fromUserID = DB.runRead("SELECT userid_from FROM messenger_friendrequests WHERE userid_to = '" + this.userID + "' AND requestid = '" + requestID + "'", null);
                                if (fromUserID == 0) // Corrupt data
                                    return;

                                virtualBuddy Buddy = new virtualBuddy(fromUserID);
                                Updates.Append(Buddy.ToString(false));
                                updateAmount++;

                                Messenger.addBuddy(Buddy, true);
                                if (userManager.containsUser(fromUserID))
                                    userManager.getUser(fromUserID).Messenger.addBuddy(Me, true);

                                DB.runQuery("INSERT INTO messenger_friendships(userid,friendid) VALUES ('" + fromUserID + "','" + this.userID + "')");
                                DB.runQuery("DELETE FROM messenger_friendrequests WHERE userid_to = '" + this.userID + "' AND requestid = '" + requestID + "' LIMIT 1");
                                currentPacket = currentPacket.Substring(Encoding.encodeVL64(requestID).Length);
                            }

                            if (updateAmount > 0)
                                sendData("@M" + "HH" + Encoding.encodeVL64(updateAmount) + Updates.ToString());
                        }
                        break;
                    }

                case "@f": // Messenger - decline friendrequests
                    {
                        if (Messenger != null)
                        {
                            int Amount = Encoding.decodeVL64(currentPacket.Substring(2));
                            currentPacket = currentPacket.Substring(Encoding.encodeVL64(Amount).Length + 2);

                            for (int i = 0; i < Amount; i++)
                            {
                                if (currentPacket == "")
                                    return;

                                int requestID = Encoding.decodeVL64(currentPacket);
                                DB.runQuery("DELETE FROM messenger_friendrequests WHERE userid_to = '" + this.userID + "' AND requestid = '" + requestID + "' LIMIT 1");

                                currentPacket = currentPacket.Substring(Encoding.encodeVL64(requestID).Length);
                            }
                        }
                        break;
                    }

                case "@h": // Messenger - remove buddy from friendlist
                    {
                        if (Messenger != null)
                        {
                            int buddyID = Encoding.decodeVL64(currentPacket.Substring(3));
                            Messenger.removeBuddy(buddyID);
                            if (userManager.containsUser(buddyID))
                                userManager.getUser(buddyID).Messenger.removeBuddy(userID);
                            DB.runQuery("DELETE FROM messenger_friendships WHERE (userid = '" + userID + "' AND friendid = '" + buddyID + "') OR (userid = '" + buddyID + "' AND friendid = '" + userID + "') LIMIT 1");
                        }
                        break;
                    }

                case "@a": // Messenger - send instant message to buddy
                    {
                        if (Messenger != null)
                        {
                            int buddyID = Encoding.decodeVL64(currentPacket.Substring(2));
                            string Message = currentPacket.Substring(Encoding.encodeVL64(buddyID).Length + 4);
                            Message = stringManager.filterSwearwords(Message); // Filter swearwords

                            if (Messenger.containsOnlineBuddy(buddyID)) // Buddy online
                                userManager.getUser(buddyID).sendData("BF" + Encoding.encodeVL64(userID) + Message + Convert.ToChar(2));
                            else // Buddy offline (or user doesn't has user in buddylist)
                                sendData("DE" + Encoding.encodeVL64(5) + Encoding.encodeVL64(userID));
                        }
                        break;
                    }

                case "@O": // Messenger - refresh friendlist
                    {
                        if (Messenger != null)
                            sendData("@M" + Messenger.getUpdates());
                        break;
                    }

                case "DF": // Messenger - follow buddy to a room
                    {
                        if (Messenger != null)
                        {
                            int ID = Encoding.decodeVL64(currentPacket.Substring(2));
                            int errorID = -1;
                            if (Messenger.hasFriendship(ID)) // Has friendship with user
                            {
                                if (userManager.containsUser(ID)) // User is online
                                {
                                    virtualUser _User = userManager.getUser(ID);
                                    if (_User._roomID > 0) // User is in room
                                    {
                                        if (_User._inPublicroom)
                                            sendData("D^" + "I" + Encoding.encodeVL64(_User._roomID));
                                        else
                                            sendData("D^" + "H" + Encoding.encodeVL64(_User._roomID));
                                    }
                                    else // User is not in a room
                                        errorID = 2;
                                }
                                else // User is offline
                                    errorID = 1;
                            }
                            else // User is not this virtual user's friend
                                errorID = 0;

                            if (errorID != -1) // Error occured
                                sendData("E]" + Encoding.encodeVL64(errorID));
                        }
                        break;
                    }

                case "@b": // Messenger - invite buddies to your room
                    {
                        if (Messenger != null && roomUser != null)
                        {
                            int Amount = Encoding.decodeVL64(currentPacket.Substring(2));
                            int[] IDs = new int[Amount];
                            currentPacket = currentPacket.Substring(Encoding.encodeVL64(Amount).Length + 2);

                            for (int i = 0; i < Amount; i++)
                            {
                                if (currentPacket == "")
                                    return;

                                int ID = Encoding.decodeVL64(currentPacket);
                                if (Messenger.hasFriendship(ID) && userManager.containsUser(ID))
                                    IDs[i] = ID;

                                currentPacket = currentPacket.Substring(Encoding.encodeVL64(ID).Length);
                            }

                            string Message = currentPacket.Substring(2);
                            string Data = "BG" + Encoding.encodeVL64(userID) + Message + Convert.ToChar(2);
                            for (int i = 0; i < Amount; i++)
                                userManager.getUser(IDs[i]).sendData(Data);
                        }
                        break;
                    }

                default:
                    return false;
            }
            return true;
        }
    }
}
