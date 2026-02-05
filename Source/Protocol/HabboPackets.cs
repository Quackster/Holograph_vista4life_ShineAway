namespace Holo.Protocol
{
    /// <summary>
    /// Contains all Habbo Hotel protocol packet header constants.
    /// Packet headers are two-character codes that identify the packet type.
    /// </summary>
    public static class HabboPackets
    {
        #region Connection & System
        /// <summary>Connection keep-alive ping response</summary>
        public const string PING = "@@";
        /// <summary>Ban reason message</summary>
        public const string BAN_REASON = "@c";
        /// <summary>User rights/permissions (fuse rights)</summary>
        public const string USER_RIGHTS = "@B";
        /// <summary>Initialization complete</summary>
        public const string INIT_COMPLETE = "@C";
        /// <summary>Session parameters</summary>
        public const string SESSION_PARAMS = "DA";
        /// <summary>Session parameters extended</summary>
        public const string SESSION_PARAMS_EXT = "DUIH";
        /// <summary>Current date response</summary>
        public const string CURRENT_DATE = "Bc";
        /// <summary>Second login connection</summary>
        public const string SECOND_CONNECTION = "DbIH";
        /// <summary>Guide status available</summary>
        public const string GUIDE_STATUS = "BKguide";
        /// <summary>Server shutdown countdown</summary>
        public const string SHUTDOWN_COUNTDOWN = "DC";
        #endregion

        #region User Data
        /// <summary>User appearance/settings update</summary>
        public const string USER_APPEARANCE = "@E";
        /// <summary>Credits balance update</summary>
        public const string CREDITS_UPDATE = "@F";
        /// <summary>Tickets balance update</summary>
        public const string TICKETS_UPDATE = "A|";
        /// <summary>Club subscription status</summary>
        public const string CLUB_STATUS = "@G";
        /// <summary>Badge list</summary>
        public const string BADGE_LIST = "Ce";
        /// <summary>Achievement tokens</summary>
        public const string ACHIEVEMENT_TOKENS = "Ft";
        /// <summary>Achievement display</summary>
        public const string ACHIEVEMENT_DISPLAY = "Dt";
        /// <summary>Group status</summary>
        public const string GROUP_STATUS = "Cd";
        /// <summary>Ignored users list</summary>
        public const string IGNORED_USERS = "Fd";
        /// <summary>Filter status</summary>
        public const string FILTER_STATUS = "Fi";
        /// <summary>Friend check response</summary>
        public const string FRIEND_CHECK = "FC";
        #endregion

        #region Messenger
        /// <summary>Friend list initialization</summary>
        public const string FRIEND_LIST = "@L";
        /// <summary>Friend requests list</summary>
        public const string FRIEND_REQUESTS = "Dz";
        /// <summary>Messenger search results</summary>
        public const string MESSENGER_SEARCH = "Fs";
        /// <summary>Instant message receive</summary>
        public const string INSTANT_MESSAGE = "BF";
        /// <summary>Instant message sent</summary>
        public const string INSTANT_MESSAGE_SENT = "BG";
        /// <summary>Buddy request sent</summary>
        public const string BUDDY_REQUEST_SENT = "BD";
        /// <summary>Buddy update</summary>
        public const string BUDDY_UPDATE = "@M";
        /// <summary>Friend update notification</summary>
        public const string FRIEND_UPDATE = "Du";
        /// <summary>Follow friend response</summary>
        public const string FOLLOW_FRIEND = "D^";
        /// <summary>Follow friend error</summary>
        public const string FOLLOW_ERROR = "E]";
        /// <summary>Message delivery error</summary>
        public const string MESSAGE_DELIVERY_ERROR = "DE";
        /// <summary>Console search results header</summary>
        public const string CONSOLE_SEARCH = "HR";
        #endregion

        #region Navigator
        /// <summary>Navigator main response</summary>
        public const string NAVIGATOR_MAIN = @"C\";
        /// <summary>Navigator categories index</summary>
        public const string NAVIGATOR_CATEGORY_INDEX = "C]";
        /// <summary>Navigator categories</summary>
        public const string NAVIGATOR_CATEGORIES = "@P";
        /// <summary>Navigator room listing</summary>
        public const string NAVIGATOR_ROOMS = "@Q";
        /// <summary>Navigator favorite rooms</summary>
        public const string NAVIGATOR_FAVORITES = "@}";
        /// <summary>Navigator owned rooms</summary>
        public const string NAVIGATOR_OWNED = "@S";
        /// <summary>Navigator search results</summary>
        public const string NAVIGATOR_SEARCH = "@w";
        /// <summary>Navigator no search results</summary>
        public const string NAVIGATOR_NO_RESULTS = "@z";
        /// <summary>Navigator no owned rooms</summary>
        public const string NAVIGATOR_NO_OWNED = "@y";
        /// <summary>Navigator recommended rooms</summary>
        public const string NAVIGATOR_RECOMMENDED = "E_";
        /// <summary>Flat info</summary>
        public const string FLAT_INFO = "@j";
        /// <summary>Flat category</summary>
        public const string FLAT_CATEGORY = "@_";
        /// <summary>Room info</summary>
        public const string ROOM_INFO = "Di";
        /// <summary>Room created response</summary>
        public const string ROOM_CREATED = "@{";
        /// <summary>Room modify trigger</summary>
        public const string ROOM_MODIFY = "C^";
        /// <summary>Room userlist (who's in here)</summary>
        public const string ROOM_USERLIST = "C_";
        /// <summary>Favorite room added</summary>
        public const string FAVORITE_ADDED = "DZ";
        /// <summary>Favorite room error</summary>
        public const string FAVORITE_ERROR = "Dj";
        /// <summary>Flat not found</summary>
        public const string FLAT_NOT_FOUND = "@[";
        #endregion

        #region Room Entry & Loading
        /// <summary>Room loading advertisement</summary>
        public const string ROOM_ADVERTISEMENT = "DB";
        /// <summary>Room details/info response</summary>
        public const string ROOM_DETAILS = "@v";
        /// <summary>Room entry ready</summary>
        public const string ROOM_READY = "@v";
        /// <summary>Room floor items</summary>
        public const string ROOM_FLOOR_ITEMS = "@^";
        /// <summary>Room wall items</summary>
        public const string ROOM_WALL_ITEMS = "@`";
        /// <summary>Room heightmap</summary>
        public const string ROOM_HEIGHTMAP = "@m";
        /// <summary>Room users/units</summary>
        public const string ROOM_UNITS = "@b";
        /// <summary>Room statuses</summary>
        public const string ROOM_STATUSES = "@n";
        /// <summary>Room publicroom items</summary>
        public const string ROOM_PUBLIC_ITEMS = "@p";
        /// <summary>Room sprite index</summary>
        public const string ROOM_SPRITE_INDEX = "Dg";
        /// <summary>Doorbell ring received</summary>
        public const string DOORBELL_RING = "BC";
        /// <summary>Doorbell answered</summary>
        public const string DOORBELL_ANSWER = "AC";
        /// <summary>Doorbell denied</summary>
        public const string DOORBELL_DENIED = "BC";
        /// <summary>Room password required</summary>
        public const string ROOM_PASSWORD_REQUIRED = "@a";
        /// <summary>Room password wrong</summary>
        public const string ROOM_PASSWORD_WRONG = "@l";
        /// <summary>Room closed/no access</summary>
        public const string ROOM_CLOSED = "@i";
        /// <summary>Room full</summary>
        public const string ROOM_FULL = "C`";
        /// <summary>Room interstitial</summary>
        public const string ROOM_INTERSTITIAL = "CP";
        /// <summary>Teleporter destination</summary>
        public const string TELEPORTER_DEST = "@~";
        #endregion

        #region Room Actions
        /// <summary>User enters room</summary>
        public const string USER_ENTERS = "@b";
        /// <summary>User exits room</summary>
        public const string USER_EXITS = "@]";
        /// <summary>User status update</summary>
        public const string USER_STATUS = "@n";
        /// <summary>User appearance in room update</summary>
        public const string USER_APPEARANCE_ROOM = "DJ";
        /// <summary>Room rights level</summary>
        public const string ROOM_RIGHTS = "@o";
        /// <summary>Room owner status</summary>
        public const string ROOM_OWNER = "@^";
        /// <summary>Poll data</summary>
        public const string POLL_DATA = "Dc";
        /// <summary>Teleporter enter animation</summary>
        public const string TELEPORTER_ENTER = "AY";
        /// <summary>Teleporter exit animation</summary>
        public const string TELEPORTER_EXIT = @"A\";
        /// <summary>Swimming pool outfit show</summary>
        public const string SWIM_OUTFIT_SHOW = "At";
        /// <summary>Swimming pool dive</summary>
        public const string SWIM_DIVE = "FO";
        #endregion

        #region Chat
        /// <summary>Chat say (proximity-based)</summary>
        public const string CHAT_SAY = "@X";
        /// <summary>Chat shout (room-wide)</summary>
        public const string CHAT_SHOUT = "@Z";
        /// <summary>Chat whisper</summary>
        public const string CHAT_WHISPER = "@Y";
        /// <summary>User typing indicator</summary>
        public const string USER_TYPING = "Ei";
        #endregion

        #region Inventory & Items
        /// <summary>Inventory/hand display</summary>
        public const string INVENTORY_HAND = "BL";
        /// <summary>Item active (item placed/updated)</summary>
        public const string ITEM_ACTIVE = "A_";
        /// <summary>Item data update</summary>
        public const string ITEM_DATA = "AZ";
        /// <summary>Item removed</summary>
        public const string ITEM_REMOVED = "A]";
        /// <summary>Item placement error (tiles blocked)</summary>
        public const string ITEM_PLACE_ERROR = "BJ";
        /// <summary>Sticky note content</summary>
        public const string STICKY_CONTENT = "@p";
        /// <summary>Dimmer presets</summary>
        public const string DIMMER_PRESETS = "Em";
        #endregion

        #region Catalogue
        /// <summary>Catalogue index</summary>
        public const string CATALOGUE_INDEX = "A~";
        /// <summary>Catalogue page content</summary>
        public const string CATALOGUE_PAGE = "A";
        /// <summary>Catalogue refresh</summary>
        public const string CATALOGUE_REFRESH = "HKRC";
        /// <summary>Transaction failed (not enough credits)</summary>
        public const string TRANSACTION_FAILED = "AD";
        /// <summary>User not found (gift/transfer)</summary>
        public const string USER_NOT_FOUND = "AL";
        /// <summary>Present opened</summary>
        public const string PRESENT_OPENED = "D^";
        #endregion

        #region Recycler
        /// <summary>Recycler setup</summary>
        public const string RECYCLER_SETUP = "Do";
        /// <summary>Recycler session status</summary>
        public const string RECYCLER_SESSION = "Dp";
        /// <summary>Recycler finished</summary>
        public const string RECYCLER_FINISHED = "Dq";
        /// <summary>Recycler confirmed</summary>
        public const string RECYCLER_CONFIRMED = "Ds";
        /// <summary>Recycler cancelled</summary>
        public const string RECYCLER_CANCELLED = "Dr";
        /// <summary>Recycler started</summary>
        public const string RECYCLER_STARTED = "EC";
        #endregion

        #region Trading
        /// <summary>Trade window open</summary>
        public const string TRADE_OPEN = "Ah";
        /// <summary>Trade completed</summary>
        public const string TRADE_COMPLETE = "Al";
        /// <summary>Trade cancelled/abort</summary>
        public const string TRADE_ABORT = "An";
        #endregion

        #region Voucher
        /// <summary>Voucher redeemed</summary>
        public const string VOUCHER_REDEEMED = "CT";
        /// <summary>Voucher invalid</summary>
        public const string VOUCHER_INVALID = "CU1";
        #endregion

        #region Soundmachine & Jukebox
        /// <summary>Sound machine songs list</summary>
        public const string SOUNDMACHINE_SONGS = "EB";
        /// <summary>Sound machine playlist</summary>
        public const string SOUNDMACHINE_PLAYLIST = "EC";
        /// <summary>Song now playing</summary>
        public const string SONG_PLAYING = "EK";
        /// <summary>Song stopped</summary>
        public const string SONG_STOPPED = "El";
        /// <summary>Sound machine playlists available</summary>
        public const string SOUNDMACHINE_PLAYLISTS = "Dl";
        /// <summary>Song editor playlist</summary>
        public const string SONG_EDITOR_PLAYLIST = "Dn";
        /// <summary>Song editor samples</summary>
        public const string SONG_EDITOR_SAMPLES = "Dm";
        /// <summary>Song editor write permission</summary>
        public const string SONG_EDITOR_PERMISSION = "Dz";
        #endregion

        #region Games
        /// <summary>Game lobby info</summary>
        public const string GAME_LOBBY_INFO = "Cg";
        /// <summary>Game list</summary>
        public const string GAME_LIST = "Ch";
        /// <summary>Game sub info</summary>
        public const string GAME_SUB_INFO = "Ci";
        /// <summary>Game create result</summary>
        public const string GAME_CREATE_RESULT = "Ck";
        /// <summary>Game started</summary>
        public const string GAME_STARTED = "Cl";
        /// <summary>Game ended</summary>
        public const string GAME_ENDED = "Cm";
        /// <summary>Game loading</summary>
        public const string GAME_LOADING = "Cq";
        /// <summary>Game countdown</summary>
        public const string GAME_COUNTDOWN = "Cw";
        /// <summary>Game status update</summary>
        public const string GAME_STATUS = "Ct";
        /// <summary>Game results/scores</summary>
        public const string GAME_RESULTS = "Cx";
        /// <summary>Game player joined</summary>
        public const string GAME_PLAYER_JOINED = "Cr";
        /// <summary>Game player exited</summary>
        public const string GAME_PLAYER_EXIT = "FS";
        #endregion

        #region Events & Voting
        /// <summary>Event setup/category count</summary>
        public const string EVENT_SETUP = "Ep";
        /// <summary>Event host button visibility</summary>
        public const string EVENT_BUTTON = "Eo";
        /// <summary>Event category OK response</summary>
        public const string EVENT_CATEGORY_OK = "Eb";
        /// <summary>Event category listing</summary>
        public const string EVENT_CATEGORY = "Eq";
        /// <summary>Event info</summary>
        public const string EVENT_INFO = "Er";
        /// <summary>Vote counter update</summary>
        public const string VOTE_UPDATE = "EY";
        #endregion

        #region Moderation
        /// <summary>Moderation tool/panel</summary>
        public const string MOD_PANEL = "DH";
        /// <summary>Moderation reply</summary>
        public const string MOD_REPLY = "DR";
        /// <summary>Room alert</summary>
        public const string ROOM_ALERT = "B!";
        /// <summary>System broadcast message</summary>
        public const string SYSTEM_BROADCAST = "BK";
        /// <summary>User kick message</summary>
        public const string USER_KICK = "@R";
        /// <summary>Moderation events</summary>
        public const string MOD_EVENT = "EA";
        /// <summary>User alert</summary>
        public const string USER_ALERT = "B!";
        #endregion

        #region Special Effects
        /// <summary>Special cast (disco lights, camera, etc)</summary>
        public const string SPECIAL_CAST = "AG";
        /// <summary>Wave animation</summary>
        public const string WAVE_ANIMATION = "As";
        #endregion
    }
}
