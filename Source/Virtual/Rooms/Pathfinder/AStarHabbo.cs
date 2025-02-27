using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace JASE.Room.Walking.Pathfinding
{
    public class AStarHabbo
    {
        public static int[,] Map = {
            {1,1,1,1,1,1},
            {1,1,1,1,1,1},
            {1,1,1,1,1,1},
            {1,1,1,1,1,1}
            };

        public static int[,] Heights = {
            {1,1,1,1,1,1},
            {1,1,1,1,1,1},
            {1,1,1,1,1,1},
            {1,1,1,1,1,1}
            };

        public static AStarNode2D calculateNext(int curx, int cury, int goalx, int goaly, int[,] mapCurrent, int[,] Heightmap)
        {
            Map = mapCurrent;
            Heights = Heightmap;

               AStar astar = new AStar();

            AStarNode2D GoalNode = new AStarNode2D(null, null, 0, goalx, goaly);
            AStarNode2D StartNode = new AStarNode2D(null, GoalNode, 0, curx, cury);

            astar.FindPath(StartNode, GoalNode);


            if (astar.Solution.Count > 1)
            {
                AStarNode2D tmpNode =  (AStarNode2D)astar.Solution[1];
                //tmpNode.Direction = MovementEquations.workDirection(curx, cury, tmpNode.X, tmpNode.Y);
                return tmpNode;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets movement cost from the 2-dimensional map
        /// </summary>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <returns>Returns movement cost at the specified point in the map</returns>
        public static int GetMap(int x, int y)
        {
            int xlen = Map.GetLength(1) - 1;
            int ylen = Map.GetLength(0) - 1;

            if (x > xlen || x < 0)
            {
                return -1;
            }

            if (y > ylen || y < 0)
            {
                return -1;
            }

            if(Map[y, x] == -1)
                return -1;

            return 0;
        }
        public static int GetHeight(int x, int y)
        {
            int xlen = Map.GetLength(1) - 1;
            int ylen = Map.GetLength(0) - 1;

            if(x > xlen || x < 0)
            {
                return -1;
            }

            if(y > ylen || y < 0)
            {
                return -1;
            }

            return Heights[x,y];
        }

    }
}

