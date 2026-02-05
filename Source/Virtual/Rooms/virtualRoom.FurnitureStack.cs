using System;

namespace Holo.Virtual.Rooms
{
    /// <summary>
    /// Partial class for virtualRoom containing the furnitureStack private class.
    /// </summary>
    public partial class virtualRoom
    {
        #region Private classes
        /// <summary>
        /// Represents a stack of virtual flooritems.
        /// </summary>
        private class furnitureStack
        {
            private int[] _itemIDs;
            /// <summary>
            /// Initializes a new stack.
            /// </summary>
            internal furnitureStack()
            {
               _itemIDs = new int[20];
            }
            /// <summary>
            /// Adds an item ID to the top position of the stack.
            /// </summary>
            /// <param name="itemID">The item ID to add.</param>
            internal void Add(int itemID)
            {
                for (int i = 0; i < 20; i++)
                {
                    if (_itemIDs[i] == 0)
                    {
                        _itemIDs[i] = itemID;
                        return;
                    }
                }
            }
            /// <summary>
            /// Removes an item ID from the stack and shrinks empty spots. [order is kept the same]
            /// </summary>
            /// <param name="itemID">The item ID to remove.</param>
            internal void Remove(int itemID)
            {
                for (int i = 0; i < 20; i++)
                    if (_itemIDs[i] == itemID)
                    {
                        _itemIDs[i] = 0;
                        break;
                    }

                int g = 0;
                int[] j = new int[20];
                for (int i = 0; i < 20; i++)
                    if (_itemIDs[i] > 0)
                    {
                        j[g] = _itemIDs[i];
                        g++;
                    }
                _itemIDs = j;
            }
            /// <summary>
            /// The most top item ID of the stack.
            /// </summary>
            internal int topItemID()
            {
                for (int i = 0; i < 20; i++)
                    if (_itemIDs[i] == 0)
                        return _itemIDs[i - 1];
                return 0;
            }
            /// <summary>
            /// The lowest located [so: first added] item ID of the stack.
            /// </summary>
            internal int bottomItemID()
            {
                return _itemIDs[0];
            }
            /// <summary>
            /// Returns the item ID located above a given item ID.
            /// </summary>
            /// <param name="aboveID">The item ID to get the item ID above of.</param>
            internal int getAboveItemID(int aboveID)
            {
                for (int i = 0; i < 20; i++)
                    if (_itemIDs[i] == aboveID)
                        return _itemIDs[i + 1];
                return 0;
            }
            /// <summary>
            /// Returns the item ID located below a given item ID.
            /// </summary>
            /// <param name="belowID">The item ID to get the item ID below of.</param>
            internal int getBelowItemID(int belowID)
            {
                for (int i = 0; i < 20; i++)
                    if (_itemIDs[i] == belowID)
                        return _itemIDs[i - 1];
                return 0;
            }
            /// <summary>
            /// Returns a bool that indicates if the stack contains a certain item ID.
            /// </summary>
            /// <param name="itemID">The item ID to check.</param>
            internal bool Contains(int itemID)
            {
                foreach (int i in _itemIDs)
                    if (i == itemID)
                        return true;

                return false;
            }
            /// <summary>
            /// The amount of item ID's in the stack.
            /// </summary>
            internal int Count
            {
                get
                {
                    int j = 0;
                    for (int i = 0; i < 20; i++)
                    {
                        if (_itemIDs[i] > 0)
                            j++;
                        else
                            return j;
                    }
                    return j;
                }
            }
        }
        #endregion
    }
}
