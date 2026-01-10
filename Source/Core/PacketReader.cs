using System;
using System.Text;
using Holo;

namespace Holo.Core
{
    /// <summary>
    /// Provides fluent packet reading with abstract pop methods for strings and integers.
    /// </summary>
    public class PacketReader
    {
        private string _packet;
        private int _position;

        /// <summary>
        /// Initializes a new PacketReader with the packet data.
        /// </summary>
        /// <param name="packet">The packet data to read from.</param>
        public PacketReader(string packet)
        {
            _packet = packet ?? string.Empty;
            _position = 0;
        }

        /// <summary>
        /// Gets the remaining packet length.
        /// </summary>
        public int Remaining => _packet.Length - _position;

        /// <summary>
        /// Gets whether there is more data to read.
        /// </summary>
        public bool HasMore => _position < _packet.Length;

        /// <summary>
        /// Pops a string from the packet. Reads until the next delimiter (char 2) or end of packet.
        /// </summary>
        /// <param name="delimiter">The delimiter character (default is char 2).</param>
        /// <returns>The string value.</returns>
        public string PopString(char delimiter = (char)2)
        {
            if (!HasMore)
                return string.Empty;

            int delimiterIndex = _packet.IndexOf(delimiter, _position);
            if (delimiterIndex == -1)
            {
                string result = _packet.Substring(_position);
                _position = _packet.Length;
                return result;
            }

            string value = _packet.Substring(_position, delimiterIndex - _position);
            _position = delimiterIndex + 1;
            return value;
        }

        /// <summary>
        /// Pops a string with a specified length from the packet.
        /// </summary>
        /// <param name="length">The length of the string to read.</param>
        /// <returns>The string value.</returns>
        public string PopString(int length)
        {
            if (!HasMore || length <= 0)
                return string.Empty;

            if (_position + length > _packet.Length)
                length = _packet.Length - _position;

            string value = _packet.Substring(_position, length);
            _position += length;
            return value;
        }

        /// <summary>
        /// Pops a Base64-encoded length string, then reads that many characters.
        /// </summary>
        /// <returns>The string value.</returns>
        public string PopB64String()
        {
            if (!HasMore || _position + 2 > _packet.Length)
                return string.Empty;

            int length = Encoding.decodeB64(_packet.Substring(_position, 2));
            _position += 2;
            return PopString(length);
        }

        /// <summary>
        /// Pops a VL64-encoded integer from the packet.
        /// </summary>
        /// <returns>The integer value.</returns>
        public int PopVL64()
        {
            if (!HasMore)
                return 0;

            try
            {
                int startPos = _position;
                char[] raw = _packet.ToCharArray();
                int pos = _position;
                int v = 0;
                bool negative = (raw[pos] & 4) == 4;
                int totalBytes = raw[pos] >> 3 & 7;
                v = raw[pos] & 3;
                pos++;
                int shiftAmount = 2;
                for (int b = 1; b < totalBytes; b++)
                {
                    if (pos >= raw.Length)
                        break;
                    v |= (raw[pos] & 0x3f) << shiftAmount;
                    shiftAmount = 2 + 6 * b;
                    pos++;
                }

                if (negative)
                    v *= -1;

                _position = pos;
                return v;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Pops a regular integer by parsing the string until a delimiter or end.
        /// </summary>
        /// <param name="delimiter">The delimiter character (default is char 2).</param>
        /// <returns>The integer value.</returns>
        public int PopInt(char delimiter = (char)2)
        {
            string value = PopString(delimiter);
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        /// <summary>
        /// Pops a boolean value (reads a single character and checks if it's 'A' or 'I').
        /// </summary>
        /// <returns>The boolean value.</returns>
        public bool PopBool()
        {
            if (!HasMore)
                return false;

            char value = _packet[_position];
            _position++;
            return value == 'A' || value == 'I';
        }

        /// <summary>
        /// Pops a single character from the packet.
        /// </summary>
        /// <returns>The character value.</returns>
        public char PopChar()
        {
            if (!HasMore)
                return (char)0;

            char value = _packet[_position];
            _position++;
            return value;
        }

        /// <summary>
        /// Skips a specified number of characters.
        /// </summary>
        /// <param name="count">The number of characters to skip.</param>
        /// <returns>The PacketReader instance for fluent chaining.</returns>
        public PacketReader Skip(int count)
        {
            _position = Math.Min(_position + count, _packet.Length);
            return this;
        }

        /// <summary>
        /// Gets the remaining packet data as a string.
        /// </summary>
        /// <returns>The remaining packet data.</returns>
        public string GetRemaining()
        {
            if (!HasMore)
                return string.Empty;

            return _packet.Substring(_position);
        }


        /// <summary>
        /// Resets the reader position to the beginning.
        /// </summary>
        /// <returns>The PacketReader instance for fluent chaining.</returns>
        public PacketReader Reset()
        {
            _position = 0;
            return this;
        }

        /// <summary>
        /// Gets the packet header (first 2 characters).
        /// </summary>
        /// <returns>The packet header.</returns>
        public string GetHeader()
        {
            if (_packet.Length < 2)
                return string.Empty;
            return _packet.Substring(0, 2);
        }
    }
}
