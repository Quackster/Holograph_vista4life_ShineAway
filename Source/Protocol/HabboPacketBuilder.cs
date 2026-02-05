using System;
using System.Text;

namespace Holo.Protocol
{
    /// <summary>
    /// Fluent builder for constructing Habbo Hotel protocol packets.
    /// Provides a type-safe, readable way to build complex packet strings.
    /// </summary>
    public class HabboPacketBuilder
    {
        private readonly StringBuilder _packet;

        #region Constructors
        /// <summary>
        /// Creates a new packet builder with the specified header.
        /// </summary>
        /// <param name="header">The two-character packet header (e.g., "@F", "BK")</param>
        public HabboPacketBuilder(string header)
        {
            _packet = new StringBuilder(header);
        }

        /// <summary>
        /// Creates a new empty packet builder.
        /// </summary>
        public HabboPacketBuilder()
        {
            _packet = new StringBuilder();
        }
        #endregion

        #region Basic Append Methods
        /// <summary>
        /// Appends a raw string value to the packet.
        /// </summary>
        public HabboPacketBuilder Append(string value)
        {
            _packet.Append(value);
            return this;
        }

        /// <summary>
        /// Appends an integer value as a string.
        /// </summary>
        public HabboPacketBuilder Append(int value)
        {
            _packet.Append(value);
            return this;
        }

        /// <summary>
        /// Appends a character.
        /// </summary>
        public HabboPacketBuilder Append(char value)
        {
            _packet.Append(value);
            return this;
        }

        /// <summary>
        /// Appends a boolean as "true" or "false" string.
        /// </summary>
        public HabboPacketBuilder Append(bool value)
        {
            _packet.Append(value.ToString().ToLower());
            return this;
        }
        #endregion

        #region Encoded Values
        /// <summary>
        /// Appends an integer encoded as VL64.
        /// </summary>
        public HabboPacketBuilder AppendVL64(int value)
        {
            _packet.Append(Encoding.encodeVL64(value));
            return this;
        }

        /// <summary>
        /// Appends an integer encoded as Base64 with specified length.
        /// </summary>
        public HabboPacketBuilder AppendB64(int value, int length)
        {
            _packet.Append(Encoding.encodeB64(value, length));
            return this;
        }

        /// <summary>
        /// Appends a boolean as VL64 (1 or 0).
        /// </summary>
        public HabboPacketBuilder AppendBoolVL64(bool value)
        {
            _packet.Append(Encoding.encodeVL64(value ? 1 : 0));
            return this;
        }

        /// <summary>
        /// Appends a boolean as Habbo protocol string (I or H).
        /// </summary>
        public HabboPacketBuilder AppendBoolHabbo(bool value)
        {
            _packet.Append(value ? HabboProtocol.BOOL_TRUE : HabboProtocol.BOOL_FALSE);
            return this;
        }
        #endregion

