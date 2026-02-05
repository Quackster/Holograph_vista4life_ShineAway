using System;
using System.Text;

using Holo.Managers;
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
                        sendData("A~" + catalogueManager.getPageIndex(_Rank));

                        break;
                    }

                case "Af": // Catalogue, open page, get page content
                    {
                        {
                            string pageIndexName = currentPacket.Split('/')[1];
                            sendData("A" + catalogueManager.getPage(pageIndexName, _Rank));
                        }
                        break;
                    }

                case "Ad": // Catalogue - purchase
                    {
                        string[] packetContent = currentPacket.Split(Convert.ToChar(13));
                        string Page = packetContent[1];
                        string Item = packetContent[3];
                        int pageID = DB.runRead("SELECT indexid FROM catalogue_pages WHERE indexname = '" + DB.Stripslash(Page) + "' AND minrank <= " + _Rank, null);
                        int templateID = DB.runRead("SELECT tid FROM catalogue_items WHERE name_cct = '" + DB.Stripslash(Item) + "'", null);
                        int Cost = DB.runRead("SELECT catalogue_cost FROM catalogue_items WHERE catalogue_id_page = '" + pageID + "' AND tid = '" + templateID + "'", null);
                        if (Cost == 0 || Cost > _Credits) { sendData("AD"); return; }

                        int receiverID = userID;
                        int presentBoxID = 0;
                        int roomID = 0; // -1 = present box, 0 = inhand

                        if (packetContent[5] == "1") // Purchased as present
                        {
                            string receiverName = packetContent[6];
                            if (receiverName != _Username)
                            {
                                int i = DB.runRead("SELECT id FROM users WHERE name = '" + DB.Stripslash(receiverName) + "'", null);
                                if (i > 0)
                                    receiverID = i;
                                else
                                {
                                    sendData("AL" + receiverName);
                                    return;
                                }
                            }

                            string boxSprite = "present_gen" + new Random().Next(1, 7);
                            string boxTemplateID = DB.runRead("SELECT tid FROM catalogue_items WHERE name_cct = '" + boxSprite + "'");
                            string boxNote = DB.Stripslash(stringManager.filterSwearwords(packetContent[7]));
                            DB.runQuery("INSERT INTO furniture(tid,ownerid,var) VALUES ('" + boxTemplateID + "','" + receiverID + "','!" + boxNote + "')");
                            presentBoxID = catalogueManager.lastItemID;
                            roomID = -1;
                        }

                        _Credits -= Cost;
                        sendData("@F" + _Credits);
                        DB.runQuery("UPDATE users SET credits = '" + _Credits + "' WHERE id = '" + userID + "' LIMIT 1");

                        if (stringManager.getStringPart(Item, 0, 4) == "deal")
                        {
                            int dealID = int.Parse(Item.Substring(4));
                            int[] itemIDs = DB.runReadColumn("SELECT tid FROM catalogue_deals WHERE id = '" + dealID + "'", 0, null);
                            int[] itemAmounts = DB.runReadColumn("SELECT amount FROM catalogue_deals WHERE id = '" + dealID + "'", 0, null);

                            for (int i = 0; i < itemIDs.Length; i++)
                                for (int j = 1; j <= itemAmounts[i]; j++)
                                {
                                    DB.runQuery("INSERT INTO furniture(tid,ownerid,roomid) VALUES ('" + itemIDs[i] + "','" + receiverID + "','" + roomID + "')");
                                    catalogueManager.handlePurchase(itemIDs[i], receiverID, roomID, 0, presentBoxID);
                                }
                        }
                        else
                        {
                            DB.runQuery("INSERT INTO furniture(tid,ownerid,roomid) VALUES ('" + templateID + "','" + receiverID + "','" + roomID + "')");
                            if (catalogueManager.getTemplate(templateID).Sprite == "wallpaper" || catalogueManager.getTemplate(templateID).Sprite == "floor" || catalogueManager.getTemplate(templateID).Sprite.Contains("landscape"))
                            {
                                string decorID = packetContent[4];
                                catalogueManager.handlePurchase(templateID, receiverID, 0, decorID, presentBoxID);
                            }
                            else if (stringManager.getStringPart(Item, 0, 11) == "prizetrophy")
                            {
                                string Inscription = DB.Stripslash(stringManager.filterSwearwords(packetContent[4]));
                                //string itemVariable = _Username + Convert.ToChar(9) + DateTime.Today.ToShortDateString() + Convert.ToChar(9) + Inscription;
                                string itemVariable = _Username + "\t" + DateTime.Today.ToShortDateString().Replace('/', '-') + "\t" + packetContent[4];
                                DB.runQuery("UPDATE furniture SET var = '" + itemVariable + "' WHERE id = '" + catalogueManager.lastItemID + "' LIMIT 1");
                                //"H" + GIVERNAME + [09] + GIVEDATE + [09] + MSG
                                catalogueManager.handlePurchase(templateID, receiverID, 0, "0", presentBoxID);
                            }
                            else if (stringManager.getStringPart(Item, 0, 11) == "greektrophy")
                            {
                                string Inscription = DB.Stripslash(stringManager.filterSwearwords(packetContent[4]));
                                //string itemVariable = _Username + Convert.ToChar(9) + DateTime.Today.ToShortDateString() + Convert.ToChar(9) + Inscription;
                                string itemVariable = _Username + "\t" + DateTime.Today.ToShortDateString().Replace('/', '-') + "\t" + packetContent[4];
                                DB.runQuery("UPDATE furniture SET var = '" + itemVariable + "' WHERE id = '" + catalogueManager.lastItemID + "' LIMIT 1");
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
                            return;

                        int itemCount = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (recyclerManager.rewardExists(itemCount))
                        {
                            recyclerManager.createSession(userID, itemCount);
                            currentPacket = currentPacket.Substring(Encoding.encodeVL64(itemCount).Length + 2);
                            for (int i = 0; i < itemCount; i++)
                            {
                                int itemID = Encoding.decodeVL64(currentPacket);
                                if (DB.checkExists("SELECT id FROM furniture WHERE ownerid = '" + userID + "' AND roomid = '0'"))
                                {
                                    DB.runQuery("UPDATE furniture SET roomid = '-2' WHERE id = '" + itemID + "' LIMIT 1");
                                    currentPacket = currentPacket.Substring(Encoding.encodeVL64(itemID).Length);
                                }
                                else
                                {
                                    recyclerManager.dropSession(userID, true);
                                    sendData("DpH");
                                    return;
                                }

                            }

                            sendData("Dp" + recyclerManager.sessionString(userID));
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

                            sendData("Dp" + recyclerManager.sessionString(userID));
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
                            return;

                        string Mode = currentPacket.Substring(2);
                        refreshHand(Mode);
                        break;
                    }

                case "LB": // Hand
                    {
                        if (Room == null || roomUser == null)
                            return;

                        string Mode = currentPacket.Substring(2);
                        refreshHand(Mode);
                        break;
                    }

                case "AB": // Item handling - apply wallpaper/floor/landscape to room
                    {
                        if (_hasRights == false || _inPublicroom || Room == null || roomUser == null)
                            return;

                        int itemID = int.Parse(currentPacket.Split('/')[1]);
                        string decorType = currentPacket.Substring(2).Split('/')[0];
                        if (decorType != "wallpaper" && decorType != "floor" && decorType != "landscape")
                            return;

                        int templateID = DB.runRead("SELECT tid FROM furniture WHERE id = '" + itemID + "' AND ownerid = '" + userID + "' AND roomid = '0'", null);
                        if (catalogueManager.getTemplate(templateID).Sprite != decorType)
                            return;

                        string decorVal = DB.runRead("SELECT var FROM furniture WHERE id = '" + itemID + "'");
                        DB.runQuery("UPDATE rooms SET " + decorType + " = '" + decorVal + "' WHERE id = '" + _roomID + "' LIMIT 1");
                        Room.sendData("@n" + decorType + "/" + decorVal);

                        DB.runQuery("DELETE FROM furniture WHERE id = '" + itemID + "' LIMIT 1");
                    }
                    break;

                case "AZ": // Item handling - place item down
                    {
                        if (_hasRights == false || _inPublicroom || Room == null || roomUser == null)
                            return;

                        int itemID = int.Parse(currentPacket.Split(' ')[0].Substring(2));
                        int templateID = DB.runRead("SELECT tid FROM furniture WHERE id = '" + itemID + "' AND ownerid = '" + userID + "' AND roomid = '0'", null);
                        if (templateID == 0)
                            return;

                        if (catalogueManager.getTemplate(templateID).typeID == 0)
                        {
                            string _INPUTPOS = currentPacket.Substring(itemID.ToString().Length + 3);
                            string _CHECKEDPOS = catalogueManager.wallPositionOK(_INPUTPOS);
                            if (_CHECKEDPOS != _INPUTPOS)
                                return;

                            string Var = DB.runRead("SELECT var FROM furniture WHERE id = '" + itemID + "'");
                            if (stringManager.getStringPart(catalogueManager.getTemplate(templateID).Sprite, 0, 7) == "post.it")
                            {
                                if (int.Parse(Var) > 1)
                                    DB.runQuery("UPDATE furniture SET var = var - 1 WHERE id = '" + itemID + "' LIMIT 1");
                                else
                                    DB.runQuery("DELETE FROM furniture WHERE id = '" + itemID + "' LIMIT 1");
                                DB.runQuery("INSERT INTO furniture(tid,ownerid) VALUES ('" + templateID + "','" + userID + "')");
                                itemID = catalogueManager.lastItemID;
                                DB.runQuery("INSERT INTO furniture_stickies(id) VALUES ('" + itemID + "')");
                                Var = "FFFF33";
                                DB.runQuery("UPDATE furniture SET var = '" + Var + "' WHERE id = '" + itemID + "' LIMIT 1");
                            }
                            Room.wallItemManager.addItem(itemID, templateID, _CHECKEDPOS, Var, true);
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
                            return;

                        int itemID = int.Parse(currentPacket.Split(' ')[2]);
                        if (Room.floorItemManager.containsItem(itemID))
                            Room.floorItemManager.removeItem(itemID, userID);
                        else if (Room.wallItemManager.containsItem(itemID) && stringManager.getStringPart(Room.wallItemManager.getItem(itemID).Sprite, 0, 7) != "post.it") // Can't pickup stickies from room
                            Room.wallItemManager.removeItem(itemID, userID);
                        else
                            return;

                        refreshHand("update");
                        break;
                    }

                case "AI": // Item handling - move/rotate item
                    {
                        if (_hasRights == false || _inPublicroom || Room == null || roomUser == null)
                            return;

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
                            return;

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
                            return;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID) == false)
                            return;

                        int[] itemIDs = DB.runReadColumn("SELECT itemid FROM furniture_presents WHERE id = '" + itemID + "'", 0, null);
                        if (itemIDs.Length > 0)
                        {
                            for (int i = 0; i < itemIDs.Length; i++)
                                DB.runQuery("UPDATE furniture SET roomid = '0' WHERE id = '" + itemIDs[i] + "' LIMIT 1");
                            Room.floorItemManager.removeItem(itemID, 0);

                            int lastItemTID = DB.runRead("SELECT tid FROM furniture WHERE id = '" + itemIDs[itemIDs.Length - 1] + "'", null);
                            catalogueManager.itemTemplate Template = catalogueManager.getTemplate(lastItemTID);

                            if (Template.typeID > 0)
                                sendData("BA" + Template.Sprite + Convert.ToChar(13) + Template.Sprite + Convert.ToChar(13) + Template.Length + Convert.ToChar(30) + Template.Width + Convert.ToChar(30) + Template.Colour);
                            else
                                sendData("BA" + Template.Sprite + Convert.ToChar(13) + Template.Sprite + " " + Template.Colour + Convert.ToChar(13));
                        }
                        DB.runQuery("DELETE FROM furniture_presents WHERE id = '" + itemID + "' LIMIT " + itemIDs.Length);
                        DB.runQuery("DELETE FROM furniture WHERE id = '" + itemID + "' LIMIT 1");
                        refreshHand("last");
                        break;
                    }

                case "Bw": // Item handling - redeem credit item
                    {
                        if (_isOwner == false || _inPublicroom || Room == null || roomUser == null)
                            return;

                        int itemID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            string Sprite = Room.floorItemManager.getItem(itemID).Sprite;
                            if (Sprite.Substring(0, 3).ToLower() != "cf_" && Sprite.Substring(0, 4).ToLower() != "cfc_")
                                return;
                            int redeemValue = 0;
                            try { redeemValue = int.Parse(Sprite.Split('_')[1]); }
                            catch { return; }

                            Room.floorItemManager.removeItem(itemID, 0);

                            _Credits += redeemValue;
                            sendData("@F" + _Credits);
                            DB.runQuery("UPDATE users SET credits = '" + _Credits + "' WHERE id = '" + userID + "' LIMIT 1");
                        }
                        break;
                    }

                case "AQ": // Item handling - teleporters - enter teleporter
                    {
                        if (_inPublicroom || Room == null || roomUser == null)
                            return;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            Rooms.Items.floorItem Teleporter = Room.floorItemManager.getItem(itemID);
                            // Prevent clientside 'jumps' to teleporter, check if user is removed one coord from teleporter entrance
                            if (Teleporter.Z == 2 && roomUser.X != Teleporter.X + 1 && roomUser.Y != Teleporter.Y)
                                return;
                            else if (Teleporter.Z == 4 && roomUser.X != Teleporter.X && roomUser.Y != Teleporter.Y + 1)
                                return;
                            roomUser.goalX = -1;
                            Room.moveUser(this.roomUser, Teleporter.X, Teleporter.Y, true);
                        }
                        break;
                    }

                case @"@\": // Item handling - teleporters - flash teleporter
                    {
                        if (_inPublicroom || Room == null || roomUser == null)
                            return;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            Rooms.Items.floorItem Teleporter1 = Room.floorItemManager.getItem(itemID);
                            if (roomUser.X != Teleporter1.X && roomUser.Y != Teleporter1.Y)
                                return;

                            int idTeleporter2 = DB.runRead("SELECT teleportid FROM furniture WHERE id = '" + itemID + "'", null);
                            int roomIDTeleporter2 = DB.runRead("SELECT roomid FROM furniture WHERE id = '" + idTeleporter2 + "'", null);
                            if (roomIDTeleporter2 > 0)
                                new TeleporterUsageSleep(useTeleporter).BeginInvoke(Teleporter1, idTeleporter2, roomIDTeleporter2, null, null);
                        }
                        break;
                    }

                case "AM": // Item handling - dices - close dice
                    {
                        if (Room == null || roomUser == null || _inPublicroom)
                            return;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            Rooms.Items.floorItem Item = Room.floorItemManager.getItem(itemID);
                            string Sprite = Item.Sprite;
                            if (Sprite != "edice" && Sprite != "edicehc") // Not a dice item
                                return;

                            if (!(Math.Abs(roomUser.X - Item.X) > 1 || Math.Abs(roomUser.Y - Item.Y) > 1)) // User is not more than one square removed from dice
                            {
                                Item.Var = "0";
                                Room.sendData("AZ" + itemID + " " + (itemID * 38));
                                DB.runQuery("UPDATE furniture SET var = '0' WHERE id = '" + itemID + "' LIMIT 1");
                            }
                        }
                        break;
                    }

                case "AL": // Item handling - dices - spin dice
                    {
                        if (Room == null || roomUser == null || _inPublicroom)
                            return;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID))
                        {
                            Rooms.Items.floorItem Item = Room.floorItemManager.getItem(itemID);
                            string Sprite = Item.Sprite;
                            if (Sprite != "edice" && Sprite != "edicehc") // Not a dice item
                                return;

                            if (!(Math.Abs(roomUser.X - Item.X) > 1 || Math.Abs(roomUser.Y - Item.Y) > 1)) // User is not more than one square removed from dice
                            {
                                Room.sendData("AZ" + itemID);

                                int rndNum = new Random(DateTime.Now.Millisecond).Next(1, 7);
                                Room.sendData("AZ" + itemID + " " + ((itemID * 38) + rndNum), 2000);
                                Item.Var = rndNum.ToString();
                                DB.runQuery("UPDATE furniture SET var = '" + rndNum + "' WHERE id = '" + itemID + "' LIMIT 1");
                            }
                        }
                        break;
                    }

                case "Cw": // Item handling - spin Wheel of fortune
                    {
                        if (_hasRights == false || Room == null || roomUser == null || _inPublicroom)
                            return;

                        int itemID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (Room.wallItemManager.containsItem(itemID))
                        {
                            Rooms.Items.wallItem Item = Room.wallItemManager.getItem(itemID);
                            if (Item.Sprite == "habbowheel")
                            {
                                int rndNum = new Random(DateTime.Now.Millisecond).Next(0, 10);
                                Room.sendData("AU" + itemID + Convert.ToChar(9) + "habbowheel" + Convert.ToChar(9) + " " + Item.wallPosition + Convert.ToChar(9) + "-1");
                                Room.sendData("AU" + itemID + Convert.ToChar(9) + "habbowheel" + Convert.ToChar(9) + " " + Item.wallPosition + Convert.ToChar(9) + rndNum, 4250);
                                DB.runQuery("UPDATE furniture SET var = '" + rndNum + "' WHERE id = '" + itemID + "' LIMIT 1");
                            }
                        }
                        break;
                    }

                case "Dz": // Item handling - activate Love shuffler sofa
                    {
                        string Message = currentPacket.Substring(4);
                        if (Room == null || roomUser == null || _inPublicroom)
                            return;

                        int itemID = Encoding.decodeVL64(currentPacket.Substring(2));
                        if (Room.floorItemManager.containsItem(itemID) && Room.floorItemManager.getItem(itemID).Sprite == "val_randomizer")
                        {
                            int rndNum = new Random(DateTime.Now.Millisecond).Next(1, 5);
                            Room.sendData("AX" + itemID + Convert.ToChar(2) + "123456789" + Convert.ToChar(2));
                            Room.sendData("AX" + itemID + Convert.ToChar(2) + rndNum + Convert.ToChar(2), 5000);
                            DB.runQuery("UPDATE furniture SET var = '" + rndNum + "' WHERE id = '" + itemID + "' LIMIT 1");
                        }
                        break;
                    }

                case "AS": // Item handling - stickies/photo's - open stickie/photo
                    {
                        string Message = currentPacket.Substring(4);
                        if (Room == null || roomUser == null || _inPublicroom)
                            return;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.wallItemManager.containsItem(itemID))
                        {
                            Message = DB.runRead("SELECT text FROM furniture_stickies WHERE id = '" + itemID + "'");
                            string Colour = DB.runRead("SELECT var FROM furniture WHERE id = '" + itemID + "'");
                            sendData("@p" + itemID + Convert.ToChar(9) + Colour + " " + Message);
                        }
                        break;
                    }

                case "AT": // Item handling - stickies - edit stickie colour/message
                    {
                        if (_hasRights == false || Room == null || roomUser == null || _inPublicroom)
                            return;

                        int itemID = int.Parse(currentPacket.Substring(2, currentPacket.IndexOf("/") - 2));
                        if (Room.wallItemManager.containsItem(itemID))
                        {
                            Rooms.Items.wallItem Item = Room.wallItemManager.getItem(itemID);
                            string Sprite = Item.Sprite;
                            if (Sprite != "post.it" && Sprite != "post.it.vd")
                                return;
                            string Colour = "FFFFFF"; // Valentine stickie default colour
                            if (Sprite == "post.it") // Normal stickie
                            {
                                Colour = currentPacket.Substring(2 + itemID.ToString().Length + 1, 6);
                                if (Colour != "FFFF33" && Colour != "FF9CFF" && Colour != "9CFF9C" && Colour != "9CCEFF")
                                    return;
                            }

                            string Message = currentPacket.Substring(2 + itemID.ToString().Length + 7);
                            if (Message.Length > 684)
                                return;
                            if (Colour != Item.Var)
                                DB.runQuery("UPDATE furniture SET var = '" + Colour + "' WHERE id = '" + itemID + "' LIMIT 1");
                            Item.Var = Colour;
                            Room.sendData("AU" + itemID + Convert.ToChar(9) + Sprite + Convert.ToChar(9) + " " + Item.wallPosition + Convert.ToChar(9) + Colour);

                            Message = DB.Stripslash(stringManager.filterSwearwords(Message)).Replace("/r", Convert.ToChar(13).ToString());
                            DB.runQuery("UPDATE furniture_stickies SET text = '" + Message + "' WHERE id = '" + itemID + "' LIMIT 1");
                        }
                        break;
                    }

                case "AU": // Item handling - stickies/photo - delete stickie/photo
                    {
                        if (_isOwner == false || Room == null || roomUser == null || _inPublicroom)
                            return;

                        int itemID = int.Parse(currentPacket.Substring(2));
                        if (Room.wallItemManager.containsItem(itemID) && stringManager.getStringPart(Room.wallItemManager.getItem(itemID).Sprite, 0, 7) == "post.it")
                        {
                            Room.wallItemManager.removeItem(itemID, 0);
                            DB.runQuery("DELETE FROM furniture_stickies WHERE id = '" + itemID + "' LIMIT 1");
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
