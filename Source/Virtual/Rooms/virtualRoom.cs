using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using Holo.Managers;
using Holo.Virtual.Users;
using Holo.Virtual.Rooms.Bots;
using Holo.Virtual.Rooms.Items;
using Holo.Virtual.Rooms.Games;

namespace Holo.Virtual.Rooms
{
    /// <summary>
    /// Represents a virtual publicroom or guestroom, with management for users, items and the map. Threaded.
    /// </summary>
    public partial class virtualRoom
    {
        #region Declares
        /// <summary>
        /// The ID of this room.
        /// </summary>
        internal int roomID;
        /// <summary>
        /// Indicates if this room is a publicroom.
        /// </summary>
        internal bool isPublicroom;
        /// <summary>
        /// Manages the flooritems inside the room.
        /// </summary>
        internal FloorItemManager floorItemManager;
        /// <summary>
        /// Manages the wallitems inside the room.
        /// </summary>
        internal WallItemManager wallItemManager;
        /// <summary>
        /// Optional. The lobby manager incase of a game lobby.
        /// </summary>
        internal gameLobby Lobby;

        private string _publicroomItems;
        private string _Heightmap;
        /// <summary>
        /// Indicates if this room has a swimming pool.
        /// </summary>
        internal bool hasSwimmingPool;

        /// <summary>
        /// The state of a certain coord on the room map.
        /// </summary>
        private squareState[,] sqSTATE;
        /// <summary>
        /// The rotation of the item on a certain coord on the room map.
        /// </summary>
        private byte[,] sqITEMROT;
        /// <summary>
        /// The floorheight of a certain coord on the room's heightmap.
        /// </summary>
        private byte[,] sqFLOORHEIGHT;
        /// <summary>
        /// The height of the item on a certain coord on the room map.
        /// </summary>
        private double[,] sqITEMHEIGHT;
        /// <summary>
        /// Indicates if there is a user/bot/pet on a certain coord of the room map.
        /// </summary>
        private bool[,] sqUNIT;
        /// <summary>
        /// The item stack on a certain coord of the room map.
        /// </summary>
        private furnitureStack[,] sqSTACK;
        public enum squareState { Open = 0, Blocked = 1, Seat = 2, Bed = 3, Rug = 4 };
        squareTrigger[,] sqTRIGGER;

        /// <summary>
        /// The collection that contains the virtualRoomUser objects for the virtual users in this room.
        /// </summary>
        private Hashtable _Users;
        /// <summary>
        /// The collection that contains the virtualBot objects for the bots in this room.
        /// </summary>
        private Hashtable _Bots;
        /// <summary>
        /// The collection that contains the IDs of the virtual user groups that are active in this room.
        /// </summary>
        private HashSet<int> _activeGroups;
        /// <summary>
        /// The thread that handles the @b status updating and walking of virtual unit.
        /// </summary>
        private Thread _statusHandler;
        /// <summary>
        /// The string that contains the status updates for the next cycle of the _statusHandler thread.
        /// </summary>
        private StringBuilder _statusUpdates;

        /// <summary>
        /// The X position of the room's door.
        /// </summary>
        internal int doorX;
        /// <summary>
        /// The Y position of the room's door.
        /// </summary>
        internal int doorY;
        /// <summary>
        /// Publicroom only. The rotation that the user should gain when staying in the room's door.
        /// </summary>
        private byte doorZ;
        /// <summary>
        /// The height that the user should gain when staying in the room's door.
        /// </summary>
        private int doorH;

        //Poll Variables Start
        /// <summary>
        /// Indicates if this room has a poll.
        /// </summary>
        internal bool containsPoll;
        /// <summary>
        /// The poll packet to send.
        /// </summary>
        internal string pollPacket;
        //Poll Variables End

        /// <summary>
        /// Sends timed 'AG' casts to the room, such as disco lights and camera's.
        /// </summary>
        private Thread specialCastHandler;
        #endregion

