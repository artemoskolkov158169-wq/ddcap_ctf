using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace DDCAP.AltayCTF
{
    public class FarmViewModel : INotifyPropertyChanged
    {
        private string _method = "GET";
        public string Method
        {
            get { return _method; }
            set { _method = value; OnPropertyChanged("Method"); }
        }

        private string _urlTemplate;
        public string UrlTemplate
        {
            get { return _urlTemplate; }
            set { _urlTemplate = value; OnPropertyChanged("UrlTemplate"); }
        }

        private string _headers;
        public string Headers
        {
            get { return _headers; }
            set { _headers = value; OnPropertyChanged("Headers"); }
        }

        private string _body;
        public string Body
        {
            get { return _body; }
            set { _body = value; OnPropertyChanged("Body"); }
        }

        private int _startTeam = 1;
        public int StartTeam
        {
            get { return _startTeam; }
            set { _startTeam = value; OnPropertyChanged("StartTeam"); }
        }

        private int _endTeam = 15;
        public int EndTeam
        {
            get { return _endTeam; }
            set { _endTeam = value; OnPropertyChanged("EndTeam"); }
        }

        private int _myTeam = 5;
        public int MyTeam
        {
            get { return _myTeam; }
            set { _myTeam = value; OnPropertyChanged("MyTeam"); }
        }

        private int _testTeam = 1;
        public int TestTeam
        {
            get { return _testTeam; }
            set { _testTeam = value; OnPropertyChanged("TestTeam"); }
        }

        private string _statusText = "Ферма готова к работе.";
        public string StatusText
        {
            get { return _statusText; }
            set { _statusText = value; OnPropertyChanged("StatusText"); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set { _isBusy = value; OnPropertyChanged("IsBusy"); }
        }

        private ObservableCollection<FarmAttackResult> _FarmResults = new ObservableCollection<FarmAttackResult>();
        public ObservableCollection<FarmAttackResult> FarmResults { get { return _FarmResults; } }

        public void LoadFromPacket(TrafficPacket p, AppConfig config)
        {
            if (p == null) return;

            Method = !string.IsNullOrEmpty(p.Method) ? p.Method.ToUpperInvariant().Trim() : "GET";
            string pattern = config != null ? config.TeamSubnetPattern : "10.60.{team}.2";
            int port = p.DstPort > 0 ? p.DstPort : (config != null ? config.ListenPort : 8000);
            string safeUri = (!string.IsNullOrEmpty(p.Uri) && !p.Uri.StartsWith("/")) ? "/" + p.Uri : (p.Uri ?? "/");
            
            UrlTemplate = "http://" + pattern + ":" + port + safeUri;
            Headers = p.ReqHeaders;
            Body = p.ReqBody;
        }

        public void InvertExploit(TrafficPacket p, AppConfig config)
        {
            if (p == null) return;

            Method = !string.IsNullOrEmpty(p.Method) ? p.Method.ToUpperInvariant().Trim() : "GET";
            string uri = p.Uri ?? "";
            string body = p.ReqBody ?? "";

            string cleanUri = CleanFlagTokens(uri);
            string cleanBody = CleanFlagTokens(body);

            int targetPort = p.DstPort > 0 ? p.DstPort : (config != null ? config.ListenPort : 8000);
            string pattern = (config != null && !string.IsNullOrEmpty(config.TeamSubnetPattern))
                ? config.TeamSubnetPattern : "10.60.{team}.2";
            string tpl = pattern.Replace("{team}", "{{team}}");
            string safeCleanUri = (!string.IsNullOrEmpty(cleanUri) && !cleanUri.StartsWith("/")) ? "/" + cleanUri : (cleanUri ?? "/");
            
            UrlTemplate = string.Format("http://" + tpl + ":{0}{1}", targetPort, safeCleanUri);
            Headers = p.ReqHeaders ?? "";
            Body = cleanBody;

            StatusText = string.Format("[>> ЗЕРКАЛЬНЫЙ УДАР]: Эксплойт #{0} инвертирован и загружен! Готов к запуску по всем командам.", p.Id);
            AsyncTrafficLogger.Log(string.Format("[EXPLOIT INVERTER] Пакет #{0} ({1}) инвертирован в реплеер для порта {2}", p.Id, p.VulnType, targetPort));
        }

        private static string CleanFlagTokens(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string cleaned = text;
            cleaned = Regex.Replace(cleaned, @"(?i)(flag|ddcap|altay|ructf)\{[^ \r\n\t\}]+\}", "{FLAG}");
            cleaned = Regex.Replace(cleaned, @"\b[A-Z0-9]{31}=\b", "{FLAG}");
            cleaned = Regex.Replace(cleaned, @"\b[a-f0-9]{32}\b", "{TOKEN}");
            return cleaned;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
