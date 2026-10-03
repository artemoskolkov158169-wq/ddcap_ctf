using System;
using System.Net.Http;
using System.Net;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // SHARED HTTP CLIENT (SINGLETON & CONNECTION POOLING)
    // =========================================================================
    public static class SharedHttpClient
    {
        private static readonly HttpClient _instance;

        static SharedHttpClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            _instance = new HttpClient(handler);
            _instance.DefaultRequestHeaders.Add("User-Agent", "DDCAP-Desktop/1.0");
            _instance.Timeout = TimeSpan.FromSeconds(120);
        }

        public static HttpClient Instance
        {
            get { return _instance; }
        }

        public static HttpClient CreateProxyClient(string proxyUrl, TimeSpan timeout)
        {
            if (string.IsNullOrWhiteSpace(proxyUrl))
                return _instance;

            string clean = proxyUrl.Trim();
            if (clean.StartsWith("socks5://", StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("SOCKS5 не поддерживается в данной версии .NET, используйте HTTP прокси.");
            }

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            handler.Proxy = new System.Net.WebProxy(clean);
            handler.UseProxy = true;
            return new HttpClient(handler) { Timeout = timeout };
        }
    }
}
