using System;

using Holo.Data.Repositories.Furniture;
using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Partial class containing trading-related packet processing.
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes trading-related packets.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        /// <returns>True if the packet was handled, false otherwise.</returns>
        private bool processTradingPackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                #region Trading
                case "AG": // Trading - start
                    {
                        if (Room != null || roomUser != null || _tradePartnerRoomUID == -1)
                        {
                            if (Config.enableTrading == false) { sendData(new HabboPacketBuilder("BK").Append(stringManager.getString("trading_disabled")).Build()); return true; }

                            int partnerUID = int.Parse(currentPacket.Substring(2));
                            if (Room.containsUser(partnerUID))
                            {
                                virtualUser Partner = Room.getUser(partnerUID);
                                if (Partner.statusManager.containsStatus("trd"))
                                    return true;

                                this._tradePartnerRoomUID = partnerUID;
                                this.statusManager.addStatus("trd", "");
                                this.roomUser.Refresh();

                                Partner._tradePartnerRoomUID = this.roomUser.roomUID;
                                Partner.statusManager.addStatus("trd", "");
                                Partner.roomUser.Refresh();

                                this.refreshTradeBoxes();
                                Partner.refreshTradeBoxes();
                            }
                        }
                        break;
                    }

                case "AH": // Trading - offer item
                    {
                        if (Room != null && roomUser != null && _tradePartnerRoomUID != -1 && Room.containsUser(_tradePartnerRoomUID))
                        {
                            int itemID = int.Parse(currentPacket.Substring(2));
                            int templateID = FurnitureRepository.Instance.GetHandItemTemplateId(itemID, userID);
                            if (templateID == 0)
                                return true;



                            _tradeItems[_tradeItemCount] = itemID;
                            _tradeItemCount++;
                            virtualUser Partner = Room.getUser(_tradePartnerRoomUID);

                            this._tradeAccept = false;
                            Partner._tradeAccept = false;

                            this.refreshTradeBoxes();
                            Partner.refreshTradeBoxes();
                        }
                        break;
                    }

                case "AD": // Trading - decline trade
                    {
                        if (Room != null && roomUser != null && _tradePartnerRoomUID != -1 && Room.containsUser(_tradePartnerRoomUID))
                        {
                            virtualUser Partner = Room.getUser(_tradePartnerRoomUID);
                            this._tradeAccept = false;
                            Partner._tradeAccept = false;
                            this.refreshTradeBoxes();
                            Partner.refreshTradeBoxes();
                        }
                        break;
                    }

                case "AE": // Trading - accept trade (and, if both partners accept, swap items]
                    {
                        if (Room != null && roomUser != null && _tradePartnerRoomUID != -1 && Room.containsUser(_tradePartnerRoomUID))
                        {
                            virtualUser Partner = Room.getUser(_tradePartnerRoomUID);
                            this._tradeAccept = true;
                            this.refreshTradeBoxes();
                            Partner.refreshTradeBoxes();

                            if (Partner._tradeAccept)
                            {
                                for (int i = 0; i < _tradeItemCount; i++)
                                    if (_tradeItems[i] > 0)
                                        FurnitureRepository.Instance.TransferItem(this._tradeItems[i], Partner.userID);

                                for (int i = 0; i < Partner._tradeItemCount; i++)
                                    if (Partner._tradeItems[i] > 0)
                                        FurnitureRepository.Instance.TransferItem(Partner._tradeItems[i], this.userID);

                                abortTrade();
                            }
                        }
                        break;
                    }

                case "AF": // Trading - abort trade
                    {
                        if (Room != null && roomUser != null && _tradePartnerRoomUID != -1 && Room.containsUser(_tradePartnerRoomUID))
                        {
                            abortTrade();
                            refreshHand("update");
                        }
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
