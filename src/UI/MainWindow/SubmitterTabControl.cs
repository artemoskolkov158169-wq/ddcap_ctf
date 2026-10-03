using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using System.Linq;

namespace DDCAP.AltayCTF
{
    public class SubmitterTabControl : UserControl
    {
        private ObservableCollection<CapturedFlagItem> _flagVault;
        private AppConfig _config;


        private Button _btnSubmitAll;
        private CheckBox _chkAutoSubmit;
        private TextBlock _tbAutoSubmitStats;
        private DataGrid _gridFlagVault;



        public SubmitterTabControl(ObservableCollection<CapturedFlagItem> flagVault, AppConfig config)
        {
            _flagVault = flagVault;
            _config = config;

            Content = CreateSubmitterTab();

            this.Loaded += (s, e) =>
            {
                var vm = ((DataContext as MainViewModel) != null ? (DataContext as MainViewModel).Submitter : null);
                if (vm != null && !vm.IsInitialized)
                {
                    vm.Initialize(_flagVault, _config);
                }
            };
        }

        private Grid CreateSubmitterTab()
        {
            Grid g = new Grid { Margin = new Thickness(8), Background = new SolidColorBrush(Color.FromRgb(5, 5, 5)) };
            DockPanel dp = new DockPanel();

            // Top Controls Area
            StackPanel spTopBar = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 6) };

