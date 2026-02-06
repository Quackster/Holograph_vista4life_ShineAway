using System;
using System.Text;
using System.Threading;

using Holo.Data.Repositories.Users;
using Holo.Managers;
using Holo.Protocol;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Partial class for virtualUser containing the main packet processing dispatcher.
    /// Routes packets to appropriate handler methods in other partial classes.
    /// </summary>
    public partial class virtualUser
    {
        #region Packet processing
        /// <summary>
        /// Processes a single packet from the client.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        private void processPacket(string currentPacket)
        {
            Out.WriteSpecialLine(currentPacket.Replace(HabboProtocol.RECORD_SEPARATOR.ToString(), "{13}"), Out.logFlags.MehAction, ConsoleColor.DarkGray, ConsoleColor.DarkYellow, "< [" + Thread.GetDomainID() + "]", 2, ConsoleColor.Blue);
            {
                if (_isLoggedIn == false)

                #region Non-logged in packet processing
                {
                    switch (currentPacket.Substring(0, 2))
                    {
                        case "CD":
                            pingOK = true;
                            break;

                        case "CN":
                            sendData(HabboPackets.SESSION_PARAMS_EXT);
                            break;

                        case "CJ":
                            {
                                var packet = new HabboPacketBuilder(HabboPackets.SESSION_PARAMS)
                                    .Append("QBHHIIKHJIPAHQAdd-MM-yyyy")
                                    .Separator()
                                    .Append("SAHPBhotel-co.uk")
                                    .Separator()
                                    .Append("QBH");
                                sendData(packet.Build());
                                break;
                            }

                        case "_R":
                            {
                                // V25+ SSO LOGIN BY vista4life
                                var packet = new HabboPacketBuilder(HabboPackets.SESSION_PARAMS)
                                    .Append("QBHIIIKHJIPAIQAdd-MM-yyyy")
                                    .Separator()
                                    .Append("SAHPB/client")
                                    .Separator()
                                    .Append("QBH")
                                    .Append("IJWVVVSNKQCFUBJASMSLKUUOJCOLJQPNSBIRSVQBRXZQOTGPMNJIHLVJCRRULBLUO")
                                    .Append(HabboProtocol.PACKET_TERMINATOR);
                                sendData(packet.Build());
                                break;
                            }

                        case "CL":
                            {
                                string ssoTicket = currentPacket.Substring(4);
                                int myID = UserRepository.Instance.GetUserIdByTicketSso(ssoTicket);
                                if (myID == 0) // No user found for this sso ticket and/or IP address
                                {
                                    Disconnect();
                                    return;
                                }

                                string banReason = userManager.getBanReason(myID);
                                if (banReason != "")
                                {
                                    sendData(new HabboPacketBuilder(HabboPackets.BAN_REASON).Append(banReason).Build());
                                    Disconnect(HabboProtocol.DISCONNECT_DELAY_MS);
                                    return;
                                }
                                this.userID = myID;
                                string[] userData = UserRepository.Instance.GetLoginUserData(myID);
                                _Username = userData[0];
                                _Figure = userData[1];
                                _Sex = char.Parse(userData[2]);
                                _Mission = userData[3];
                                _Rank = byte.Parse(userData[4]);
                                _consoleMission = userData[5];
                                userManager.addUser(myID, this);
                                _isLoggedIn = true;

                                sendData(new HabboPacketBuilder(HabboPackets.USER_RIGHTS).Append(rankManager.fuseRights(_Rank)).Build());
                                sendData(HabboPackets.SECOND_CONNECTION);
                                sendData(HabboPackets.INIT_COMPLETE);

                                int isguide = UserRepository.Instance.GetGuideStatus(userID);

                                if (isguide == 1)
                                    sendData(HabboPackets.GUIDE_STATUS);
                                sendData(new HabboPacketBuilder(HabboPackets.FILTER_STATUS).Append(HabboProtocol.BOOL_TRUE).Build());
                                sendData(HabboPackets.FRIEND_CHECK);

                                if (Config.enableWelcomeMessage)
                                    sendData(HabboPacketBuilder.SystemMessage(stringManager.getString("welcomemessage_text")));

                                //Send list of ignored users
                                int[] ignoredUsers = UserRepository.Instance.GetIgnoredUserIds(userID);
                                if (ignoredUsers.Length > 0)
                                {
                                    var ignoredPacket = new HabboPacketBuilder(HabboPackets.IGNORED_USERS)
                                        .AppendVL64(ignoredUsers.Length);
                                    for (int x = 0; x < ignoredUsers.Length; x++)
                                    {
                                        ignoreList.Add(ignoredUsers[x]);
                                        ignoredPacket.Append(ignoredUsers[x]).Separator();
                                    }
                                    sendData(ignoredPacket.Build());
                                }
                                break;
                            }

                        default:
                            Disconnect();
                            break;
                    }
                }
                #endregion
                else
                #region Logged-in packet processing
                {
                    // Handle common packets first
                    switch (currentPacket.Substring(0, 2))
                    {
                        case "CD": // Client - response to @r ping
                            pingOK = true;
                            return;

                        case "@q": // Client - request current date
                            sendData(new HabboPacketBuilder(HabboPackets.CURRENT_DATE).Append(DateTime.Today.ToShortDateString()).Build());
                            return;
                    }

                    // Route to partial class handlers - order matters for performance
                    // Most frequent packets should be checked first
                    if (processInRoomPackets(currentPacket)) return;
                    if (processRoomPackets(currentPacket)) return;
                    if (processMessengerPackets(currentPacket)) return;
                    if (processNavigatorPackets(currentPacket)) return;
                    if (processItemPackets(currentPacket)) return;
                    if (processLoginPackets(currentPacket)) return;
                    if (processTradingPackets(currentPacket)) return;
                    if (processGamePackets(currentPacket)) return;
                    if (processSoundmachinePackets(currentPacket)) return;
                    if (processModerationPackets(currentPacket)) return;
                }
                #endregion
            }
        }
        #endregion
    }
}
