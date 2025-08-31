using System;
using System.Text;
using UnityEngine;

namespace NetworkSystem.Protocol
{
    public static class PacketSerializer
    {
        public static byte[] SerializePlayerInput(Vector2 movement, bool jump, float deltaTime)
        {
            var buffer = new byte[13];
            var offset = 0;

            BitConverter.GetBytes(movement.x).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(movement.y).CopyTo(buffer, offset);
            offset += 4;
            buffer[offset++] = jump ? (byte)1 : (byte)0;
            BitConverter.GetBytes(deltaTime).CopyTo(buffer, offset);

            return buffer;
        }

        public static (Vector2 movement, bool jump, float deltaTime) DeserializePlayerInput(byte[] data)
        {
            if (data.Length < 13) return (Vector2.zero, false, 0f);

            var offset = 0;
            var x = BitConverter.ToSingle(data, offset);
            offset += 4;
            var y = BitConverter.ToSingle(data, offset);
            offset += 4;
            var jump = data[offset++] == 1;
            var deltaTime = BitConverter.ToSingle(data, offset);

            return (new Vector2(x, y), jump, deltaTime);
        }

        public static byte[] SerializePlayerState(uint playerId, Vector3 position, Vector3 velocity, float rotation, byte animState)
        {
            var buffer = new byte[29];
            var offset = 0;

            BitConverter.GetBytes(playerId).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(position.x).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(position.y).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(position.z).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(velocity.x).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(velocity.y).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(velocity.z).CopyTo(buffer, offset);
            offset += 4;
            BitConverter.GetBytes(rotation).CopyTo(buffer, offset);
            offset += 4;
            buffer[offset] = animState;

            return buffer;
        }

        public static (uint playerId, Vector3 position, Vector3 velocity, float rotation, byte animState) DeserializePlayerState(byte[] data)
        {
            if (data.Length < 29) return (0, Vector3.zero, Vector3.zero, 0f, 0);

            var offset = 0;
            var playerId = BitConverter.ToUInt32(data, offset);
            offset += 4;
            var posX = BitConverter.ToSingle(data, offset);
            offset += 4;
            var posY = BitConverter.ToSingle(data, offset);
            offset += 4;
            var posZ = BitConverter.ToSingle(data, offset);
            offset += 4;
            var velX = BitConverter.ToSingle(data, offset);
            offset += 4;
            var velY = BitConverter.ToSingle(data, offset);
            offset += 4;
            var velZ = BitConverter.ToSingle(data, offset);
            offset += 4;
            var rotation = BitConverter.ToSingle(data, offset);
            offset += 4;
            var animState = data[offset];

            return (playerId, new Vector3(posX, posY, posZ), new Vector3(velX, velY, velZ), rotation, animState);
        }

        public static byte[] SerializeGameEvent(byte eventType, uint playerId, byte[] eventData = null)
        {
            var dataLength = eventData?.Length ?? 0;
            var buffer = new byte[6 + dataLength];
            var offset = 0;

            buffer[offset++] = eventType;
            BitConverter.GetBytes(playerId).CopyTo(buffer, offset);
            offset += 4;
            buffer[offset++] = (byte)dataLength;

            if (eventData != null && dataLength > 0)
            {
                eventData.CopyTo(buffer, offset);
            }

            return buffer;
        }

        public static (byte eventType, uint playerId, byte[] eventData) DeserializeGameEvent(byte[] data)
        {
            if (data.Length < 6) return (0, 0, null);

            var eventType = data[0];
            var playerId = BitConverter.ToUInt32(data, 1);
            var dataLength = data[5];

            byte[] eventData = null;
            if (dataLength > 0 && data.Length >= 6 + dataLength)
            {
                eventData = new byte[dataLength];
                Array.Copy(data, 6, eventData, 0, dataLength);
            }

            return (eventType, playerId, eventData);
        }
    }
}