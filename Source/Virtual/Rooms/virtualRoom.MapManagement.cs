using System;

namespace Holo.Virtual.Rooms
{
    /// <summary>
    /// Partial class for virtualRoom containing map management methods and the squareTrigger class.
    /// </summary>
    public partial class virtualRoom
    {
        #region Map management
        internal bool squareBlocked(int X, int Y, int Length, int Width)
        {
            for (int jX = X; jX < X + Width; jX++)
            {
                for (int jY = Y; jY < X + Length; jY++)
                {
                    if (sqUNIT[jX, jY])
                        return true;
                }
            }
            return false;
        }
        internal void setSquareState(int X, int Y, int Length, int Width, squareState State)
        {
            for (int jX = X; jX < X + Width; jX++)
            {
                for (int jY = Y; jY < Y + Length; jY++)
                {
                    sqSTATE[jX, jY] = State;
                }
            }
        }
        internal squareTrigger getTrigger(int X, int Y)
        {
            try {return sqTRIGGER[X, Y];}
            catch {return null;}
        }
        internal int[] getMapBorders()
        {
            int[] i = new int[2];
            i[0] = sqUNIT.GetUpperBound(0);
            i[1] = sqUNIT.GetUpperBound(1);
            return i;
        }
        /// <summary>
        /// Represents a trigger square that invokes a special event.
        /// </summary>
        internal class squareTrigger
        {
            /// <summary>
            /// The object of this trigger.
            /// </summary>
            internal readonly string Object;
            /// <summary>
            /// Optional. The new destination X of the virtual unit that invokes the trigger, walking.
            /// </summary>
            internal readonly int goalX;
            /// <summary>
            /// Optional. The new destination Y of the virtual unit that invokes the trigger, walking.
            /// </summary>
            internal readonly int goalY;
            /// <summary>
            /// Optional. The next X step of the virtual unit that invokes the trigger, stepping.
            /// </summary>
            internal readonly int stepX;
            /// <summary>
            /// Optional. The next Y step of the virtual unit that invokes the trigger, stepping.
            /// </summary>
            internal readonly int stepY;
            /// <summary>
            /// Optional. Optional. In case of a warp tile, this is the database ID of the destination room.
            /// </summary>
            internal readonly int roomID;
            /// <summary>
            /// Optional. A boolean flag for the trigger.
            /// </summary>
            internal bool State;
            /// <summary>
            /// Initializes the new trigger.
            /// </summary>
            /// <param name="Object">The object of this rigger.</param>
            /// <param name="goalX">Optional. The destination X of the virtual unit that invokes the trigger, walking.</param>
            /// <param name="goalY">Optional. The destination Y of the virtual unit that invokes the trigger, walking.</param>
            /// <param name="stepX">Optional. The next X step of the virtual unit that invokes the trigger, stepping.</param>
            /// <param name="stepY">Optional. The next Y step of the virtual unit that invokes the trigger, stepping.</param>
            /// <param name="roomID">Optional. In case of a warp tile, this is the database ID of the destination room.</param>
            /// <param name="State">Optional. A boolean flag for the trigger.</param>
            internal squareTrigger(string Object, int goalX, int goalY, int stepX, int stepY, bool State, int roomID)
            {
                this.Object = Object;
                this.goalX = goalX;
                this.goalY = goalY;
                this.stepX = stepX;
                this.stepY = stepY;
                this.roomID = roomID;
                this.State = State;
            }
        }
        #endregion
    }
}
