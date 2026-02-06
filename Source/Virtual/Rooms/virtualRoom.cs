using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Holo.Managers;
using Holo.Virtual.Users;
using Holo.Virtual.Rooms.Bots;
using Holo.Virtual.Rooms.Items;
using Holo.Virtual.Rooms.Games;
using Holo.Data.Repositories.Rooms;
using Holo.Data.Repositories.Furniture;
using Holo.Data.Repositories.Bots;
using Holo.Data.Repositories.Games;

namespace Holo.Virtual.Rooms;


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
        private Dictionary<int, virtualRoomUser> _Users = new();
        /// <summary>
        /// The collection that contains the virtualBot objects for the bots in this room.
        /// </summary>
        private Dictionary<int, virtualBot> _Bots = new();
        /// <summary>
        /// The collection that contains the IDs of the virtual user groups that are active in this room.
        /// </summary>
        private HashSet<int> _activeGroups;
        /// <summary>
        /// The cancellation token source for stopping background tasks when the room is destroyed.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource;
        /// <summary>
        /// The string that contains the status updates for the next cycle of the status handler task.
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

            string roomModel = RoomRepository.Instance.GetRoomModel(roomID) ?? "";
            doorX = RoomModelRepository.Instance.GetDoorX(roomModel);
            doorY = RoomModelRepository.Instance.GetDoorY(roomModel);
            doorH = RoomModelRepository.Instance.GetDoorH(roomModel);
            doorZ = byte.Parse(RoomModelRepository.Instance.GetDoorZ(roomModel) ?? "0");
            _Heightmap = RoomModelRepository.Instance.GetHeightmap(roomModel) ?? "";

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
                string publicroomItemsData = RoomModelRepository.Instance.GetPublicroomItems(roomModel) ?? "";
                string[] Items = publicroomItemsData.Split("\n".ToCharArray());
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

                int[] triggerIDs = RoomModelRepository.Instance.GetTriggerIds(roomModel);
                if (triggerIDs.Length > 0)
                {
                    for (int i = 0; i < triggerIDs.Length; i++)
                    {
                        string Object = RoomModelRepository.Instance.GetTriggerObject(triggerIDs[i]) ?? "";
                        int[] Nums = RoomModelRepository.Instance.GetTriggerData(triggerIDs[i]);
                        sqTRIGGER[Nums[0], Nums[1]] = new squareTrigger(Object, Nums[2], Nums[3],Nums[4],Nums[5],(Nums[7] == 1),Nums[6]);
                    }
                }

                if(RoomModelRepository.Instance.HasSpecialCast(roomModel))
                {
                    _ = HandleSpecialCastsAsync(roomModel, _cancellationTokenSource.Token);
                }

                hasSwimmingPool = RoomModelRepository.Instance.HasSwimmingPool(roomModel);

                if (GameRepository.Instance.IsGameLobby(roomID))
                {
                    string[] Settings = GameRepository.Instance.GetLobbySettings(roomID);
                    this.Lobby = new gameLobby(this, (Settings[0] == "bb"), Settings[1]);
                }
            }
            else
            {
                int[] itemIDs = FurnitureRepository.Instance.GetRoomItemIds(roomID);
                floorItemManager = new FloorItemManager(this);
                wallItemManager = new WallItemManager(this);

                if(itemIDs.Length > 0)
                {
                    int[] itemTIDs = FurnitureRepository.Instance.GetRoomItemTemplateIds(roomID);
                    int[] itemXs = FurnitureRepository.Instance.GetRoomItemXs(roomID);
                    int[] itemYs = FurnitureRepository.Instance.GetRoomItemYs(roomID);
                    int[] itemZs = FurnitureRepository.Instance.GetRoomItemZs(roomID);
                    string[] itemHs = FurnitureRepository.Instance.GetRoomItemHs(roomID);
                    string[] itemVars = FurnitureRepository.Instance.GetRoomItemVars(roomID);
                    string[] itemWallPos = FurnitureRepository.Instance.GetRoomItemWallPositions(roomID);

                    for(int i = 0; i < itemIDs.Length; i++)
                    {
                        if(itemWallPos[i] == "") // Flooritem
                            floorItemManager.addItem(itemIDs[i], itemTIDs[i], itemXs[i], itemYs[i], itemZs[i], double.Parse(itemHs[i]), itemVars[i]);
                        else // Wallitem
                            wallItemManager.addItem(itemIDs[i], itemTIDs[i], itemWallPos[i], itemVars[i], false);
                    }
                }
            }
            _Users = new Dictionary<int, virtualRoomUser>();
            _Bots = new Dictionary<int, virtualBot>();
            loadBots();

            _activeGroups = new HashSet<int>();
            _statusUpdates = new StringBuilder();
            _cancellationTokenSource = new CancellationTokenSource();
            _ = CycleStatusesAsync(_cancellationTokenSource.Token);
            sqSTATE[doorX, doorY] = 0; // Door always walkable
            containsPoll = PollRepository.Instance.RoomHasPoll(roomID);
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
