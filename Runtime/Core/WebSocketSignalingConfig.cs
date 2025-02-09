using System;
using System.Security.Authentication;

namespace Netick.Transport.WebRTC
{
    [Serializable]
    public struct WebSocketSignalingConfig
    {
        public bool ConnectSecurely;
        public WebSocketServerSignalingConfig SignalingServerConfig;
    }

    [Serializable]
    public struct WebSocketServerSignalingConfig
    {
        public bool EnableSSL;
        public SslProtocols SslProtocols;
        public string SslFilePath;

        public static WebSocketServerSignalingConfig Default()
        {
            WebSocketServerSignalingConfig config = new()
            {
                SslFilePath = "./cert.json",
                SslProtocols = SslProtocols.Tls12
            };

            return config;
        }
    }
}