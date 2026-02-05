using System;
using System.Text;
using System.Threading;

using Holo.Managers;

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
            Out.WriteSpecialLine(currentPacket.Replace(Convert.ToChar(13).ToString(), "{13}"), Out.logFlags.MehAction, ConsoleColor.DarkGray, ConsoleColor.DarkYellow, "< [" + Thread.GetDomainID() + "]", 2, ConsoleColor.Blue);
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
                            sendData("DUIH");
                            break;

                        case "CJ":
                            sendData("DAQBHHIIKHJIPAHQAdd-MM-yyyy" + Convert.ToChar(2) + "SAHPBhotel-co.uk" + Convert.ToChar(2) + "QBH");
                            break;

                        case "_R":
                           sendData("DA" + "QBHIIIKHJIPAIQAdd-MM-yyyy" + Convert.ToChar(2) + "SAHPB/client" + Convert.ToChar(2) + "QBH" + "IJWVVVSNKQCFUBJASMSLKUUOJCOLJQPNSBIRSVQBRXZQOTGPMNJIHLVJCRRULBLUO" + Convert.ToChar(1)); // V25+ SSO LOGIN BY vista4life
                           break;

                        case "CL":
                            {
                                string ssoTicket = DB.Stripslash(currentPacket.Substring(4));
                                int myID = DB.runRead("SELECT id FROM users WHERE ticket_sso = '" + ssoTicket + "'", null);
                                if (myID == 0) // No user found for this sso ticket and/or IP address
                                {
                                    Disconnect();
                                    return;
                                }

                                string banReason = userManager.getBanReason(myID);
                                if (banReason != "")
                                {
                                    sendData("@c" + banReason);
                                    Disconnect(1000);
                                    return;
                                }
                                this.userID = myID;
                                string[] userData = DB.runReadRow("SELECT name,figure,sex,mission,rank,consolemission FROM users WHERE id = '" + myID + "'");
                                _Username = userData[0];
                                _Figure = userData[1];
                                _Sex = char.Parse(userData[2]);
                                _Mission = userData[3];
                                _Rank = byte.Parse(userData[4]);
                                _consoleMission = userData[5];
                                userManager.addUser(myID, this);
                                _isLoggedIn = true;

                                sendData("@B" + rankManager.fuseRights(_Rank));
                                sendData("DbIH");
                                sendData("@C");

                                int isguide = DB.runRead("SELECT guide FROM users WHERE id = '" + userID + "'", null);

                                if (isguide == 1)
                                    sendData("BKguide");
                                sendData("Fi" + "I");
                                sendData("FC");

                                if (Config.enableWelcomeMessage)
                                    sendData("BK" + stringManager.getString("welcomemessage_text"));

                                //Send list of ignored users
                                int[] ignoredUsers = DB.runReadColumn("SELECT targetid FROM user_ignores WHERE userid = '" + userID + "'", 0, null);
                                if (ignoredUsers.Length > 0)
                                {
                                    StringBuilder sb = new StringBuilder("Fd" + Encoding.encodeVL64(ignoredUsers.Length));
                                    for (int x = 0; x < ignoredUsers.Length; x++)
                                    {
                                        ignoreList.Add(ignoredUsers[x]);
                                        sb.Append(ignoredUsers[x] + Convert.ToChar(2));
                                    }
                                    sendData(sb.ToString());
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
                            sendData("Bc" + DateTime.Today.ToShortDateString());
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
