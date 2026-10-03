using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Threading;

namespace DDCAP.AltayCTF
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<TrafficPacket> _AllPackets = new ObservableCollection<TrafficPacket>();
        public ObservableCollection<TrafficPacket> AllPackets { get { return _AllPackets; } }
        private ObservableCollection<TrafficPacket> _FilteredPackets = new ObservableCollection<TrafficPacket>();
        public ObservableCollection<TrafficPacket> FilteredPackets { get { return _FilteredPackets; } }
        private ObservableCollection<CapturedFlagItem> _FlagVault = new ObservableCollection<CapturedFlagItem>();
        public ObservableCollection<CapturedFlagItem> FlagVault { get { return _FlagVault; } }
        private ObservableCollection<FarmAttackResult> _FarmResults = new ObservableCollection<FarmAttackResult>();
        public ObservableCollection<FarmAttackResult> FarmResults { get { return _FarmResults; } }

        private int _totalPacketsCount;
        public int TotalPacketsCount
        {
            get { return _totalPacketsCount; }
            set { _totalPacketsCount = value; OnPropertyChanged("TotalPacketsCount"); }
        }

        private int _totalCheckerCount;
        public int TotalCheckerCount
        {
            get { return _totalCheckerCount; }
            set { _totalCheckerCount = value; OnPropertyChanged("TotalCheckerCount"); }
        }

        private int _totalAttacksCount;
        public int TotalAttacksCount
        {
            get { return _totalAttacksCount; }
            set { _totalAttacksCount = value; OnPropertyChanged("TotalAttacksCount"); }
        }

        private int _totalFlagsCount;
        public int TotalFlagsCount
        {
            get { return _totalFlagsCount; }
            set { _totalFlagsCount = value; OnPropertyChanged("TotalFlagsCount"); }
        }

        private TrafficPacket _selectedPacket;
        public TrafficPacket SelectedPacket
        {
            get { return _selectedPacket; }
            set
            {
                _selectedPacket = value;
                OnPropertyChanged("SelectedPacket");
                if (Inspector != null) Inspector.SelectedPacket = value;
            }
        }

        private string _activeFilter = "ALL";
        public string ActiveFilter
        {
            get { return _activeFilter; }
            set { _activeFilter = value; OnPropertyChanged("ActiveFilter"); }
        }

        private string _searchQuery = string.IsNullOrEmpty("") ? "" : ""; // Default to empty
        public string SearchQuery
        {
            get { return _searchQuery; }
            set { _searchQuery = value; OnPropertyChanged("SearchQuery"); }
        }

        private string _filterService = "Все сервисы";
        public string FilterService
        {
            get { return _filterService; }
            set { _filterService = value; OnPropertyChanged("FilterService"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public ThreatMatrixViewModel ThreatMatrix { get; private set; }


        private InspectorViewModel _Inspector = new InspectorViewModel();
        public InspectorViewModel Inspector { get { return _Inspector; } }
        private FarmViewModel _Farm = new FarmViewModel();
        public FarmViewModel Farm { get { return _Farm; } }
        private SubmitterViewModel _Submitter = new SubmitterViewModel();
        public SubmitterViewModel Submitter { get { return _Submitter; } }

        public MainViewModel()
        {
            StatePersistence.LoadState(this);
            FlagVault.CollectionChanged += (s, e) => {
                if (e.NewItems != null)
                {
                    foreach (CapturedFlagItem f in e.NewItems) StatePersistence.SaveFlag(f);
                }
            };
        }

        public void InitializeThreatMatrix(AppConfig config)
        {
            ThreatMatrix = new ThreatMatrixViewModel(this, config);
        }



        public bool MatchesFilter(TrafficPacket p)
        {
            if (p == null) return false;

            // Service Filter
            if (!string.IsNullOrEmpty(FilterService) && FilterService != "Все сервисы")
            {
                bool match = (p.ServiceName == FilterService) ||
                             FilterService.StartsWith(p.ServiceName + " (:", StringComparison.OrdinalIgnoreCase);
                if (!match) return false;
            }

            // Type Filter
            if (ActiveFilter == "CHECKER" && p.TrafficType != "ЧЕКЕР") return false;
            if (ActiveFilter == "ATTACKS" && !(p.TrafficType == "АТАКА" || p.TrafficType == "НОВЫЙ ВЕКТОР" || p.TrafficType == "УТЕЧКА" || p.TrafficType == "0-DAY")) return false;
            if (ActiveFilter == "FLAGS" && !p.HasFlag && p.TrafficType != "УТЕЧКА") return false;
            if (ActiveFilter == "MULTISTAGE" && !p.IsMultiStage && p.TrafficType != "ЦЕПОЧКА") return false;
            if (ActiveFilter == "ANOMALY" && !p.IsAnomaly && p.TrafficType != "0-DAY") return false;

            // Search query
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                string q = SearchQuery.Trim();
                if (p.DstPort.ToString() == q) return true;
                if (!string.IsNullOrEmpty(p.Uri) && p.Uri.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (!string.IsNullOrEmpty(p.SrcIp) && p.SrcIp.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (!string.IsNullOrEmpty(p.ServiceName) && p.ServiceName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (!string.IsNullOrEmpty(p.VulnType) && p.VulnType.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (!string.IsNullOrEmpty(p.FlagToken) && p.FlagToken.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (!string.IsNullOrEmpty(p.DecodedPayload) && p.DecodedPayload.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (!string.IsNullOrEmpty(p.ReqBody) && p.ReqBody.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                if (!string.IsNullOrEmpty(p.CorrelationNotes) && p.CorrelationNotes.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                return false;
            }

            return true;
        }



    }
}
