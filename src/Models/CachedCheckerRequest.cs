using System;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // CACHED CHECKER REQUEST MODEL
    // =========================================================================
    public class CachedCheckerRequest
    {
        public int Port { get; set; }
        public string Method { get; set; }
        public string Uri { get; set; }
        public string Headers { get; set; }
        public string Body { get; set; }
        public int ExpectedStatusCode { get; set; }
        public string ExpectedResponseSubstring { get; set; }
        public DateTime Timestamp { get; set; }

        public CachedCheckerRequest()
        {
            Port = 8000;
            Method = "GET";
            Uri = "/";
            Headers = "";
            Body = "";
            ExpectedStatusCode = 200;
            ExpectedResponseSubstring = "";
            Timestamp = DateTime.Now;
        }
    }
}
