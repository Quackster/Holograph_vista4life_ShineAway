using System;
using System.Text;
using Holo;

namespace Holo.Core
{
    /// <summary>
    /// Provides fluent packet building for sending packets to clients.
    /// </summary>
    public class PacketBuilder
    {
        private StringBuilder _builder;

        /// <summary>
        /// Initializes a new PacketBuilder with an optional header.
        /// </summary>
        /// <param name="header">The packet header (e.g., "CD", "@L").</param>
        public PacketBuilder(string header = "")
        {
            _builder = new StringBuilder();
            if (!string.IsNullOrEmpty(header))
                _builder.Append(header);
        }

        /// <summary>
        /// Appends a string to the packet.
        /// </summary>
        /// <param name="value">The string value to append.</param>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder Append(string value)
        {
            _builder.Append(value);
            return this;
        }

        /// <summary>
        /// Appends a VL64-encoded integer to the packet.
        /// </summary>
        /// <param name="value">The integer value to append.</param>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder AppendVL64(int value)
        {
            _builder.Append(Encoding.encodeVL64(value));
            return this;
        }

        /// <summary>
        /// Appends a Base64-encoded length string, followed by the string itself.
        /// </summary>
        /// <param name="value">The string value to append.</param>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder AppendB64String(string value)
        {
            if (value == null)
                value = string.Empty;
            _builder.Append(Encoding.encodeB64(value));
            _builder.Append(value);
            return this;
        }

        /// <summary>
        /// Appends a string with a delimiter (default is char 2).
        /// </summary>
        /// <param name="value">The string value to append.</param>
        /// <param name="delimiter">The delimiter character (default is char 2).</param>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder AppendString(string value, char delimiter = (char)2)
        {
            _builder.Append(value);
            _builder.Append(delimiter);
            return this;
        }

        /// <summary>
        /// Appends an integer as a string.
        /// </summary>
        /// <param name="value">The integer value to append.</param>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder AppendInt(int value)
        {
            _builder.Append(value);
            return this;
        }

        /// <summary>
        /// Appends a character to the packet.
        /// </summary>
        /// <param name="value">The character value to append.</param>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder AppendChar(char value)
        {
            _builder.Append(value);
            return this;
        }

        /// <summary>
        /// Appends a boolean value ('A' for true, 'H' for false).
        /// </summary>
        /// <param name="value">The boolean value to append.</param>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder AppendBool(bool value)
        {
            _builder.Append(value ? 'A' : 'H');
            return this;
        }

        /// <summary>
        /// Appends a delimiter character (default is char 2).
        /// </summary>
        /// <param name="delimiter">The delimiter character (default is char 2).</param>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder AppendDelimiter(char delimiter = (char)2)
        {
            _builder.Append(delimiter);
            return this;
        }

        /// <summary>
        /// Builds and returns the packet as a string.
        /// </summary>
        /// <returns>The packet string.</returns>
        public string Build()
        {
            return _builder.ToString();
        }

        /// <summary>
        /// Clears the packet builder.
        /// </summary>
        /// <returns>The PacketBuilder instance for fluent chaining.</returns>
        public PacketBuilder Clear()
        {
            _builder.Clear();
            return this;
        }

        /// <summary>
        /// Implicit conversion to string for easy usage.
        /// </summary>
        public static implicit operator string(PacketBuilder builder)
        {
            return builder.Build();
        }

        /// <summary>
        /// Creates a new PacketBuilder with a header.
        /// </summary>
        /// <param name="header">The packet header.</param>
        /// <returns>A new PacketBuilder instance.</returns>
        public static PacketBuilder Create(string header)
        {
            return new PacketBuilder(header);
        }
    }
}
