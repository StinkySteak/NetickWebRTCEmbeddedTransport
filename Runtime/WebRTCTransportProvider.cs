using System;
using System.Collections.Generic;
using Netick.Transport.WebRTC;
using Netick.Unity;
using UnityEngine;

namespace Netick.Transport
{
    [CreateAssetMenu(fileName = nameof(WebRTCTransportProvider), menuName = "Netick/Transport/WebRTCTransportProvider")]
    public unsafe class WebRTCTransportProvider : NetworkTransportProvider
    {
        [SerializeField] private string[] _iceServers;
        [SerializeField] private float _timeoutDuration = 10f;
        [SerializeField] private IceTricklingConfig _iceTricklingConfig;

        [Space]
        [SerializeField] private WebSocketSignalingConfig _webSocketSignalingConfig;
        [SerializeField] private JamesFrowen.SimpleWeb.Log.Levels _signalingServerLogLevel;

        private void Reset()
        {
            _iceServers = new string[]
            {
                "stun:stun.l.google.com:19302"
            };

            _iceTricklingConfig.IsManual = true;
            _iceTricklingConfig.Duration = 0.5f;
        }

        public override NetworkTransport MakeTransportInstance()
        {
            WebRTCTransport transport = new();

            UserRTCConfig rtcConfig = new UserRTCConfig();
            rtcConfig.TimeoutDuration = _timeoutDuration;
            rtcConfig.IceTricklingConfig = _iceTricklingConfig;
            rtcConfig.IceServers = _iceServers;
            transport.SetConfig(rtcConfig, _webSocketSignalingConfig);

            JamesFrowen.SimpleWeb.Log.level = _signalingServerLogLevel;

            return transport;
        }

        public class WebRTCConnection : TransportConnection
        {
            public BaseWebRTCPeer Peer;

            public override IEndPoint EndPoint => Peer.EndPoint;

            public override int Mtu => 1200;

            public override void Send(IntPtr ptr, int length)
            {
                Peer.Send(ptr, length);
            }
        }

        public unsafe class WebRTCTransport : NetworkTransport, IWebRTCNetEventListener
        {
            private Dictionary<BaseWebRTCPeer, WebRTCConnection> _connections;
            private Queue<WebRTCConnection> _freeClients;
            private WebRTCNetManager _netManager;
            private BitBuffer _bitBuffer;

            private UserRTCConfig _userRTCConfig;
            private WebSocketSignalingConfig _webSocketSignalingConfig;

            public override void Init()
            {
                _connections = new(Engine.Config.MaxPlayers);
                _freeClients = new(Engine.Config.MaxPlayers);
                _netManager = new WebRTCNetManager(this);

                for (int i = 0; i < Engine.Config.MaxPlayers; i++)
                    _freeClients.Enqueue(new WebRTCConnection());

                _bitBuffer = new BitBuffer(createChunks: false);

                _netManager.Init(Engine.Config.MaxPlayers);
                _netManager.SetConfig(_userRTCConfig, _webSocketSignalingConfig);
            }

            public void SetConfig(UserRTCConfig userRTCConfig, WebSocketSignalingConfig webSocketSignalingConfig)
            {
                _userRTCConfig = userRTCConfig;
                _webSocketSignalingConfig = webSocketSignalingConfig;
            }

            public override void Connect(string address, int port, byte[] connectionData, int connectionDataLength)
            {
                _netManager.Connect(address, port);
            }

            public override void Disconnect(TransportConnection connection)
            {
                WebRTCConnection webRTCConnection = (WebRTCConnection)connection;

                _netManager.DisconnectPeer(webRTCConnection.Peer);
            }

            public override void Run(RunMode mode, int port)
            {
                if (mode == RunMode.Client)
                {
                    _netManager.Start(mode);
                }
                else if (mode == RunMode.Server)
                {
                    _netManager.Start(mode, port);
                }
            }

            public override void Shutdown()
            {
                _netManager.Stop();
            }

            public override void PollEvents()
            {
                _netManager.PollUpdate();
            }

            void IWebRTCNetEventListener.OnPeerConnected(BaseWebRTCPeer peer)
            {
                WebRTCConnection connection = _freeClients.Dequeue();
                connection.Peer = peer;

                _connections.Add(peer, connection);
                NetworkPeer.OnConnected(connection);
            }

            void IWebRTCNetEventListener.OnNetworkReceive(BaseWebRTCPeer peer, byte[] bytes)
            {
                if (_connections.TryGetValue(peer, out var connection))
                {
                    fixed (byte* ptr = bytes)
                    {
                        _bitBuffer.SetFrom(ptr, bytes.Length, bytes.Length);
                        NetworkPeer.Receive(connection, _bitBuffer);
                    }
                }
            }

            void IWebRTCNetEventListener.OnMessageReceiveUnmanaged(BaseWebRTCPeer peer, IntPtr ptr, int length)
            {
                if (_connections.TryGetValue(peer, out var connection))
                {
                    _bitBuffer.SetFrom((byte*)ptr, length, length);
                    NetworkPeer.Receive(connection, _bitBuffer);
                }
            }

            void IWebRTCNetEventListener.OnPeerDisconnected(BaseWebRTCPeer peer, DisconnectReason disconnectReason)
            {
                Debug.Log($"IsServer: {Engine.IsServer} OnPeerDisconnected: {peer.EndPoint} reason: {disconnectReason}");

                if (Engine.IsClient)
                {
                    if (disconnectReason == DisconnectReason.SignalingServerUnreachable || disconnectReason == DisconnectReason.Timeout)
                    {
                        NetworkPeer.OnConnectFailed(ConnectionFailedReason.Timeout);
                        return;
                    }

                    if (disconnectReason == DisconnectReason.ConnectionRejected)
                    {
                        NetworkPeer.OnConnectFailed(ConnectionFailedReason.Refused);
                        return;
                    }
                }

                if (_connections.TryGetValue(peer, out var connection))
                {
                    TransportDisconnectReason reason = disconnectReason == DisconnectReason.Timeout ? TransportDisconnectReason.Timeout : TransportDisconnectReason.Shutdown;

                    NetworkPeer.OnDisconnected(connection, reason);

                    _connections.Remove(peer);
                    _freeClients.Enqueue(connection);
                }
            }
        }
    }
}