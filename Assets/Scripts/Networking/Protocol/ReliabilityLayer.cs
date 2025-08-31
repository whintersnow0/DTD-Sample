using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace NetworkSystem.Protocol
{
    public class ReliabilityLayer
    {
        private Dictionary<IPEndPoint, ConnectionState> _connections = new Dictionary<IPEndPoint, ConnectionState>();
        private Queue<NetworkPacket> _outgoingPackets = new Queue<NetworkPacket>();

        public event Action<NetworkPacket, IPEndPoint> OnReliablePacketReceived;
        public event Action<byte[], IPEndPoint> OnPacketToSend;

        private class ConnectionState
        {
            public uint LocalSequence;
            public uint RemoteSequence;
            public Dictionary<uint, NetworkPacket> PendingAcks = new Dictionary<uint, NetworkPacket>();
            public HashSet<uint> ReceivedPackets = new HashSet<uint>();
            public float LastHeartbeat;
        }

        public void ProcessIncomingPacket(NetworkPacket packet, IPEndPoint sender)
        {
            if (!_connections.TryGetValue(sender, out var connection))
            {
                connection = new ConnectionState();
                _connections[sender] = connection;
            }

            if (packet.Header.Flags.HasFlag(PacketFlags.Reliable))
            {
                if (!connection.ReceivedPackets.Contains(packet.Header.SequenceNumber))
                {
                    connection.ReceivedPackets.Add(packet.Header.SequenceNumber);
                    SendAck(packet.Header.SequenceNumber, sender);
                    OnReliablePacketReceived?.Invoke(packet, sender);
                }
                else
                {
                    SendAck(packet.Header.SequenceNumber, sender);
                }
            }
            else
            {
                OnReliablePacketReceived?.Invoke(packet, sender);
            }

            if (packet.Header.Type == PacketType.Acknowledgment)
            {
                ProcessAck(packet.Header.AckNumber, sender);
            }

            connection.RemoteSequence = Math.Max(connection.RemoteSequence, packet.Header.SequenceNumber);
        }

        public void SendPacket(NetworkPacket packet, IPEndPoint target)
        {
            if (!_connections.TryGetValue(target, out var connection))
            {
                connection = new ConnectionState();
                _connections[target] = connection;
            }

            packet.Header.SequenceNumber = ++connection.LocalSequence;

            if (packet.Header.Flags.HasFlag(PacketFlags.Reliable))
            {
                connection.PendingAcks[packet.Header.SequenceNumber] = packet;
            }

            OnPacketToSend?.Invoke(packet.ToBytes(), target);
        }

        private void SendAck(uint sequenceNumber, IPEndPoint target)
        {
            var ackPacket = new NetworkPacket(PacketType.Acknowledgment, null);
            ackPacket.Header.AckNumber = sequenceNumber;

            OnPacketToSend?.Invoke(ackPacket.ToBytes(), target);
        }

        private void ProcessAck(uint ackNumber, IPEndPoint sender)
        {
            if (_connections.TryGetValue(sender, out var connection))
            {
                connection.PendingAcks.Remove(ackNumber);
            }
        }

        public void Update()
        {
            var currentTime = Time.time;

            foreach (var kvp in _connections)
            {
                var endpoint = kvp.Key;
                var connection = kvp.Value;

                var packetsToRetry = new List<NetworkPacket>();

                foreach (var pendingKvp in connection.PendingAcks)
                {
                    var packet = pendingKvp.Value;

                    if (currentTime - packet.SendTime > 0.1f)
                    {
                        packet.RetryCount++;

                        if (packet.RetryCount < 5)
                        {
                            packet.SendTime = currentTime;
                            packetsToRetry.Add(packet);
                        }
                        else
                        {
                            connection.PendingAcks.Remove(pendingKvp.Key);
                        }
                    }
                }

                foreach (var packet in packetsToRetry)
                {
                    OnPacketToSend?.Invoke(packet.ToBytes(), endpoint);
                }

                if (currentTime - connection.LastHeartbeat > 1.0f)
                {
                    var heartbeat = new NetworkPacket(PacketType.Heartbeat, null);
                    SendPacket(heartbeat, endpoint);
                    connection.LastHeartbeat = currentTime;
                }
            }
        }

        public void CleanupOldConnections()
        {
            var currentTime = Time.time;
            var toRemove = new List<IPEndPoint>();

            foreach (var kvp in _connections)
            {
                if (currentTime - kvp.Value.LastHeartbeat > 10.0f)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var endpoint in toRemove)
            {
                _connections.Remove(endpoint);
            }
        }
    }
}