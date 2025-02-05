namespace Netick.Transport.WebRTC
{
    public class WebRTCEndPoint : IEndPoint
    {
        private string _ipAddress;
        private int _port;

        public string IPAddress => _ipAddress;
        public int Port => _port;

        public void Init(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", _ipAddress, _port);
        }
    }
}
