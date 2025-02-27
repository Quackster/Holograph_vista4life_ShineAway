using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace JASE.Room.Walking.Pathfinding
{
    public class AStarNode2D : AStarNode
    {
        #region Properties

        public int Direction
        {
            get
            {
                return FDirection;
            }

            set
            {
                FDirection = value;
            }
        }
        private int FDirection;

        /// <summary>
        /// The X-coordinate of the node
        /// </summary>
        public int X
        {
            get
            {
                return FX;
            }
        }
        private int FX;

        /// <summary>
        /// The Y-coordinate of the node
        /// </summary>
        public int Y
        {
            get
            {
                return FY;
            }
        }
        private int FY;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor for a node in a 2-dimensional map
        /// </summary>
        /// <param name="AParent">Parent of the node</param>
        /// <param name="AGoalNode">Goal node</param>
        /// <param name="ACost">Accumulative cost</param>
        /// <param name="AX">X-coordinate</param>
        /// <param name="AY">Y-coordinate</param>
        public AStarNode2D(AStarNode AParent, AStarNode AGoalNode, double ACost, int AX, int AY)
            : base(AParent, AGoalNode, ACost)
        {
            FX = AX;
            FY = AY;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a successor to a list if it is not impassible or the parent node
        /// </summary>
        /// <param name="ASuccessors">List of successors</param>
        /// <param name="AX">X-coordinate</param>
        /// <param name="AY">Y-coordinate</param>
        private void AddSuccessor(ArrayList ASuccessors, int AX, int AY)
        {
            int CurrentCost = AStarHabbo.GetMap(AX, AY);
            int CurrentHeight = AStarHabbo.GetHeight(AX, AY);
            int OldHeight = AStarHabbo.GetHeight(this.X, this.Y);
            if(CurrentCost == -1)
            {
                return;
            }
            for (int x = -1; x < 2; x++)
            {
                if(CurrentHeight == OldHeight - x)
                    goto IsReal;
            }
            return;
            IsReal:
            AStarNode2D NewNode = new AStarNode2D(this, GoalNode, Cost + CurrentCost, AX, AY);
            if(NewNode.IsSameState(Parent))
            {
                return;
            }
            ASuccessors.Add(NewNode);
        }

        #endregion

        #region Overidden Methods

        /// <summary>
        /// Determines wheather the current node is the same state as the on passed.
        /// </summary>
        /// <param name="ANode">AStarNode to compare the current node to</param>
        /// <returns>Returns true if they are the same state</returns>
        public override bool IsSameState(AStarNode ANode)
        {
            if(ANode == null)
            {
                return false;
            }
            return ( ( ( ( AStarNode2D ) ANode ).X == FX ) &&
                ( ( ( AStarNode2D ) ANode ).Y == FY ) );
        }

        /// <summary>
        /// Calculates the estimated cost for the remaining trip to the goal.
        /// </summary>
        public override void Calculate()
        {
            if(GoalNode != null)
            {
                double xd = FX - ( ( AStarNode2D ) GoalNode ).X;
                double yd = FY - ( ( AStarNode2D ) GoalNode ).Y;
                // "Euclidean distance" - Used when search can move at any angle.
                GoalEstimate = Math.Sqrt(( xd * xd ) + ( yd * yd ));
            }
            else
            {
                GoalEstimate = 0;
            }
        }

        /// <summary>
        /// Gets all successors nodes from the current node and adds them to the successor list
        /// </summary>
        /// <param name="ASuccessors">List in which the successors will be added</param>
        public override void GetSuccessors(ArrayList ASuccessors)
        {
            ASuccessors.Clear();

            AddSuccessor(ASuccessors, FX - 1, FY);
            AddSuccessor(ASuccessors, FX, FY + 1);
            AddSuccessor(ASuccessors, FX, FY - 1);
            AddSuccessor(ASuccessors, FX + 1, FY);
            if(AStarHabbo.GetMap(FX, FY - 1) != -1) // Works
                if(AStarHabbo.GetMap(FX - 1, FY) != -1) // Works
                    AddSuccessor(ASuccessors, FX - 1, FY - 1);
            if(AStarHabbo.GetMap(FX, FY - 1) != -1) // Works
                if(AStarHabbo.GetMap(FX + 1, FY) != -1) // Works
                    AddSuccessor(ASuccessors, FX + 1, FY - 1);
            if(AStarHabbo.GetMap(FX + 1, FY) != -1) // Works
                if(AStarHabbo.GetMap(FX, FY + 1) != -1) // Works
                    AddSuccessor(ASuccessors, FX + 1, FY + 1);
            if(AStarHabbo.GetMap(FX - 1, FY - 0) != -1) // Works
                if(AStarHabbo.GetMap(FX - 0, FY + 1) != -1) // Works
                    AddSuccessor(ASuccessors, FX - 1, FY + 1);
        }

        /// <summary>
        /// Prints information about the current node
        /// </summary>
        public override void PrintNodeInfo()
        {
            Console.WriteLine("X:\t{0}\tY:\t{1}\tCost:\t{2}\tEst:\t{3}\tTotal:\t{4}", FX, FY, Cost, GoalEstimate, TotalCost);
        }

        #endregion
    }
}
