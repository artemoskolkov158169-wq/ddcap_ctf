using System.Net.Sockets;
using System.Threading;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // PORT BINDING MODEL FOR MULTI-PORT PROXY
    // =========================================================================
    public class PortBinding
    {
        public int ListenPort { get; set; }
        public string TargetUrl { get; set; }
        public string ServiceName { get; set; }
        public TcpListener Listener { get; set; }
        public CancellationTokenSource Cts { get; set; }
        public bool IsRunning { get; set; }
    }
}
