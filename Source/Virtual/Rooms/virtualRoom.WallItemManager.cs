using System;
using System.Text;
using System.Collections.Generic;

using Holo.Managers;
using Holo.Protocol;
using Holo.Virtual.Rooms.Items;

namespace Holo.Virtual.Rooms;

/// <summary>
/// Partial class for virtualRoom containing the WallItemManager nested class.
/// </summary>
public partial class virtualRoom
{
    #region Item managers - WallItemManager
    internal class WallItemManager
    {
        private virtualRoom _Room;
        private Dictionary<int, wallItem> _Items = new Dictionary<int, wallItem>();
        /// <summary>
        /// Initializes the manager.
        /// </summary>
        /// <param name="Room">The parent room.</param>
        public WallItemManager(virtualRoom Room)
        {
            this._Room = Room;
        }
        /// <summary>
        /// Removes all the items from the item manager and destructs all objects inside.
        /// </summary>
        internal void Clear()
        {
            try {_Items.Clear();}
            catch {}
            _Room = null;
            _Items = null;
        }
        /// <summary>
        /// Adds a virtual wallitem to the item manager and optionally makes it appear in the room.
        /// </summary>
        /// <param name="itemID">The ID of the item to add.</param>
        /// <param name="Item">The item to add.</param>
        /// <param name="Place">Indicates if the item is put in the room now, so updating database and sending appear packet to room.</param>
        internal void addItem(int itemID, int templateID, string wallPosition, string Var, bool Place)
        {
            if (_Items.ContainsKey(itemID) == false)
            {
                var Item = new wallItem(itemID,templateID,wallPosition,Var);
                _Items.Add(itemID,Item);
                if (Place)
                {
                    _Room.sendData(new HabboPacketBuilder("AS").Append(Item.ToString()).Build());
                    DB.runQuery("UPDATE furniture SET roomid = '" + _Room.roomID + "',wallpos = '" + wallPosition + "' WHERE id = '" + itemID + "' LIMIT 1");
                    if (DB.checkExists("SELECT id FROM furniture_moodlight WHERE id = '" + itemID.ToString() + "'"))
                    {
                        DB.runQuery("UPDATE furniture_moodlight SET roomid = '" + _Room.roomID + "' WHERE id = '" + itemID + "' LIMIT 1");
                    }
                }
            }
        }
        /// <summary>
        /// Removes a virtual wallitem from the item manager, updates the database row/drops the item from database and makes it disappear in the room.
        /// </summary>
        /// <param name="itemID">The ID of the item to remove.</param>
        /// <param name="ownerID">The ID of the user that owns this item. If 0, then the item will be dropped from the database.</param>
        internal void removeItem(int itemID, int ownerID)
        {
            if (_Items.ContainsKey(itemID))
            {
                _Room.sendData(new HabboPacketBuilder("AT").Append(itemID).Build());
                _Items.Remove(itemID);
                if (ownerID > 0)
                    DB.runQuery("UPDATE furniture SET ownerid = '" + ownerID + "',roomid = '0' WHERE id = '" + itemID + "' LIMIT 1");
                else
                    DB.runQuery("DELETE FROM furniture WHERE id = '" + itemID + "' LIMIT 1");
            }
        }
        /// <summary>
        /// Updates the status of a virtual wallitem and updates it in the virtual room and in the database. Certain items can't switch status by this way, and they will be ignored to prevent exploiting.
        /// </summary>
        /// <param name="itemID">The ID of the item to update.</param>
        /// <param name="toStatus">The new status of the item.</param>
        internal void toggleItemStatus(int itemID, int toStatus)
        {
            if (_Items.ContainsKey(itemID) == false)
                return;

            var Item = _Items[itemID];
            string itemSprite = Item.Sprite;
            if (itemSprite == "roomdimmer" || itemSprite == "post.it" || itemSprite == "post.it.vd" || itemSprite == "poster" || itemSprite == "habbowheel")
                return;

            Item.Var = toStatus.ToString();
            _Room.sendData(new HabboPacketBuilder("AU").Append(itemID).TabSeparator().Append(itemSprite).TabSeparator().Append(" ").Append(Item.wallPosition).TabSeparator().Append(Item.Var).Build());
            DB.runQuery("UPDATE furniture SET var = '" + toStatus + "' WHERE id = '" + itemID + "' LIMIT 1");

        }
        /// <summary>
        /// Returns a string with all the virtual wallitems in this item manager.
        /// </summary>
        internal string Items
        {
            get
            {
                var itemList = new StringBuilder();
                foreach (var Item in _Items.Values)
                    itemList.Append(Item.ToString() + Convert.ToChar(13));

                return itemList.ToString();
            }
        }
        /// <summary>
        /// Returns a bool that indicates if the item manager contains a certain virtual wallitem.
        /// </summary>
        /// <param name="itemID">The ID of the item to check.</param>
        internal bool containsItem(int itemID)
        {
            return _Items.ContainsKey(itemID);
        }
        /// <summary>
        /// Returns the wallItem object of a certain virtual wallitem in the item manager.
        /// </summary>
        /// <param name="itemID">The ID of the item to get the wallItem object of.</param>
        internal wallItem getItem(int itemID)
        {
            return _Items[itemID];
        }
    }
    #endregion
}
