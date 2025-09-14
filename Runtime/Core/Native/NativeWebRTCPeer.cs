#if UNITY_WEBGL && !UNITY_EDITOR
#define BROWSER
#endif

#if !BROWSER
using Unity.WebRTC;
#endif

using System;
using Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using StinkySteak.Timer;
using UnityEngine.Rendering;
using StinkySteak.WebRealtimeCommunication;

namespace Netick.Transport.WebRTC
{
    public class NativeWebRTCPeer : BaseWebRTCPeer
    {
#if BROWSER

        public override IEndPoint EndPoint => throw new NotImplementedException();

        public override bool IsConnectionOpen => false;

        public override bool IsTimedOut => false;

        public override void CloseConnection() { }

        public override void Connect(string address, int port) { }

        public override void OnReceivedOfferFromClient(string offer) { }

        public override void PollUpdate() { }

        public override void Send(IntPtr ptr, int length, bool isReliable) { }

        public override void SetConfig(UserRTCConfig userConfig, WebSocketSignalingConfig webSocketSignalingConfig) { }

        public override void SetConnectionId(int id) { }

        public override void SetSignalingServer(WebSocketSignalingServer signalingServer) { }

        public override void Start(RunMode runMode) { }
#else

        private WebSocketClientSignalingService _signalingServiceClient;
        private WebSocketSignalingServer _signalingServiceServer;

        private UserRTCConfig _rtcConfig;
        private WebSocketSignalingConfig _webSocketSignalingConfig;

        private FlexTimer _timerLocalTimeout;
        private FlexTimer _timerIceTrickling;
        private bool _hasSentIceGatheringComplete;

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
        private RTCDataChannel _dataChannelSecond;

        private WebRTCEndPoint _endPoint = new();
        private bool _isTimedOut;

        private StringEnumConverter _jsonSettings = new StringEnumConverter()
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };

        public override bool IsTimedOut => _isTimedOut;
        public override bool IsConnectionOpen => _dataChannel != null && _dataChannel.ReadyState == RTCDataChannelState.Open;
        public override IEndPoint EndPoint => _endPoint;

        private const string LabelSendChannel = "sendChannel";
        private const string LabelSendChannelReliable = "sendChannelReliable";

        public RTCDataChannelState GetDataChannelState()
        {
            if (_dataChannel == null)
                return RTCDataChannelState.Closed;

            return _dataChannel.ReadyState;
        }

        public override void SetConfig(UserRTCConfig userRTCConfig, WebSocketSignalingConfig webSocketSignalingConfig)
        {
            _rtcConfig = userRTCConfig;
            _webSocketSignalingConfig = webSocketSignalingConfig;
        }

        public override void Start(RunMode peerMode)
        {
            _peerMode = peerMode;

            if (peerMode == RunMode.Server)
            {
                ConstructRTCPeerConnection();
            }
            else if (peerMode == RunMode.Client)
            {
                _signalingServiceClient = new WebSocketClientSignalingService();
            }
        }

        public override void Connect(string address, int port)
        {
            Log("Starting as Client");

            _signalingServiceClient.OnDisconnectedFromServer += OnDisconnectedFromSignalingServer;
            _signalingServiceClient.OnConnectedToServer += OnClientConnectedToSignalingServer;
            _signalingServiceClient.SetConfig(_webSocketSignalingConfig);

            _signalingServiceClient.Start();
            _signalingServiceClient.Connect(address, port);

            _timerLocalTimeout = FlexTimer.CreateFromSeconds(_rtcConfig.TimeoutDuration);
        }

        private void OnDisconnectedFromSignalingServer()
        {
            bool isSuccess = _signalingServiceClient.IsSuccess;

            if (isSuccess) return;

            TimeoutLocalPeer();
            CloseConnection();
        }

        public override void SetConnectionId(int id)
        {
            _connectionId = id;
        }

        public override void SetSignalingServer(WebSocketSignalingServer signalingServerClient)
        {
            _signalingServiceServer = signalingServerClient;
        }


        public override void CloseConnection()
        {
            _peerConnection?.Close();
            _dataChannel?.Close();
            _dataChannelSecond?.Close();
        }

        private void ConstructRTCPeerConnection()
        {
            RTCConfiguration configuration = GetSelectedSdpSemantics();
            _peerConnection = new RTCPeerConnection(ref configuration);
            _peerConnection.OnIceCandidate = OnIceCandidate;
            _peerConnection.OnIceConnectionChange = OnIceConnectionChange;
            _peerConnection.OnIceGatheringStateChange = OnIceGatheringStateChanged;
            _peerConnection.OnDataChannel = OnDataChannelCreated;
        }

        private void Log(string msg)
        {
            Debug.Log($"[{this}]: {msg}");
        }

        private void LogError(string msg)
        {
            Debug.LogError($"[{this}]: {msg}");
        }

        private void TimeoutLocalPeer()
        {
            _timerLocalTimeout = FlexTimer.None;
            _isTimedOut = true;
            BroadcastOnTimeout();
        }

