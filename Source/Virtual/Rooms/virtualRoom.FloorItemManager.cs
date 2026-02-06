using System;
using System.Text;
using System.Collections;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms.Items;

namespace Holo.Virtual.Rooms
{
    /// <summary>
    /// Partial class for virtualRoom containing the FloorItemManager nested class.
    /// </summary>
    public partial class virtualRoom
    {
        #region Item managers - FloorItemManager
        /// <summary>
        /// Provides management for virtual flooritems in a virtual room.
        /// </summary>
        internal class FloorItemManager
        {
            private virtualRoom _Room;
            private Hashtable _Items = new Hashtable();
            /// <summary>
            /// The database ID of the soundmachine in this FloorItemManager.
            /// </summary>
            internal int soundMachineID;
            /// <summary>
            /// Initializes the manager.
            /// </summary>
            /// <param name="Room">The parent room.</param>
            public FloorItemManager(virtualRoom Room)
            {
                this._Room = Room;
            }
            /// <summary>
            /// Removes all the items from the item manager and destructs all objects inside.
            /// </summary>
            internal void Clear()
            {
                try { _Items.Clear(); }
                catch { }
                _Room = null;
                _Items = null;
            }
            /// <summary>
            /// Adds a new virtual flooritem to the manager at initialization.
            /// </summary>
            /// <param name="itemID">The ID of the new item.</param>
            /// <param name="templateID">The template ID of the new item.</param>
            /// <param name="X">The X position of the new item.</param>
            /// <param name="Y">The Y position of the new item.</param>
            /// <param name="Z">The Z [rotation] of the new item.</param>
            /// <param name="H">The H position [height] of the new item.</param>
            /// <param name="Var">The variable of the new item.</param>
            internal void addItem(int itemID, int templateID, int X, int Y, int Z, double H, string Var)
            {
                catalogueManager.itemTemplate Template = catalogueManager.getTemplate(templateID);
                if (stringManager.getStringPart(Template.Sprite, 0, 13) == "sound_machine")
                    soundMachineID = itemID;

                int Length = 0;
                int Width = 0;
                if (Z == 2 || Z == 6)
                {
                    Length = Template.Length;
                    Width = Template.Width;
                }
                else
                {
                    Length = Template.Width;
                    Width = Template.Length;
                }

                for (int jX = X; jX < X + Width; jX++)
                    for (int jY = Y; jY < Y + Length; jY++)
                    {
                        furnitureStack Stack = _Room.sqSTACK[jX,jY];
                        if (Stack == null)
                        {
                            if (Template.typeID != 2 && Template.typeID != 3)
                            {
                                Stack = new furnitureStack();
                                Stack.Add(itemID);
                            }
                        }
                        else
                            Stack.Add(itemID);

                        _Room.sqSTATE[jX, jY] = (squareState)Template.typeID;
                        if (Template.typeID == 2 || Template.typeID == 3)
                        {
                            _Room.sqITEMHEIGHT[jX, jY] = H + Template.topH;
                            _Room.sqITEMROT[jX, jY] = Convert.ToByte(Z);
                        }
                        else
                        {
                            if (Template.typeID == 4)
                                _Room.sqITEMHEIGHT[jX, jY] = H;
                        }
                        _Room.sqSTACK[jX, jY] = Stack;
                    }
                floorItem Item = new floorItem(itemID, templateID, X, Y, Z, H, Var);
                _Items.Add(itemID, Item);
            }
            /// <summary>
            /// Removes a virtual flooritem from the item manager, handles the heightmap, makes it disappear in room and returns it back to the owners hand, or deletes it.
            /// </summary>
            /// <param name="itemID">The ID of the item to remove.</param>
            /// <param name="ownerID">The ID of the user who owns this item. If 0, then the item will be dropped from the database.</param>
            internal void removeItem(int itemID, int ownerID)
            {
                if(_Items.ContainsKey(itemID))
                {
                    floorItem Item = (floorItem)_Items[itemID];
                    catalogueManager.itemTemplate Template = catalogueManager.getTemplate(Item.templateID);

                    int Length = 0;
                    int Width = 0;
                    if(Item.Z == 2 || Item.Z == 6)
                    {
                        Length = Template.Length;
                        Width = Template.Width;
                    }
                    else
                    {
                        Length = Template.Width;
                        Width = Template.Length;
                    }

                    for (int jX = Item.X; jX < Item.X + Width; jX++)
                    {
                        for (int jY = Item.Y; jY < Item.Y + Length; jY++)
                        {
                            furnitureStack Stack = _Room.sqSTACK[jX, jY];
                            if (Stack != null && Stack.Count > 1)
                            {
                                if (itemID == Stack.bottomItemID())
                                {
                                    int topID = Stack.topItemID();
                                    floorItem topItem = (floorItem)_Items[topID];
                                    if (catalogueManager.getTemplate(topItem.templateID).typeID == 2)
                                        _Room.sqSTATE[jX, jY] = squareState.Seat;
                                    else
                                        _Room.sqSTATE[jX, jY] = 0;
                                }
                                else if (itemID == Stack.topItemID())
                                {
                                    int belowID = Stack.getBelowItemID(itemID);
                                    floorItem belowItem = (floorItem)_Items[belowID];
                                    byte typeID = catalogueManager.getTemplate(belowItem.templateID).typeID;

                                    _Room.sqSTATE[jX, jY] = (squareState)typeID;
                                    if (typeID == 2 || typeID == 3)
                                    {
                                        _Room.sqITEMROT[jX, jY] = belowItem.Z;
                                        _Room.sqITEMHEIGHT[jX, jY] = belowItem.H + catalogueManager.getTemplate(belowItem.templateID).topH;
                                    }
                                    else if (typeID == 4)
                                    {
                                        _Room.sqITEMHEIGHT[jX, jY] = belowItem.H;
                                    }
                                }
                                Stack.Remove(itemID);
                                _Room.sqSTACK[jX, jY] = Stack;
                            }
                            else
                            {
                                _Room.sqSTATE[jX, jY] = 0;
                                _Room.sqITEMHEIGHT[jX, jY] = 0;
                                _Room.sqITEMROT[jX, jY] = 0;
                                _Room.sqSTACK[jX, jY] = null;
                            }
                            if(Template.typeID == 2 || Template.typeID == 3)
                                _Room.refreshCoord(jX, jY);
                        }
                    }

                    if (this.soundMachineID == 0 && stringManager.getStringPart(Template.Sprite, 0, 13) == "sound_machine")
                        soundMachineID = 0;

                    _Room.sendData(new HabboPacketBuilder("A^").Append(itemID).Build());
                    _Items.Remove(itemID);
                    if (ownerID > 0) // Return to current owner/new owner
                        DB.runQuery("UPDATE furniture SET x = '0',y = '0',z = '0', h = '0', ownerid = '" + ownerID + "',roomid = '0' WHERE id = '" + itemID + "' LIMIT 1");
                    else
                        DB.runQuery("DELETE FROM furniture WHERE id = '" + itemID + "' LIMIT 1");
                }
            }
            internal void placeItem(int itemID, int templateID, int X, int Y, byte typeID, byte Z)
            {
                if (_Items.ContainsKey(itemID))
                    return;

                try
                {
                    catalogueManager.itemTemplate Template = catalogueManager.getTemplate(templateID);
                    bool isSoundMachine = (stringManager.getStringPart(Template.Sprite, 0, 13) == "sound_machine");
                    if (isSoundMachine && soundMachineID > 0)
                        return;

                    int Length = 0;
                    int Width = 0;
                    if (Z == 2 || Z == 6)
                    {
                        Length = Template.Length;
                        Width = Template.Width;
                    }
                    else
                    {
                        Length = Template.Width;
                        Width = Template.Length;
                    }

                    double testH = _Room.sqFLOORHEIGHT[X, Y];
                    double H = testH;
                    if (_Room.sqSTACK[X, Y] != null)
                    {
                        floorItem topItem = (floorItem)_Items[_Room.sqSTACK[X, Y].topItemID()];
                        H = topItem.H + catalogueManager.getTemplate(topItem.templateID).topH;
                    }

                    for (int jX = X; jX < X + Width; jX++)
                    {
                        for (int jY = Y; jY < Y + Length; jY++)
                        {
                            if (_Room.sqUNIT[jX, jY]) // Dynamical unit here
                                return;

                            squareState jState = _Room.sqSTATE[jX, jY];
                            if (jState != squareState.Open)
                            {
                                if (jState == squareState.Blocked)
                                {
                                    if (_Room.sqSTACK[jX, jY] == null) // Square blocked and no stack here
                                        return;
                                    else
                                    {
                                        floorItem topItem = (floorItem)_Items[_Room.sqSTACK[jX, jY].topItemID()];
                                        catalogueManager.itemTemplate topItemTemplate = catalogueManager.getTemplate(topItem.templateID);
                                        if (topItemTemplate.topH == 0 || topItemTemplate.typeID == 2 || topItemTemplate.typeID == 3) // No stacking on seat/bed
                                            return;
                                        else
                                        {
                                            if (topItem.H + topItemTemplate.topH > H) // Higher than previous topheight
                                                H = topItem.H + topItemTemplate.topH;
                                        }
                                    }
                                }
                                else if (jState == squareState.Rug && _Room.sqSTACK[jX, jY] != null)
                                {
                                    double jH = ((floorItem)_Items[_Room.sqSTACK[jX, jY].topItemID()]).H + 0.1;
                                    if (jH > H)
                                        H = jH;
                                }
                                else // Seat etc
                                    return;
                            }
                        }
                    }

                    if (H > Config.Items_Stacking_maxHeight)
                        H = Config.Items_Stacking_maxHeight;

                    for (int jX = X; jX < X + Width; jX++)
                    {
                        for (int jY = Y; jY < Y + Length; jY++)
                        {
                            furnitureStack Stack = null;
                            if (_Room.sqSTACK[jX, jY] == null)
                            {
                                if ((Template.typeID == 1 && Template.topH > 0) || Template.typeID == 4)
                                {
                                    Stack = new furnitureStack();
                                    Stack.Add(itemID);
                                }
                            }
                            else
                            {
                                Stack = _Room.sqSTACK[jX, jY];
                                Stack.Add(itemID);
                            }

                            _Room.sqSTATE[jX, jY] = (squareState)Template.typeID;
                            _Room.sqSTACK[jX, jY] = Stack;
                            if (Template.typeID == 2 || Template.typeID == 3)
                            {
                                _Room.sqITEMHEIGHT[jX, jY] = H + Template.topH;
                                _Room.sqITEMROT[jX, jY] = Z;
                            }
                            else if (Template.typeID == 4)
                                _Room.sqITEMHEIGHT[jX, jY] = H;
                        }
                    }

                    string Var = DB.runRead("SELECT var FROM furniture WHERE id = '" + itemID + "'");
                    DB.runQuery("UPDATE furniture SET roomid = '" + _Room.roomID + "',x = '" + X + "',y = '" + Y + "',z = '" + Z + "',h = '" + H.ToString().Replace(',','.') + "' WHERE id = '" + itemID + "' LIMIT 1");
                    floorItem Item = new floorItem(itemID, templateID, X, Y, Z, H, Var);
                    _Items.Add(itemID, Item);
                    _Room.sendData(new HabboPacketBuilder("A]").Append(Item.ToString()).Build());

                    if(isSoundMachine)
                        this.soundMachineID = itemID;
                }
                catch { }
            }
            internal void rotateItem(int itemID, byte Z)
            {
                floorItem Item = (floorItem)_Items[itemID];
                catalogueManager.itemTemplate Template = catalogueManager.getTemplate(Item.templateID);

                int Length = 1;
                int Width = 1;
                if (Template.Length > 1 && Template.Width > 1)
                {
                    if (Z == 2 || Z == 6)
                    {
                        Length = Template.Length;
                        Width = Template.Width;
                    }
                    else
                    {
                        Length = Template.Width;
                        Width = Template.Length;
                    }
                }

                for (int jX = Item.X; jX < Item.X + Width; jX++)
                    for (int jY = Item.Y; jY < Item.Y + Length; jY++)
                    {
                        furnitureStack Stack = _Room.sqSTACK[jX, jY];
                        if (Stack != null && Stack.topItemID() != itemID)
                        {

                        }
                    }

            }
            internal void relocateItem(int itemID, int X, int Y, byte Z)
            {
                try
                {
                    floorItem Item = (floorItem)_Items[itemID];
                    catalogueManager.itemTemplate Template = catalogueManager.getTemplate(Item.templateID);

                    int Length = 0;
                    int Width = 0;
                    if (Z == 2 || Z == 6)
                    {
                        Length = Template.Length;
                        Width = Template.Width;
                    }
                    else
                    {
                        Length = Template.Width;
                        Width = Template.Length;
                    }

                    double baseFloorH = _Room.sqFLOORHEIGHT[X, Y];
                    double H = baseFloorH;
                    if (_Room.sqSTACK[X, Y] != null)
                    {
                        floorItem topItem = (floorItem)_Items[_Room.sqSTACK[X, Y].topItemID()];
                        if (topItem != Item)
                        {
                            catalogueManager.itemTemplate topTemplate = catalogueManager.getTemplate(topItem.templateID);
                            if (topTemplate.typeID == 1)
                                H = topItem.H + topTemplate.topH;
                        }
                        else if (_Room.sqSTACK[X, Y].Count > 1)
                            H = topItem.H;
                    }

                    for (int jX = X; jX < X + Width; jX++)
                    {
                        for (int jY = Y; jY < Y + Length; jY++)
                        {
                            if (Template.typeID != 2 && _Room.sqUNIT[jX, jY])
                                return;

                            squareState jState = _Room.sqSTATE[jX, jY];
                            furnitureStack Stack = _Room.sqSTACK[jX, jY];
                            if (jState != squareState.Open)
                            {
                                if (Stack == null)
                                {
                                    if (jX != Item.X || jY != Item.Y)
                                        return;
                                }
                                else
                                {
                                    floorItem topItem = (floorItem)_Items[Stack.topItemID()];
                                    if (topItem != Item)
                                    {
                                        catalogueManager.itemTemplate topItemTemplate = catalogueManager.getTemplate(topItem.templateID);
                                        if (topItemTemplate.typeID == 1 && topItemTemplate.topH > 0)
                                        {
                                            if (topItem.H + topItemTemplate.topH > H)
                                                H = topItem.H + topItemTemplate.topH;
                                        }
                                        else
                                        {
                                            if (topItemTemplate.typeID == 2)
                                                return;
                                        }
                                        //return;
                                    }
                                }
                            }
                        }
                    }

                    int oldLength = 1;
                    int oldWidth = 1;
                    if (Template.Length > 1 || Template.Width > 1)
                    {
                        if (Item.Z == 2 || Item.Z == 6)
                        {
                            oldLength = Template.Length;
                            oldWidth = Template.Width;
                        }
                        else
                        {
                            oldLength = Template.Width;
                            oldWidth = Template.Length;
                        }
                    }
                    if (H > Config.Items_Stacking_maxHeight)
                        H = Config.Items_Stacking_maxHeight;

                    for (int jX = Item.X; jX < Item.X + oldWidth; jX++)
                    {
                        for (int jY = Item.Y; jY < Item.Y + oldLength; jY++)
                        {
                            furnitureStack Stack = _Room.sqSTACK[jX, jY];
                            if (Stack != null && Stack.Count > 1)
                            {
                                if (itemID == Stack.bottomItemID())
                                {
                                    if(catalogueManager.getTemplate(((floorItem)_Items[Stack.topItemID()]).templateID).typeID == 2)
                                        _Room.sqSTATE[jX,jY] = squareState.Seat;
                                    else
                                        _Room.sqSTATE[jX,jY] = squareState.Open;
                                }
                                else if (itemID == Stack.topItemID())
                                {
                                    floorItem belowItem = (floorItem)_Items[Stack.getBelowItemID(itemID)];
                                    byte typeID = catalogueManager.getTemplate(belowItem.templateID).typeID;

                                    _Room.sqSTATE[jX, jY] = (squareState)typeID;
                                    if (typeID == 2 || typeID == 3)
                                    {
                                        _Room.sqITEMROT[jX, jY] = belowItem.Z;
                                        _Room.sqITEMHEIGHT[jX, jY] = belowItem.H + catalogueManager.getTemplate(belowItem.templateID).topH;
                                    }
                                    else if (typeID == 4)
                                        _Room.sqITEMHEIGHT[jX, jY] = belowItem.H;
                                }
                                Stack.Remove(itemID);
                                _Room.sqSTACK[jX, jY] = Stack;
                            }
                            else
                            {
                                _Room.sqSTATE[jX, jY] = 0;
                                _Room.sqITEMHEIGHT[jX, jY] = 0;
                                _Room.sqITEMROT[jX, jY] = 0;
                                _Room.sqSTACK[jX, jY] = null;
                            }
                            if(Template.typeID == 2 || Template.typeID == 3)
                                _Room.refreshCoord(jX, jY);
                        }
                    }

                    Item.X = X;
                    Item.Y = Y;
                    Item.Z = Z;
                    Item.H = H;
                    _Room.sendData(new HabboPacketBuilder("A_").Append(Item.ToString()).Build());
                    DB.runQuery("UPDATE furniture SET x = '" + X + "',y = '" + Y + "',z = '" + Z + "',h = '" + H.ToString().Replace(',','.') + "' WHERE id = '" + itemID + "' LIMIT 1");

                    for (int jX = X; jX < X + Width; jX++)
                    {
                        for (int jY = Y; jY < Y + Length; jY++)
                        {
                            furnitureStack Stack = null;
                            if (_Room.sqSTACK[jX, jY] == null)
                            {
                                if (Template.topH > 0 && Template.typeID != 2 && Template.typeID != 3 && Template.typeID != 4)
                                {
                                    Stack = new furnitureStack();
                                    Stack.Add(itemID);
                                }
                            }
                            else
                            {
                                Stack = _Room.sqSTACK[jX, jY];
                                Stack.Add(itemID);
                            }

                            _Room.sqSTATE[jX, jY] = (squareState)Template.typeID;
                            _Room.sqSTACK[jX, jY] = Stack;
                            if (Template.typeID == 2 || Template.typeID == 3)
                            {
                                _Room.sqITEMHEIGHT[jX, jY] = H + Template.topH;
                                _Room.sqITEMROT[jX, jY] = Z;
                                _Room.refreshCoord(jX, jY);
                            }
                            else if (Template.typeID == 4)
                                _Room.sqITEMHEIGHT[jX, jY] = H;
                        }
                    }
                }
                catch { }
            }
            /// <summary>
            /// Updates the status of a virtual flooritem and updates it in the virtual room and in the database. Door items are also being handled if opened/closed.
            /// </summary>
            /// <param name="itemID">The ID of the item to update.</param>
            /// <param name="toStatus">The new status of the item.</param>
            /// <param name="hasRights">The bool that indicates if the user that signs this item has rights.</param>
            internal void toggleItemStatus(int itemID, string toStatus, bool hasRights)
            {
                if (_Items.ContainsKey(itemID) == false)
                    return;

                floorItem Item = (floorItem)_Items[itemID];
                string itemSprite = Item.Sprite;
                if (itemSprite == "edice" || itemSprite == "edicehc" || stringManager.getStringPart(itemSprite, 0, 11) == "prizetrophy" || stringManager.getStringPart(itemSprite, 0, 11) == "greektrophy" || stringManager.getStringPart(itemSprite, 0, 7) == "present") // Items that can't be signed [this regards dicerigging etc]
                    return;

                if (toStatus.ToLower() == "c" || toStatus.ToLower() == "o") // Door
                {
                    if (hasRights) // Rights check
                    {
                        #region Open/close doors
                        catalogueManager.itemTemplate Template = catalogueManager.getTemplate(Item.templateID);
                        if (Template.isDoor == false)
                            return;
                        int Length = 1;
                        int Width = 1;
                        if (Template.Length > 1 || Template.Width > 1)
                        {
                            if (Item.Z == 2 || Item.Z == 6)
                            {
                                Length = Template.Length;
                                Width = Template.Width;
                            }
                            else
                            {
                                Length = Template.Width;
                                Width = Template.Length;
                            }
                        }
                        if (toStatus.ToLower() == "c")
                        {
                            if (_Room.squareBlocked(Item.X, Item.Y, Length, Width))
                                return;
                            _Room.setSquareState(Item.X, Item.Y, Length, Width, squareState.Blocked);
                        }
                        else
                            _Room.setSquareState(Item.X, Item.Y, Length, Width, squareState.Open);
                        #endregion
                        Item.Var = toStatus;
                        _Room.sendData(new HabboPacketBuilder("AX").Append(itemID).Separator().Append(toStatus).Separator().Build());
                        DB.runQuery("UPDATE furniture SET var = '" + toStatus + "' WHERE id = '" + itemID + "' LIMIT 1");
                    }
                    return;
                }
                Item.Var = toStatus;
                _Room.sendData(new HabboPacketBuilder("AX").Append(itemID).Separator().Append(toStatus).Separator().Build());
                DB.runQuery("UPDATE furniture SET var = '" + toStatus + "' WHERE id = '" + itemID + "' LIMIT 1");
            }
            /// <summary>
            /// Returns a string with all the virtual wallitems in this item manager.
            /// </summary>
            internal string Items
            {
                get
                {
                    StringBuilder itemList = new StringBuilder(Encoding.encodeVL64(_Items.Count));
                    foreach (floorItem Item in _Items.Values)
                        itemList.Append(Item.ToString());

                    return itemList.ToString();
                }
            }
            /// <summary>
            /// Returns a bool that indicates if the item manager contains a certain virtual flooritem.
            /// </summary>
            /// <param name="itemID">The ID of the item to check.</param>
            internal bool containsItem(int itemID)
            {
                return _Items.ContainsKey(itemID);
            }
            /// <summary>
            /// Returns the floorItem object of a certain virtual flooritem in the item manager.
            /// </summary>
            /// <param name="itemID">The ID of the item to get the floorItem object of.</param>
            internal floorItem getItem(int itemID)
            {
                return (floorItem)_Items[itemID];
            }
        }
        #endregion
    }
}