            // Row 1: Configuration & Actions
            WrapPanel spConfig = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };


            _btnSubmitAll = new Button
            {
                Content = "[ СДАТЬ ВСЕ НОВЫЕ ФЛАГИ ]",
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(12, 4, 12, 4),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };
            DdcapUiStyles.ApplyDarkButtonStyle(_btnSubmitAll, Color.FromRgb(28, 11, 11), Color.FromRgb(239, 68, 68), Color.FromRgb(220, 38, 38));
            _btnSubmitAll.Click += OnSubmitAllFlagsClick;
            spConfig.Children.Add(_btnSubmitAll);

            Button btnExport = new Button
            {
                Content = "[ EXPORT ]",
                FontFamily = new FontFamily("Consolas"),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 10, 0)
            };
            DdcapUiStyles.ApplyDarkButtonStyle(btnExport, Color.FromRgb(20, 20, 20), Brushes.Cyan.Color, Color.FromRgb(64, 64, 64));
            btnExport.Click += (s, e) =>
            {
                try
                {
                    List<string> lines = new List<string>();
                    lock (_flagVault)
                    {
                        foreach (var f in _flagVault) lines.Add(f.Flag);
                    }
                    File.WriteAllLines(AppPaths.FlagsExport, lines);
                    MessageBox.Show("Экспортировано в " + AppPaths.FlagsExport, "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch { }
            };
            spConfig.Children.Add(btnExport);

            _chkAutoSubmit = new CheckBox
            {
                Content = "[ AUTO-SUBMIT LOOP ]",
                Foreground = new SolidColorBrush(Color.FromRgb(250, 204, 21)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0),
                Cursor = Cursors.Hand
            };
            _chkAutoSubmit.SetBinding(CheckBox.IsCheckedProperty, new Binding("Submitter.IsAutoSubmitEnabled") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            spConfig.Children.Add(_chkAutoSubmit);

            _tbAutoSubmitStats = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163)),
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 10, 0),
                FontSize = 11
            };
            _tbAutoSubmitStats.SetBinding(TextBlock.TextProperty, new Binding("Submitter.AutoSubmitStats"));
            spConfig.Children.Add(_tbAutoSubmitStats);

            TextBox txtManualFlag = new TextBox
            {
                Width = 150,
                Height = 22,
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 5, 0),
                ToolTip = "Введите флаг вручную"
            };
            spConfig.Children.Add(txtManualFlag);

            Button btnAddManual = new Button
            {
                Content = "[ ДОБАВИТЬ ]",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 10, 0)
            };
            DdcapUiStyles.ApplyDarkButtonStyle(btnAddManual, Color.FromRgb(20, 20, 20), Brushes.Cyan.Color, Color.FromRgb(64, 64, 64));
            
            Action addFlagAction = () =>
            {
                string f = txtManualFlag.Text.Trim();
                if (!string.IsNullOrEmpty(f))
                {
                    lock (_flagVault)
                    {
                        var flagItem = new CapturedFlagItem
                        {
                            Id = _flagVault.Count + 1,
                            Time = DateTime.Now.ToString("HH:mm:ss"),
                            Flag = f,
                            SourceType = "MANUAL",
                            OriginIp = "127.0.0.1",
                            Status = "Новый"
                        };
                        if (_flagVault.Count > 5000) _flagVault.RemoveAt(0);
                        _flagVault.Add(flagItem);
                    }
                    txtManualFlag.Text = "";
                }
            };

            btnAddManual.Click += (s, e) => addFlagAction();
            txtManualFlag.KeyDown += (s, e) => { if (e.Key == System.Windows.Input.Key.Enter) addFlagAction(); };

            spConfig.Children.Add(btnAddManual);

            Button btnRetry = new Button
            {
                Content = "[ ПЕРЕСДАТЬ ОШИБКИ ]",
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(5, 0, 0, 0)
            };
            DdcapUiStyles.ApplyDarkButtonStyle(btnRetry, Color.FromRgb(20, 20, 20), Brushes.Orange.Color, Color.FromRgb(64, 64, 64));
            btnRetry.Click += (s, e) =>
            {
                lock (_flagVault)
                {
                    foreach (var f in _flagVault)
                    {
                        if (f.Status != null && (f.Status.Contains("Ошибка") || f.Status.Contains("Отклонен")))
                        {
                            f.Status = "Новый";
                        }
                    }
                    if (_gridFlagVault != null) _gridFlagVault.Items.Refresh();
                }
            };
            spConfig.Children.Add(btnRetry);

            // Row 2: Filters and Statistics
            Grid filterGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Left filters
            filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right stats

            StackPanel spFilters = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            CheckBox chkHideSubmitted = new CheckBox 
            { 
                Content = "[ Скрыть уже сданные ]", 
                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)), 
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(0, 0, 15, 0) 
            };
            
            CheckBox chkHideOwn = new CheckBox 
            { 
                Content = "[ Скрыть свои флаги ]", 
                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)), 
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center, 
                Margin = new Thickness(0, 0, 15, 0) 
            };

            Action applyFilters = () =>
            {
                var view = CollectionViewSource.GetDefaultView(_flagVault);
                view.Filter = (item) =>
                {
                    var flag = item as CapturedFlagItem;
                    if (flag == null) return false;
                    
                    if (chkHideSubmitted.IsChecked == true && flag.Status != null && (flag.Status.Contains("Принят") || flag.Status.Contains("Отклонен") || flag.Status.Contains("Ошибка")))
                        return false;
                    
                    if (chkHideOwn.IsChecked == true)
                    {
                        bool isAlien = (flag.SourceType == "MANUAL" || flag.SourceType == "RED TEAM" || flag.OriginIp == "127.0.0.1");
                        if (!isAlien) return false;
                    }
                        
                    return true;
                };
            };
            
            chkHideSubmitted.Checked += (s, e) => applyFilters();
            chkHideSubmitted.Unchecked += (s, e) => applyFilters();
            chkHideOwn.Checked += (s, e) => applyFilters();
            chkHideOwn.Unchecked += (s, e) => applyFilters();
            
            spFilters.Children.Add(new TextBlock { Text = "ФИЛЬТРЫ:", Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)), FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) });
            spFilters.Children.Add(chkHideSubmitted);
            spFilters.Children.Add(chkHideOwn);
            Grid.SetColumn(spFilters, 0);
            filterGrid.Children.Add(spFilters);

            TextBlock tbVaultStats = new TextBlock
            {
                Text = "ВСЕГО: 0 | НОВЫХ: 0 | ПРИНЯТО: 0 | ОШИБОК: 0",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 150)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(tbVaultStats, 1);
            filterGrid.Children.Add(tbVaultStats);

            // Auto-update stats timer
            DispatcherTimer statsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            statsTimer.Tick += (s, e) =>
            {
                int total = _flagVault.Count;
                int nw = 0, ok = 0, err = 0;
                lock (_flagVault)
                {
                    foreach (var f in _flagVault)
                    {
                        if (f.Status == null) continue;
                        if (f.Status.Contains("Новый")) nw++;
                        else if (f.Status.Contains("Принят") || f.Status.Contains("200") || f.Status.Contains("ok") || f.Status.Contains("accepted")) ok++;
                        else if (f.Status.Contains("Ошибка") || f.Status.Contains("Отклонен") || f.Status.Contains("FAIL")) err++;
                    }
                }
                tbVaultStats.Text = string.Format("ВСЕГО: {0} | НОВЫХ: {1} | ПРИНЯТО: {2} | ОШИБОК: {3}", total, nw, ok, err);
            };
            statsTimer.Start();

            spTopBar.Children.Add(spConfig);
            spTopBar.Children.Add(filterGrid);
            DockPanel.SetDock(spTopBar, Dock.Top);
            dp.Children.Add(spTopBar);

            _gridFlagVault = new DataGrid
            {
                ItemsSource = _flagVault,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 15)),
                Foreground = Brushes.White,
                RowBackground = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(25, 25, 25)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                BorderThickness = new Thickness(1),
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                ColumnHeaderStyle = DdcapUiStyles.CreateDarkColumnHeaderStyle(),
                CellStyle = DdcapUiStyles.CreateDarkCellStyle(),
                RowStyle = DdcapUiStyles.CreateDarkRowStyle()
            };

            _gridFlagVault.Columns.Add(new DataGridTextColumn { Header = "ID", Binding = new Binding("Id"), Width = 60 });
            _gridFlagVault.Columns.Add(new DataGridTextColumn { Header = "ВРЕМЯ", Binding = new Binding("Time"), Width = 80 });
            _gridFlagVault.Columns.Add(new DataGridTextColumn { Header = "ФЛАГ", Binding = new Binding("Flag"), Width = 280 });
            _gridFlagVault.Columns.Add(new DataGridTextColumn { Header = "ИСТОЧНИК", Binding = new Binding("SourceType"), Width = 100 });
            _gridFlagVault.Columns.Add(new DataGridTextColumn { Header = "IP-АДРЕС", Binding = new Binding("OriginIp"), Width = 120 });
            
            var statusCol = new DataGridTextColumn { Header = "СТАТУС", Binding = new Binding("Status"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) };
            _gridFlagVault.Columns.Add(statusCol);

            dp.Children.Add(_gridFlagVault);
            g.Children.Add(dp);
            return g;
        }

        private async void OnSubmitAllFlagsClick(object sender, RoutedEventArgs e)
        {
            var vm = ((DataContext as MainViewModel) != null ? (DataContext as MainViewModel).Submitter : null);
            if (vm == null) return;

            _btnSubmitAll.IsEnabled = false;
            int ok = await vm.SubmitAllFlagsAsync();
            
            if (ok >= 0)
            {
                MessageBox.Show(string.Format("Отправка завершена. Сдано успешно: {0}", ok), "Результат", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Укажите URL жюри в настройках!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            _btnSubmitAll.IsEnabled = true;
        }
    }
}
