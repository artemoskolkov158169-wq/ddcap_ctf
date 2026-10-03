using System.ComponentModel;

namespace DDCAP.AltayCTF
{
    public class InspectorViewModel : INotifyPropertyChanged
    {
        private TrafficPacket _selectedPacket;
        public TrafficPacket SelectedPacket
        {
            get { return _selectedPacket; }
            set
            {
                _selectedPacket = value;
                OnPropertyChanged("SelectedPacket");
                OnPropertyChanged("ReqHeaders");
                OnPropertyChanged("ReqBody");
                OnPropertyChanged("ResHeaders");
                OnPropertyChanged("ResBody");
                OnPropertyChanged("DecodedPreview");
            }
        }

        public string ReqHeaders { get { return (_selectedPacket != null ? _selectedPacket.ReqHeaders : ""); } }
        public string ReqBody { get { return (_selectedPacket != null ? _selectedPacket.ReqBody : ""); } }
        public string ResHeaders { get { return (_selectedPacket != null ? _selectedPacket.ResHeaders : ""); } }
        public string ResBody { get { return (_selectedPacket != null ? _selectedPacket.ResBody : ""); } }
        
        public string DecodedPreview 
        {
            get
            {
                if (_selectedPacket == null) 
                    return "[ ДЕОБФУСЦИРОВАННАЯ ПОЛЕЗНАЯ НАГРУЗКА ]: выберите пакет в таблице для анализа...";
                return string.Format("ДЕОБФУСЦИРОВАНО [{0}]: {1}", _selectedPacket.ServiceName, _selectedPacket.DecodedPayload);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
