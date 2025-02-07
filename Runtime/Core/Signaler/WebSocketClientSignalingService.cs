using System;
using JamesFrowen.SimpleWeb;
using Newtonsoft.Json;
using UnityEngine;

namespace Netick.Transport.WebRTC
{
    public class WebSocketClientSignalingService
    {
        private SimpleWebClient _webClient;

        public event Action<int, string> OnServerAnswered;
        public event Action OnConnectedToServer;
        public event Action OnDisconnectedFromServer;

        public void PollUpdate()
        {
            _webClient?.ProcessMessageQueue();
        }

        public void Start()
        {
            TcpConfig tcpConfig = new TcpConfig(noDelay: false, sendTimeout: 5000, receiveTimeout: 20000);
            _webClient = SimpleWebClient.Create(ushort.MaxValue, 5000, tcpConfig);

            _webClient.onConnect += OnWebsocketConnected;
            _webClient.onDisconnect += OnDisconnected;
            _webClient.onData += OnWebsocketData;
            _webClient.onError += (exception) => Debug.Log($"Error because of Server, Error:{exception}");
        }

        private void OnDisconnected()
        {
            OnDisconnectedFromServer?.Invoke();
            Debug.Log($"Disconnected from signaling server");
        }

        public void Connect(string url, int port)
        {
            UriBuilder builder = new()
            {
                Scheme = "ws",
                Host = url,
                Port = port
            };

            _webClient.Connect(builder.Uri);
        }

        private void OnWebsocketConnected()
        {
            Debug.Log($"Connected to signaling server");
            OnConnectedToServer?.Invoke();
        }

        public void SendOffer(string offer)
        {
            SignalingMessage message = new();
            message.Content = offer;
            message.Type = SignalingMessageType.Offer;

            string json = JsonConvert.SerializeObject(message);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            ArraySegment<byte> dataSegment = new(data);

            _webClient.Send(dataSegment);
        }

        private void OnWebsocketData(ArraySegment<byte> data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data);

            SignalingMessage message = JsonConvert.DeserializeObject<SignalingMessage>(json);

            if (message.Type == SignalingMessageType.Answer)
            {
                string answer = message.Content;
                int myId = message.To;

                OnServerAnswered?.Invoke(myId, answer);

                Debug.Log($"Signaling Client: disconnecting...");

                _webClient.Disconnect();
            }
        }
    }
}