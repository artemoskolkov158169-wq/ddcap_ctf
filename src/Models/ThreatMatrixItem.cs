using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DDCAP.AltayCTF
{
    public class ThreatMatrixItem : INotifyPropertyChanged
    {
        private int _teamNumber;
        public int TeamNumber
        {
            get { return _teamNumber; }
            set { _teamNumber = value; OnPropertyChanged("TeamNumber"); }
        }

        private string _teamName;
        public string TeamName
        {
            get { return _teamName; }
            set { _teamName = value; OnPropertyChanged("TeamName"); }
        }

        private string _teamIp;
        public string TeamIp
        {
            get { return _teamIp; }
            set { _teamIp = value; OnPropertyChanged("TeamIp"); }
        }

        private int _totalAttacks;
        public int TotalAttacks
        {
            get { return _totalAttacks; }
            set { _totalAttacks = value; OnPropertyChanged("TotalAttacks"); }
        }

        private string _threatLevel;
        public string ThreatLevel
        {
            get { return _threatLevel; }
            set { _threatLevel = value; OnPropertyChanged("ThreatLevel"); }
        }

        private string _topVector;
        public string TopVector
        {
            get { return _topVector; }
            set { _topVector = value; OnPropertyChanged("TopVector"); }
        }

        private Dictionary<string, int> _serviceStats = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, int> ServiceStats
        {
            get { return _serviceStats; }
            set
            {
                _serviceStats = value;
                OnPropertyChanged("ServiceStats");
                OnPropertyChanged("Item[]");
            }
        }

        public string this[string key]
        {
            get
            {
                if (_serviceStats != null && _serviceStats.ContainsKey(key))
                    return _serviceStats[key].ToString();
                return "0";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }

        public void NotifyChanged(string name)
        {
            OnPropertyChanged(name);
        }
    }
}
