using System;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace DDCAP.AltayCTF
{
    public class SubmitterViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CapturedFlagItem> _flagVault;
        private AppConfig _config;
        private bool _isAutoSubmitting;
        private System.Windows.Threading.DispatcherTimer _autoSubmitTimer;

        public bool IsInitialized { get; private set; }

        public void Initialize(ObservableCollection<CapturedFlagItem> flagVault, AppConfig config)
        {
            if (IsInitialized) return;
            
            _flagVault = flagVault;
            _config = config;

            _autoSubmitTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _autoSubmitTimer.Tick += OnAutoSubmitTimerTick;
            _autoSubmitTimer.Start();
            
            IsInitialized = true;
        }

        private string _submitterUrl = "http://10.60.0.1/api/v1/submit";
        public string SubmitterUrl
        {
            get { return _submitterUrl; }
            set { _submitterUrl = value; OnPropertyChanged("SubmitterUrl"); }
        }

        private string _teamToken = "ddcap_secret_token_altay";
        public string TeamToken
        {
            get { return _teamToken; }
            set { _teamToken = value; OnPropertyChanged("TeamToken"); }
        }

        private bool _isAutoSubmitEnabled;
        public bool IsAutoSubmitEnabled
        {
            get { return _isAutoSubmitEnabled; }
            set { _isAutoSubmitEnabled = value; OnPropertyChanged("IsAutoSubmitEnabled"); }
        }

        private string _autoSubmitStats = "[ АВТОСДАЧА: ВЫКЛ | В очереди: 0 | Сдано: 0 ]";
        public string AutoSubmitStats
        {
            get { return _autoSubmitStats; }
            set { _autoSubmitStats = value; OnPropertyChanged("AutoSubmitStats"); }
        }

        private int _totalSubmittedCount = 0;
        public int TotalSubmittedCount
        {
            get { return _totalSubmittedCount; }
            set
            {
                _totalSubmittedCount = value;
                OnPropertyChanged("TotalSubmittedCount");
            }
        }

        public async System.Threading.Tasks.Task<int> SubmitAllFlagsAsync()
        {
            if (string.IsNullOrEmpty(SubmitterUrl) || _flagVault == null) return -1;

            var toSubmit = new System.Collections.Generic.List<CapturedFlagItem>();
            lock (_flagVault)
            {
                foreach (var f in _flagVault)
                {
                    if (f != null && f.Status != null && f.Status.Contains("Новый"))
                        toSubmit.Add(f);
                }
            }

            if (toSubmit.Count == 0) return 0;

            foreach (var item in toSubmit) item.UpdateStatus("Отправка...");

            int ok = 0;
            for (int i = 0; i < toSubmit.Count; i++)
            {
                var item = toSubmit[i];
                string res = await JuryFlagSubmitter.SubmitFlagAsync(SubmitterUrl, TeamToken, item.Flag);
                item.UpdateStatus(res);

                if (res != null && (res.IndexOf("accepted", StringComparison.OrdinalIgnoreCase) >= 0
                    || res.IndexOf("ok", StringComparison.OrdinalIgnoreCase) >= 0
                    || res.Contains("200")))
                {
                    ok++;
                    TotalSubmittedCount++;
                }

                MainWindow.NotifyFlagSubmitResult(item.Flag, res);

                if (i < toSubmit.Count - 1) await System.Threading.Tasks.Task.Delay(200);
            }
            return ok;
        }

        private async void OnAutoSubmitTimerTick(object sender, EventArgs e)
        {
            if (_flagVault == null) return;

            int pendingCount = 0;
            lock (_flagVault)
            {
                foreach (var f in _flagVault)
                {
                    if (f != null && f.Status != null && f.Status.Contains("Новый")) pendingCount++;
                }
            }

            if (!IsAutoSubmitEnabled)
            {
                AutoSubmitStats = string.Format("[ АВТОСДАЧА: ВЫКЛ | В очереди: {0} | Сдано: {1} ]", pendingCount, TotalSubmittedCount);
                return;
            }

            if (_isAutoSubmitting) return;
            if (string.IsNullOrEmpty(SubmitterUrl)) return;

            int rps = (_config != null && _config.FlagSubmitRps > 0) ? _config.FlagSubmitRps : 5;
            var batch = new System.Collections.Generic.List<CapturedFlagItem>();
            lock (_flagVault)
            {
                foreach (var f in _flagVault)
                {
                    if (f != null && f.Status != null && f.Status.Contains("Новый"))
                    {
                        batch.Add(f);
                        if (batch.Count >= rps) break;
                    }
                }
            }

            if (batch.Count == 0)
            {
                AutoSubmitStats = string.Format("[ АВТОСДАЧА: ОЖИДАНИЕ | В очереди: 0 | Сдано: {0} ]", TotalSubmittedCount);
                return;
            }

            _isAutoSubmitting = true;
            try
            {
                foreach (var item in batch) item.UpdateStatus("Отправка...");

                AutoSubmitStats = string.Format("[ АВТОСДАЧА: Отправка {0} шт... | В очереди: {1} | Сдано: {2} ]", batch.Count, Math.Max(0, pendingCount - batch.Count), TotalSubmittedCount);

                foreach (var item in batch)
                {
                    string res = await JuryFlagSubmitter.SubmitFlagAsync(SubmitterUrl, TeamToken, item.Flag);
                    item.UpdateStatus(res);

                    if (res != null && (res.IndexOf("accepted", StringComparison.OrdinalIgnoreCase) >= 0
                        || res.IndexOf("ok", StringComparison.OrdinalIgnoreCase) >= 0
                        || res.Contains("200")))
                    {
                        TotalSubmittedCount++;
                    }
                    MainWindow.NotifyFlagSubmitResult(item.Flag, res);
                }
            }
            finally
            {
                _isAutoSubmitting = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
