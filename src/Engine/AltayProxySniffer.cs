using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Web.Script.Serialization;

namespace DDCAP.AltayCTF
{
    public class AltayProxySniffer
    {
        public class RawTrafficData
        {
            public string ClientIp { get; set; }
            public int ClientPort { get; set; }
            public int LocalPort { get; set; }
            public string Method { get; set; }
            public string Uri { get; set; }
            public string ReqHeaders { get; set; }
            public string ReqBody { get; set; }
            public string ResHeaders { get; set; }
            public string ResBody { get; set; }
            public int ResStatus { get; set; }
            public string ResolvedService { get; set; }
        }

        private AsyncBoundedQueue<RawTrafficData> _processingChannel = new AsyncBoundedQueue<RawTrafficData>(10000);

        private readonly ConcurrentDictionary<int, PortBinding> _activePorts = new ConcurrentDictionary<int, PortBinding>();
        private static readonly ConcurrentDictionary<string, int> _seenSignatures = new ConcurrentDictionary<string, int>();
        private static readonly ConcurrentDictionary<int, CachedCheckerRequest> _lastCheckerPerPort = new ConcurrentDictionary<int, CachedCheckerRequest>();
        public static readonly ConcurrentQueue<TrafficPacket> GlobalRecentPackets = new ConcurrentQueue<TrafficPacket>();
        private volatile bool _isRunning;
        private readonly object _startStopLock = new object();
        private int _primaryPort = 8000;
        private string _primaryTargetUrl = "http://127.0.0.1:5000";
        private int _packetId = 0;
        private AppConfig _config;
        private AutonomousSocAgent _autonomousSoc;

