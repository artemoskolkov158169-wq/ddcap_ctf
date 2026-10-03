using System;
using System.Windows.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DDCAP.AltayCTF
{
    public partial class MainWindow : Window
    {
        // =====================================================================
        // TOP CONTROL BAR (Grid-based for scaling)
        // =====================================================================
        private Border CreateTopBar()
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(8, 8, 8)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(12, 6, 12, 6)
            };
            
            // Allow drag-moving the borderless window
            b.MouseLeftButtonDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) this.DragMove(); };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Left side
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Right side (controls)
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Far Right side (Window Buttons)

            // Left: ASCII Brand & Timer
            StackPanel spBrand = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock tbLogo = new TextBlock
            {
                Text = "[::] DDCAP // DEFENSE MATRIX",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 150)), // Cyberpunk green
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 14,
                Margin = new Thickness(0, 0, 8, 0)
            };
            TextBlock tbSub = new TextBlock
            {
                Text = "[ALTAY 2026 // DDCAP SUITE]",
                Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163)),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 14, 0)
            };

            Border bSession = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 25, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 200, 255)), // Cyan accent
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 14, 0)
            };
            _tbSessionUptime = new TextBlock
            {
                Text = "[ СЕССИЯ: 00:00:00 ]",
                Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 255)),
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11
            };
            bSession.Child = _tbSessionUptime;

            spBrand.Children.Add(tbLogo);
            spBrand.Children.Add(tbSub);
            spBrand.Children.Add(bSession);

            Grid.SetColumn(spBrand, 0);
            grid.Children.Add(spBrand);

            // Right: Proxy Controls & Settings
            StackPanel spControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            spControls.Children.Add(new TextBlock { Text = "ПОРТ:", Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163)), FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 4, 0) });
            _txtListenPort = new TextBox
            {
                Text = _config != null ? _config.ListenPort.ToString() : "8000",
                Width = 48,
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 25)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 70)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas")
            };
            _txtListenPort.KeyDown += (s, ev) => { if (ev.Key == Key.Enter) ApplyTopBarConfigChange(); };
            _txtListenPort.LostFocus += (s, ev) => ApplyTopBarConfigChange();
            spControls.Children.Add(_txtListenPort);



            _btnToggleSniffer = new Button
            {
                Content = "[ ЗАПУСТИТЬ ПЕРЕХВАТ ]",
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            DdcapUiStyles.ApplyDarkButtonStyle(_btnToggleSniffer, Color.FromRgb(20, 35, 25), Color.FromRgb(0, 255, 150), Color.FromRgb(0, 255, 150));
            _btnToggleSniffer.Click += OnToggleSnifferClick;
            spControls.Children.Add(_btnToggleSniffer);

            _btnSettings = new Button
            {
                Content = "[ НАСТРОЙКИ ]",
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };
            DdcapUiStyles.ApplyDarkButtonStyle(_btnSettings, Color.FromRgb(25, 25, 30), Color.FromRgb(220, 220, 220), Color.FromRgb(70, 70, 90));
            _btnSettings.Click += OnSettingsClick;
            spControls.Children.Add(_btnSettings);

            _tbSnifferStatus = new TextBlock
            {
                Text = "[ ОСТАНОВЛЕН ]",
                Foreground = new SolidColorBrush(Color.FromRgb(115, 115, 115)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            spControls.Children.Add(_tbSnifferStatus);

            Grid.SetColumn(spControls, 1);
            grid.Children.Add(spControls);

            // Window Controls (Minimize, Maximize, Close)
            StackPanel spWinControls = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(15, -6, -12, 0) };
            
            Button btnMin = new Button { Content = "_", Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, FontSize = 14, Width = 30, Height = 25, Cursor = Cursors.Hand };
            Button btnMax = new Button { Content = "□", Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, FontSize = 14, Width = 30, Height = 25, Cursor = Cursors.Hand };
            Button btnClose = new Button { Content = "X", Background = Brushes.Transparent, Foreground = Brushes.White, BorderThickness = new Thickness(0), FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold, FontSize = 14, Width = 30, Height = 25, Cursor = Cursors.Hand };

            btnMin.Click += (s, e) => this.WindowState = WindowState.Minimized;
            btnMax.Click += (s, e) => this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            btnClose.Click += (s, e) => this.Close();
            
            btnClose.MouseEnter += (s, e) => { btnClose.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)); };
            btnClose.MouseLeave += (s, e) => { btnClose.Background = Brushes.Transparent; };
            btnMin.MouseEnter += (s, e) => { btnMin.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)); };
            btnMin.MouseLeave += (s, e) => { btnMin.Background = Brushes.Transparent; };
            btnMax.MouseEnter += (s, e) => { btnMax.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)); };
            btnMax.MouseLeave += (s, e) => { btnMax.Background = Brushes.Transparent; };
            
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btnMin, true);
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btnMax, true);
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btnClose, true);

            spWinControls.Children.Add(btnMin);
            spWinControls.Children.Add(btnMax);
            spWinControls.Children.Add(btnClose);
            
            Grid.SetColumn(spWinControls, 2);
            grid.Children.Add(spWinControls);

            b.Child = grid;
            return b;
        }

        // =====================================================================
        // METRICS STRIP
        // =====================================================================
        private Border CreateMetricStrip()
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(8, 8, 8)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 5, 12, 5)
            };

            UniformGrid ug = new UniformGrid { Rows = 1, Columns = 5 };

            _mbTotalPackets = CreateMetricItem(ug, "ВСЕГО ПАКЕТОВ", "TotalPacketsCount", Color.FromRgb(245, 245, 245));
            _mbTotalChecker = CreateMetricItem(ug, "ЧЕКЕР (SLA)", "TotalCheckerCount", Color.FromRgb(212, 212, 212));
            _mbTotalAttacks = CreateMetricItem(ug, "АТАКИ СОПЕРНИКОВ", "TotalAttacksCount", Color.FromRgb(239, 68, 68));
            _mbTotalFlags = CreateMetricItem(ug, "ПЕРЕХВАТ ФЛАГОВ", "TotalFlagsCount", Color.FromRgb(239, 68, 68));
            _mbSubmittedFlags = CreateMetricItem(ug, "СДАНО В ЖЮРИ", "TotalFlagsCount", Color.FromRgb(255, 255, 255)); // Placeholder for submitted flags

            b.Child = ug;
            return b;
        }

        private TextBlock CreateMetricItem(Panel parent, string title, string bindingPath, Color color)
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(13, 13, 13)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
                Margin = new Thickness(3, 0, 3, 0),
                Padding = new Thickness(10, 4, 10, 4)
            };

            StackPanel sp = new StackPanel();
            sp.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromRgb(115, 115, 115))
            });

            TextBlock tbVal = new TextBlock
            {
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(color),
                FontFamily = new FontFamily("Consolas")
            };
            tbVal.SetBinding(TextBlock.TextProperty, new Binding(bindingPath) { Source = ViewModel });

            sp.Children.Add(tbVal);
            card.Child = sp;
            parent.Children.Add(card);

            return tbVal;
        }

        // =====================================================================
        // FILTER TOOLBAR
        // =====================================================================
        private Border CreateFilterBar()
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 5, 12, 5)
            };

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Filters
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Search

            // Left: Filter Buttons & Service Filter
            StackPanel spFilters = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            spFilters.Children.Add(CreateFilterButton("ВСЕ", "ALL", Color.FromRgb(20, 20, 20), Color.FromRgb(64, 64, 64), Color.FromRgb(220, 220, 220)));
            spFilters.Children.Add(CreateFilterButton("ЧЕКЕР", "CHECKER", Color.FromRgb(20, 20, 20), Color.FromRgb(64, 64, 64), Color.FromRgb(245, 245, 245)));
            spFilters.Children.Add(CreateFilterButton("АТАКА", "ATTACKS", Color.FromRgb(28, 11, 11), Color.FromRgb(127, 29, 29), Color.FromRgb(239, 68, 68)));
            spFilters.Children.Add(CreateFilterButton("УТЕЧКА", "FLAGS", Color.FromRgb(28, 11, 11), Color.FromRgb(220, 38, 38), Color.FromRgb(239, 68, 68)));
            spFilters.Children.Add(CreateFilterButton("ЦЕПОЧКА", "MULTISTAGE", Color.FromRgb(28, 11, 11), Color.FromRgb(127, 29, 29), Color.FromRgb(239, 68, 68)));

            // Service Filter Dropdown
            spFilters.Children.Add(new TextBlock
            {
                Text = ":: СЕРВИС:",
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 4, 0),
                FontSize = 11
            });
            _cbFilterService = new ComboBox
            {
                Width = 140,
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 10, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11
            };
            DdcapUiStyles.ApplyDarkComboBoxStyle(_cbFilterService, Color.FromRgb(20, 20, 20), Color.FromRgb(240, 240, 240));
            UpdateServiceFilterDropdown();
            _cbFilterService.SelectionChanged += (s, e) => 
            {
                if (_cbFilterService.SelectedItem != null)
                {
                    ViewModel.FilterService = _cbFilterService.SelectedItem as string;
                }
                ApplyPacketFilter();
            };
            spFilters.Children.Add(_cbFilterService);

            Grid.SetColumn(spFilters, 0);
            grid.Children.Add(spFilters);

            // Right: Live Search & Auto-Scroll
            StackPanel spSearch = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            spSearch.Children.Add(new TextBlock
            {
                Text = ">> ПОИСК:",
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0),
                FontSize = 11
            });
            _txtSearch = new TextBox
            {
                Width = 200,
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 15)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2, 4, 2),
                Margin = new Thickness(0, 0, 10, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11
            };
            
            // Note: _searchDebounceTimer initialized elsewhere (or we can move it here if safe)
            if (_searchDebounceTimer == null)
            {
                _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer();
                _searchDebounceTimer.Interval = TimeSpan.FromMilliseconds(250);
                _searchDebounceTimer.Tick += (ts, te) =>
                {
                    _searchDebounceTimer.Stop();
                    ApplyPacketFilter();
                };
            }
            
            _txtSearch.TextChanged += (s, e) =>
            {
                ViewModel.SearchQuery = _txtSearch.Text;
                _searchDebounceTimer.Stop();
                _searchDebounceTimer.Start();
            };
            spSearch.Children.Add(_txtSearch);

            _chkAutoScroll = new CheckBox
            {
                Content = "[x] Автоскролл",
                IsChecked = true,
                Foreground = new SolidColorBrush(Color.FromRgb(212, 212, 212)),
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11
            };
            spSearch.Children.Add(_chkAutoScroll);

            Grid.SetColumn(spSearch, 1);
            grid.Children.Add(spSearch);

            b.Child = grid;
            return b;
        }

        private Button CreateFilterButton(string text, string filterKey, Color bgColor, Color borderColor, Color fgColor)
        {
            Button btn = new Button
            {
                Content = text,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 4, 0),
                Cursor = Cursors.Hand
            };
            DdcapUiStyles.ApplyDarkButtonStyle(btn, bgColor, fgColor, borderColor);
            btn.Click += (s, e) =>
            {
                ViewModel.ActiveFilter = filterKey;
                ApplyPacketFilter();
            };
            return btn;
        }

        private void UpdateServiceFilterDropdown()
        {
            if (_cbFilterService == null) return;
            string current = _cbFilterService.SelectedItem as string;
            _cbFilterService.Items.Clear();
            _cbFilterService.Items.Add("Все сервисы");
            if (_config != null && _config.Services != null)
            {
                foreach (var svc in _config.Services)
                {
                    if (svc.Enabled)
                    {
                        int p = svc.ListenPort > 0 ? svc.ListenPort : _config.ListenPort;
                        _cbFilterService.Items.Add(string.Format("{0} (:{1})", svc.Name, p));
                    }
                }
            }
            int idx = 0;
            if (!string.IsNullOrEmpty(current))
            {
                for (int i = 0; i < _cbFilterService.Items.Count; i++)
                {
                    string itemStr = _cbFilterService.Items[i].ToString();
                    if (itemStr == current || itemStr.StartsWith(current, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
            }
            _cbFilterService.SelectedIndex = idx;
        }
    }
}
