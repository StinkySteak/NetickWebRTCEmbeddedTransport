using System.Text.RegularExpressions;

namespace Netick.Transport.WebRTC
{
    public class SDPParser
    {
        public static void ParseSDP(string sdp, out string ip, out int port)
        {
            ip = string.Empty;
            port = 0;

            var portMatch = Regex.Match(sdp, @"m=application (\d+) ");
            if (portMatch.Success)
            {
                port = int.Parse(portMatch.Groups[1].Value);
            }

            var ipMatch = Regex.Match(sdp, @"c=IN IP4 ([\d\.]+)");
            if (ipMatch.Success)
            {
                ip = ipMatch.Groups[1].Value;
            }
        }
    }
}
