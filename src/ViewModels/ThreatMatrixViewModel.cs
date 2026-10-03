using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace DDCAP.AltayCTF
{
    public class ThreatAttacker : INotifyPropertyChanged
    {
        private string _ip;
        public string Ip { get { return _ip; } set { _ip = value; OnPropertyChanged("Ip"); } }

        private string _team;
        public string Team { get { return _team; } set { _team = value; OnPropertyChanged("Team"); } }

        private int _attackCount;
        public int AttackCount { get { return _attackCount; } set { _attackCount = value; OnPropertyChanged("AttackCount"); } }

        private DateTime _lastAttackTime;
        public DateTime LastAttackTime { get { return _lastAttackTime; } set { _lastAttackTime = value; OnPropertyChanged("LastAttackTime"); } }

        private ConcurrentDictionary<string, ServiceAttackStats> _serviceStats = new ConcurrentDictionary<string, ServiceAttackStats>();

        public string ServicesSummary
        {
            get
            {
                var list = _serviceStats.Values.Select(s => $"{s.ServiceName} ({string.Join(",", s.Ports)}) : {s.AttackCount}");
                return string.Join(" | ", list);
            }
        }

        public void AddAttack(string serviceName, int port, DateTime time)
        {
            string sName = string.IsNullOrEmpty(serviceName) ? "Unknown" : serviceName;
            var stat = _serviceStats.GetOrAdd(sName, k => new ServiceAttackStats { ServiceName = k });
            
            lock (stat.Ports)
            {
                stat.Ports.Add(port);
            }
            stat.AttackCount++;
            AttackCount++;
            
            if (time > LastAttackTime) LastAttackTime = time;

            OnPropertyChanged("ServicesSummary");
            OnPropertyChanged("AttackCount");
            OnPropertyChanged("LastAttackTime");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop) { if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(prop)); }
    }

    public class ServiceAttackStats
    {
        public string ServiceName { get; set; }
        public int AttackCount { get; set; }
        public System.Collections.Generic.HashSet<int> Ports { get; set; } = new System.Collections.Generic.HashSet<int>();
    }

    public class ThreatMatrixViewModel : INotifyPropertyChanged
    {
        private MainViewModel _main;
        private AppConfig _config;

        public ObservableCollection<ThreatAttacker> Attackers { get; private set; }
        private ConcurrentDictionary<string, ThreatAttacker> _attackersMap;

        public ThreatMatrixViewModel(MainViewModel main, AppConfig config)
        {
            _main = main;
            _config = config;
            Attackers = new ObservableCollection<ThreatAttacker>();
            _attackersMap = new ConcurrentDictionary<string, ThreatAttacker>();
            BindingOperations.EnableCollectionSynchronization(Attackers, new object());
        }

        public void RecordThreatAttack(TrafficPacket p)
        {
            if (p == null || (p.TrafficType != "АТАКА" && p.TrafficType != "0-DAY")) return;

            string ip = p.SrcIp ?? "Unknown";
            string team = "Team " + ExtractTeamNumber(ip);

            var attacker = _attackersMap.GetOrAdd(ip, k =>
            {
                var newAttacker = new ThreatAttacker { Ip = ip, Team = team, AttackCount = 0 };
                App.Current.Dispatcher.InvokeAsync(() => Attackers.Add(newAttacker));
                return newAttacker;
            });

            DateTime parsedTime;
            if (!DateTime.TryParse(p.Time, out parsedTime)) parsedTime = DateTime.Now;

            attacker.AddAttack(p.ServiceName, p.DstPort, parsedTime);
        }

        private string ExtractTeamNumber(string ip)
        {
            try
            {
                var parts = ip.Split('.');
                if (parts.Length == 4) return parts[2];
            }
            catch { }
            return "?";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