        // FIX B6: Compiled Regexes for hot paths
        private static readonly Regex RxCtfFlag = new Regex(@"\b[A-Z0-9]{31}=\b", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
        private static readonly Regex RxSniffFlagCustom = new Regex(@"(?i)(flag|ddcap|altay|ructf)\{[^ \r\n\t\}]{6,80}\}", RegexOptions.Compiled);
        private static readonly Regex RxSniffFlagB64 = new Regex(@"\b[A-Z0-9]{31}=\b", RegexOptions.Compiled);
        private static readonly Regex RxSniffFlagMd5 = new Regex(@"\b[a-f0-9]{32}\b", RegexOptions.Compiled);

        private static readonly Regex RxCheckerHeaders = new Regex(@"(?i)(checker|checksystem|jury|altay_checker|ructf|bot|tester)", RegexOptions.Compiled);
        private static readonly Regex RxCheckerUri = new Regex(@"(?i)(/check|/putflag|/getflag|/health|/status|/ping)", RegexOptions.Compiled);
        private static readonly Regex RxCheckerBody = new Regex(@"(?i)(putflag|getflag|service_check)", RegexOptions.Compiled);

        private static readonly Regex RxVulnSqli = new Regex(@"(?i)(union[\s\+]+(?:all[\s\+]+)?select|information_schema|sys\.tables|pg_catalog|order[\s\+]+by[\s\+]+\d+|'[\s\+]*or[\s\+]*(?:\d+|'.*')|""[\s\+]*or[\s\+]*(?:\d+|"".*"")|benchmark\(|sleep\(|pg_sleep|group_concat|updatexml\(|extractvalue\(|load_file\(|select[\s\+]+.*[\s\+]+from|having[\s\+]+\d+=\d+|xp_cmdshell|exec[\s\+]*\(|waitfor[\s\+]+delay|concat_ws\(|ascii\(|substring\()", RegexOptions.Compiled);
        private static readonly Regex RxVulnRce = new Regex(@"(?i)(system\(|passthru\(|shell_exec\(|exec\(|popen\(|proc_open\(|pcntl_exec\(|;\s*cat\s+|;\s*id\b|\|\s*whoami|nc\s+-e|/bin/sh|/bin/bash|/bin/zsh|\$\([^)]+\)|\`[^`]+\`|cat\s+/etc/passwd|cat\s+flag|wget\s+|curl\s+|bash\s+-i|/api/debug/eval|/service/system|python\s+-c|perl\s+-e|ruby\s+-e|powershell|cmd\.exe|WScript\.Shell|\$IFS|\$\{IFS\}|base64\s+-d|xxd\s+-r|/dev/tcp/|/dev/udp/)", RegexOptions.Compiled);
        private static readonly Regex RxVulnLfi = new Regex(@"(?i)(\.\./|\.\.\\|/etc/passwd|/etc/shadow|/proc/self/|win\.ini|boot\.ini|flag\.txt|php://filter|php://input|data://|file://|phar://|zip://|expect://|/var/log/|/etc/hosts|/etc/issue|\%2e\%2e\%2f|\%2e\%2e/|\.\.\%2f|\.\.\%5c|\.\.\%c0\%af|\.\.\%252f)", RegexOptions.Compiled);
        private static readonly Regex RxVulnSsti = new Regex(@"\{\{[^}\r\n]{1,500}\}\}|\$\{[^}\r\n]{1,500}\}|\{%[^%\r\n]{1,500}%\}|\*\{[^}\r\n]{1,500}\}|#\{[^}\r\n]{1,500}\}|@\{[^}\r\n]{1,500}\}|<%[^%\r\n]{1,500}%>|T\(java\.lang\.Runtime\)", RegexOptions.Compiled);
        private static readonly Regex RxVulnDeserialization = new Regex(@"(?i)(cos\nsystem|__reduce__|O:\d+:""|rO0AB|!!python/object|ysoserial|T:System|java\.lang\.Runtime|fastjson|jackson|c__builtin__|cposix\n|org\.apache\.commons\.collections\.functors)", RegexOptions.Compiled);
        private static readonly Regex RxVulnSsrf = new Regex(@"(?i)(127\.0\.0\.1|localhost|0\.0\.0\.0|169\.254\.169\.254|metadata\.google|gopher://|dict://|ftp://|http://127|http://localhost|http://0|2130706433|0x7f000001|127\.1|0177\.0\.0\.1|\[::1\]|xip\.io|nip\.io)", RegexOptions.Compiled);
        private static readonly Regex RxVulnWebshell = new Regex(@"(?i)(eval\s*\(|assert\s*\(|base64_decode\s*\(|cmd\s*=|passthru\s*\(|\.php\b|exploit|ALTAYopponents|<jsp:|<%|%>\s*|eval\(base64)", RegexOptions.Compiled);
        private static readonly Regex RxVulnTypeJuggling = new Regex(@"\b[a-zA-Z0-9_]+\[\]=", RegexOptions.Compiled);
        private static readonly Regex RxVulnXxe = new Regex(@"(?i)(<!ENTITY|<!DOCTYPE|SYSTEM\s+""|PUBLIC\s+""|xxe|ent1)", RegexOptions.Compiled);
        private static readonly Regex RxVulnXss = new Regex(@"(?i)(<script>|javascript:|onerror=|onload=|alert\(|prompt\(|confirm\(|document\.cookie|<svg|onmouseover=)", RegexOptions.Compiled);
        private static readonly Regex RxVulnCmdInj = new Regex(@"(?i)(;|\||&|\$|\`|>|<)(?:\s*)(cat|id|whoami|nc|bash|sh|ls|pwd|wget|curl|ping|awk|sed)(?:\s*)", RegexOptions.Compiled);

        private static readonly Regex RxVulnLog4Shell = new Regex(@"(?i)(\$\{jndi:(?:ldap|rmi|dns|iiop|http|https)://)", RegexOptions.Compiled);
        private static readonly Regex RxVulnNoSql = new Regex(@"(?i)(\{\s*""?\$gt""?\s*:|\{\s*""?\$ne""?\s*:|\{\s*""?\$where""?\s*:|\|\|\s*1==1)", RegexOptions.Compiled);
        private static readonly Regex RxVulnLdap = new Regex(@"(?i)(\(\|\(uid=|\(&\(objectClass=|\)\s*\)\s*\(|admin\*\))", RegexOptions.Compiled);
        
        private static readonly Regex RxVulnProtoPollution = new Regex(@"(?i)(__proto__|constructor\.prototype|constructor\s*\[\s*""prototype""\s*\])", RegexOptions.Compiled);
        private static readonly Regex RxVulnGraphQL = new Regex(@"(?i)(\b__schema\b|\b__type\b|\b__typename\b|fragment\s+\w+\s+on\s+)", RegexOptions.Compiled);
        private static readonly Regex RxVulnOgnl = new Regex(@"(?i)(@java\.lang\.Runtime|#_memberAccess|#context\[|#application\[|@java\.lang\.ProcessBuilder|#request\[|#session\[)", RegexOptions.Compiled);
        private static readonly Regex RxVulnSsi = new Regex(@"(?i)(<!--#exec\s+cmd|<!--#include\s+(?:virtual|file)|<!--#echo\s+var)", RegexOptions.Compiled);
        private static readonly Regex RxVulnCrlf = new Regex(@"(?i)(%0d%0a|\\r\\n|\r\n)(?:Set-Cookie|Location|Content-Type|X-XSS-Protection):", RegexOptions.Compiled);

        private static readonly Regex RxVulnOast = new Regex(@"(?i)(interact\.sh|burpcollaborator\.net|dnslog\.cn|ceye\.io|ngrok\.io|webhook\.site|requestbin\.net|oastify\.com)", RegexOptions.Compiled);
        private static readonly Regex RxVulnConfigLeak = new Regex(@"(?i)(/\.git/config|/\.env|/WEB-INF/web\.xml|/docker-compose\.yml|/\.aws/credentials|/\.ssh/id_rsa|/\.bash_history)", RegexOptions.Compiled);
        private static readonly Regex RxVulnActuator = new Regex(@"(?i)(/actuator/env|/actuator/refresh|/actuator/heapdump|/actuator/gateway|/actuator/jolokia|/actuator/trace|/console)", RegexOptions.Compiled);

        private static readonly Regex RxVulnMassAssignment = new Regex(@"(?i)(""?is_admin""?\s*:\s*(true|1)|""?role""?\s*:\s*""?admin""?|""?permissions""?\s*:\s*\[|""?debug""?\s*:\s*(true|1))", RegexOptions.Compiled);
        private static readonly Regex RxVulnJwtManip = new Regex(@"(?i)(eyJhbGciOiJub25lI|eyJhbGciOiJOT05FI)", RegexOptions.Compiled);
        private static readonly Regex RxVulnPhpCallbacks = new Regex(@"(?i)(call_user_func|array_map|array_filter|create_function|mb_ereg_replace|preg_replace\s*\(\s*'.*?e')", RegexOptions.Compiled);

        // Extended CTF / A/D Threat Vectors
        private static readonly Regex RxVulnXPath = new Regex(@"(?i)(count\(/|//user\[|'[\s]*or[\s]*1=1[\s]*or[\s]*''='|/text\(\)=)", RegexOptions.Compiled);
        private static readonly Regex RxVulnFormatStr = new Regex(@"(%[xXpnNsS]\s*%[xXpnNsS]|%n%n%n)", RegexOptions.Compiled);
        private static readonly Regex RxVulnRubyYamlPickle = new Regex(@"(?i)(---\s+!ruby/object|c__main__|cposix)", RegexOptions.Compiled);
        private static readonly Regex RxVulnSpring4Shell = new Regex(@"(?i)(class\.module\.classLoader|\$\{script:javascript:)", RegexOptions.Compiled);
        private static readonly Regex RxVulnBof = new Regex(@"(A{256,}|%41{256,}|\x90{50,})", RegexOptions.Compiled);
        private static readonly Regex RxVulnCacheCrLf = new Regex(@"(?i)(%0d%0aSET\s+|\\r\\nSET\s+|%0d%0aFLUSHALL|\\r\\nFLUSHALL)", RegexOptions.Compiled);
        private static readonly Regex RxVulnSqliBypass = new Regex(@"(?i)(UNION/\*\*/SELECT|/\*\!50000SELECT\*/|0x[0-9a-fA-F]{12,})", RegexOptions.Compiled);
        private static readonly Regex RxVulnRceBypass = new Regex(@"(?i)(\$\{PATH:0:1\}|\$\{empty\}|\$\{\w+:-.*?\}|cat\$IFS)", RegexOptions.Compiled);

        private static readonly Regex RxSkelFlag = new Regex(@"(?i)(flag|ddcap|altay|ructf)\{[^ \r\n\t\}]+\}", RegexOptions.Compiled);
        private static readonly Regex RxSkelB64 = new Regex(@"\b[A-Za-z0-9+/]{28,32}={0,2}\b", RegexOptions.Compiled);
        private static readonly Regex RxSkelUuid = new Regex(@"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b", RegexOptions.Compiled);
        private static readonly Regex RxSkelHex = new Regex(@"\b[0-9a-fA-F]{32,64}\b", RegexOptions.Compiled);
        private static readonly Regex RxSkelDigits = new Regex(@"\b\d+\b", RegexOptions.Compiled);
        private static readonly Regex RxSkelSpaces = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly Regex RxSkelPathDigits = new Regex(@"/\d+", RegexOptions.Compiled);

        public AltayProxySniffer()
        {
            // Убрали Task.Run(ProcessingLoopAsync), он теперь запускается в Start()
        }

        public static CachedCheckerRequest GetLastCheckerRequest(int port)
        {
            CachedCheckerRequest req;
            if (_lastCheckerPerPort.TryGetValue(port, out req)) return req;
            return null;
        }

        public static void SaveCheckerRequest(int port, string method, string uri, string headers, string body, int statusCode, string resBody)
        {
            string sub = "";
            if (!string.IsNullOrEmpty(resBody))
            {
                sub = resBody.Length > 64 ? resBody.Substring(0, 64) : resBody;
            }
            _lastCheckerPerPort[port] = new CachedCheckerRequest
            {
                Port = port,
                Method = !string.IsNullOrEmpty(method) ? method : "GET",
                Uri = !string.IsNullOrEmpty(uri) ? uri : "/",
                Headers = headers ?? "",
                Body = body ?? "",
                ExpectedStatusCode = statusCode,
                ExpectedResponseSubstring = sub,
                Timestamp = DateTime.Now
            };
        }

        public static string ExtractAttackSkeleton(string method, string uri, string payload)
        {
            string cleanMethod = !string.IsNullOrEmpty(method) ? method.ToUpperInvariant().Trim() : "GET";
            string cleanUri = uri ?? "";
            string cleanPayload = payload ?? "";

            string pathPrefix = cleanUri;
            int qIdx = pathPrefix.IndexOf('?');
            string queryString = "";
            if (qIdx >= 0)
            {
                queryString = pathPrefix.Substring(qIdx);
                pathPrefix = pathPrefix.Substring(0, qIdx);
            }

            string dynamicData = queryString + " " + cleanPayload;

            // Clean dynamic values: flags, hashes, tokens, UUIDs, numbers
            dynamicData = RxSkelFlag.Replace(dynamicData, "{VAL}");
            dynamicData = RxSkelB64.Replace(dynamicData, "{VAL}");
            dynamicData = RxSkelUuid.Replace(dynamicData, "{VAL}");
            dynamicData = RxSkelHex.Replace(dynamicData, "{VAL}");
            dynamicData = RxSkelDigits.Replace(dynamicData, "{VAL}");
            dynamicData = RxSkelSpaces.Replace(dynamicData, " ").Trim();

            pathPrefix = RxSkelPathDigits.Replace(pathPrefix, "/{VAL}");
            if (string.IsNullOrEmpty(pathPrefix)) pathPrefix = "/";

            string normPayloadSig = dynamicData.Length > 200 ? dynamicData.Substring(0, 200) : dynamicData;
            return string.Format("{0}:{1}:{2}", cleanMethod, pathPrefix, normPayloadSig);
        }

        public event Action<TrafficPacket> PacketCaptured;
        public static event Action<TrafficPacket> GlobalPacketCaptured;
        public event Action<CapturedFlagItem> FlagCaptured;

        public bool IsRunning { get { return _isRunning; } }
        public int ListenPort { get { return _primaryPort; } }
        public string TargetBaseUrl { get { return _primaryTargetUrl; } }
        public IEnumerable<int> ActivePorts { get { return _activePorts.Keys; } }

        public string ActivePortsSummary
        {
            get
            {
                if (_activePorts.Count == 0) return "Остановлен";
                var list = new List<int>(_activePorts.Keys);
                list.Sort();
                return string.Join(", ", list);
            }
        }

        public int DeviceCount { get { return _devices.Count; } }
        private System.Collections.Generic.List<SharpPcap.ILiveDevice> _devices = new System.Collections.Generic.List<SharpPcap.ILiveDevice>();
        
        public void Start(int port, string targetUrl, AppConfig config)
        {
            lock (_startStopLock)
            {
                if (_isRunning) return;

                _primaryPort = port;
                _primaryTargetUrl = targetUrl.TrimEnd('/');
                _config = config;
                _autonomousSoc = new AutonomousSocAgent(_config);
                _autonomousSoc.Start();
                _isRunning = true;

                _processingChannel = new AsyncBoundedQueue<RawTrafficData>(10000);
                Task.Run(new Func<Task>(ProcessingLoopAsync));

                _activePorts.TryAdd(_primaryPort, new PortBinding { ListenPort = _primaryPort, ServiceName = "Основной" });
                if (_config != null && _config.Services != null)
                {
                    foreach (var svc in _config.Services)
                    {
                        if (svc.Enabled && svc.ListenPort > 0 && svc.ListenPort != _primaryPort)
                        {
                            _activePorts.TryAdd(svc.ListenPort, new PortBinding { ListenPort = svc.ListenPort, ServiceName = svc.Name });
                        }
                    }
                }

                try
                {
                    App.EnsureNpcapLoaded();
                    var devices = SharpPcap.CaptureDeviceList.Instance;
                    if (devices.Count == 0)
                    {
                        AsyncTrafficLogger.Log("[!] SharpPcap не нашел ни одного сетевого интерфейса. Установлен ли Npcap/WinPcap?");
                    }
                    
                    foreach (var dev in devices)
                    {
                        try
                        {
                            dev.OnPacketArrival += new SharpPcap.PacketArrivalEventHandler(Device_OnPacketArrival);
                            var cfg = new SharpPcap.DeviceConfiguration();
                            cfg.Mode = SharpPcap.DeviceModes.Promiscuous;
                            cfg.ReadTimeout = 1000;
                            dev.Open(cfg);
                            dev.Filter = "tcp";
                            dev.StartCapture();
                            _devices.Add(dev);
                        }
                        catch (Exception innerEx)
                        {
                            AsyncTrafficLogger.Log($"[-] Ошибка открытия интерфейса {dev.Description}: {innerEx.Message}");
                        }
                    }
                    AsyncTrafficLogger.Log($"[+] SharpPcap успешно запущен на {_devices.Count} из {devices.Count} интерфейсах.");
                }
                catch (Exception ex)
                {
                    AsyncTrafficLogger.Log($"[!] Критическая ошибка запуска SharpPcap: {ex.Message}");
                }
            }
        }

        private void Device_OnPacketArrival(object sender, SharpPcap.PacketCapture e)
        {
            if (!_isRunning) return;

            try
            {
                var rawPacket = e.GetPacket();
                var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);

                var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
                var ipPacket = packet.Extract<PacketDotNet.IPPacket>();

                if (tcpPacket != null && tcpPacket.PayloadData != null && tcpPacket.PayloadData.Length > 0)
                {
                    int srcPort = tcpPacket.SourcePort;
                    int dstPort = tcpPacket.DestinationPort;
                    
                    bool isRequest = _activePorts.TryGetValue(dstPort, out var reqBinding);
                    bool isResponse = _activePorts.TryGetValue(srcPort, out var resBinding);

                    // HTTP Auto-Detection (Packmate-like feature)
                    if (!isRequest && !isResponse)
                    {
                        string peek = Encoding.UTF8.GetString(tcpPacket.PayloadData, 0, Math.Min(16, tcpPacket.PayloadData.Length));
                        if (peek.StartsWith("GET ") || peek.StartsWith("POST ") || peek.StartsWith("PUT ") || peek.StartsWith("DELETE ") || peek.StartsWith("HEAD ") || peek.StartsWith("OPTIONS ") || peek.StartsWith("PATCH "))
                        {
                            isRequest = true;
                            reqBinding = new PortBinding { ListenPort = dstPort, ServiceName = "Auto:" + dstPort };
                            _activePorts.TryAdd(dstPort, reqBinding);
                            AsyncTrafficLogger.Log("[+] Авто-определён HTTP порт: " + dstPort);
                        }
                        else if (peek.StartsWith("HTTP/1."))
                        {
                            isResponse = true;
                            resBinding = new PortBinding { ListenPort = srcPort, ServiceName = "Auto:" + srcPort };
                            _activePorts.TryAdd(srcPort, resBinding);
                            AsyncTrafficLogger.Log("[+] Авто-определён HTTP порт (response): " + srcPort);
                        }
                    }

                    if (isRequest || isResponse)
                    {
                        string payloadStr = Encoding.UTF8.GetString(tcpPacket.PayloadData);
                        int headEnd = payloadStr.IndexOf("\r\n\r\n");
                        
                        string headers = "";
                        string body = "";
                        string method = "GET";
                        string uri = "/";
                        int statusCode = 200;

                        if (headEnd >= 0)
                        {
                            headers = payloadStr.Substring(0, headEnd);
                            body = payloadStr.Substring(headEnd + 4);
                            
                            var lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.None);
                            if (lines.Length > 0)
                            {
                                var parts = lines[0].Split(' ');
                                if (isRequest)
                                {
                                    if (parts.Length > 0) method = parts[0];
                                    if (parts.Length > 1) uri = parts[1];
                                }
                                else if (isResponse)
                                {
                                    if (parts.Length > 1 && int.TryParse(parts[1], out int sc)) statusCode = sc;
                                }
                            }
                        }
                        else
                        {
                            body = payloadStr;
                        }

                        string clientIp = ipPacket != null 
                            ? (isRequest ? ipPacket.SourceAddress.ToString() : ipPacket.DestinationAddress.ToString()) 
                            : "127.0.0.1";

                        var data = new RawTrafficData
                        {
                            ClientIp = clientIp,
                            ClientPort = isRequest ? srcPort : dstPort,
                            LocalPort = isRequest ? dstPort : srcPort,
                            Method = method,
                            Uri = uri,
                            ReqHeaders = isRequest ? headers : "",
                            ReqBody = isRequest ? body : "",
                            ResHeaders = isResponse ? headers : "",
                            ResBody = isResponse ? body : "",
                            ResStatus = statusCode,
                            ResolvedService = isRequest ? reqBinding.ServiceName : resBinding.ServiceName
                        };

                        _processingChannel.TryWrite(data);
                    }
                }
            }
            catch { }
        }

