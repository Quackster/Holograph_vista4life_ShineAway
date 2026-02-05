namespace Holo.Protocol
{
    /// <summary>
    /// Contains Habbo Hotel protocol constants including separators, limits, and configuration values.
    /// </summary>
    public static class HabboProtocol
    {
        #region Separator Characters
        /// <summary>Packet terminator character (char 1) - automatically appended by sendData</summary>
        public const char PACKET_TERMINATOR = '\x01';

        /// <summary>Primary field separator (char 2) - used to delimit fields in structured data</summary>
        public const char FIELD_SEPARATOR = '\x02';

        /// <summary>Tab separator (char 9) - used in advertisements and trade data</summary>
        public const char TAB_SEPARATOR = '\x09';

        /// <summary>Record/line separator (char 13) - used for multi-record data</summary>
        public const char RECORD_SEPARATOR = '\x0D';

        /// <summary>Item field separator (char 30) - used in inventory item parsing</summary>
        public const char ITEM_FIELD_SEPARATOR = '\x1E';
        #endregion

        #region Chat & Proximity
        /// <summary>Maximum distance (in squares) for proximity-based chat</summary>
        public const int CHAT_PROXIMITY_RANGE = 5;

        /// <summary>Distance check value for chat (CHAT_PROXIMITY_RANGE + 1)</summary>
        public const int CHAT_PROXIMITY_CHECK = 6;
        #endregion

        #region Inventory & Trading
        /// <summary>Number of items per page in hand/inventory</summary>
        public const int HAND_PAGE_SIZE = 9;

        /// <summary>Maximum number of items in a trade</summary>
        public const int TRADE_ITEMS_MAX = 65;

        /// <summary>Maximum furniture stack size</summary>
        public const int FURNITURE_STACK_MAX = 20;
        #endregion

        #region Navigator
        /// <summary>Maximum number of rooms returned by navigator search</summary>
        public const int NAVIGATOR_MAX_ROOMS = 10000;

        /// <summary>Maximum number of favorite rooms</summary>
        public const int MAX_FAVORITE_ROOMS = 30;

        /// <summary>Maximum number of friends in search results</summary>
        public const int MESSENGER_SEARCH_LIMIT = 50;
        #endregion

        #region Room Limits
        /// <summary>Default maximum users per room</summary>
        public const int DEFAULT_MAX_VISITORS = 25;

        /// <summary>Maximum room description length</summary>
        public const int MAX_ROOM_DESCRIPTION = 255;

        /// <summary>Maximum room name length</summary>
        public const int MAX_ROOM_NAME = 30;
        #endregion

        #region Status & Animation Timings
        /// <summary>Duration of wave animation in seconds</summary>
        public const int WAVE_DURATION_SECONDS = 2;

        /// <summary>Duration of carry item display in seconds</summary>
        public const int CARRY_DURATION_SECONDS = 120;

        /// <summary>Duration of dance animation before stopping (0 = infinite)</summary>
        public const int DANCE_DURATION_SECONDS = 0;

        /// <summary>Thread sleep interval for status handler (milliseconds)</summary>
        public const int STATUS_CYCLE_INTERVAL_MS = 470;

        /// <summary>Disconnect delay in milliseconds</summary>
        public const int DISCONNECT_DELAY_MS = 1000;
        #endregion

        #region Game Settings
        /// <summary>Number of tickets deducted per game</summary>
        public const int GAME_TICKET_COST = 2;

        /// <summary>BattleBall tile state score multipliers</summary>
        public const int BB_SCORE_TOUCHED = 1;
        public const int BB_SCORE_CLICKED = 3;
        public const int BB_SCORE_PRESSED = 5;
        public const int BB_SCORE_SEALED = 7;
        #endregion

        #region Encoding Constants
        /// <summary>Base offset for Base64 encoding</summary>
        public const int BASE64_OFFSET = 64;

        /// <summary>Bit mask for VL64/B64 encoding</summary>
        public const int ENCODING_MASK = 0x3F;

        /// <summary>VL64 negative indicator bit position</summary>
        public const int VL64_NEGATIVE_BIT = 4;

        /// <summary>Initial shift amount for VL64 decoding</summary>
        public const int VL64_INITIAL_SHIFT = 3;

        /// <summary>Shift amount per iteration in VL64 encoding</summary>
        public const int VL64_SHIFT_AMOUNT = 6;
        #endregion

        #region Bot & AI Settings
        /// <summary>Chance (1 in N) for bot to respond to shout with "don't shout" message</summary>
        public const int BOT_SHOUT_RESPONSE_CHANCE = 10;

        /// <summary>Delay between bot chat messages in milliseconds</summary>
        public const int BOT_CHAT_DELAY_MS = 3000;
        #endregion

        #region Swimming Pool
        /// <summary>Default swim outfit color</summary>
        public const string DEFAULT_SWIM_OUTFIT = "ch=s02/";

        /// <summary>Diving board height offset</summary>
        public const double DIVING_BOARD_HEIGHT = 1.0;
        #endregion

        #region Pet Settings
        /// <summary>Default pet colour (hex)</summary>
        public const string DEFAULT_PET_COLOUR = "98A1C5";

        /// <summary>Pet type: Cat</summary>
        public const string PET_TYPE_CAT = "1";
        /// <summary>Pet type: Dog</summary>
        public const string PET_TYPE_DOG = "2";
        /// <summary>Pet type: Croc</summary>
        public const string PET_TYPE_CROC = "3";
        #endregion

        #region Boolean String Values
        /// <summary>Boolean true as Habbo protocol string</summary>
        public const string BOOL_TRUE = "I";

        /// <summary>Boolean false as Habbo protocol string</summary>
        public const string BOOL_FALSE = "H";

        /// <summary>Alternative boolean values (numeric)</summary>
        public const string NUMERIC_TRUE = "1";
        public const string NUMERIC_FALSE = "0";
        #endregion

        #region Room Access States
        /// <summary>Room is open/public</summary>
        public const int ROOM_STATE_OPEN = 0;
        /// <summary>Room requires doorbell</summary>
        public const int ROOM_STATE_DOORBELL = 1;
        /// <summary>Room requires password</summary>
        public const int ROOM_STATE_PASSWORD = 2;
        #endregion

        #region User Ranks
        /// <summary>Minimum rank for moderation commands</summary>
        public const byte RANK_MODERATOR = 4;
        /// <summary>Minimum rank for admin commands</summary>
        public const byte RANK_ADMIN = 6;
        /// <summary>Minimum rank for superadmin commands</summary>
        public const byte RANK_SUPERADMIN = 7;
        #endregion
    }
}