        #region Separators
        /// <summary>
        /// Appends the primary field separator (char 2).
        /// </summary>
        public HabboPacketBuilder Separator()
        {
            _packet.Append(HabboProtocol.FIELD_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends a custom separator character.
        /// </summary>
        public HabboPacketBuilder Separator(char separator)
        {
            _packet.Append(separator);
            return this;
        }

        /// <summary>
        /// Appends the record separator (char 13).
        /// </summary>
        public HabboPacketBuilder RecordSeparator()
        {
            _packet.Append(HabboProtocol.RECORD_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends the tab separator (char 9).
        /// </summary>
        public HabboPacketBuilder TabSeparator()
        {
            _packet.Append(HabboProtocol.TAB_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends the item field separator (char 30).
        /// </summary>
        public HabboPacketBuilder ItemSeparator()
        {
            _packet.Append(HabboProtocol.ITEM_FIELD_SEPARATOR);
            return this;
        }
        #endregion

        #region Common Patterns
        /// <summary>
        /// Appends a string value followed by the field separator.
        /// </summary>
        public HabboPacketBuilder AppendField(string value)
        {
            _packet.Append(value);
            _packet.Append(HabboProtocol.FIELD_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends an integer value followed by the field separator.
        /// </summary>
        public HabboPacketBuilder AppendField(int value)
        {
            _packet.Append(value);
            _packet.Append(HabboProtocol.FIELD_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends a VL64 encoded integer followed by a string and separator.
        /// </summary>
        public HabboPacketBuilder AppendVL64Field(int value, string text)
        {
            _packet.Append(Encoding.encodeVL64(value));
            _packet.Append(text);
            _packet.Append(HabboProtocol.FIELD_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends user info (username + separator).
        /// </summary>
        public HabboPacketBuilder AppendUsername(string username)
        {
            _packet.Append(username);
            _packet.Append(HabboProtocol.FIELD_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends a slash-terminated value (used in item lists).
        /// </summary>
        public HabboPacketBuilder AppendSlashTerminated(string value)
        {
            _packet.Append(value);
            _packet.Append("/");
            return this;
        }
        #endregion

        #region Item Building Helpers
        /// <summary>
        /// Starts a new inventory item record (SI format).
        /// </summary>
        public HabboPacketBuilder StartInventoryItem()
        {
            _packet.Append("SI");
            _packet.Append(HabboProtocol.ITEM_FIELD_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends an inventory item field with separator.
        /// </summary>
        public HabboPacketBuilder AppendItemField(string value)
        {
            _packet.Append(value);
            _packet.Append(HabboProtocol.ITEM_FIELD_SEPARATOR);
            return this;
        }

        /// <summary>
        /// Appends an inventory item field with separator.
        /// </summary>
        public HabboPacketBuilder AppendItemField(int value)
        {
            _packet.Append(value);
            _packet.Append(HabboProtocol.ITEM_FIELD_SEPARATOR);
            return this;
        }
        #endregion

        #region User Data Helpers
        /// <summary>
        /// Appends user room UID as VL64.
        /// </summary>
        public HabboPacketBuilder AppendRoomUID(int roomUID)
        {
            _packet.Append(Encoding.encodeVL64(roomUID));
            return this;
        }

        /// <summary>
        /// Appends coordinates (X, Y) as VL64.
        /// </summary>
        public HabboPacketBuilder AppendCoords(int x, int y)
        {
            _packet.Append(Encoding.encodeVL64(x));
            _packet.Append(Encoding.encodeVL64(y));
            return this;
        }

        /// <summary>
        /// Appends coordinates (X, Y, H) as VL64.
        /// </summary>
        public HabboPacketBuilder AppendCoords(int x, int y, int h)
        {
            _packet.Append(Encoding.encodeVL64(x));
            _packet.Append(Encoding.encodeVL64(y));
            _packet.Append(Encoding.encodeVL64(h));
            return this;
        }

        /// <summary>
        /// Appends position with rotation (X, Y, H, Z) as VL64.
        /// </summary>
        public HabboPacketBuilder AppendPosition(int x, int y, int h, int z)
        {
            _packet.Append(Encoding.encodeVL64(x));
            _packet.Append(Encoding.encodeVL64(y));
            _packet.Append(Encoding.encodeVL64(h));
            _packet.Append(Encoding.encodeVL64(z));
            return this;
        }
        #endregion

        #region Conditional Methods
        /// <summary>
        /// Conditionally appends a value if the condition is true.
        /// </summary>
        public HabboPacketBuilder AppendIf(bool condition, string value)
        {
            if (condition)
                _packet.Append(value);
            return this;
        }

        /// <summary>
        /// Conditionally appends a VL64 value if the condition is true.
        /// </summary>
        public HabboPacketBuilder AppendVL64If(bool condition, int value)
        {
            if (condition)
                _packet.Append(Encoding.encodeVL64(value));
            return this;
        }

        /// <summary>
        /// Appends different values based on condition.
        /// </summary>
        public HabboPacketBuilder AppendChoice(bool condition, string trueValue, string falseValue)
        {
            _packet.Append(condition ? trueValue : falseValue);
            return this;
        }
        #endregion

        #region StringBuilder Integration
        /// <summary>
        /// Appends the contents of a StringBuilder.
        /// </summary>
        public HabboPacketBuilder Append(StringBuilder sb)
        {
            _packet.Append(sb);
            return this;
        }

        /// <summary>
        /// Gets the internal StringBuilder for advanced manipulation.
        /// </summary>
        public StringBuilder GetBuilder()
        {
            return _packet;
        }
        #endregion

        #region Build Methods
        /// <summary>
        /// Builds and returns the final packet string.
        /// </summary>
        public string Build()
        {
            return _packet.ToString();
        }

        /// <summary>
        /// Returns the current length of the packet.
        /// </summary>
        public int Length => _packet.Length;

        /// <summary>
        /// Implicit conversion to string.
        /// </summary>
        public static implicit operator string(HabboPacketBuilder builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// Returns the packet string.
        /// </summary>
        public override string ToString()
        {
            return _packet.ToString();
        }
        #endregion

        #region Static Factory Methods
        /// <summary>
        /// Creates a simple packet with just a header.
        /// </summary>
        public static string Simple(string header)
        {
            return header;
        }

        /// <summary>
        /// Creates a packet with a header and single value.
        /// </summary>
        public static string WithValue(string header, string value)
        {
            return header + value;
        }

        /// <summary>
        /// Creates a packet with a header and VL64 encoded value.
        /// </summary>
        public static string WithVL64(string header, int value)
        {
            return header + Encoding.encodeVL64(value);
        }

        /// <summary>
        /// Creates a credits update packet.
        /// </summary>
        public static string CreditsUpdate(int credits)
        {
            return HabboPackets.CREDITS_UPDATE + credits;
        }

        /// <summary>
        /// Creates a tickets update packet.
        /// </summary>
        public static string TicketsUpdate(int tickets)
        {
            return HabboPackets.TICKETS_UPDATE + tickets;
        }

        /// <summary>
        /// Creates a system broadcast message packet.
        /// </summary>
        public static string SystemMessage(string message)
        {
            return HabboPackets.SYSTEM_BROADCAST + message;
        }

        /// <summary>
        /// Creates a room alert packet.
        /// </summary>
        public static string RoomAlert(string message)
        {
            return HabboPackets.ROOM_ALERT + message + HabboProtocol.FIELD_SEPARATOR;
        }
        #endregion
    }
}
