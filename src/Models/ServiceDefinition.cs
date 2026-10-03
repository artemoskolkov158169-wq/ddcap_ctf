using System;
using System.ComponentModel;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // SERVICE DEFINITION
    // =========================================================================
    public class ServiceDefinition : INotifyPropertyChanged
    {
        private string _name;
        private int _listenPort;
        private string _targetUrl;
        private string _pathPrefixes;
        private string[] _cachedPrefixes;
        private bool _enabled;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public int ListenPort // Port on which DDCAP proxy listens (e.g. 8000, 8001, 8002)
        {
            get { return _listenPort; }
            set
            {
                if (_listenPort != value)
                {
                    _listenPort = value;
                    OnPropertyChanged("ListenPort");
                }
            }
        }

        public string TargetUrl
        {
            get { return _targetUrl; }
            set
            {
                if (_targetUrl != value)
                {
                    _targetUrl = value;
                    OnPropertyChanged("TargetUrl");
                }
            }
        }

        public string PathPrefixes
        {
            get { return _pathPrefixes; }
            set
            {
                if (_pathPrefixes != value)
                {
                    _pathPrefixes = value;
                    CachePrefixes();
                    OnPropertyChanged("PathPrefixes");
                }
            }
        }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }

        public ServiceDefinition()
        {
            ListenPort = 0;
            Enabled = true;
            _cachedPrefixes = new string[0];
        }

        public void CachePrefixes()
        {
            if (!string.IsNullOrEmpty(_pathPrefixes))
            {
                string[] parts = _pathPrefixes.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                _cachedPrefixes = new string[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    _cachedPrefixes[i] = parts[i].Trim();
            }
            else
            {
                _cachedPrefixes = new string[0];
            }
        }

        public bool MatchesUri(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return false;
            if (_cachedPrefixes == null) CachePrefixes();
            for (int i = 0; i < _cachedPrefixes.Length; i++)
            {
                string clean = _cachedPrefixes[i];
                if (!string.IsNullOrEmpty(clean) && uri.StartsWith(clean, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}

