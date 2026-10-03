using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DDCAP.AltayCTF
{
    public class ThreatMatrixTabControl : UserControl
    {
        private AppConfig _config;

        public ThreatMatrixTabControl(AppConfig config)
        {
            _config = config;
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var header = new TextBlock
            {
                Text = "ATTACKER THREAT MATRIX",
                Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 10)
            };
            grid.Children.Add(header);
            Grid.SetRow(header, 0);

            var listView = new ListView
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 25)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 70)),
                FontFamily = new FontFamily("Consolas"),
                View = CreateGridView()
            };

            var headerStyle = new Style(typeof(GridViewColumnHeader));
            headerStyle.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(30, 30, 35))));
            headerStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            headerStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(50, 50, 70))));
            headerStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 1, 1)));
            headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(5)));
            listView.Resources.Add(typeof(GridViewColumnHeader), headerStyle);

            var itemStyle = new Style(typeof(ListViewItem));
            itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            itemStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(50, 50, 70))));
            itemStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
            listView.ItemContainerStyle = itemStyle;

            listView.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("ThreatMatrix.Attackers"));
            
            grid.Children.Add(listView);
            Grid.SetRow(listView, 1);

            Content = grid;
        }

        private GridView CreateGridView()
        {
            var gv = new GridView();
            gv.Columns.Add(new GridViewColumn { Header = "IP-адрес", DisplayMemberBinding = new Binding("Ip"), Width = 150 });
            gv.Columns.Add(new GridViewColumn { Header = "Команда", DisplayMemberBinding = new Binding("Team"), Width = 100 });
            gv.Columns.Add(new GridViewColumn { Header = "Всего атак", DisplayMemberBinding = new Binding("AttackCount"), Width = 100 });
            gv.Columns.Add(new GridViewColumn { Header = "Атакованные сервисы (порты : кол-во)", DisplayMemberBinding = new Binding("ServicesSummary"), Width = 400 });
            gv.Columns.Add(new GridViewColumn { Header = "Последняя активность", DisplayMemberBinding = new Binding("LastAttackTime") { StringFormat = "HH:mm:ss" }, Width = 180 });
            return gv;
        }
    }
}
