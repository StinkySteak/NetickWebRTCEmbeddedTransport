using System;

namespace Netick.Transport.WebRTC
{
    public interface IWebRTCNetEventListener
    {
        void OnPeerConnected(BaseWebRTCPeer peer);
        void OnPeerDisconnected(BaseWebRTCPeer peer, DisconnectReason disconnectReason);
        void OnNetworkReceive(BaseWebRTCPeer peer, byte[] bytes);
        void OnMessageReceiveUnmanaged(BaseWebRTCPeer peer, IntPtr ptr, int length);
    }
}
