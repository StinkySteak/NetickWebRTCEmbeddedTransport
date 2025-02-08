using System;
using System.Collections.Generic;
using JamesFrowen.SimpleWeb;

namespace Netick.Transport.WebRTC
{
    public class WebRTCNetManager
    {
        private IWebRTCNetEventListener _listener;
        private WebSocketSignalingServer _signalingServer;
        private RunMode _runMode;
        private bool _isRunning;

        private List<BaseWebRTCPeer> _candidateClients;
        private List<BaseWebRTCPeer> _activeClients;

        private BaseWebRTCPeer _serverConnectionCandidate;
        private BaseWebRTCPeer _serverConnection;
        private UserRTCConfig _userRTCConfig;
        private WebSocketSignalingConfig _webSocketSignalingConfig;

        public WebRTCNetManager(IWebRTCNetEventListener listener)
        {
            _listener = listener;
        }

        public void AddPeer(BaseWebRTCPeer peer)
        {
            peer.OnMessageReceived += OnMessageReceived;
            peer.OnMessageReceivedUnmanaged += OnMessageReceivedUnmanaged;

            _activeClients.Add(peer);
        }

        public void SetSignalingServerInstance(WebSocketSignalingServer signalingServer)
        {
            _signalingServer = signalingServer;
        }

        public void Init(int maxClients)
        {
            _activeClients = new List<BaseWebRTCPeer>(maxClients);
            _candidateClients = new List<BaseWebRTCPeer>(maxClients);
        }

        public void SetConfig(UserRTCConfig userRTCConfig, WebSocketSignalingConfig webSocketSignalingConfig)
        {
            _userRTCConfig = userRTCConfig;
            _webSocketSignalingConfig = webSocketSignalingConfig;
        }

        public void Start(RunMode runMode, int port = 0)
        {
            _runMode = runMode;

            if (runMode == RunMode.Server)
            {
                _signalingServer = new WebSocketSignalingServer();
                _signalingServer.OnClientOffered += OnClientOffered;

                _signalingServer.Start((ushort)port);
            }

            _isRunning = true;
        }

        public void Connect(string address, int port)
        {
            _serverConnectionCandidate = ConstructWebRTCPeer();

            _serverConnectionCandidate.SetConfig(_userRTCConfig, _webSocketSignalingConfig);
            _serverConnectionCandidate.Start(RunMode.Client);

            _serverConnectionCandidate.OnConnectionClosed += OnServerConnectionClosed;
            _serverConnectionCandidate.Connect(address, port);
            _serverConnectionCandidate.OnTimeout += OnServerConnectionTimeout;
        }

        private BaseWebRTCPeer ConstructWebRTCPeer()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return new BrowserWebRTCPeer();
#else
            return new NativeWebRTCPeer();
#endif
        }

        private void OnServerConnectionTimeout(BaseWebRTCPeer serverConnection)
        {
            _listener.OnPeerDisconnected(_serverConnection, DisconnectReason.Timeout);
        }

        private void OnServerConnectionClosed(BaseWebRTCPeer serverConnection)
        {
            _listener.OnPeerDisconnected(serverConnection, DisconnectReason.ConnectionClosed);
        }

        public void Stop()
        {
            for (int i = 0; i < _activeClients.Count; i++)
            {
                BaseWebRTCPeer client = _activeClients[i];

                client.CloseConnection();
            }

            for (int i = 0; i < _candidateClients.Count; i++)
            {
                BaseWebRTCPeer client = _candidateClients[i];

                client.CloseConnection();
            }

            _activeClients.Clear();
            _candidateClients.Clear();

            if (_signalingServer != null)
                _signalingServer.Stop();
        }

        public void DisconnectPeer(BaseWebRTCPeer peer)
        {
            peer.CloseConnection();
        }

        private void OnClientOffered(int clientId, string offer)
        {
            NativeWebRTCPeer candidatePeer = new NativeWebRTCPeer();

            candidatePeer.SetConfig(_userRTCConfig, _webSocketSignalingConfig);
            candidatePeer.SetSignalingServer(_signalingServer);
            candidatePeer.Start(RunMode.Server);

            candidatePeer.OnReceivedOfferFromClient(offer);
            candidatePeer.SetConnectionId(clientId);

            candidatePeer.OnMessageReceived += OnMessageReceived;
            candidatePeer.OnMessageReceivedUnmanaged += OnMessageReceivedUnmanaged;

            _candidateClients.Add(candidatePeer);
        }

        private void OnMessageReceived(BaseWebRTCPeer peer, byte[] bytes)
        {
            _listener.OnNetworkReceive(peer, bytes);
        }

        private void OnMessageReceivedUnmanaged(BaseWebRTCPeer peer, IntPtr ptr, int length)
        {
            _listener.OnMessageReceiveUnmanaged(peer, ptr, length);
        }

        public void PollUpdate()
        {
            if (!_isRunning) return;

            for (int i = _candidateClients.Count - 1; i >= 0; i--)
            {
                BaseWebRTCPeer candidateClient = _candidateClients[i];

                candidateClient.PollUpdate();

                if (candidateClient.IsTimedOut)
                {
                    _candidateClients.RemoveAt(i);
                }

                if (candidateClient.IsConnectionOpen)
                {
                    _candidateClients.RemoveAt(i);
                    _activeClients.Add(candidateClient);
                    _listener.OnPeerConnected(candidateClient);
                }
            }

            for (int i = _activeClients.Count - 1; i >= 0; i--)
            {
                BaseWebRTCPeer activeClient = _activeClients[i];

                if (!activeClient.IsConnectionOpen)
                {
                    _activeClients.RemoveAt(i);
                    _listener.OnPeerDisconnected(activeClient, DisconnectReason.ConnectionClosed);
                }
            }

            if (_runMode == RunMode.Server)
            {
                _signalingServer.PollUpdate();
            }

            if (_runMode == RunMode.Client)
            {
                if (_serverConnection != null)
                    _serverConnection.PollUpdate();

                if (_serverConnectionCandidate != null)
                {
                    _serverConnectionCandidate.PollUpdate();

                    if (_serverConnectionCandidate.IsConnectionOpen)
                    {
                        _serverConnection = _serverConnectionCandidate;
                        _serverConnectionCandidate = null;
                        AddPeer(_serverConnection);
                        _listener.OnPeerConnected(_serverConnection);
                    }
                }
            }
        }
    }

    public enum DisconnectReason
    {
        SignalingServerUnreachable,
        Timeout,
        ConnectionRejected,
        Shutdown,
        ConnectionClosed
    }
}
