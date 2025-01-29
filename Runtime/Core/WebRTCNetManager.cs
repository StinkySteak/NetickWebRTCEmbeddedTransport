using System.Collections.Generic;
using Netick;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class WebRTCNetManager
    {
        private IWebRTCNetEventListener _listener;
        private WebSocketSignalingServer _signalingServer;
        private RunMode _runMode;
        private bool _isRunning;
        private float _maxIceTricklingDuration;

        private List<WebRTCPeer> _candidateClients;
        private List<WebRTCPeer> _activeClients;

        private WebRTCPeer _serverConnectionCandidate;
        private WebRTCPeer _serverConnection;
        private string[] _iceServers;

        public WebRTCNetManager(IWebRTCNetEventListener listener)
        {
            _listener = listener;
        }

        public void AddPeer(WebRTCPeer peer)
        {
            peer.OnMessageReceived += OnMessageReceived;

            _activeClients.Add(peer);
        }

        public void SetSignalingServerInstance(WebSocketSignalingServer signalingServer)
        {
            _signalingServer = signalingServer;
        }

        public void Init(int maxClients)
        {
            _activeClients = new List<WebRTCPeer>(maxClients);
            _candidateClients = new List<WebRTCPeer>(maxClients);
        }

        public void SetConfig(string[] iceServers, float maxIceTricklingDuration)
        {
            _iceServers = iceServers;
            _maxIceTricklingDuration = maxIceTricklingDuration;
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
            _serverConnectionCandidate = new WebRTCPeer();

            _serverConnectionCandidate.SetConfig(_iceServers, _maxIceTricklingDuration);
            _serverConnectionCandidate.Start(RunMode.Client);

            _serverConnectionCandidate.Connect(address, port);
        }

        public void Stop()
        {
            if (_signalingServer != null)
                _signalingServer.Stop();
        }

        private void OnClientOffered(int clientId, string offer)
        {
            Debug.Log("OnClientOffered");

            WebRTCPeer candidatePeer = new();

            candidatePeer.SetConfig(_iceServers, _maxIceTricklingDuration);
            candidatePeer.SetSignalingServer(_signalingServer);
            candidatePeer.Start(RunMode.Server);

            candidatePeer.OnClientOffered(offer);
            candidatePeer.SetConnectionId(clientId);

            candidatePeer.OnMessageReceived += OnMessageReceived;

            _candidateClients.Add(candidatePeer);
            _activeClients.Add(candidatePeer);
        }

        private void OnMessageReceived(WebRTCPeer peer, byte[] bytes)
        {
            _listener.OnNetworkReceive(peer, bytes);
        }

        public void PollUpdate()
        {
            if (!_isRunning) return;

            for (int i = _candidateClients.Count - 1; i >= 0; i--)
            {
                WebRTCPeer candidateClient = _candidateClients[i];

                candidateClient.TriggerUpdate();

                if (candidateClient.IsConnectionOpen)
                {
                    _candidateClients.RemoveAt(i);
                    _listener.OnPeerConnected(candidateClient);
                }
            }

            if (_runMode == RunMode.Server)
            {
                _signalingServer.PollUpdate();
            }

            if (_runMode == RunMode.Client)
            {
                if (_serverConnection != null)
                    _serverConnection.TriggerUpdate();

                if (_serverConnectionCandidate != null)
                {
                    _serverConnectionCandidate.TriggerUpdate();

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

    public interface IWebRTCNetEventListener
    {
        void OnPeerConnected(WebRTCPeer peer);
        void OnPeerDisconnected(WebRTCPeer peer);
        void OnNetworkReceive(WebRTCPeer peer, byte[] bytes);
    }
}
