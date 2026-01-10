using System;
using System.Net.Sockets;
using System.Threading;

namespace HolographEmulator.Domain.Users
{
    /// <summary>
    /// Manages the network connection for a user.
    /// </summary>
    public class UserConnection
    {
        private int _connectionId;
        private Socket _connectionSocket;
        private byte[] _dataBuffer = new byte[1024];
        private bool _isDisconnected;
        private bool _isLoggedIn;
        internal bool PingOk;
        private bool _receivedSpriteIndex;

        public int ConnectionId => _connectionId;
        public Socket Socket => _connectionSocket;
        public byte[] DataBuffer => _dataBuffer;
        public bool IsDisconnected => _isDisconnected;
        public bool IsLoggedIn => _isLoggedIn;
        public bool ReceivedSpriteIndex => _receivedSpriteIndex;

        public UserConnection(int connectionId, Socket connectionSocket)
        {
            _connectionId = connectionId;
            _connectionSocket = connectionSocket;
        }

        public void SetLoggedIn(bool value)
        {
            _isLoggedIn = value;
        }

        public void SetDisconnected(bool value)
        {
            _isDisconnected = value;
        }

        public void SetReceivedSpriteIndex(bool value)
        {
            _receivedSpriteIndex = value;
        }

        public string GetRemoteIp()
        {
            return _connectionSocket?.RemoteEndPoint?.ToString()?.Split(':')[0] ?? "";
        }

        public void Close()
        {
            try { _connectionSocket?.Close(); }
            catch { }
            _connectionSocket = null;
        }

        public void ShutdownReceive()
        {
            try { _connectionSocket?.Shutdown(SocketShutdown.Receive); }
            catch { }
        }
    }
}