        public override void PollUpdate()
        {
            if (_peerMode == RunMode.Client)
            {
                if (_timerLocalTimeout.IsExpired())
                {
                    TimeoutLocalPeer();
                    CloseConnection();
                    return;
                }

                _signalingServiceClient.PollUpdate();

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

                if (_rtcConfig.IceTricklingConfig.IsManual)
                    _timerIceTrickling = FlexTimer.CreateFromSeconds(_rtcConfig.IceTricklingConfig.Duration);
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

            RTCDataChannelInit rtcDataChannelReliableConfig = new RTCDataChannelInit();

            ConstructRTCPeerConnection();
            _dataChannel = _peerConnection.CreateDataChannel(LabelSendChannel, rtcDataChannelConfig);
            _dataChannel.OnClose = OnDataChannelClose;
            _dataChannel.OnOpen = OnDataChannelOpen;
            _dataChannel.OnMessage = OnDataChannelMessage;

            _dataChannelSecond = _peerConnection.CreateDataChannel(LabelSendChannelReliable, rtcDataChannelReliableConfig);
            _dataChannelSecond.OnClose = OnDataChannelReliableClose;
            _dataChannelSecond.OnOpen = OnDataChannelReliableOpen;
            _dataChannelSecond.OnMessage = OnDataChannelReliableMessage;

            Log("Creating Offer...");
            _opCreateOffer = _peerConnection.CreateOffer();
        }

        private void OnDataChannelReliableOpen()
        {
            Log("OnDataChannelReliableOpen");
        }

        private void OnDataChannelReliableMessage(byte[] bytes)
        {
            Log("OnDataChannelReliableMessage");
            BroadcastOnMessage(bytes);
        }

        private void OnDataChannelReliableClose()
        {
            Log("OnDataChannelReliableClose");
        }

        public override void OnReceivedOfferFromClient(WebRTCSessionDescription offer)
        {
            Log("Getting an offer from a client. Applying offer as remote description...");

            RTCSessionDescription sdpOffer = JsonConvert.DeserializeObject<RTCSessionDescription>(offer, _jsonSettings);

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

            RTCSessionDescription sdp = JsonConvert.DeserializeObject<RTCSessionDescription>(message, _jsonSettings);
            _opSetAnswerRemote = _peerConnection.SetRemoteDescription(ref sdp);
        }

        private RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = GetRTCIceFromUserIce(_rtcConfig.IceServers);

            return config;
        }

        protected RTCIceServer[] GetRTCIceFromUserIce(IceServer[] iceServers)
        {
            RTCIceServer[] rtcIceServers = new RTCIceServer[iceServers.Length];

            for (int i = 0; i < iceServers.Length; i++)
            {
                IceServer ice = iceServers[i];

                RTCIceServer rtcIce = new RTCIceServer()
                {
                    credential = ice.Credential,
                    credentialType = RTCIceCredentialType.Password,
                    urls = ice.Url,
                    username = ice.Username,
                };

                rtcIceServers[i] = rtcIce;
            }

            return rtcIceServers;
        }

        private void OnDataChannelOpen()
        {
            Log("OnDataChannelOpen");
            SDPParser.ParseSDP(_peerConnection.RemoteDescription.sdp, out string ip, out int port);

            _endPoint.Init(ip, port);
            _timerLocalTimeout = FlexTimer.None;
        }

        private void OnDataChannelClose()
        {
            BroadcastOnConnectionClosed();
        }

        private void OnDataChannelReliableClosed()
        {

        }

        private void OnDataChannelCreated(RTCDataChannel dataChannel)
        {
            if (dataChannel.Label == LabelSendChannel)
            {
                _dataChannel = dataChannel;
                _dataChannel.OnMessage = OnDataChannelMessage;
                _dataChannel.OnClose = OnDataChannelClose;

                SDPParser.ParseSDP(_peerConnection.RemoteDescription.sdp, out string ip, out int port);

                _endPoint.Init(ip, port);
            }
            else if (dataChannel.Label == LabelSendChannelReliable)
            {
                _dataChannelSecond = dataChannel;
                _dataChannelSecond.OnMessage = OnDataChannelReliableMessage;
                _dataChannelSecond.OnClose = OnDataChannelReliableClosed;
            }
        }

        private void OnDataChannelMessage(byte[] bytes)
        {
            Log("OnDataChannelMessage");
            BroadcastOnMessage(bytes);
        }

        public override void Send(IntPtr ptr, int length, bool isReliable)
        {
            if (!isReliable)
            {
                _dataChannel.Send(ptr, length);
                return;
            }

            _dataChannelSecond.Send(ptr, length);
        }

        private void PollIceCandidate()
        {
            if (_hasSentIceGatheringComplete) return;

            if (_peerConnection == null) return;

            if (_peerConnection.GatheringState == RTCIceGatheringState.Complete || _timerIceTrickling.IsExpired())
            {
                _timerIceTrickling = FlexTimer.None;

                _hasSentIceGatheringComplete = true;

                if (_peerMode == RunMode.Client)
                {
                    Log("Sending offer to the server...");
                    _offer = JsonConvert.SerializeObject(_peerConnection.LocalDescription, _jsonSettings);
                    SendOfferToServer();
                }
                if (_peerMode == RunMode.Server)
                {
                    Log("Sending answer to the client...");
                    _answer = JsonConvert.SerializeObject(_peerConnection.LocalDescription, _jsonSettings);
                    Debug.Log($"_answer: {_answer}");
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

            if (state == RTCIceConnectionState.Connected)
            {
                _timerLocalTimeout = FlexTimer.None;
            }
        }

        private void OnIceGatheringStateChanged(RTCIceGatheringState state)
        {
            Log($"OnIceGatheringStateChanged: {state}");
        }
#endif
    }
}