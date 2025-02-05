using System;
using System.Threading;
using Newtonsoft.Json;
using StinkySteak.SimulationTimer;
using Unity.WebRTC;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class WebRTCPeer
    {
        private WebSocketClientSignalingService _signalingServiceClient;
        private WebSocketSignalingServer _signalingServiceServer;

        private string[] _iceServers;
        private float _timeoutDuration;

        private SimulationTimer _timerTimeout;
        private bool _hasSentReply;

        private RTCPeerConnection _peerConnection;
        private RunMode _peerMode;

        private string _offer;
        private string _answer;

        private RTCSessionDescriptionAsyncOperation _opCreateOffer;
        private RTCSetSessionDescriptionAsyncOperation _opSetOfferLocal;
        private RTCSetSessionDescriptionAsyncOperation _opSetAnswerRemote;

        private RTCSessionDescriptionAsyncOperation _opCreateAnswer;
        private RTCSetSessionDescriptionAsyncOperation _opSetOfferRemote;
        private RTCSetSessionDescriptionAsyncOperation _opSetAnswerLocal;

        private int _connectionId;
        private RTCDataChannel _dataChannel;

        private WebRTCEndPoint _endPoint = new();
        private bool _isTimedOut;
        public bool IsTimedOut => _isTimedOut;
        public bool IsConnectionOpen => _dataChannel != null && _dataChannel.ReadyState == RTCDataChannelState.Open;
        public event Action<WebRTCPeer, byte[]> OnMessageReceived;
        public event Action<WebRTCPeer> OnTimeout;
        public event Action<WebRTCPeer> OnConnectionClosed;
        public WebRTCEndPoint EndPoint => _endPoint;

        public RTCDataChannelState GetDataChannelState()
        {
            if (_dataChannel == null)
                return RTCDataChannelState.Closed;

            return _dataChannel.ReadyState;
        }

        public void SetConfig(string[] iceServers, float timeoutDuration)
        {
            _iceServers = iceServers;
            _timeoutDuration = timeoutDuration;
        }

        public void Start(RunMode peerMode)
        {
            _peerMode = peerMode;
            _signalingServiceClient = new WebSocketClientSignalingService();

            if (peerMode == RunMode.Server)
            {
                ConstructRTCPeerConnection();
            }
        }

        public void Connect(string address, int port)
        {
            Log("Starting as Client");

            _signalingServiceClient.OnConnectedToServer += OnClientConnectedToSignalingServer;
            _signalingServiceClient.Start();

            _signalingServiceClient.Connect(address, port);

            _timerTimeout = SimulationTimer.CreateFromSeconds(_timeoutDuration);
        }

        public void SetConnectionId(int id)
        {
            _connectionId = id;
        }

        public void SetSignalingServer(WebSocketSignalingServer signalingServerClient)
        {
            _signalingServiceServer = signalingServerClient;
        }


        public void CloseConnection()
        {
            _peerConnection.Close();
            _dataChannel?.Close();
        }

        private void ConstructRTCPeerConnection()
        {
            RTCConfiguration configuration = GetSelectedSdpSemantics();
            _peerConnection = new RTCPeerConnection(ref configuration);
            _peerConnection.OnIceCandidate = OnIceCandidate;
            _peerConnection.OnIceConnectionChange = OnIceConnectionChange;
            _peerConnection.OnIceGatheringStateChange = OnIceGatheringStateChanged;
            _peerConnection.OnDataChannel = OnDataChannel;
        }

        public void TriggerUpdate()
        {
            PollUpdate();
        }

        private void Log(string msg)
        {
            Debug.Log($"[{this}]: {msg}");
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[{this}]: {msg}");
        }

        private void PollUpdate()
        {
            if (_peerMode == RunMode.Client)
            {
                if (_timerTimeout.IsExpired())
                {
                    _timerTimeout = SimulationTimer.None;

                    CloseConnection();

                    _isTimedOut = true;
                    OnTimeout?.Invoke(this);
                    return;
                }

                _signalingServiceClient?.PollUpdate();

                // Client
                PollOpCreateOffer();
                PollOpSetLocalOffer();
                PollOpSetRemoteAnswer();
            }

            if (_peerMode == RunMode.Server)
            {
                // Server
                PollOpSetRemoteOffer();
                PollOpCreateAnswer();
                PollOpSetLocalAnswer();
            }

            // All
            PollIceCandidate();
        }

        private void PollOpSetLocalAnswer()
        {
            if (_opSetAnswerLocal == null) return;

            if (_opSetAnswerLocal.IsDone)
            {
                Log("Answer has been set to local description!");

                _opSetAnswerLocal = null;
            }
        }

        private void PollOpCreateAnswer()
        {
            if (_opCreateAnswer == null) return;

            if (_opCreateAnswer.IsDone)
            {
                Log("Answer is created. Setting it as local description...");

                RTCSessionDescription answer = _opCreateAnswer.Desc;

                _opSetAnswerLocal = _peerConnection.SetLocalDescription(ref answer);

                _opCreateAnswer = null;
            }
        }

        private void PollOpSetRemoteOffer()
        {
            if (_opSetOfferRemote == null) return;

            if (_opSetOfferRemote.IsDone)
            {
                Log("Offer has been set to remote description. Creating an answer...");

                _opCreateAnswer = _peerConnection.CreateAnswer();

                _opSetOfferRemote = null;
            }
        }

        private void PollOpSetLocalOffer()
        {
            if (_opSetOfferLocal == null) return;

            if (_opSetOfferLocal.IsDone)
            {
                Log("Offer has been set to local!");

                _opSetOfferLocal = null;
            }
        }

        private void PollOpSetRemoteAnswer()
        {
            if (_opSetAnswerRemote == null) return;

            if (_opSetAnswerRemote.IsDone)
            {
                Log("Answer has been set to remote description");

                _opSetAnswerRemote = null;
            }
        }

        private void PollOpCreateOffer()
        {
            if (_opCreateOffer == null) return;

            if (_opCreateOffer.IsDone)
            {
                Log("Offer Created. Setting it to local...");
                RTCSessionDescription offer = _opCreateOffer.Desc;

                _opSetOfferLocal = _peerConnection.SetLocalDescription(ref offer);

                _opCreateOffer = null;
            }
        }

        private void OnClientConnectedToSignalingServer()
        {
            RTCDataChannelInit rtcDataChannelConfig = new RTCDataChannelInit();
            rtcDataChannelConfig.maxRetransmits = 0;
            rtcDataChannelConfig.ordered = false;

            ConstructRTCPeerConnection();
            _dataChannel = _peerConnection.CreateDataChannel("sendData", rtcDataChannelConfig);
            _dataChannel.OnClose = OnChannelClose;
            _dataChannel.OnOpen = OnChannelOpen;
            _dataChannel.OnMessage = OnMessage;

            Log("Creating Offer...");
            _opCreateOffer = _peerConnection.CreateOffer();
        }

        public void OnClientOffered(string offer)
        {
            Log("Getting an offer from a client. Creating an answer...");

            RTCSessionDescription sdpOffer = JsonConvert.DeserializeObject<RTCSessionDescription>(offer);

            _opSetOfferRemote = _peerConnection.SetRemoteDescription(ref sdpOffer);
        }

        private void SendOfferToServer()
        {
            _signalingServiceClient.OnServerAnswered += OnServerAnswered;

            _signalingServiceClient.SendOffer(_offer);
        }

        private void OnServerAnswered(int clientId, string message)
        {
            _connectionId = clientId;

            RTCSessionDescription sdp = JsonConvert.DeserializeObject<RTCSessionDescription>(message);
            _opSetAnswerRemote = _peerConnection.SetRemoteDescription(ref sdp);
        }

        private RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCIceServer iceServer = new()
            {
                urls = _iceServers
            };

            RTCConfiguration config = default;
            config.iceServers = new[]
            {
                iceServer
            };

            return config;
        }

        private void OnChannelOpen()
        {
            SDPParser.ParseSDP(_peerConnection.RemoteDescription.sdp, out string ip, out int port);

            _endPoint.Init(ip, port);
        }

        private void OnChannelClose()
        {
            OnConnectionClosed?.Invoke(this);
        }

        private void OnDataChannel(RTCDataChannel dataChannel)
        {
            _dataChannel = dataChannel;
            _dataChannel.OnMessage = OnMessage;
            _dataChannel.OnClose = OnChannelClose;

            SDPParser.ParseSDP(_peerConnection.RemoteDescription.sdp, out string ip, out int port);

            _endPoint.Init(ip, port);
        }


        private void OnMessage(byte[] bytes)
        {
            OnMessageReceived?.Invoke(this, bytes);
        }

        public void Send(IntPtr ptr, int length)
        {
            _dataChannel.Send(ptr, length);
        }

        private void PollIceCandidate()
        {
            if (_hasSentReply) return;

            if (_peerConnection.GatheringState == RTCIceGatheringState.Complete)
            {
                _hasSentReply = true;

                if (_peerMode == RunMode.Client)
                {
                    Log("Sending offer to the server...");
                    _offer = JsonConvert.SerializeObject(_peerConnection.LocalDescription);
                    SendOfferToServer();
                }
                if (_peerMode == RunMode.Server)
                {
                    Log("Sending answer to the client...");
                    _answer = JsonConvert.SerializeObject(_peerConnection.LocalDescription);
                    SendAnswerToClient();
                }
            }
        }

        private void OnIceCandidate(RTCIceCandidate iceCandidate)
        {
            Log($"On Ice Candidate Added: {iceCandidate.Candidate}");
        }

        private void SendAnswerToClient()
        {
            _signalingServiceServer.SendAnswerToClient(_connectionId, _answer);
        }

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            Log($"OnIceConnectionChange: {state}");

            if(state == RTCIceConnectionState.Connected)
            {
                _timerTimeout = SimulationTimer.None;
            }
        }

        private void OnIceGatheringStateChanged(RTCIceGatheringState state)
        {
            Log($"OnIceGatheringStateChanged: {state}");
        }
    }
}