        public void UpdateConfig(AppConfig config)
        {
            Stop();
            Start(config.ListenPort, config.TargetBaseUrl, config);
        }

        public void Stop()
        {
            lock (_startStopLock)
            {
                if (!_isRunning) return;
                _isRunning = false;

                foreach (var dev in _devices)
                {
                    try 
                    { 
                        dev.StopCapture(); 
                        dev.Close(); 
                    } 
                    catch { }
                }
                _devices.Clear();
                _activePorts.Clear();
                _processingChannel.Complete();
                if (_autonomousSoc != null) _autonomousSoc.Stop();
            }
        }

private async Task ProcessingLoopAsync()
        {
            try
            {
                while (_isRunning || _processingChannel.Count > 0)
                {
                    var data = await _processingChannel.ReadAsync(150).ConfigureAwait(false);
                    if (data != null)
                    {
                        try
                        {
                            ProcessRawTrafficData(data);
                        }
                        catch (Exception ex)
                        {
                            AsyncTrafficLogger.Log("[!] Ошибка обработки пакета: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        public static string Deobfuscate(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string decoded = input;
            try
            {
                decoded = Regex.Replace(decoded, @"\\x([0-9A-Fa-f]{2})", m => ((char)Convert.ToByte(m.Groups[1].Value, 16)).ToString());
                decoded = Regex.Replace(decoded, @"\\[uU]([0-9A-Fa-f]{4})", m => ((char)int.Parse(m.Groups[1].Value, System.Globalization.NumberStyles.HexNumber)).ToString());
                for (int i = 0; i < 3; i++)
                {
                    string prev = decoded;
                    decoded = Uri.UnescapeDataString(decoded.Replace("+", " "));
                    if (prev == decoded) break;
                }
                decoded = decoded.Replace("/**/", " ");
            }
            catch { }
            return decoded;
        }

        private void ProcessRawTrafficData(RawTrafficData data)
        {
            int currentId = Interlocked.Increment(ref _packetId);

            string decodedPayload = Deobfuscate(data.Uri + " " + data.ReqBody);
            
            // Explicitly setting variables that were implicitly captured or undeclared before extraction
            double entropy = 0.0;
            bool isMultiStage = false;
            bool isAnomaly = false;
            string correlationNotes = "";

            string userAgent = "";
            if (data.ReqHeaders != null)
            {
                var match = Regex.Match(data.ReqHeaders, @"(?i)User-Agent:\s*([^\r\n]+)");
                if (match.Success) userAgent = match.Groups[1].Value.Trim();
            }

            if (!string.IsNullOrEmpty(userAgent))
            {
                if (Regex.IsMatch(userAgent, @"(?i)(python|curl|wget|fuzz|sqlmap|nmap|nikto|burpsuite|postman|insomnia|httpx|gobuster|dirbuster|hydra|bot)"))
                {
                    isAnomaly = true;
                    correlationNotes += "Подозрительный User-Agent: " + userAgent + ". ";
                }
            }

            string detectedVuln = ClassifyVulnerability(decodedPayload, data.Uri, data.ReqBody, entropy);

            string reqFlag = SniffFlagToken(data.Uri + " " + data.ReqBody);
            string resFlag = SniffFlagToken(data.ResBody);
            bool hasFlag = !string.IsNullOrEmpty(reqFlag) || !string.IsNullOrEmpty(resFlag);
            string activeFlag = !string.IsNullOrEmpty(resFlag) ? resFlag : reqFlag;

            bool isJuryChecker = IsCheckerTraffic(data.ReqHeaders, data.Uri, data.ReqBody, data.ClientIp, _config);

            string trafficType = "НОРМА";
            if (!string.IsNullOrEmpty(resFlag) && !isJuryChecker)
            {
                trafficType = "УТЕЧКА";
            }
            else if (detectedVuln != "Чистый трафик")
            {
                trafficType = "АТАКА";
            }
            else if (isJuryChecker)
            {
                trafficType = "ЧЕКЕР";
            }
            else if (isAnomaly)
            {
                trafficType = "АНОМАЛИЯ";
            }

            bool isFirstSeen = false;
            if (trafficType == "АТАКА" || trafficType == "УТЕЧКА" || trafficType == "0-DAY" || trafficType == "АНОМАЛИЯ")
            {
                string skeleton = ExtractAttackSkeleton(data.Method, data.Uri, data.ReqBody + " " + decodedPayload);
                if (_seenSignatures.TryAdd(skeleton, currentId))
                {
                    isFirstSeen = true;
                    if (trafficType == "АТАКА") trafficType = "НОВЫЙ ВЕКТОР";
                }
            }

            if (isJuryChecker && data.ResStatus == 200)
            {
                SaveCheckerRequest(data.LocalPort, data.Method, data.Uri, data.ReqHeaders, data.ReqBody, data.ResStatus, data.ResBody);
            }

            if (!string.IsNullOrEmpty(activeFlag))
            {
                string srcDescription = !string.IsNullOrEmpty(resFlag) ? "Утечка из нашего сервиса" : "Входящий эксплойт";
                var flagItem = new CapturedFlagItem
                {
                    Id = currentId,
                    Time = DateTime.Now.ToString("HH:mm:ss"),
                    Flag = activeFlag,
                    SourceType = srcDescription,
                    OriginIp = data.ClientIp,
                    Status = "Новый"
                };
                var fHandler = FlagCaptured;
                if (fHandler != null) fHandler(flagItem);
            }

            var packet = new TrafficPacket
            {
                Id = currentId,
                Time = DateTime.Now.ToString("HH:mm:ss.fff"),
                Method = data.Method,
                SrcIp = data.ClientIp,
                SrcPort = data.ClientPort,
                DstPort = data.LocalPort,
                ServiceName = data.ResolvedService,
                Uri = data.Uri,
                Status = data.ResStatus,
                TrafficType = trafficType,
                VulnType = detectedVuln,
                HasFlag = hasFlag,
                FlagToken = activeFlag,
                Entropy = Math.Round(entropy, 2),
                IsMultiStage = isMultiStage,
                IsAnomaly = isAnomaly,
                IsFirstSeen = isFirstSeen,
                CorrelationNotes = correlationNotes,
                ReqHeaders = data.ReqHeaders,
                ReqBody = data.ReqBody,
                ResHeaders = data.ResHeaders,
                ResBody = data.ResBody,
                DecodedPayload = decodedPayload,
                VulnerabilityAnalysis = null,
                SlaSafePatchCode = null,
                AiPromptTemplate = null
            };

            var pHandler = PacketCaptured;
            if (pHandler != null) pHandler(packet);
            
            var gpHandler = GlobalPacketCaptured;
            if (gpHandler != null) gpHandler(packet);

            GlobalRecentPackets.Enqueue(packet);
            while (GlobalRecentPackets.Count > 500)
            {
                TrafficPacket d;
                GlobalRecentPackets.TryDequeue(out d);
            }

            if (isFirstSeen || isMultiStage || trafficType == "АНОМАЛИЯ")
            {
                if (_autonomousSoc != null)
                {
                    _autonomousSoc.EnqueueSuspiciousPacket(packet);
                }
            }

            string logLine = string.Format("[{0}] #{1} | {2} (:{3}) | {4} {5} -> {6} | {7} | {8} | H={9} | Flag={10}",
                packet.Time, packet.Id, packet.ServiceName, packet.DstPort, packet.Method, packet.Uri, packet.Status, packet.TrafficType, packet.VulnType, packet.Entropy, packet.FlagToken);
            AsyncTrafficLogger.Log(logLine);
        }

        // =====================================================================
        // HEURISTICS & SNIFFERS
        // =====================================================================
        public static string SniffFlagToken(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            try
            {
                var mCtf = RxCtfFlag.Match(text);
                if (mCtf.Success) return mCtf.Value;
            }
            catch (RegexMatchTimeoutException) { }

            var m1 = RxSniffFlagCustom.Match(text);
            if (m1.Success) return m1.Value;

            var m2 = RxSniffFlagB64.Match(text);
            if (m2.Success) return m2.Value;

            var m3 = RxSniffFlagMd5.Match(text);
            if (m3.Success && !text.Contains("md5") && !text.Contains("hash") && !text.Contains("csrf")) return m3.Value;

            return "";
        }

        public static bool IsCheckerTraffic(string headers, string uri, string body, string clientIp, AppConfig config)
        {
            if (config != null && config.IsJuryIp(clientIp)) return true;
            if (headers != null && RxCheckerHeaders.IsMatch(headers)) return true;
            if (uri != null && RxCheckerUri.IsMatch(uri)) return true;
            if (body != null && RxCheckerBody.IsMatch(body)) return true;
            return false;
        }

        public static string ClassifyVulnerability(string decoded, string uri, string body, double entropy)
        {
            if (string.IsNullOrEmpty(decoded) && string.IsNullOrEmpty(uri) && string.IsNullOrEmpty(body)) return "Чистый трафик";
            string text = (decoded ?? "") + " " + (uri ?? "") + " " + (body ?? "");

            // 1. SQL Injection
            if (RxVulnSqli.IsMatch(text))
                return "SQL Injection (SQLi)";

            // 2. Remote Code Execution
            if (RxVulnRce.IsMatch(text))
                return "Remote Code Exec (RCE)";
                
            // 2b. Command Injection (Basic)
            if (RxVulnCmdInj.IsMatch(text))
                return "Command Injection";

            // 3. Path Traversal / LFI
            if (RxVulnLfi.IsMatch(text))
                return "Path Traversal (LFI)";

            // 4. Server-Side Template Injection (SSTI)
            if (RxVulnSsti.IsMatch(text))
                return "Server-Side Template Inj. (SSTI)";

            // 5. Insecure Deserialization
            if (RxVulnDeserialization.IsMatch(text))
                return "Insecure Deserialization";

            // 6. SSRF / Loopback Probe
            if (RxVulnSsrf.IsMatch(text))
                return "SSRF / Loopback Probe";

            // 7. WebShell / Command Invocation
            if (RxVulnWebshell.IsMatch(text))
                return "WebShell / Command Inj";

            // 8. Parameter Tampering / Type Juggling
            if (RxVulnTypeJuggling.IsMatch(text))
                return "Type Juggling / Array Poisoning";

            // 8b. XXE
            if (RxVulnXxe.IsMatch(text))
                return "XML External Entity (XXE)";
                
            // 8c. XSS
            if (RxVulnXss.IsMatch(text))
                return "Cross-Site Scripting (XSS)";

            // 10. Log4Shell
            if (RxVulnLog4Shell.IsMatch(text))
                return "Log4Shell (JNDI)";

            // 11. NoSQL Injection
            if (RxVulnNoSql.IsMatch(text))
                return "NoSQL Injection";

            // 12. LDAP Injection
            if (RxVulnLdap.IsMatch(text))
                return "LDAP Injection";

            // 13. Prototype Pollution (Node.js)
            if (RxVulnProtoPollution.IsMatch(text))
                return "Prototype Pollution";

            // 14. GraphQL Introspection / Abuse
            if (RxVulnGraphQL.IsMatch(text))
                return "GraphQL Abuse / Introspection";

            // 15. OGNL / EL Injection (Java)
            if (RxVulnOgnl.IsMatch(text))
                return "OGNL / EL Injection";

            // 16. Server-Side Includes (SSI)
            if (RxVulnSsi.IsMatch(text))
                return "Server-Side Includes (SSI)";

            // 17. CRLF Injection / HTTP Response Splitting
            if (RxVulnCrlf.IsMatch(text))
                return "CRLF / HTTP Splitting";

            // 18. Out-of-Band (OAST) / DNSLog
            if (RxVulnOast.IsMatch(text))
                return "OAST / DNSLog Callback";

            // 19. Config & Secrets Exposure
            if (RxVulnConfigLeak.IsMatch(text))
                return "Config/Secrets Exposure";

            // 20. Framework Endpoints (Actuators, Consoles)
            if (RxVulnActuator.IsMatch(text))
                return "Exposed Framework Endpoint";

            // 21. Mass Assignment / Privilege Escalation
            if (RxVulnMassAssignment.IsMatch(text))
                return "Mass Assignment / PrivEsc";

            // 22. JWT 'None' Algorithm / Manipulation
            if (RxVulnJwtManip.IsMatch(text))
                return "JWT Algorithm Bypass";

            // 23. PHP Callback RCE
            if (RxVulnPhpCallbacks.IsMatch(text))
                return "PHP Callback RCE";

            // 24. XPath Injection
            if (RxVulnXPath.IsMatch(text))
                return "XPath Injection";

            // 25. Format String Vulnerabilities
            if (RxVulnFormatStr.IsMatch(text))
                return "Format String (BoF/Leak)";

            // 26. Advanced Deserialization (Ruby/YAML/Pickle)
            if (RxVulnRubyYamlPickle.IsMatch(text))
                return "Adv. Deserialization (Ruby/Pickle)";

            // 27. Spring4Shell / Text4Shell
            if (RxVulnSpring4Shell.IsMatch(text))
                return "Spring4Shell / Framework RCE";

            // 28. Buffer Overflow (BoF)
            if (RxVulnBof.IsMatch(text))
                return "Buffer Overflow (BoF)";

            // 29. Cache Injection (Redis/Memcached CRLF)
            if (RxVulnCacheCrLf.IsMatch(text))
                return "Cache Injection (CRLF)";

            // 30. SQLi WAF Bypasses
            if (RxVulnSqliBypass.IsMatch(text))
                return "SQLi Bypass (Obfuscated)";

            // 31. RCE WAF Bypasses
            if (RxVulnRceBypass.IsMatch(text))
                return "RCE Bypass (Obfuscated)";

            if (text.Contains("%00") || text.Contains("\\x00") || text.Contains("\0"))
                return "Null-Byte Injection";

            // 9. Zero-Day High Entropy
            if (entropy >= 4.75 && (decoded.Length >= 24 || body.Length >= 24))
                return string.Format("Аномальная энтропия ({0:F2}) ", entropy);

            return "Чистый трафик";
        }

        public static string GenerateVulnerabilityReport(string trafficType, string vuln, string ip, string uri, string payload, double entropy, string correlation, string service)
        {
            return "Ожидание анализа нейросети...\r\n" +
                   "Нажмите 'ГЕНЕРИРОВАТЬ AI ПАТЧ', чтобы провести полноценный анализ данного запроса и выявить потенциальные угрозы.";
        }

        public static string GenerateSlaSafePatch(string vuln, string uri)
        {
            return "# --------------------------------------------------------------------\n" +
                   "# [DDCAP SLA-SAFE PATCH: ОЖИДАНИЕ AI]\n" +
                   "# --------------------------------------------------------------------\n\n" +
                   "# Нажмите кнопку 'ГЕНЕРИРОВАТЬ AI ПАТЧ', чтобы получить хирургический\n" +
                   "# патч, сгенерированный нейросетью с учетом бизнес-логики сервиса.";
        }

        public static string GenerateAiPrompt(string method, string uri, string ip, string vuln, string decoded, string reqHeaders, string reqBody, string service)
        {
            return string.Format(
@"# ======================================================================================
# DDCAP ALTAY 2026 // AI SLA-SAFE PATCH PROMPT ДЛЯ LLM
# ======================================================================================
Ты ведущий Application Security специалист команды DDCAP на Attack-Defense CTF соревновании.
Наш сервис VulnBox был атакован командой противника.

ДЕТАЛИ ПЕРЕХВАЧЕННОГО ЗАПРОСА:
- Сервис: {0}
- Метод HTTP: {1}
- Целевой URI: {2}
- IP противника: {3}
- Тип уязвимости: {4}

ДЕОБФУСЦИРОВАННАЯ ПОЛЕЗНАЯ НАГРУЗКА:
{5}

СЫРЫЕ ЗАГОЛОВКИ ЗАПРОСА:
{6}

СЫРОЕ ТЕЛО ЗАПРОСА:
{7}

ПРАВИЛА И ОГРАНИЧЕНИЯ (SLA REGULATIONS):
1. Категорически НЕ ИСПОЛЬЗУЙ WAF, блокировку по IP, iptables DROP или HTTP 403 Forbidden! Это сломает чекер жюри и приведет к потере очков SLA.
2. Чекер жюри выполняет регулярные проверки (putflag/getflag) на этот же сервис. Валидные данные обязаны обрабатываться корректно и возвращать 200 OK.
3. Напиши хирургический патч для обработчика (Python, PHP или Go):
   - Используй строгую валидацию или параметризацию (Prepared Statements).
   - Если входные данные некорректны, возвращай мягкую обработку ошибки (200 OK с сообщением в JSON), чтобы не ломать чекер.
4. Выведи точный diff (БЫЛО -> СТАЛО) и поясни, почему чекер продолжит работать.",
                service, method, uri, ip, vuln, decoded, reqHeaders, reqBody);
        }
    }
}
