using System.ComponentModel;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // TRAFFIC PACKET MODEL
    // =========================================================================
    public class TrafficPacket : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Time { get; set; }
        public string Method { get; set; }
        public string SrcIp { get; set; }
        public int SrcPort { get; set; }
        public int DstPort { get; set; }
        public string ServiceName { get; set; }
        public string Uri { get; set; }
        public int Status { get; set; }
        public string TrafficType { get; set; } // "ЧЕКЕР", "НОВЫЙ ВЕКТОР", "АТАКА", "УТЕЧКА", "ЦЕПОЧКА", "0-DAY", "НОРМА"
        public string VulnType { get; set; }    // SQLi, RCE, LFI, SSTI, Deserialization, SSRF, WebShell, Type Juggling, etc.
        public bool HasFlag { get; set; }
        public string FlagToken { get; set; }
        public double Entropy { get; set; }
        public bool IsMultiStage { get; set; }
        public bool IsAnomaly { get; set; }
        private string _correlationNotes;
        public string CorrelationNotes 
        { 
            get { return _correlationNotes; }
            set { _correlationNotes = value; OnPropertyChanged("CorrelationNotes"); }
        }
        
        public bool IsFirstSeen { get; set; }
        public string ReqHeaders { get; set; }
        public string ReqBody { get; set; }
        public string ResHeaders { get; set; }
        public string ResBody { get; set; }
        public string DecodedPayload { get; set; }
        
        private string _vulnerabilityAnalysis;
        public string VulnerabilityAnalysis 
        { 
            get { return _vulnerabilityAnalysis; }
            set { _vulnerabilityAnalysis = value; OnPropertyChanged("VulnerabilityAnalysis"); }
        }
        
        private string _slaSafePatchCode;
        public string SlaSafePatchCode 
        { 
            get { return _slaSafePatchCode; }
            set { _slaSafePatchCode = value; OnPropertyChanged("SlaSafePatchCode"); }
        }
        public string AiPromptTemplate { get; set; }

        public void EnsureReportsGenerated()
        {
            if (VulnerabilityAnalysis == null)
            {
                VulnerabilityAnalysis = AltayProxySniffer.GenerateVulnerabilityReport(TrafficType, VulnType, SrcIp, Uri, DecodedPayload, Entropy, CorrelationNotes, ServiceName);
                SlaSafePatchCode = AltayProxySniffer.GenerateSlaSafePatch(VulnType, Uri);
                AiPromptTemplate = AltayProxySniffer.GenerateAiPrompt(Method, Uri, SrcIp, VulnType, DecodedPayload, ReqHeaders, ReqBody, ServiceName);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
