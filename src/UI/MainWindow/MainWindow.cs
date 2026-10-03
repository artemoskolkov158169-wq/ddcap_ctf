using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Media;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;

namespace DDCAP.AltayCTF
{
    public partial class MainWindow : Window
    {
        private AltayProxySniffer _sniffer;
        private AppConfig _config;

        private MainViewModel _ViewModel = new MainViewModel();
        public MainViewModel ViewModel { get { return _ViewModel; } }

        // Collections forwarding to ViewModel
        private ObservableCollection<TrafficPacket> _allPackets { get { return ViewModel.AllPackets; } }
        private ObservableCollection<TrafficPacket> _filteredPackets { get { return ViewModel.FilteredPackets; } }
        private ObservableCollection<CapturedFlagItem> _flagVault { get { return ViewModel.FlagVault; } }
        private ObservableCollection<FarmAttackResult> _farmResults { get { return ViewModel.FarmResults; } }

        // Top Controls
        private TextBox _txtListenPort;

        private Button _btnToggleSniffer;
        private Button _btnSettings;
        private TextBlock _tbSnifferStatus;
        private TextBox _txtSearch;
        private ComboBox _cbFilterService;
        private CheckBox _chkAutoScroll;

        // Metric TextBlocks
        private TextBlock _mbTotalPackets;
        private TextBlock _mbTotalChecker;
        private TextBlock _mbTotalAttacks;
        private TextBlock _mbTotalFlags;
        private TextBlock _mbSubmittedFlags;

        // Session & Uptime Tracker
        private TextBlock _tbSessionUptime;
        private DispatcherTimer _sessionTimer;
        private DateTime _sessionStartTime;

        // UI Grids
        private DataGrid _gridPackets;

        // Inspector Tab Control
        private InspectorTabControl _inspectorTab;

        // Farm Tab Control
        private FarmTabControl _farmTab;

        // Submitter Tab Controls
        private TabControl _bottomTabs;


        private int _totalPacketsCount { get { return ViewModel.TotalPacketsCount; } set { ViewModel.TotalPacketsCount = value; } }
        private int _totalCheckerCount { get { return ViewModel.TotalCheckerCount; } set { ViewModel.TotalCheckerCount = value; } }
        private int _totalAttacksCount { get { return ViewModel.TotalAttacksCount; } set { ViewModel.TotalAttacksCount = value; } }
        private int _totalFlagsCount { get { return ViewModel.TotalFlagsCount; } set { ViewModel.TotalFlagsCount = value; } }
        private DispatcherTimer _searchDebounceTimer;



        // High-load UI batching & ring buffer extracted to TrafficManager
        private TrafficManager _trafficManager;

        // FIX B2: Static frozen brushes for LoadingRow
        private static readonly SolidColorBrush FrozenBrushNewVectorBg = MakeFrozen(48, 14, 14);
        private static readonly SolidColorBrush FrozenBrushLeakBg = MakeFrozen(60, 15, 15);
        private static readonly SolidColorBrush FrozenBrushLeakFg = MakeFrozen(255, 180, 180);
        private static readonly SolidColorBrush FrozenBrushChainBg = MakeFrozen(45, 15, 15);
        private static readonly SolidColorBrush FrozenBrushChainFg = MakeFrozen(255, 200, 200);
        private static readonly SolidColorBrush FrozenBrushZeroDayBg = MakeFrozen(50, 10, 10);
        private static readonly SolidColorBrush FrozenBrushZeroDayFg = MakeFrozen(255, 150, 150);
        private static readonly SolidColorBrush FrozenBrushAttackBg = MakeFrozen(40, 10, 10);
        private static readonly SolidColorBrush FrozenBrushAttackFg = MakeFrozen(255, 120, 120);
        private static readonly SolidColorBrush FrozenBrushCheckerBg = MakeFrozen(10, 10, 10);
        private static readonly SolidColorBrush FrozenBrushNormalEvenBg = MakeFrozen(8, 8, 8);
        private static readonly SolidColorBrush FrozenBrushNormalOddBg = MakeFrozen(14, 14, 14);
        private static readonly SolidColorBrush FrozenBrushNormalFg = MakeFrozen(170, 170, 170);

