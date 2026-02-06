using System;
using System.Text;

using Holo.Data.Repositories.Catalogue;
using Holo.Data.Repositories.Furniture;
using Holo.Data.Repositories.Rooms;
using Holo.Data.Repositories.Users;
using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms;
using Holo.Virtual.Rooms.Items;

namespace Holo.Virtual.Users
{
    /// <summary>
    /// Partial class for virtualUser containing catalogue, recycler, hand, and item handling packet processing.
    /// </summary>
    public partial class virtualUser
    {
        /// <summary>
        /// Processes item-related packets including catalogue, recycler, hand, and item handling.
        /// </summary>
        /// <param name="currentPacket">The packet to process.</param>
        private bool processItemPackets(string currentPacket)
        {
            switch (currentPacket.Substring(0, 2))
            {
                #region Catalogue and Recycler
                case "Ae": // Catalogue - open, retrieve index of pages
                    {
                        sendData(new HabboPacketBuilder("A~").Append(catalogueManager.getPageIndex(_Rank)).Build());

                        break;
                    }

                case "Af": // Catalogue, open page, get page content
                    {
                        {
                            string pageIndexName = currentPacket.Split('/')[1];
                            sendData(new HabboPacketBuilder("A").Append(catalogueManager.getPage(pageIndexName, _Rank)).Build());
                        }
                        break;
                    }

                case "Ad": // Catalogue - purchase
                    {
                        string[] packetContent = currentPacket.Split(Convert.ToChar(13));
                        string Page = packetContent[1];
                        string Item = packetContent[3];
                        int pageID = CatalogueRepository.Instance.GetPageIdByIndexName(Page, _Rank);
                        int templateID = CatalogueRepository.Instance.GetTemplateIdByCct(Item);
                        int Cost = CatalogueRepository.Instance.GetItemCostByPageAndTemplate(pageID, templateID);
                        if (Cost == 0 || Cost > _Credits) { sendData("AD"); return true; }

                        int receiverID = userID;
                        int presentBoxID = 0;
                        int roomID = 0; // -1 = present box, 0 = inhand

                        if (packetContent[5] == "1") // Purchased as present
                        {
                            string receiverName = packetContent[6];
                            if (receiverName != _Username)
                            {
                                int i = UserRepository.Instance.GetUserId(receiverName);
                                if (i > 0)
                                    receiverID = i;
                                else
                                {
                                    sendData(new HabboPacketBuilder("AL").Append(receiverName).Build());
                                    return true;
                                }
                            }

                            string boxSprite = "present_gen" + new Random().Next(1, 7);
                            int boxTemplateID = CatalogueRepository.Instance.GetTemplateIdByCct(boxSprite);
                            string boxNote = stringManager.filterSwearwords(packetContent[7]);
                            presentBoxID = (int)FurnitureRepository.Instance.CreateItemWithVar(boxTemplateID, receiverID, "!" + boxNote);
                            roomID = -1;
                        }

                        _Credits -= Cost;
                        sendData(new HabboPacketBuilder("@F").Append(_Credits).Build());
                        UserRepository.Instance.UpdateCredits(userID, _Credits);

                        if (stringManager.getStringPart(Item, 0, 4) == "deal")
                        {
                            int dealID = int.Parse(Item.Substring(4));
                            int[] itemIDs = CatalogueRepository.Instance.GetDealItemIds(dealID);
                            int[] itemAmounts = CatalogueRepository.Instance.GetDealItemAmounts(dealID);

                            for (int i = 0; i < itemIDs.Length; i++)
                                for (int j = 1; j <= itemAmounts[i]; j++)
                                {
                                    FurnitureRepository.Instance.CreateItem(itemIDs[i], receiverID, roomID);
                                    catalogueManager.handlePurchase(itemIDs[i], receiverID, roomID, 0, presentBoxID);
                                }
                        }
                        else
                        {
                            FurnitureRepository.Instance.CreateItem(templateID, receiverID, roomID);
                            if (catalogueManager.getTemplate(templateID).Sprite == "wallpaper" || catalogueManager.getTemplate(templateID).Sprite == "floor" || catalogueManager.getTemplate(templateID).Sprite.Contains("landscape"))
                            {
                                string decorID = packetContent[4];
                                catalogueManager.handlePurchase(templateID, receiverID, 0, decorID, presentBoxID);
                            }
                            else if (stringManager.getStringPart(Item, 0, 11) == "prizetrophy")
                            {
                                string Inscription = stringManager.filterSwearwords(packetContent[4]);
                                //string itemVariable = _Username + Convert.ToChar(9) + DateTime.Today.ToShortDateString() + Convert.ToChar(9) + Inscription;
                                string itemVariable = _Username + "\t" + DateTime.Today.ToShortDateString().Replace('/', '-') + "\t" + packetContent[4];
                                FurnitureRepository.Instance.UpdateVar(catalogueManager.lastItemID, itemVariable);
                                //"H" + GIVERNAME + [09] + GIVEDATE + [09] + MSG
                                catalogueManager.handlePurchase(templateID, receiverID, 0, "0", presentBoxID);
                            }
                            else if (stringManager.getStringPart(Item, 0, 11) == "greektrophy")
                            {
                                string Inscription = stringManager.filterSwearwords(packetContent[4]);
                                //string itemVariable = _Username + Convert.ToChar(9) + DateTime.Today.ToShortDateString() + Convert.ToChar(9) + Inscription;
                                string itemVariable = _Username + "\t" + DateTime.Today.ToShortDateString().Replace('/', '-') + "\t" + packetContent[4];
                                FurnitureRepository.Instance.UpdateVar(catalogueManager.lastItemID, itemVariable);
                                //"H" + GIVERNAME + [09] + GIVEDATE + [09] + MSG
                                catalogueManager.handlePurchase(templateID, receiverID, 0, "0", presentBoxID);
                            }
                            else
                                catalogueManager.handlePurchase(templateID, receiverID, roomID, "0", presentBoxID);
                        }

                        if (receiverID == userID)
                            refreshHand("last");
                        else
                            if (userManager.containsUser(receiverID)) { userManager.getUser(receiverID).refreshHand("last"); }
                        //if (presentBoxID > 0)
                        //Out.WriteLine(_Username + " Buy a present.");
                        break;
                    }

                case "Ca": // Recycler - proceed input items
                    {
                        if (Config.enableRecycler == false || Room == null || recyclerManager.sessionExists(userID))
                            return true;

                        int itemCount = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (recyclerManager.rewardExists(itemCount))
                        {
                            recyclerManager.createSession(userID, itemCount);
                            currentPacket = currentPacket.Substring(Encoding.encodeVL64(itemCount).Length + 2);
                            for (int i = 0; i < itemCount; i++)
                            {
                                int itemID = Encoding.decodeVL64(currentPacket);
                                if (FurnitureRepository.Instance.HasItemInHand(userID))
                                {
                                    FurnitureRepository.Instance.MoveItemToRecycler(itemID);
                                    currentPacket = currentPacket.Substring(Encoding.encodeVL64(itemID).Length);
                                }
                                else
                                {
                                    recyclerManager.dropSession(userID, true);
                                    sendData("DpH");
                                    return true;
                                }

                            }

                            sendData(new HabboPacketBuilder("Dp").Append(recyclerManager.sessionString(userID)).Build());
                            refreshHand("update");
                        }

                        break;
                    }

                case "Cb": // Recycler - redeem/cancel session
                    {
                        if (Config.enableRecycler == false || Room != null && recyclerManager.sessionExists(userID))
                        {
                            bool Redeem = (currentPacket.Substring(2) == "I");
                            if (Redeem && recyclerManager.sessionReady(userID))
                                recyclerManager.rewardSession(userID);
                            recyclerManager.dropSession(userID, Redeem);

                            sendData(new HabboPacketBuilder("Dp").Append(recyclerManager.sessionString(userID)).Build());
                            if (Redeem)
                                refreshHand("last");
                            else
                                refreshHand("new");
                        }
                        break;
                    }
                #endregion

                #region Hand and item handling
                case "AA": // Hand
                    {
                        if (Room == null || roomUser == null)
                            return true;

                        string Mode = currentPacket.Substring(2);
                        refreshHand(Mode);
                        break;
                    }

                case "LB": // Hand
                    {
                        if (Room == null || roomUser == null)
                            return true;

                        string Mode = currentPacket.Substring(2);
                        refreshHand(Mode);
                        break;
                    }

                case "AB": // Item handling - apply wallpaper/floor/landscape to room
                    {
                        if (_hasRights == false || _inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = int.Parse(currentPacket.Split('/')[1]);
                        string decorType = currentPacket.Substring(2).Split('/')[0];
                        if (decorType != "wallpaper" && decorType != "floor" && decorType != "landscape")
                            return true;

                        int templateID = FurnitureRepository.Instance.GetHandItemTemplateId(itemID, userID);
                        if (catalogueManager.getTemplate(templateID).Sprite != decorType)
                            return true;

                        string? decorVal = FurnitureRepository.Instance.GetVar(itemID);
                        if (decorType == "wallpaper")
                            RoomRepository.Instance.UpdateWallpaper(_roomID, decorVal ?? "");
                        else if (decorType == "floor")
                            RoomRepository.Instance.UpdateFloor(_roomID, decorVal ?? "");
                        else if (decorType == "landscape")
                            RoomRepository.Instance.UpdateLandscape(_roomID, decorVal ?? "");
                        Room.sendData(new HabboPacketBuilder("@n").Append(decorType).Append("/").Append(decorVal ?? "").Build());

                        FurnitureRepository.Instance.DeleteItem(itemID);
                    }
                    break;

                case "AZ": // Item handling - place item down
                    {
                        if (_hasRights == false || _inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = int.Parse(currentPacket.Split(' ')[0].Substring(2));
                        int templateID = FurnitureRepository.Instance.GetHandItemTemplateId(itemID, userID);
                        if (templateID == 0)
                            return true;

                        if (catalogueManager.getTemplate(templateID).typeID == 0)
                        {
                            string _INPUTPOS = currentPacket.Substring(itemID.ToString().Length + 3);
                            string _CHECKEDPOS = catalogueManager.wallPositionOK(_INPUTPOS);
                            if (_CHECKEDPOS != _INPUTPOS)
                                return true;

                            string? Var = FurnitureRepository.Instance.GetVar(itemID);
                            if (stringManager.getStringPart(catalogueManager.getTemplate(templateID).Sprite, 0, 7) == "post.it")
                            {
                                if (int.Parse(Var ?? "0") > 1)
                                    FurnitureRepository.Instance.DecrementPostItStack(itemID);
                                else
                                    FurnitureRepository.Instance.DeleteItem(itemID);
                                itemID = (int)FurnitureRepository.Instance.CreateItem(templateID, userID);
                                FurnitureExtrasRepository.Instance.CreateSticky(itemID);
                                Var = "FFFF33";
                                FurnitureRepository.Instance.UpdateVar(itemID, Var);
                            }
                            Room.wallItemManager.addItem(itemID, templateID, _CHECKEDPOS, Var ?? "", true);
                        }
                        else
                        {
                            string[] locDetails = currentPacket.Split(' ');
                            int X = int.Parse(locDetails[1]);
                            int Y = int.Parse(locDetails[2]);
                            byte Z = byte.Parse(locDetails[3]);
                            byte typeID = catalogueManager.getTemplate(templateID).typeID;
                            Room.floorItemManager.placeItem(itemID, templateID, X, Y, typeID, Z);
                        }
                        break;
                    }

                case "AC": // Item handling - pickup item
                    {
                        if (_isOwner == false || _inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = int.Parse(currentPacket.Split(' ')[2]);
                        if (Room.floorItemManager.containsItem(itemID))
                            Room.floorItemManager.removeItem(itemID, userID);
                        else if (Room.wallItemManager.containsItem(itemID) && stringManager.getStringPart(Room.wallItemManager.getItem(itemID).Sprite, 0, 7) != "post.it") // Can't pickup stickies from room
                            Room.wallItemManager.removeItem(itemID, userID);
                        else
                            return true;

                        refreshHand("update");
                        break;
                    }

                case "AI": // Item handling - move/rotate item
                    {
                        if (_hasRights == false || _inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = int.Parse(currentPacket.Split(' ')[0].Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            string[] locDetails = currentPacket.Split(' ');
                            int X = int.Parse(locDetails[1]);
                            int Y = int.Parse(locDetails[2]);
                            byte Z = byte.Parse(locDetails[3]);

                            Room.floorItemManager.relocateItem(itemID, X, Y, Z);
                        }
                        break;
                    }

                case "CV": // Item handling - toggle wallitem status
                    {
                        if (_inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(4, Encoding.decodeB64(currentPacket.Substring(2, 2))));
                        int toStatus = Encoding.decodeVL64(currentPacket.Substring(itemID.ToString().Length + 4));
                        Room.wallItemManager.toggleItemStatus(itemID, toStatus);
                        break;
                    }

                case "AJ": // Item handling - toggle flooritem status
                    {
                        try
                        {
                            int itemID = int.Parse(currentPacket.Substring(4, Encoding.decodeB64(currentPacket.Substring(2, 2))));
                            string toStatus = DB.Stripslash(currentPacket.Substring(itemID.ToString().Length + 6));
                            Room.floorItemManager.toggleItemStatus(itemID, toStatus, _hasRights);
                        }
                        catch { }
                        break;
                    }

                case "AN": // Item handling - open presentbox
                    {
                        if (_isOwner == false || _inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID) == false)
                            return true;

                        int[] itemIDs = FurnitureExtrasRepository.Instance.GetPresentItems(itemID);
                        if (itemIDs.Length > 0)
                        {
                            for (int i = 0; i < itemIDs.Length; i++)
                                FurnitureRepository.Instance.MoveItemToHand(itemIDs[i]);
                            Room.floorItemManager.removeItem(itemID, 0);

                            int lastItemTID = FurnitureRepository.Instance.GetTemplateId(itemIDs[itemIDs.Length - 1]);
                            catalogueManager.itemTemplate Template = catalogueManager.getTemplate(lastItemTID);

                            if (Template.typeID > 0)
                                sendData(new HabboPacketBuilder("BA")
                                    .Append(Template.Sprite).RecordSeparator()
                                    .Append(Template.Sprite).RecordSeparator()
                                    .Append(Template.Length).Append(Convert.ToChar(30))
                                    .Append(Template.Width).Append(Convert.ToChar(30))
                                    .Append(Template.Colour)
                                    .Build());
                            else
                                sendData(new HabboPacketBuilder("BA")
                                    .Append(Template.Sprite).RecordSeparator()
                                    .Append(Template.Sprite).Append(" ").Append(Template.Colour).RecordSeparator()
                                    .Build());
                        }
                        FurnitureExtrasRepository.Instance.DeletePresent(itemID);
                        FurnitureRepository.Instance.DeleteItem(itemID);
                        refreshHand("last");
                        break;
                    }

                case "Bw": // Item handling - redeem credit item
                    {
                        if (_isOwner == false || _inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            string Sprite = Room.floorItemManager.getItem(itemID).Sprite;
                            if (Sprite.Substring(0, 3).ToLower() != "cf_" && Sprite.Substring(0, 4).ToLower() != "cfc_")
                                return true;
                            int redeemValue = 0;
                            try { redeemValue = int.Parse(Sprite.Split('_')[1]); }
                            catch { return true; }

                            Room.floorItemManager.removeItem(itemID, 0);

                            _Credits += redeemValue;
                            sendData(new HabboPacketBuilder("@F").Append(_Credits).Build());
                            UserRepository.Instance.UpdateCredits(userID, _Credits);
                        }
                        break;
                    }

                case "AQ": // Item handling - teleporters - enter teleporter
                    {
                        if (_inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            Rooms.Items.floorItem Teleporter = Room.floorItemManager.getItem(itemID);
                            // Prevent clientside 'jumps' to teleporter, check if user is removed one coord from teleporter entrance
                            if (Teleporter.Z == 2 && roomUser.X != Teleporter.X + 1 && roomUser.Y != Teleporter.Y)
                                return true;
                            else if (Teleporter.Z == 4 && roomUser.X != Teleporter.X && roomUser.Y != Teleporter.Y + 1)
                                return true;
                            roomUser.goalX = -1;
                            Room.moveUser(this.roomUser, Teleporter.X, Teleporter.Y, true);
                        }
                        break;
                    }

                case @"@\": // Item handling - teleporters - flash teleporter
                    {
                        if (_inPublicroom || Room == null || roomUser == null)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            Rooms.Items.floorItem Teleporter1 = Room.floorItemManager.getItem(itemID);
                            if (roomUser.X != Teleporter1.X && roomUser.Y != Teleporter1.Y)
                                return true;

                            int idTeleporter2 = FurnitureRepository.Instance.GetTeleporterId(itemID);
                            int roomIDTeleporter2 = FurnitureRepository.Instance.GetItemRoomId(idTeleporter2);
                            if (roomIDTeleporter2 > 0)
                                _ = UseTeleporterAsync(Teleporter1, idTeleporter2, roomIDTeleporter2);
                        }
                        break;
                    }

                case "AM": // Item handling - dices - close dice
                    {
                        if (Room == null || roomUser == null || _inPublicroom)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            Rooms.Items.floorItem Item = Room.floorItemManager.getItem(itemID);
                            string Sprite = Item.Sprite;
                            if (Sprite != "edice" && Sprite != "edicehc") // Not a dice item
                                return true;

                            if (!(Math.Abs(roomUser.X - Item.X) > 1 || Math.Abs(roomUser.Y - Item.Y) > 1)) // User is not more than one square removed from dice
                            {
                                Item.Var = "0";
                                Room.sendData(new HabboPacketBuilder("AZ").Append(itemID).Append(" ").Append(itemID * 38).Build());
                                FurnitureRepository.Instance.UpdateVar(itemID, "0");
                            }
                        }
                        break;
                    }

                case "AL": // Item handling - dices - spin dice
                    {
                        if (Room == null || roomUser == null || _inPublicroom)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            Rooms.Items.floorItem Item = Room.floorItemManager.getItem(itemID);
                            string Sprite = Item.Sprite;
                            if (Sprite != "edice" && Sprite != "edicehc") // Not a dice item
                                return true;

                            if (!(Math.Abs(roomUser.X - Item.X) > 1 || Math.Abs(roomUser.Y - Item.Y) > 1)) // User is not more than one square removed from dice
                            {
                                Room.sendData(new HabboPacketBuilder("AZ").Append(itemID).Build());

                                int rndNum = new Random(DateTime.Now.Millisecond).Next(1, 7);
                                Room.sendData(new HabboPacketBuilder("AZ").Append(itemID).Append(" ").Append((itemID * 38) + rndNum).Build(), 2000);
                                Item.Var = rndNum.ToString();
                                FurnitureRepository.Instance.UpdateVar(itemID, rndNum.ToString());
                            }
                        }
                        break;
                    }

                case "Cw": // Item handling - spin Wheel of fortune
                    {
                        if (_hasRights == false || Room == null || roomUser == null || _inPublicroom)
                            return true;

                        int itemID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (Room.wallItemManager.containsItem(itemID))
                        {
                            Rooms.Items.wallItem Item = Room.wallItemManager.getItem(itemID);
                            if (Item.Sprite == "habbowheel")
                            {
                                int rndNum = new Random(DateTime.Now.Millisecond).Next(0, 10);
                                Room.sendData(new HabboPacketBuilder("AU")
                                    .Append(itemID).TabSeparator()
                                    .Append("habbowheel").TabSeparator()
                                    .Append(" ").Append(Item.wallPosition).TabSeparator()
                                    .Append("-1").Build());
                                Room.sendData(new HabboPacketBuilder("AU")
                                    .Append(itemID).TabSeparator()
                                    .Append("habbowheel").TabSeparator()
                                    .Append(" ").Append(Item.wallPosition).TabSeparator()
                                    .Append(rndNum).Build(), 4250);
                                FurnitureRepository.Instance.UpdateVar(itemID, rndNum.ToString());
                            }
                        }
                        break;
                    }

                case "Dz": // Item handling - activate Love shuffler sofa
                    {
                        string Message = currentPacket.Substring(4);
                        if (Room == null || roomUser == null || _inPublicroom)
                            return true;

                        int itemID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID) && Room.floorItemManager.getItem(itemID).Sprite == "val_randomizer")
                        {
                            int rndNum = new Random(DateTime.Now.Millisecond).Next(1, 5);
                            Room.sendData(new HabboPacketBuilder("AX")
                                .Append(itemID).Separator()
                                .Append("123456789").Separator()
                                .Build());
                            Room.sendData(new HabboPacketBuilder("AX")
                                .Append(itemID).Separator()
                                .Append(rndNum).Separator()
                                .Build(), 5000);
                            FurnitureRepository.Instance.UpdateVar(itemID, rndNum.ToString());
                        }
                        break;
                    }

