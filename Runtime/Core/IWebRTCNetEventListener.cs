namespace Netick.Transport.WebRTC
{
    public interface IWebRTCNetEventListener
    {
        void OnPeerConnected(WebRTCPeer peer);
        void OnPeerDisconnected(WebRTCPeer peer, DisconnectReason disconnectReason);
        void OnNetworkReceive(WebRTCPeer peer, byte[] bytes);
    }
}