        private static SolidColorBrush MakeFrozen(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }

        // Global Event for Agent Integration
        public static event Action<string, string> FlagSubmitResultReceived;

        public static void NotifyFlagSubmitResult(string flag, string result)
        {
            if (FlagSubmitResultReceived != null)
            {
                FlagSubmitResultReceived(flag, result);
            }
        }

        public MainWindow()
        {
            _config = AppConfig.Load();
            
            // Custom Window Chrome setup for Borderless UI
            this.WindowStyle = WindowStyle.None;
            this.Background = new SolidColorBrush(Color.FromRgb(15, 15, 15));
            
            this.Resources.Add(typeof(TextBox), DdcapUiStyles.CreateDarkTextBoxStyle());
            this.Resources.Add(typeof(CheckBox), DdcapUiStyles.CreateDarkCheckBoxStyle());
            DdcapUiStyles.ApplyGlobalDarkScrollBarStyle(this);

            System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(5),
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(1)
            });

            InitializeComponentLayout();
            
            ViewModel.InitializeThreatMatrix(_config);

            _trafficManager = new TrafficManager(ViewModel);
            _trafficManager.OnThreatDetected += (p) => 
            {
                ViewModel.ThreatMatrix.RecordThreatAttack(p);
            };
            _trafficManager.OnBatchProcessed += (anyAddedToFiltered) =>
            {
                if (anyAddedToFiltered && _chkAutoScroll != null && _chkAutoScroll.IsChecked == true && _filteredPackets.Count > 0)
                {
                    _gridPackets.ScrollIntoView(_filteredPackets[_filteredPackets.Count - 1]);
                }
            };
            _trafficManager.Start();

            InitializeSniffer();
            InitializeSessionTimer();
            InitializePersistenceAndHotReload();

            this.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                {
                    if (_txtSearch != null)
                    {
                        _txtSearch.Focus();
                        _txtSearch.SelectAll();
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    if (_txtSearch != null && _txtSearch.IsFocused)
                    {
                        _txtSearch.Text = "";
                        Keyboard.ClearFocus();
                        e.Handled = true;
                    }
                }
                else if (e.Key == Key.F5)
                {
                    if (_btnToggleSniffer != null)
                    {
                        _btnToggleSniffer.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                        e.Handled = true;
                    }
                }
            };