                case "AS": // Item handling - stickies/photo's - open stickie/photo
                    {
                        string Message = currentPacket.Substring(4);
                        if (Room == null || roomUser == null || _inPublicroom)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.wallItemManager.containsItem(itemID))
                        {
                            Message = FurnitureExtrasRepository.Instance.GetStickyText(itemID) ?? "";
                            string? Colour = FurnitureRepository.Instance.GetVar(itemID);
                            sendData(new HabboPacketBuilder("@p")
                                .Append(itemID).TabSeparator()
                                .Append(Colour ?? "").Append(" ").Append(Message)
                                .Build());
                        }
                        break;
                    }

                case "AT": // Item handling - stickies - edit stickie colour/message
                    {
                        if (_hasRights == false || Room == null || roomUser == null || _inPublicroom)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(2, currentPacket.IndexOf("/") - 2));
                        if (Room.wallItemManager.containsItem(itemID))
                        {
                            Rooms.Items.wallItem Item = Room.wallItemManager.getItem(itemID);
                            string Sprite = Item.Sprite;
                            if (Sprite != "post.it" && Sprite != "post.it.vd")
                                return true;
                            string Colour = "FFFFFF"; // Valentine stickie default colour
                            if (Sprite == "post.it") // Normal stickie
                            {
                                Colour = currentPacket.Substring(2 + itemID.ToString().Length + 1, 6);
                                if (Colour != "FFFF33" && Colour != "FF9CFF" && Colour != "9CFF9C" && Colour != "9CCEFF")
                                    return true;
                            }

                            string Message = currentPacket.Substring(2 + itemID.ToString().Length + 7);
                            if (Message.Length > 684)
                                return true;
                            if (Colour != Item.Var)
                                FurnitureRepository.Instance.UpdateVar(itemID, Colour);
                            Item.Var = Colour;
                            Room.sendData(new HabboPacketBuilder("AU")
                                .Append(itemID).TabSeparator()
                                .Append(Sprite).TabSeparator()
                                .Append(" ").Append(Item.wallPosition).TabSeparator()
                                .Append(Colour)
                                .Build());

                            Message = stringManager.filterSwearwords(Message).Replace("/r", Convert.ToChar(13).ToString());
                            FurnitureExtrasRepository.Instance.UpdateStickyText(itemID, Message);
                        }
                        break;
                    }

                case "AU": // Item handling - stickies/photo - delete stickie/photo
                    {
                        if (_isOwner == false || Room == null || roomUser == null || _inPublicroom)
                            return true;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.wallItemManager.containsItem(itemID) && stringManager.getStringPart(Room.wallItemManager.getItem(itemID).Sprite, 0, 7) == "post.it")
                        {
                            Room.wallItemManager.removeItem(itemID, 0);
                            FurnitureExtrasRepository.Instance.DeleteSticky(itemID);
                        }
                        break;
                    }

                case "Ah": // Statuses - Lido Voting
                    {
                        if (Room != null && roomUser != null && statusManager.containsStatus("sit") == false && statusManager.containsStatus("lay") == false)
                        {
                            if (currentPacket.Length == 2)
                                statusManager.addStatus("sign", "");
                            else
                            {
                                string signID = currentPacket.Substring(2);
                                statusManager.handleStatus("sign", signID, Config.Statuses_Wave_waveDuration);
                            }

                            statusManager.Refresh();
                        }
                        break;
                    }
                #endregion

                default:
                    return false;
            }
            return true;
        }
    }
}
