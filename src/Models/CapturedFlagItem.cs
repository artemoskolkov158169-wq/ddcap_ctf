using System.ComponentModel;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // CAPTURED FLAG ITEM MODEL
    // =========================================================================
    public class CapturedFlagItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Time { get; set; }
        public string Flag { get; set; }
        public string SourceType { get; set; }
        public string OriginIp { get; set; }
        public string Status { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void UpdateStatus(string s)
        {
            Status = s;
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs("Status"));
        }
    }
}
