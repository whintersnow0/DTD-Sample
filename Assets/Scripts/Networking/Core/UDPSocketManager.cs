using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NetworkSystem.Core
{
    public class UDPSocketManager : IDisposable
    {
        private UdpClient _udpClient;
        private IPEndPoint _localEndPoint;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isListening;

        public event Action<byte[], IPEndPoint> OnDataReceived;
        public event Action<Exception> OnSocketError;

        public bool IsActive => _udpClient != null && _isListening;
        public IPEndPoint LocalEndPoint => _localEndPoint;

        public async Task<bool> StartAsync(int port = 0)
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _localEndPoint = new IPEndPoint(IPAddress.Any, port);
                _udpClient = new UdpClient(_localEndPoint);

                _localEndPoint = (IPEndPoint)_udpClient.Client.LocalEndPoint;
                _isListening = true;

                _ = Task.Run(ReceiveLoop, _cancellationTokenSource.Token);

                return true;
            }
            catch (Exception ex)
            {
                OnSocketError?.Invoke(ex);
                return false;
            }
        }

        public async Task SendAsync(byte[] data, IPEndPoint target)
        {
            if (!_isListening) return;

            try
            {
                await _udpClient.SendAsync(data, data.Length, target);
            }
            catch (Exception ex)
            {
                OnSocketError?.Invoke(ex);
            }
        }

        private async Task ReceiveLoop()
        {
            while (_isListening && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync();
                    OnDataReceived?.Invoke(result.Buffer, result.RemoteEndPoint);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (_isListening)
                        OnSocketError?.Invoke(ex);
                }
            }
        }

        public void Stop()
        {
            _isListening = false;
            _cancellationTokenSource?.Cancel();
            _udpClient?.Close();
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
            _udpClient?.Dispose();
        }
    }
}