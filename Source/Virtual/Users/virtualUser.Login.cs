using System;
using System.Text;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Users.Messenger;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Contains login-related packet handlers for the virtualUser class.
    /// Handles Login region packets (@L, @Z, @G, @H, B], Cd, C^, C_),
    /// Guide system region (Ej, Ek), and Purse region (Ai, BA).
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes login-related packets including initialization, guide system, and purse operations.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        /// <returns>True if the packet was handled, false otherwise.</returns>
        private bool processLoginPackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                #region Login
                case "@L": // Login - initialize messenger
                    Messenger = new Messenger.virtualMessenger(userID);
                    sendData(new HabboPacketBuilder(HabboPackets.FRIEND_LIST).Append(Messenger.friendList()).Build());
                    sendData(new HabboPacketBuilder(HabboPackets.FRIEND_REQUESTS).Append(Messenger.friendRequests()).Build());
                    break;

                case "@Z": // Login - initialize Club subscription status
                    refreshClub();
                    break;

                case "@G": // Login - initialize/refresh appearance
                    refreshAppearance(false, true, false);
                    break;

                case "@H": // Login - initialize/refresh valueables [credits, tickets, etc]
                    refreshValueables(true, true);
                    break;

                case "B]": // Login - initialize/refresh badges
                    refreshBadges();
                    break;

                case "Cd": // Login - initialize/refresh group status
                    refreshGroupStatus();
                    break;

                case "C^": // Recycler - receive recycler setup
                    sendData(new HabboPacketBuilder(HabboPackets.RECYCLER_SETUP).Append(recyclerManager.setupString).Build());
                    break;

                case "C_": // Recycler - receive recycler session status
                    sendData(new HabboPacketBuilder(HabboPackets.RECYCLER_SESSION).Append(recyclerManager.sessionString(userID)).Build());
                    break;
                #endregion

                #region Guide system

                case "Ej":
                    {
                        DB.runQuery("UPDATE users SET guideavailable = '1' WHERE id = '" + userID + "' LIMIT 1");
                        break;
                    }

                case "Ek":
                    {
                        DB.runQuery("UPDATE users SET guideavailable = '0' WHERE id = '" + userID + "' LIMIT 1");
                        break;
                    }

                #endregion

                #region Purse (voucher redeeming, transactions gametickets etc)

                case "Ai": // Buy game-tickets
                    {
                        string args = currentPacket.Substring(2);
                        int Amount = Encoding.decodeVL64(args.Substring(0, 3));
                        string Receiver = args.Substring(3);
                        int Ticketamount = 0;
                        int Price = 0;

                        if (Amount == 1) // Look how much tickets you want
                        {
                            Ticketamount = 2;
                            Price = 1;
                        }
                        else if (Amount == 2) // And again
                        {
                            Ticketamount = 20;
                            Price = 6;
                        }
                        else // Wrong parameter
                            return true;

                        if (Price > _Credits) // Enough credits?
                        {
                            sendData(HabboPackets.TRANSACTION_FAILED);
                            return true;
                        }

                        int ReceiverID = DB.runRead("SELECT id FROM users WHERE name = '" + DB.Stripslash(Receiver) + "'", null);
                        if (!(ReceiverID > 0)) // Does the user exist?
                        {
                            sendData(new HabboPacketBuilder(HabboPackets.USER_NOT_FOUND).Append(Receiver).Build());
                            return true;
                        }

                        _Credits -= Price; // New credit amount
                        sendData(HabboPacketBuilder.CreditsUpdate(_Credits));
                        DB.runQuery("UPDATE users SET credits = '" + _Credits + "' WHERE id = '" + userID + "' LIMIT 1");
                        DB.runQuery("UPDATE users SET tickets = tickets+" + Ticketamount + " WHERE id = '" + ReceiverID + "' LIMIT 1");

                        if (userManager.containsUser(ReceiverID)) // Check or the user is online
                        {
                            virtualUser _Receiver = userManager.getUser(ReceiverID); // Get him/her
                            _Receiver._Tickets = _Receiver._Tickets + Ticketamount; // Update ticketamount

                            if (ReceiverID == userID)
                                _Receiver.refreshValueables(false, true);
                            else
                                _Receiver.refreshValueables(true, true);
                        }


                        break;
                    }

                case "BA": // Purse - redeem credit voucher
                    {
                        string Code = DB.Stripslash(currentPacket.Substring(4));
                        if (DB.checkExists("SELECT voucher FROM vouchers WHERE voucher = '" + Code + "'"))
                        {
                            int voucherAmount = DB.runRead("SELECT credits FROM vouchers WHERE voucher = '" + Code + "'", null);
                            DB.runQuery("DELETE FROM vouchers WHERE voucher = '" + Code + "' LIMIT 1");

                            _Credits += voucherAmount;
                            sendData(HabboPacketBuilder.CreditsUpdate(_Credits));
                            sendData(HabboPackets.VOUCHER_REDEEMED);
                            DB.runQuery("UPDATE users SET credits = '" + _Credits + "' WHERE id = '" + userID + "' LIMIT 1");
                        }
                        else
                            sendData(HabboPackets.VOUCHER_INVALID);
                        break;
                    }

                #endregion

                default:
                    return false; // Packet was not handled
            }
            return true; // Packet was handled
        }
    }
}
