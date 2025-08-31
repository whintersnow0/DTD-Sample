using System;

namespace NetworkSystem.Protocol
{
    [Flags]
    public enum PacketFlags : byte
    {
        None = 0,
        Reliable = 1,
        Ordered = 2,
        Fragmented = 4
    }

    public enum PacketType : byte
    {
        PlayerInput = 1,
        PlayerState = 2,
        GameEvent = 3,
        Heartbeat = 4,
        Acknowledgment = 5
    }

    public struct NetworkPacketHeader
    {
        public uint SequenceNumber;
        public uint AckNumber;
        public PacketFlags Flags;
        public PacketType Type;
        public ushort PayloadSize;

        public const int Size = 12;
    }

    public class NetworkPacket
    {
        public NetworkPacketHeader Header;
        public byte[] Payload;
        public float SendTime;
        public int RetryCount;

        public NetworkPacket()
        {
            SendTime = UnityEngine.Time.time;
        }

        public NetworkPacket(PacketType type, byte[] payload, bool reliable = false)
        {
            Header = new NetworkPacketHeader
            {
                Type = type,
                Flags = reliable ? PacketFlags.Reliable : PacketFlags.None,
                PayloadSize = (ushort)(payload?.Length ?? 0)
            };
            Payload = payload;
            SendTime = UnityEngine.Time.time;
        }

        public byte[] ToBytes()
        {
            var buffer = new byte[NetworkPacketHeader.Size + (Payload?.Length ?? 0)];
            var offset = 0;

            BitConverter.GetBytes(Header.SequenceNumber).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(Header.AckNumber).CopyTo(buffer, offset);
            offset += 4;
            buffer[offset++] = (byte)Header.Flags;
            buffer[offset++] = (byte)Header.Type;
            BitConverter.GetBytes(Header.PayloadSize).CopyTo(buffer, offset);
            offset += 2;

            if (Payload != null && Payload.Length > 0)
            {
                Payload.CopyTo(buffer, offset);
            }

            return buffer;
        }

        public static NetworkPacket FromBytes(byte[] data)
        {
            if (data.Length < NetworkPacketHeader.Size)
                return null;

            var packet = new NetworkPacket();
            var offset = 0;

            packet.Header.SequenceNumber = BitConverter.ToUInt32(data, offset);
            offset += 4;
            packet.Header.AckNumber = BitConverter.ToUInt32(data, offset);
            offset += 4;
            packet.Header.Flags = (PacketFlags)data[offset++];
            packet.Header.Type = (PacketType)data[offset++];
            packet.Header.PayloadSize = BitConverter.ToUInt16(data, offset);
            offset += 2;

            if (packet.Header.PayloadSize > 0)
            {
                packet.Payload = new byte[packet.Header.PayloadSize];
                Array.Copy(data, offset, packet.Payload, 0, packet.Header.PayloadSize);
            }

            return packet;
        }
    }
}