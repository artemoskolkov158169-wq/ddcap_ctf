using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DDCAP.AltayCTF
{
    public class FarmTabControl : UserControl
    {
        private ComboBox _cbFarmMethod;
        private TextBox _txtFarmUrlTemplate;
        private TextBox _txtFarmHeaders;
        private TextBox _txtFarmBody;
        private TextBox _txtFarmStartTeam;
        private TextBox _txtFarmEndTeam;
        private TextBox _txtFarmMyTeam;
        private TextBox _txtFarmTestTeam;
        private Button _btnTestSingleTeam;
        private Button _btnRunFarm;
        private TextBlock _tbFarmStatus;
        private ProgressBar _pbFarmProgress;
        private DataGrid _gridFarm;

        private AppConfig _config;
        private ObservableCollection<CapturedFlagItem> _flagVault;

        // Event to notify MainWindow about captured flags count change
        public event EventHandler<int> FlagCaptured;

        public FarmTabControl(ObservableCollection<CapturedFlagItem> flagVault, AppConfig config)
        {
            _flagVault = flagVault;
            _config = config;
            Content = CreateFarmTab();
        }

        private Grid CreateFarmTab()
        {
            Grid g = new Grid { Margin = new Thickness(8), Background = new SolidColorBrush(Color.FromRgb(5, 5, 5)) };
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left: Configuration of Attack
            DockPanel dpLeft = new DockPanel { Margin = new Thickness(0, 0, 4, 0) };

            DockPanel row1 = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
            _cbFarmMethod = new ComboBox
            {
                Width = 75,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold
            };
            DdcapUiStyles.ApplyDarkComboBoxStyle(_cbFarmMethod, Color.FromRgb(20, 20, 20), Color.FromRgb(240, 240, 240));
            _cbFarmMethod.Items.Add("GET");
            _cbFarmMethod.Items.Add("POST");
            _cbFarmMethod.Items.Add("PUT");
            _cbFarmMethod.Items.Add("DELETE");
            _cbFarmMethod.Items.Add("PYTHON");
            _cbFarmMethod.SetBinding(ComboBox.SelectedItemProperty, new Binding("Farm.Method") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            DockPanel.SetDock(_cbFarmMethod, Dock.Left);
            row1.Children.Add(_cbFarmMethod);

            _txtFarmUrlTemplate = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(4)
            };
            _txtFarmUrlTemplate.SetBinding(TextBox.TextProperty, new Binding("Farm.UrlTemplate") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            row1.Children.Add(_txtFarmUrlTemplate);
            DockPanel.SetDock(row1, Dock.Top);
            dpLeft.Children.Add(row1);

            // Headers & Body
            _txtFarmHeaders = new TextBox
            {
                AcceptsReturn = true,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                Foreground = new SolidColorBrush(Color.FromRgb(229, 229, 229)),
                FontFamily = new FontFamily("Consolas"),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                FontSize = 10.5,
                Height = 65,
                Margin = new Thickness(0, 0, 0, 4),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _txtFarmHeaders.SetBinding(TextBox.TextProperty, new Binding("Farm.Headers") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            DockPanel.SetDock(_txtFarmHeaders, Dock.Top);
            dpLeft.Children.Add(_txtFarmHeaders);

            _txtFarmBody = new TextBox
            {
                AcceptsReturn = true,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                Foreground = new SolidColorBrush(Color.FromRgb(229, 229, 229)),
                FontFamily = new FontFamily("Consolas"),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                FontSize = 10.5,
                Margin = new Thickness(0, 0, 0, 4),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _txtFarmBody.SetBinding(TextBox.TextProperty, new Binding("Farm.Body") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            dpLeft.Children.Add(_txtFarmBody);

            // Range Controls
            WrapPanel spRange = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            spRange.Children.Add(new TextBlock { Text = "Команды с:", Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163)), FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center });
            _txtFarmStartTeam = new TextBox { Width = 36, Height = 22, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)), BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)), BorderThickness = new Thickness(1), Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), TextAlignment = TextAlignment.Center };
            _txtFarmStartTeam.SetBinding(TextBox.TextProperty, new Binding("Farm.StartTeam") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            spRange.Children.Add(_txtFarmStartTeam);

            spRange.Children.Add(new TextBlock { Text = "по:", Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163)), FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center });
            _txtFarmEndTeam = new TextBox { Width = 36, Height = 22, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 6, 0), Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)), BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)), BorderThickness = new Thickness(1), Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), TextAlignment = TextAlignment.Center };
            _txtFarmEndTeam.SetBinding(TextBox.TextProperty, new Binding("Farm.EndTeam") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            spRange.Children.Add(_txtFarmEndTeam);

            spRange.Children.Add(new TextBlock { Text = "Наша:", Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163)), FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center });
            _txtFarmMyTeam = new TextBox { Width = 36, Height = 22, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)), BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)), BorderThickness = new Thickness(1), Foreground = Brushes.White, FontFamily = new FontFamily("Consolas"), TextAlignment = TextAlignment.Center };
            _txtFarmMyTeam.SetBinding(TextBox.TextProperty, new Binding("Farm.MyTeam") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            spRange.Children.Add(_txtFarmMyTeam);

            spRange.Children.Add(new TextBlock { Text = "Тест:", Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163)), FontFamily = new FontFamily("Consolas"), VerticalAlignment = VerticalAlignment.Center });
            _txtFarmTestTeam = new TextBox { Width = 36, Height = 22, VerticalAlignment = VerticalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)), BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)), BorderThickness = new Thickness(1), Foreground = Brushes.Cyan, FontFamily = new FontFamily("Consolas"), TextAlignment = TextAlignment.Center };
            _txtFarmTestTeam.SetBinding(TextBox.TextProperty, new Binding("Farm.TestTeam") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            spRange.Children.Add(_txtFarmTestTeam);

            _btnTestSingleTeam = new Button
            {
                Content = string.Format("[ ТЕСТ КОМАНДЫ {0} ]", _txtFarmTestTeam.Text.Trim()),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            _txtFarmTestTeam.TextChanged += (s, e) => 
            {
                if (_btnTestSingleTeam != null)
                {
                    string teamNum = _txtFarmTestTeam.Text.Trim();
                    if (string.IsNullOrEmpty(teamNum)) teamNum = "?";
                    _btnTestSingleTeam.Content = string.Format("[ ТЕСТ КОМАНДЫ {0} ]", teamNum);
                }
            };
            DdcapUiStyles.ApplyDarkButtonStyle(_btnTestSingleTeam, Color.FromRgb(20, 20, 20), Brushes.White.Color, Color.FromRgb(64, 64, 64));
            _btnTestSingleTeam.Click += OnTestSingleTeamClick;
            spRange.Children.Add(_btnTestSingleTeam);

            _btnRunFarm = new Button
            {
                Content = "[ АТАКА ВСЕХ ]",
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(8, 4, 8, 4),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            DdcapUiStyles.ApplyDarkButtonStyle(_btnRunFarm, Color.FromRgb(28, 11, 11), Color.FromRgb(239, 68, 68), Color.FromRgb(220, 38, 38));
            _btnRunFarm.Click += OnRunFarmClick;
            spRange.Children.Add(_btnRunFarm);

            DockPanel.SetDock(spRange, Dock.Bottom);
            dpLeft.Children.Add(spRange);

            Grid.SetColumn(dpLeft, 0);
            g.Children.Add(dpLeft);

            // Right: Broadcast Results Grid
            DockPanel dpRight = new DockPanel { Margin = new Thickness(4, 0, 0, 0) };

            StackPanel spStatus = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 4) };

            _tbFarmStatus = new TextBlock
            {
                Foreground = new SolidColorBrush(Color.FromRgb(115, 115, 115)),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 0, 0, 4)
            };
            _tbFarmStatus.SetBinding(TextBlock.TextProperty, new Binding("Farm.StatusText"));

            _pbFarmProgress = new ProgressBar
            {
                Height = 4,
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                BorderThickness = new Thickness(0),
                Visibility = Visibility.Collapsed
            };

            spStatus.Children.Add(_tbFarmStatus);
            spStatus.Children.Add(_pbFarmProgress);

            DockPanel.SetDock(spStatus, Dock.Top);
            dpRight.Children.Add(spStatus);

            _gridFarm = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                RowBackground = new SolidColorBrush(Color.FromRgb(8, 8, 8)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(14, 14, 14)),
                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                ColumnHeaderStyle = DdcapUiStyles.CreateDarkColumnHeaderStyle(),
                CellStyle = DdcapUiStyles.CreateDarkCellStyle(),
                RowStyle = DdcapUiStyles.CreateDarkRowStyle()
            };
            _gridFarm.SetBinding(DataGrid.ItemsSourceProperty, new Binding("Farm.FarmResults"));
            VirtualizingPanel.SetIsVirtualizing(_gridFarm, true);
            VirtualizingPanel.SetVirtualizationMode(_gridFarm, VirtualizationMode.Recycling);
            _gridFarm.EnableRowVirtualization = true;
            _gridFarm.Columns.Add(new DataGridTextColumn { Header = "Команда", Binding = new Binding("TeamNumber"), Width = 65 });
            _gridFarm.Columns.Add(new DataGridTextColumn { Header = "IP Цели", Binding = new Binding("TeamIp"), Width = 110 });
            _gridFarm.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new Binding("HttpCode"), Width = 45 });
            _gridFarm.Columns.Add(new DataGridTextColumn { Header = "RTT (мс)", Binding = new Binding("LatencyMs"), Width = 65 });
            _gridFarm.Columns.Add(new DataGridTextColumn { Header = "Добытый флаг", Binding = new Binding("CapturedFlag"), Width = 190 });
            _gridFarm.Columns.Add(new DataGridTextColumn { Header = "Ответ", Binding = new Binding("ResponsePreview"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

            ContextMenu cmFarm = new ContextMenu { FontFamily = new FontFamily("Consolas") };
            MenuItem miCopyFlag = new MenuItem { Header = "[F] Копировать добытый флаг", Foreground = Brushes.Cyan, Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)) };
            miCopyFlag.Click += (s, e) => {
                var sel = _gridFarm.SelectedItem as FarmAttackResult;
                if (sel != null && !string.IsNullOrEmpty(sel.CapturedFlag))
                    try { Clipboard.SetText(sel.CapturedFlag); } catch { }
            };
            MenuItem miCopyRes = new MenuItem { Header = "[R] Копировать полный ответ сервера", Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)) };
            miCopyRes.Click += (s, e) => {
                var sel = _gridFarm.SelectedItem as FarmAttackResult;
                if (sel != null && !string.IsNullOrEmpty(sel.FullResponse))
                    try { Clipboard.SetText(sel.FullResponse); } catch { }
            };
            cmFarm.Items.Add(miCopyFlag);
            cmFarm.Items.Add(miCopyRes);
            _gridFarm.ContextMenu = cmFarm;

            dpRight.Children.Add(_gridFarm);
            Grid.SetColumn(dpRight, 1);
            g.Children.Add(dpRight);

            return g;
        }

        public void LoadIntoFarm(TrafficPacket p)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null) vm.Farm.LoadFromPacket(p, _config);
        }

        public void InvertExploitToFarm(TrafficPacket p)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null) vm.Farm.InvertExploit(p, _config);
        }

        // CleanFlagTokens moved to ViewModel

        private async void OnRunFarmClick(object sender, RoutedEventArgs e)
        {
            var vm = ((DataContext as MainViewModel) != null ? (DataContext as MainViewModel).Farm : null);
            if (vm == null) return;

            string urlTemplate = vm.UrlTemplate ?? "";
            string method = vm.Method ?? "GET";
            string headers = vm.Headers ?? "";
            string body = vm.Body ?? "";

            int startTeam = vm.StartTeam;
            int endTeam = vm.EndTeam;
            int myTeam = vm.MyTeam;

            if (_btnRunFarm != null) _btnRunFarm.IsEnabled = false;
            if (_btnTestSingleTeam != null) _btnTestSingleTeam.IsEnabled = false;
            vm.StatusText = "Атака запущена. Сканирование команд " + startTeam + ".." + endTeam + "...";

            int totalTeams = endTeam - startTeam + 1;
            if (startTeam <= myTeam && endTeam >= myTeam) totalTeams--;
            
            if (_pbFarmProgress != null)
            {
                _pbFarmProgress.Maximum = totalTeams > 0 ? totalTeams : 1;
                _pbFarmProgress.Value = 0;
                _pbFarmProgress.Visibility = Visibility.Visible;
            }

            vm.FarmResults.Clear();

            int capturedInRun = 0;
            int completedTasks = 0;

            try
            {
                var results = await ExploitFarmEngine.RunBroadcastAttackAsync(
                    urlTemplate, method, headers, body, startTeam, endTeam, myTeam,
                    res =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            completedTasks++;
                            if (_pbFarmProgress != null) _pbFarmProgress.Value = completedTasks;
                            if (vm.FarmResults.Count > 5000) vm.FarmResults.RemoveAt(0);
                            vm.FarmResults.Add(res);
                            if (res.FlagFound)
                            {
                                capturedInRun++;
                                var flagItem = new CapturedFlagItem
                                {
                                    Id = _flagVault.Count + 1,
                                    Time = DateTime.Now.ToString("HH:mm:ss"),
                                    Flag = res.CapturedFlag,
                                    SourceType = "Украден с Команды " + res.TeamNumber,
                                    OriginIp = res.TeamIp,
                                    Status = "Новый"
                                };
                                if (_flagVault.Count > 5000) _flagVault.RemoveAt(0);
                                _flagVault.Add(flagItem);
                                
                                if (FlagCaptured != null) FlagCaptured(this, 1);
                            }
                        }));
                    });

                vm.StatusText = string.Format("Атака завершена. Атаковано: {0} команд. Украдено флагов: {1} шт.", results.Count, capturedInRun);
            }
            catch (Exception ex)
            {
                vm.StatusText = "Ошибка выполнения атаки: " + ex.Message;
            }
            finally
            {
                if (_btnRunFarm != null) _btnRunFarm.IsEnabled = true;
                if (_btnTestSingleTeam != null) _btnTestSingleTeam.IsEnabled = true;
                if (_pbFarmProgress != null) _pbFarmProgress.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnTestSingleTeamClick(object sender, RoutedEventArgs e)
        {
            var vm = ((DataContext as MainViewModel) != null ? (DataContext as MainViewModel).Farm : null);
            if (vm == null) return;

            string urlTemplate = vm.UrlTemplate ?? "";
            string method = vm.Method ?? "GET";
            string headers = vm.Headers ?? "";
            string body = vm.Body ?? "";

            int testTeam = vm.TestTeam;

            if (_btnRunFarm != null) _btnRunFarm.IsEnabled = false;
            if (_btnTestSingleTeam != null) _btnTestSingleTeam.IsEnabled = false;
            vm.StatusText = "Проверка на команде " + testTeam + "...";

            try
            {
                var res = await ExploitFarmEngine.AttackSingleTargetAsync(urlTemplate, method, headers, body, testTeam);
                if (vm.FarmResults.Count > 5000) vm.FarmResults.RemoveAt(vm.FarmResults.Count - 1);
                vm.FarmResults.Insert(0, res);
                if (res.FlagFound)
                {
                    vm.StatusText = string.Format("УСПЕХ: Флаг получен от команды {0}: {1}", testTeam, res.CapturedFlag);
                    var flagItem = new CapturedFlagItem
                    {
                        Id = _flagVault.Count + 1,
                        Time = DateTime.Now.ToString("HH:mm:ss"),
                        Flag = res.CapturedFlag,
                        SourceType = "Резервная ферма (Команда " + res.TeamNumber + ")",
                        OriginIp = res.TeamIp,
                        Status = "Новый"
                    };
                    if (_flagVault.Count > 5000) _flagVault.RemoveAt(0);
                    _flagVault.Add(flagItem);
                    
                    if (FlagCaptured != null) FlagCaptured(this, 1);
                }
                else
                {
                    vm.StatusText = string.Format("Команда {0}: HTTP {1}, {2} мс. Ответ: {3}", testTeam, res.HttpCode, res.LatencyMs, res.ResponsePreview);
                }
            }
            catch (Exception ex)
            {
                vm.StatusText = "Ошибка одиночной проверки: " + ex.Message;
            }
            finally
            {
                if (_btnRunFarm != null) _btnRunFarm.IsEnabled = true;
                if (_btnTestSingleTeam != null) _btnTestSingleTeam.IsEnabled = true;
            }
        }
    }
}