            Closing += (s, e) =>
            {
                try { if (_sessionTimer != null) _sessionTimer.Stop(); } catch { }
                try { if (_trafficManager != null) _trafficManager.Stop(); } catch { }

                try { if (_sniffer != null) _sniffer.Stop(); } catch { }
                try { AsyncTrafficLogger.Shutdown(); } catch { }
            };
        }

        private void InitializeSessionTimer()
        {
            _sessionStartTime = DateTime.UtcNow;
            _sessionTimer = new DispatcherTimer();
            _sessionTimer.Interval = TimeSpan.FromSeconds(1);
            int secondsCounter = 0;
            _sessionTimer.Tick += (s, e) =>
            {
                secondsCounter++;
                if (secondsCounter % 120 == 0)
                {

                }
                UpdateSessionDisplay();
            };
            _sessionTimer.Start();
            UpdateSessionDisplay();
        }

        private void UpdateSessionDisplay()
        {
            if (_tbSessionUptime != null)
            {
                TimeSpan uptime = DateTime.UtcNow - _sessionStartTime;
                _tbSessionUptime.Text = string.Format("[ СЕССИЯ: {0:D2}:{1:D2}:{2:D2} ]", (int)uptime.TotalHours, uptime.Minutes, uptime.Seconds);
            }
        }

        private FileSystemWatcher _configWatcher;
        private DispatcherTimer _stateSaverTimer;

        private void InitializePersistenceAndHotReload()
        {
            // 1. Hot Reload
            try
            {
                string configDir = Path.GetDirectoryName(AppPaths.ConfigFile);
                if (string.IsNullOrEmpty(configDir)) configDir = AppDomain.CurrentDomain.BaseDirectory;
                
                if (Directory.Exists(configDir))
                {
                    _configWatcher = new FileSystemWatcher(configDir, "ddcap_config.json");
                    _configWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    _configWatcher.Changed += (s, e) =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                Thread.Sleep(200); // allow file write to finish
                                _config = AppConfig.Load();
                                if (_sniffer != null) _sniffer.UpdateConfig(_config);
                                AsyncTrafficLogger.Log("[SYSTEM] Конфигурация успешно перезагружена (Hot-Reload).");
                            }
                            catch { }
                        }));
                    };
                    _configWatcher.EnableRaisingEvents = true;
                }
            }
            catch { }

            // 2. State Saver (1 minute interval) + Log Rotation
            _stateSaverTimer = new DispatcherTimer();
            _stateSaverTimer.Interval = TimeSpan.FromMinutes(1);
            int minutesPassed = 0;
            _stateSaverTimer.Tick += (s, e) =>
            {
                minutesPassed++;
                
                // Log Rotation every 60 minutes
                if (minutesPassed % 60 == 0)
                {
                    Task.Run(() => 
                    {
                        string archive = AsyncTrafficLogger.RotateLog();
                        AsyncTrafficLogger.Log("[SYSTEM] Логи ротированы в " + archive);
                        CleanOldLogs();
                    });
                }
            };
            _stateSaverTimer.Start();
        }

        private void CleanOldLogs()
        {
            try
            {
                string logDir = AppPaths.BaseDir;
                string[] files = Directory.GetFiles(logDir, "traffic_captured_round_*.log");
                DateTime cutoff = DateTime.Now.AddHours(-12);
                foreach (string file in files)
                {
                    FileInfo fi = new FileInfo(file);
                    if (fi.CreationTime < cutoff)
                    {
                        try { fi.Delete(); } catch { }
                    }
                }
            }
            catch { }
        }

        private void InitializeComponentLayout()
        {
            Title = "[::] DDCAP DEFENSE TERMINAL // ALTAY 2026";
            Width = 1260;
            Height = 740;
            MinWidth = 950;
            MinHeight = 550;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Background = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            Foreground = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            FontFamily = new FontFamily("Consolas");

            DockPanel root = new DockPanel { LastChildFill = true };

            // 1. TOP TITLE & CONFIG BAR
            Border topBar = CreateTopBar();
            DockPanel.SetDock(topBar, Dock.Top);
            root.Children.Add(topBar);

            // 2. METRIC KPI STRIP
            Border metricStrip = CreateMetricStrip();
            DockPanel.SetDock(metricStrip, Dock.Top);
            root.Children.Add(metricStrip);

            // 3. SEARCH & QUICK FILTER TOOLBAR
            Border filterBar = CreateFilterBar();
            DockPanel.SetDock(filterBar, Dock.Top);
            root.Children.Add(filterBar);

            // 4. MAIN WORKSPACE (Packets DataGrid + Splitter + Bottom Tabs)
            Grid workspaceGrid = new Grid();
            workspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) });
            workspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4, GridUnitType.Pixel) });
            workspaceGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });

            _gridPackets = CreatePacketsDataGrid();
            Grid.SetRow(_gridPackets, 0);
            workspaceGrid.Children.Add(_gridPackets);

            GridSplitter splitter = new GridSplitter
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 4,
                Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)), // Red separator line
                ShowsPreview = true
            };
            Grid.SetRow(splitter, 1);
            workspaceGrid.Children.Add(splitter);

            _bottomTabs = CreateBottomTabs();
            Grid.SetRow(_bottomTabs, 2);
            workspaceGrid.Children.Add(_bottomTabs);

            root.Children.Add(workspaceGrid);
            Content = root;
        }







        // =====================================================================
        // PACKETS DATAGRID (DDCAP BLACK / WHITE / RED ASCII)
        // =====================================================================


        // =====================================================================
        // BOTTOM TABS (DDCAP BLACK / WHITE / RED ASCII)
        // =====================================================================
        private TabControl CreateBottomTabs()
        {
            TabControl tc = new TabControl
            {
                Background = new SolidColorBrush(Color.FromRgb(5, 5, 5)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                FontFamily = new FontFamily("Consolas"),
                ItemContainerStyle = DdcapUiStyles.CreateDarkTabItemStyle()
            };

            _inspectorTab = new InspectorTabControl();
            _inspectorTab.DataContext = ViewModel.Inspector;
            tc.Items.Add(new TabItem { Header = "[01] ИНСПЕКТОР ТРАФИКА", Content = _inspectorTab });

            _farmTab = new FarmTabControl(_flagVault, _config);
            _farmTab.DataContext = ViewModel;
            _farmTab.FlagCaptured += (s, count) => {
                _totalFlagsCount += count;
                if (_mbTotalFlags != null) _mbTotalFlags.Text = _totalFlagsCount.ToString();
            };
            tc.Items.Add(new TabItem { Header = "[02] РЕЗЕРВНЫЙ РЕПЛЕЕР", Content = _farmTab });
            var submitterTab = new SubmitterTabControl(_flagVault, _config);
            submitterTab.DataContext = ViewModel;
            tc.Items.Add(new TabItem { Header = "[03] БАНК ФЛАГОВ И ЖЮРИ", Content = submitterTab });

            var threatMatrixTab = new ThreatMatrixTabControl(_config);
            threatMatrixTab.DataContext = ViewModel;
            tc.Items.Add(new TabItem { Header = "[04] МАТРИЦА УГРОЗ", Content = threatMatrixTab });

            tc.Items.Add(new TabItem { Header = "[05] AI AGENTS", Content = new AgentChatTabControl() });

            return tc;
        }








        // =====================================================================
        // LOGIC & EVENT HANDLERS
        // =====================================================================
        private void UpdateSnifferStatusUI()
        {
            if (_sniffer == null) return;
            if (_sniffer.IsRunning)
            {
                if (_sniffer.DeviceCount > 0)
                {
                    _tbSnifferStatus.Text = "[ ПЕРЕХВАТ: " + _sniffer.ActivePortsSummary + " (" + _sniffer.DeviceCount + " адапт.) ]";
                    _tbSnifferStatus.Foreground = Brushes.White;
                }
                else
                {
                    _tbSnifferStatus.Text = "[ ОШИБКА: 0 АДАПТЕРОВ NPCAP ]";
                    _tbSnifferStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                }
            }
            else
            {
                _tbSnifferStatus.Text = "[ ОСТАНОВЛЕН ]";
                _tbSnifferStatus.Foreground = new SolidColorBrush(Color.FromRgb(115, 115, 115));
            }
        }

        private void InitializeSniffer()
        {
            _sniffer = new AltayProxySniffer();
            _sniffer.PacketCaptured += OnPacketCaptured;
            _sniffer.FlagCaptured += OnFlagCaptured;

            try
            {
                int port = int.Parse(_txtListenPort.Text);
                _sniffer.Start(port, "", _config);

                _btnToggleSniffer.Content = "[ ОСТАНОВИТЬ ]";
                _btnToggleSniffer.Background = new SolidColorBrush(Color.FromRgb(28, 11, 11));
                _btnToggleSniffer.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                _btnToggleSniffer.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                UpdateSnifferStatusUI();
            }
            catch (SocketException)
            {
                _tbSnifferStatus.Text = "[ ПОРТ " + _txtListenPort.Text + " ЗАНЯТ ]";
                _tbSnifferStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                _btnToggleSniffer.Content = "[ ЗАПУСТИТЬ ]";
                _btnToggleSniffer.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                _btnToggleSniffer.BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                _btnToggleSniffer.Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212));
            }
            catch (Exception ex)
            {
                _tbSnifferStatus.Text = "[ ОШИБКА: " + ex.Message + " ]";
                _tbSnifferStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                _btnToggleSniffer.Content = "[ ЗАПУСТИТЬ ]";
                _btnToggleSniffer.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                _btnToggleSniffer.BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                _btnToggleSniffer.Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212));
            }
        }

        private void OnToggleSnifferClick(object sender, RoutedEventArgs e)
        {
            if (_sniffer.IsRunning)
            {
                _sniffer.Stop();
                _btnToggleSniffer.Content = "[ ЗАПУСТИТЬ ]";
                _btnToggleSniffer.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
                _btnToggleSniffer.BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                _btnToggleSniffer.Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212));
                UpdateSnifferStatusUI();
            }
            else
            {
                try
                {
                    int port = int.Parse(_txtListenPort.Text);
                    _sniffer.Start(port, "", _config);

                    _btnToggleSniffer.Content = "[ ОСТАНОВИТЬ ]";
                    _btnToggleSniffer.Background = new SolidColorBrush(Color.FromRgb(28, 11, 11));
                    _btnToggleSniffer.BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38));
                    _btnToggleSniffer.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    UpdateSnifferStatusUI();
                }
                catch (SocketException)
                {
                    _tbSnifferStatus.Text = "[ ПОРТ " + _txtListenPort.Text + " ЗАНЯТ ]";
                    _tbSnifferStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    MessageBox.Show("Порт " + _txtListenPort.Text + " уже занят другой программой!\nУкажите свободный порт (например, 8080) или закройте предыдущую копию.", "Порт занят", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (Exception ex)
                {
                    _tbSnifferStatus.Text = "[ ОШИБКА: " + ex.Message + " ]";
                    _tbSnifferStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
                    MessageBox.Show("Ошибка запуска: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            var dlg = new StartupWindow(_config, true) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                _config = dlg.Config;
                _config.Save();
                _sniffer.UpdateConfig(_config);
                UpdateServiceFilterDropdown();
                if (_sniffer.IsRunning)
                {
                    UpdateSnifferStatusUI();
                }
                else
                {
                    _tbSnifferStatus.Text = "[ ГОТОВ К ЗАПУСКУ НА ПОРТАХ: " + _config.ListenPort + " ]";
                }

                _txtListenPort.Text = _config.ListenPort.ToString();
                // _txtFarmUrlTemplate.Text = "http://" + _config.TeamSubnetPattern + ":" + _config.ListenPort;
                // _txtFarmEndTeam.Text = _config.TeamsCount.ToString();
                // _txtFarmMyTeam.Text = _config.MyTeamNumber.ToString();



                ApplyPacketFilter();
            }
        }

        private void ApplyTopBarConfigChange()
        {
            if (_config == null) return;
            bool changed = false;

            int newPort;
            if (int.TryParse(_txtListenPort.Text.Trim(), out newPort) && newPort > 0 && newPort <= 65535)
            {
                if (newPort != _config.ListenPort)
                {
                    _config.ListenPort = newPort;
                    if (_config.Services != null && _config.Services.Count > 0)
                    {
                        _config.Services[0].ListenPort = newPort;
                    }
                    changed = true;
                }
            }

            if (changed)
            {
                _config.Save();
                if (_sniffer != null)
                {
                    _sniffer.UpdateConfig(_config);
                    if (_sniffer.IsRunning)
                    {
                        UpdateSnifferStatusUI();
                    }
                }
                ViewModel.Farm.UrlTemplate = "http://" + _config.TeamSubnetPattern + ":" + _config.ListenPort;
            }
        }

        private void OnPacketCaptured(TrafficPacket p)
        {
            if (p == null) return;
            if (_trafficManager != null)
            {
                _trafficManager.EnqueuePacket(p);
            }
        }

        private void OnFlagCaptured(CapturedFlagItem f)
        {
            if (f == null) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_flagVault.Count > 5000) _flagVault.RemoveAt(0);
                _flagVault.Add(f);
                _totalFlagsCount++;
                if (_mbTotalFlags != null) _mbTotalFlags.Text = _totalFlagsCount.ToString();
            }));
        }

        private void OnPacketSelected(object sender, SelectionChangedEventArgs e)
        {
            var p = _gridPackets.SelectedItem as TrafficPacket;
            if (p == null) return;

            p.EnsureReportsGenerated();
            ViewModel.SelectedPacket = p;
            
            // Pre-fill Farm
            if (_farmTab != null) _farmTab.LoadIntoFarm(p);
        }












        private void ApplyPacketFilter()
        {
            _filteredPackets.Clear();
            int start = Math.Max(0, _allPackets.Count - 5000); // 5000 is MAX_FILTERED_PACKETS
            for (int i = start; i < _allPackets.Count; i++)
            {
                var p = _allPackets[i];
                if (ViewModel.MatchesFilter(p)) _filteredPackets.Add(p);
            }
        }
    }
}