        #region Constructors/Destructors
        /// <summary>
        /// Initializes a new instance of a virtual room. The room is prepared for usage.
        /// </summary>
        /// <param name="roomID"></param>
        /// <param name="isPublicroom"></param>
        public virtualRoom(int roomID, bool isPublicroom)
        {
            this.roomID = roomID;
            this.isPublicroom = isPublicroom;

            string roomModel = DB.runRead("SELECT model FROM rooms WHERE id = '" + roomID + "'");
            doorX = DB.runRead("SELECT door_x FROM room_modeldata WHERE model = '" + roomModel + "'", null);
            doorY = DB.runRead("SELECT door_y FROM room_modeldata WHERE model = '" + roomModel + "'", null);
            doorH = DB.runRead("SELECT door_h FROM room_modeldata WHERE model = '" + roomModel + "'", null);
            doorZ = byte.Parse(DB.runRead("SELECT door_z FROM room_modeldata WHERE model = '" + roomModel + "'"));
            _Heightmap = DB.runRead("SELECT heightmap FROM room_modeldata WHERE model = '" + roomModel + "'");

            string[] tmpHeightmap = _Heightmap.Split(Convert.ToChar(13));
            int colX = tmpHeightmap[0].Length;
            int colY = tmpHeightmap.Length - 1;

            sqSTATE = new squareState[colX, colY];
            sqFLOORHEIGHT = new byte[colX, colY];
            sqITEMROT = new byte[colX, colY];
            sqITEMHEIGHT = new double[colX, colY];
            sqUNIT = new bool[colX, colY];
            sqSTACK = new furnitureStack[colX, colY];
            sqTRIGGER = new squareTrigger[colX,colY];

            for (int y = 0; y < colY; y++)
            {
                for (int x = 0; x < colX; x++)
                {
                    string _SQ = tmpHeightmap[y].Substring(x, 1).Trim().ToLower();
                    if (_SQ == "x")
                        sqSTATE[x, y] = squareState.Blocked;
                    else
                    {
                        sqSTATE[x, y] = squareState.Open;
                        try { sqFLOORHEIGHT[x, y] = byte.Parse(_SQ); }
                        catch { }
                    }
                }
            }

            if (isPublicroom)
            {
                string[] Items = DB.runRead("SELECT publicroom_items FROM room_modeldata WHERE model = '" + roomModel + "'").Split("\n".ToCharArray());
                for (int i = 0; i < Items.Length; i++)
                {
                    string[] itemData = Items[i].Split(' ');
                    int X = int.Parse(itemData[2]);
                    int Y = int.Parse(itemData[3]);
                    squareState sType = (squareState)int.Parse(itemData[6]);
                    sqSTATE[X, Y] = sType;
                    if (sType == squareState.Seat)
                    {
                        sqITEMROT[X, Y] = byte.Parse(itemData[5]);
                        sqITEMHEIGHT[X, Y] = 1.0;
                    }

                    _publicroomItems += itemData[0] + " " + itemData[1] + " " + itemData[2] + " " + itemData[3] + " " + itemData[4] + " " + itemData[5] + Convert.ToChar(13);
                }

                int[] triggerIDs = DB.runReadColumn("SELECT id FROM room_modeldata_triggers WHERE model = '" + roomModel + "'",0,null);
                if (triggerIDs.Length > 0)
                {
                    for (int i = 0; i < triggerIDs.Length; i++)
                    {
                        string Object = DB.runRead("SELECT object FROM room_modeldata_triggers WHERE id = '" + triggerIDs[i] + "'");
                        int[] Nums = DB.runReadRow("SELECT x,y,goalx,goaly,stepx,stepy,roomid,state FROM room_modeldata_triggers WHERE id = '" + triggerIDs[i] + "'",null);
                        sqTRIGGER[Nums[0], Nums[1]] = new squareTrigger(Object, Nums[2], Nums[3],Nums[4],Nums[5],(Nums[7] == 1),Nums[6]);
                    }
                }

                if(DB.checkExists("SELECT specialcast_interval FROM room_modeldata WHERE model = '" + roomModel + "' AND specialcast_interval > 0"))
                {
                    specialCastHandler = new Thread(new ParameterizedThreadStart(handleSpecialCasts));
                    specialCastHandler.Priority = ThreadPriority.Lowest;
                    specialCastHandler.Start(roomModel);
                }

                hasSwimmingPool = DB.checkExists("SELECT swimmingpool FROM room_modeldata WHERE model = '" + roomModel + "' AND swimmingpool = '1'");

                if (DB.checkExists("SELECT id FROM games_lobbies WHERE id = '" + roomID + "'"))
                {
                    string[] Settings = DB.runReadRow("SELECT type,rank FROM games_lobbies WHERE id = '" + roomID + "'");
                    this.Lobby = new gameLobby(this, (Settings[0] == "bb"), Settings[1]);
                }
            }
            else
            {
                int[] itemIDs = DB.runReadColumn("SELECT id FROM furniture WHERE roomid = '" + roomID + "' ORDER BY h ASC",0,null);
                floorItemManager = new FloorItemManager(this);
                wallItemManager = new WallItemManager(this);

                if(itemIDs.Length > 0)
                {
                    int[] itemTIDs = DB.runReadColumn("SELECT tid FROM furniture WHERE roomid = '" + roomID + "' ORDER BY h ASC",0,null);
                    int[] itemXs = DB.runReadColumn("SELECT x FROM furniture WHERE roomid = '" + roomID + "' ORDER BY h ASC",0,null);
                    int[] itemYs = DB.runReadColumn("SELECT y FROM furniture WHERE roomid = '" + roomID + "' ORDER BY h ASC",0,null);
                    int[] itemZs = DB.runReadColumn("SELECT z FROM furniture WHERE roomid = '" + roomID + "' ORDER BY h ASC",0,null);
                    string[] itemHs = DB.runReadColumn("SELECT h FROM furniture WHERE roomid = '" + roomID + "' ORDER BY h ASC",0);
                    string[] itemVars = DB.runReadColumn("SELECT var FROM furniture WHERE roomid = '" + roomID + "' ORDER BY h ASC",0);
                    string[] itemWallPos = DB.runReadColumn("SELECT wallpos FROM furniture WHERE roomid = '" + roomID + "' ORDER BY h ASC",0);

                    for(int i = 0; i < itemIDs.Length; i++)
                    {
                        if(itemWallPos[i] == "") // Flooritem
                            floorItemManager.addItem(itemIDs[i], itemTIDs[i], itemXs[i], itemYs[i], itemZs[i], double.Parse(itemHs[i]), itemVars[i]);
                        else // Wallitem
                            wallItemManager.addItem(itemIDs[i], itemTIDs[i], itemWallPos[i], itemVars[i], false);
                    }
                }
            }
            _Users = new Hashtable();
            _Bots = new Hashtable();
            loadBots();

            _activeGroups = new HashSet<int>();
            _statusUpdates = new StringBuilder();
            _statusHandler = new Thread(new ThreadStart(cycleStatuses));
            _statusHandler.Start();
            sqSTATE[doorX, doorY] = 0; // Door always walkable
            containsPoll = DB.checkExists("SELECT pid FROM poll WHERE rid = '" + roomID + "'");
            if (containsPoll)
            {
                pollPacket = roomManager.getPoll(roomID);
            }
        }
        /// <summary>
        /// Invoked by CRL garbage collector. Destroys all the remaining objects if all references to this object have been removed.
        /// </summary>
        ~virtualRoom()
        {
            //Holo.Out.WriteLine(".", Out.logFlags.ImportantAction, ConsoleColor.Cyan, ConsoleColor.DarkCyan);
        }
        #endregion

        #region Virtual content properties
        /// <summary>
        /// Returns the heightmap of this virtual room.
        /// </summary>
        internal string Heightmap
        {
            get
            {
                return _Heightmap;
            }
        }
        /// <summary>
        /// Returns a string with all the virtual flooritems in this room.
        /// </summary>
        internal string Flooritems
        {
            get
            {
                if (isPublicroom) // check for lido tiles
                    return "H";

                return floorItemManager.Items;
            }
        }
        /// <summary>
        /// Returns a string with all the virtual wallitems in this room.
        /// </summary>
        internal string Wallitems
        {
            get
            {
                if (isPublicroom)
                    return "";

                return wallItemManager.Items;
            }
        }
        /// <summary>
        /// Returns a string with all the virtual publicroom items in this virtual room.
        /// </summary>
        internal string PublicroomItems
        {
            get
            {
                return _publicroomItems;
            }
        }
        #endregion
    }
}
