using System;
using System.Collections.Generic;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Channels;
using Holo.Protocol;
using TextEncoding = System.Text.Encoding;

namespace Holo.Network;

/// <summary>
/// DotNetty decoder for Habbo B64-encoded packets.
/// Decodes the Base64 length prefix and extracts complete packets.
/// </summary>
public class HabboPacketDecoder : ByteToMessageDecoder
{
    protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
    {
        // Need at least 3 bytes for header (1 byte type + 2 bytes B64 length)
        while (input.ReadableBytes >= 3)
        {
            input.MarkReaderIndex();

            // Read the first byte (packet type indicator, usually '@')
            input.ReadByte();

            // Read 2 bytes for B64-encoded length
            byte[] lengthBytes = new byte[2];
            input.ReadBytes(lengthBytes);
            string lengthB64 = TextEncoding.Default.GetString(lengthBytes);

            int packetLength;
            try
            {
                packetLength = Encoding.decodeB64(lengthB64);
            }
            catch
            {
                // Invalid B64 encoding, skip this byte and try again
                input.ResetReaderIndex();
                input.ReadByte(); // Skip one byte
                continue;
            }

            // Check if we have the complete packet
            if (input.ReadableBytes < packetLength)
            {
                // Not enough data yet, wait for more
                input.ResetReaderIndex();
                return;
            }

            // Read the packet content
            byte[] packetBytes = new byte[packetLength];
            input.ReadBytes(packetBytes);
            string packetContent = TextEncoding.Default.GetString(packetBytes);

            // Output the complete packet for the handler
            output.Add(packetContent);
        }
    }
}
