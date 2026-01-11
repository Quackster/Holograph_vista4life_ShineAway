using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Holo.Domain.Games.Pathfinding
{
    /// <summary>
    /// Provides a very simple yet fast pathfinder that returns the next step to a coord.
    /// </summary>
    public sealed class GamePathfinder
    {
        public static Coord getNextStep(int X, int Y, int goalX, int goalY)
        {
            Coord Next = new Coord(-1, -1);
            if (X > goalX && Y > goalY)
                Next = new Coord(X - 1, Y - 1);
            else if (X < goalX && Y < goalY)
                Next = new Coord(X + 1, Y + 1);
            else if (X > goalX && Y < goalY)
                Next = new Coord(X - 1, Y + 1);
            else if (X < goalX && Y > goalY)
                Next = new Coord(X + 1, Y - 1);
            else if (X > goalX)
                Next = new Coord(X - 1, Y);
            else if (X < goalX)
                Next = new Coord(X + 1, Y);
            else if (Y < goalY)
                Next = new Coord(X, Y + 1);
            else if (Y > goalY)
                Next = new Coord(X, Y - 1);

            return Next;
        }
    }
}
