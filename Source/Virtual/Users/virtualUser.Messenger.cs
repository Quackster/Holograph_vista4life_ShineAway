using System;
using System.Text;

using Holo.Data.Repositories.Messenger;
using Holo.Data.Repositories.Users;
using Holo.Managers;
using Holo.Protocol;
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
                    sendData(new HabboPacketBuilder(HabboPackets.CONSOLE_SEARCH).Append("L").Build());
                    break;

                case "@i": // Search in console
                    {
                        // Variables
                        string Search = currentPacket.Substring(4);
                        var packetBuilder = new HabboPacketBuilder(HabboPackets.MESSENGER_SEARCH);
                        var packetFriends = new HabboPacketBuilder();
                        var packetOthers = new HabboPacketBuilder();
                        int countFriends = 0;
                        int countOthers = 0;

                        // Database
                        string[] IDs = UserRepository.Instance.SearchUsersByName(Search, HabboProtocol.MESSENGER_SEARCH_LIMIT);

                        // Loop through results
                        for (int i = 0; i < IDs.Length; i++)
                        {
                            int thisID = Convert.ToInt16(IDs[i]);
                            bool online = userManager.containsUser(thisID);
                            string onlineStr = online ? HabboProtocol.BOOL_TRUE : HabboProtocol.BOOL_FALSE;

                            string[] row = UserRepository.Instance.GetUserSearchData(thisID);
                            var userEntry = new HabboPacketBuilder()
                                .AppendVL64(thisID)
                                .Append(row[0]).Separator()
                                .Append(row[1]).Separator()
                                .Append(onlineStr).Append(onlineStr).Separator()
                                .Append(onlineStr).Append(online ? row[3] : "").Separator()
                                .Append(online ? "" : row[2]).Separator();

                            // Friend or not?
                            if (Messenger.hasFriendship(thisID))
                            {
                                countFriends++;
                                packetFriends.Append(userEntry.Build());
                            }
                            else
                            {
                                countOthers++;
                                packetOthers.Append(userEntry.Build());
                            }
                        }

                        // Build final packet: header + friend count + friends + other count + others
                        packetBuilder.AppendVL64(countFriends)
                            .Append(packetFriends.Build())
                            .AppendVL64(countOthers)
                            .Append(packetOthers.Build());

                        string finalPacket = packetBuilder.Build();
                        Out.WriteLine(finalPacket);
                        sendData(finalPacket);

                        break;
                    }


                case "@g": // Messenger - request user as friend
                    {
                        if (Messenger != null)
                        {
                            string Username = currentPacket.Substring(4);
                            int toID = UserRepository.Instance.GetUserId(Username);
                            if (toID > 0 && Messenger.hasFriendRequests(toID) == false && Messenger.hasFriendship(toID) == false)
                            {
                                int requestID = MessengerRepository.Instance.GetMaxRequestId(toID) + 1;
                                MessengerRepository.Instance.CreateFriendRequestWithId(userID, toID, requestID);
                                var requestPacket = new HabboPacketBuilder(HabboPackets.BUDDY_REQUEST_SENT)
                                    .Append(HabboProtocol.BOOL_TRUE)
                                    .Append(_Username).Separator()
                                    .Append(userID).Separator();
                                userManager.getUser(toID).sendData(requestPacket.Build());
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
                                    return true;
                                int requestID = Encoding.decodeVL64(currentPacket);
                                int fromUserID = MessengerRepository.Instance.GetRequestSender(this.userID, requestID);
                                if (fromUserID == 0) // Corrupt data
                                    return true;

                                virtualBuddy Buddy = new virtualBuddy(fromUserID);
                                Updates.Append(Buddy.ToString(false));
                                updateAmount++;

                                Messenger.addBuddy(Buddy, true);
                                if (userManager.containsUser(fromUserID))
                                    userManager.getUser(fromUserID).Messenger.addBuddy(Me, true);

                                MessengerRepository.Instance.AddFriendship(fromUserID, this.userID);
                                MessengerRepository.Instance.DeleteFriendRequestById(this.userID, requestID);
                                currentPacket = currentPacket.Substring(Encoding.encodeVL64(requestID).Length);
                            }

                            if (updateAmount > 0)
                        {
                            var updatePacket = new HabboPacketBuilder(HabboPackets.BUDDY_UPDATE)
                                .Append(HabboProtocol.BOOL_FALSE)
                                .Append(HabboProtocol.BOOL_FALSE)
                                .AppendVL64(updateAmount)
                                .Append(Updates.ToString());
                            sendData(updatePacket.Build());
                        }
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
                                    return true;

                                int requestID = Encoding.decodeVL64(currentPacket);
                                MessengerRepository.Instance.DeleteFriendRequestById(this.userID, requestID);

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
                            MessengerRepository.Instance.RemoveFriendship(userID, buddyID);
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
                            {
                                var msgPacket = new HabboPacketBuilder(HabboPackets.INSTANT_MESSAGE)
                                    .AppendVL64(userID)
                                    .Append(Message).Separator();
                                userManager.getUser(buddyID).sendData(msgPacket.Build());
                            }
                            else // Buddy offline (or user doesn't has user in buddylist)
                            {
                                var errorPacket = new HabboPacketBuilder(HabboPackets.MESSAGE_DELIVERY_ERROR)
                                    .AppendVL64(5)
                                    .AppendVL64(userID);
                                sendData(errorPacket.Build());
                            }
                        }
                        break;
                    }

                case "@O": // Messenger - refresh friendlist
                    {
                        if (Messenger != null)
                            sendData(new HabboPacketBuilder(HabboPackets.BUDDY_UPDATE).Append(Messenger.getUpdates()).Build());
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
                                        var followPacket = new HabboPacketBuilder(HabboPackets.FOLLOW_FRIEND)
                                            .Append(_User._inPublicroom ? HabboProtocol.BOOL_TRUE : HabboProtocol.BOOL_FALSE)
                                            .AppendVL64(_User._roomID);
                                        sendData(followPacket.Build());
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
                                sendData(new HabboPacketBuilder(HabboPackets.FOLLOW_ERROR).AppendVL64(errorID).Build());
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
                                    return true;

                                int ID = Encoding.decodeVL64(currentPacket);
                                if (Messenger.hasFriendship(ID) && userManager.containsUser(ID))
                                    IDs[i] = ID;

                                currentPacket = currentPacket.Substring(Encoding.encodeVL64(ID).Length);
                            }

                            string Message = currentPacket.Substring(2);
                            var invitePacket = new HabboPacketBuilder(HabboPackets.INSTANT_MESSAGE_SENT)
                                .AppendVL64(userID)
                                .Append(Message).Separator();
                            string inviteData = invitePacket.Build();
                            for (int i = 0; i < Amount; i++)
                                userManager.getUser(IDs[i]).sendData(inviteData);
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
