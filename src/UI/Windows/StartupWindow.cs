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
    public class StartupWindow : Window
    {
        public AppConfig Config { get; private set; }
        private bool _isReconfig;
        private TextBox _txtListenPortInput;
        private TextBox _txtPorts;
        private TextBox _txtTeamSubnet;
        private TextBox _txtMyTeam;
        private TextBox _txtTotalTeams;
        private TextBox _txtFlagRps;
        private TextBox _txtJuryIps;
        private ComboBox _cbAiAgentRole;
        private PasswordBox _txtApiKey;
        private ComboBox _cbModel;
        private Button _btnTestConnection;
        private Button _btnStartEngine;
        private ListView _lvServices;
        private ObservableCollection<ServiceDefinition> _servicesCollection;

        // Per-agent profile inputs
        private PasswordBox _txtBlueApiKey;
        private ComboBox _cbBlueModel;
        private TextBox _txtBlueBaseUrl;
        private PasswordBox _txtRedApiKey;
        private ComboBox _cbRedModel;
        private TextBox _txtRedBaseUrl;
        private PasswordBox _txtSocApiKey;
        private ComboBox _cbSocModel;
        private TextBox _txtSocBaseUrl;

        public StartupWindow(AppConfig config, bool isReconfig = false)
        {
            Config = config ?? AppConfig.Load();
            _isReconfig = isReconfig;
            
            this.Title = "DDCAP - Initialization & Settings Control Panel";
            this.Width = 1000;
            this.Height = 700;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Background = new SolidColorBrush(Color.FromRgb(15, 15, 15));
            this.FontFamily = new FontFamily("Consolas");
            this.Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            this.BorderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 40));
            this.BorderThickness = new Thickness(1);
            
            // Custom Window Chrome setup for Borderless UI
            this.WindowStyle = WindowStyle.None;
            System.Windows.Shell.WindowChrome.SetWindowChrome(this, new System.Windows.Shell.WindowChrome
            {
                CaptionHeight = 0,
                ResizeBorderThickness = new Thickness(5),
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(1)
            });

            var res = new ResourceDictionary();
            
            var txtStyle = new Style(typeof(TextBox));
            txtStyle.Setters.Add(new Setter(TextBox.BackgroundProperty, new SolidColorBrush(Color.FromRgb(20, 20, 25))));
            txtStyle.Setters.Add(new Setter(TextBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220, 220, 220))));
            txtStyle.Setters.Add(new Setter(TextBox.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(50, 50, 70))));
            txtStyle.Setters.Add(new Setter(TextBox.BorderThicknessProperty, new Thickness(1)));
            txtStyle.Setters.Add(new Setter(TextBox.PaddingProperty, new Thickness(8)));
            txtStyle.Setters.Add(new Setter(TextBox.FontSizeProperty, 14.0));
            res.Add(typeof(TextBox), txtStyle);

            var pwdStyle = new Style(typeof(PasswordBox));
            pwdStyle.Setters.Add(new Setter(PasswordBox.BackgroundProperty, new SolidColorBrush(Color.FromRgb(20, 20, 25))));
            pwdStyle.Setters.Add(new Setter(PasswordBox.ForegroundProperty, new SolidColorBrush(Color.FromRgb(220, 220, 220))));
            pwdStyle.Setters.Add(new Setter(PasswordBox.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(50, 50, 70))));
            pwdStyle.Setters.Add(new Setter(PasswordBox.BorderThicknessProperty, new Thickness(1)));
            pwdStyle.Setters.Add(new Setter(PasswordBox.PaddingProperty, new Thickness(8)));
            pwdStyle.Setters.Add(new Setter(PasswordBox.FontSizeProperty, 14.0));
            res.Add(typeof(PasswordBox), pwdStyle);

            var btnStyle = new Style(typeof(Button));
            btnStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush(Color.FromRgb(25, 25, 30))));
            btnStyle.Setters.Add(new Setter(Button.ForegroundProperty, Brushes.White));
            btnStyle.Setters.Add(new Setter(Button.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(70, 70, 90))));
            btnStyle.Setters.Add(new Setter(Button.BorderThicknessProperty, new Thickness(1)));
            btnStyle.Setters.Add(new Setter(Button.PaddingProperty, new Thickness(15, 8, 15, 8)));
            btnStyle.Setters.Add(new Setter(Button.CursorProperty, Cursors.Hand));
            res.Add(typeof(Button), btnStyle);

            res.Add(typeof(CheckBox), DdcapUiStyles.CreateDarkCheckBoxStyle());

            this.Resources = res;
            DdcapUiStyles.ApplyGlobalDarkScrollBarStyle(this);

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) }); // Custom Title Bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            this.Content = rootGrid;

            // Custom Title Bar
            var titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(20, 20, 20)) };
            titleBar.MouseLeftButtonDown += (s, e) => { if (e.ChangedButton == MouseButton.Left) this.DragMove(); };
            var titleText = new TextBlock { Text = "DDCAP // SETTINGS", Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10,0,0,0), FontFamily = new FontFamily("Consolas"), FontWeight = FontWeights.Bold };
            titleBar.Children.Add(titleText);
            
            Button btnClose = new Button { 
                Content = "X", 
                Background = Brushes.Transparent, 
                Foreground = Brushes.White, 
                BorderThickness = new Thickness(0), 
                FontFamily = new FontFamily("Consolas"), 
                FontWeight = FontWeights.Bold, 
                FontSize = 14, 
                Width = 40, 
                Height = 30, 
                HorizontalAlignment = HorizontalAlignment.Right, 
                Cursor = Cursors.Hand,
                Padding = new Thickness(0),
                Style = null
            };
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => { btnClose.Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)); };
            btnClose.MouseLeave += (s, e) => { btnClose.Background = Brushes.Transparent; };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(btnClose, true);
            titleBar.Children.Add(btnClose);

            rootGrid.Children.Add(titleBar);
            Grid.SetRow(titleBar, 0);

            var mainGrid = new Grid { Margin = new Thickness(20) };
            rootGrid.Children.Add(mainGrid);
            Grid.SetRow(mainGrid, 1);
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            if (!_isReconfig)
            {
                var headerBlock = new TextBlock 
                { 
                    Text = "DDCAP_NEURAL_INTERCEPTOR // INITIALIZATION_MATRIX", 
                    FontSize = 24, 
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                    Margin = new Thickness(0, 0, 0, 20)
                };
                mainGrid.Children.Add(headerBlock);
                Grid.SetRow(headerBlock, 0);
            }

            var contentGrid = new Grid();
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.Children.Add(contentGrid);
            Grid.SetRow(contentGrid, 1);

            // LEFT PANEL
            var leftPanel = new StackPanel();
            contentGrid.Children.Add(leftPanel);
            Grid.SetColumn(leftPanel, 0);

            leftPanel.Children.Add(CreateSectionHeader("1. NETWORK & PORT MATRIX"));
            
            int initialListenPort = Config.ListenPort > 0 ? Config.ListenPort : 8000;
            _txtListenPortInput = new TextBox { Text = initialListenPort.ToString(), Margin = new Thickness(0,0,0,10) };
            leftPanel.Children.Add(CreateLabel("PRIMARY PROXY LISTEN PORT (Основной порт перехвата)"));
            leftPanel.Children.Add(_txtListenPortInput);

            string initialPortsStr = Config.Services != null && Config.Services.Count > 0
                ? string.Join(", ", Config.Services.Select(s => s.ListenPort).Distinct())
                : initialListenPort.ToString();
            _txtPorts = new TextBox { Text = initialPortsStr, Margin = new Thickness(0,0,0,10) };
            leftPanel.Children.Add(CreateLabel("ALL ACTIVE PROXY PORTS (Все порты через запятую)"));
            leftPanel.Children.Add(_txtPorts);



            leftPanel.Children.Add(CreateSectionHeader("2. CTF GAME SCOPE"));
            
            _txtTeamSubnet = new TextBox { Text = Config.TeamSubnetPattern, Margin = new Thickness(0,0,0,10) };
            leftPanel.Children.Add(CreateLabel("TEAM IP TEMPLATE (e.g. 10.60.{team}.2)"));
            leftPanel.Children.Add(_txtTeamSubnet);

            var scopeGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0,0,0,10) };
            var p1 = new StackPanel { Margin = new Thickness(0,0,10,0) };
            _txtMyTeam = new TextBox { Text = Config.MyTeamNumber.ToString() };
            p1.Children.Add(CreateLabel("OUR TEAM #"));
            p1.Children.Add(_txtMyTeam);
            
            var p2 = new StackPanel { Margin = new Thickness(10,0,0,0) };
            _txtTotalTeams = new TextBox { Text = Config.TeamsCount.ToString() };
            p2.Children.Add(CreateLabel("TOTAL TEAMS"));
            p2.Children.Add(_txtTotalTeams);
            
            scopeGrid.Children.Add(p1);
            scopeGrid.Children.Add(p2);
            leftPanel.Children.Add(scopeGrid);

            _txtJuryIps = new TextBox { Text = Config.JuryIps, Margin = new Thickness(0,0,0,10) };
            leftPanel.Children.Add(CreateLabel("JURY CHECKER IPs (Comma separated)"));
            leftPanel.Children.Add(_txtJuryIps);

            _txtFlagRps = new TextBox { Text = Config.FlagSubmitRps.ToString(), Margin = new Thickness(0,0,0,20) };
            leftPanel.Children.Add(CreateLabel("FLAG SUBMISSION RPS LIMIT"));
            leftPanel.Children.Add(_txtFlagRps);

            // RIGHT PANEL
            var rightPanel = new StackPanel();
            contentGrid.Children.Add(rightPanel);
            Grid.SetColumn(rightPanel, 2);

            rightPanel.Children.Add(CreateSectionHeader("3. PER-AGENT API CONFIGURATION"));

            // Default active role selector (backward compat)
            _cbAiAgentRole = new ComboBox 
            { 
                Margin = new Thickness(0,0,0,8),
                Padding = new Thickness(8),
                FontSize = 13,
                IsEditable = false
            };
            _cbAiAgentRole.Items.Add("Blue Team DevSecOps (Defense)");
            _cbAiAgentRole.Items.Add("SOC Analyst (Monitoring)");
            _cbAiAgentRole.Items.Add("Red Team Pentester (Exploits)");
            string currentRole = Config.GetEffectiveAiAgentRole();
            _cbAiAgentRole.Text = AppConfig.GetAiAgentRoleDisplayName(currentRole);
            DdcapUiStyles.ApplyDarkComboBoxStyle(_cbAiAgentRole, Color.FromRgb(30, 30, 35), Color.FromRgb(0, 255, 150));
            rightPanel.Children.Add(CreateLabel("DEFAULT AGENT ROLE (for auto-analysis)"));
            rightPanel.Children.Add(_cbAiAgentRole);

            // Per-agent configuration tabs
            Config.EnsureAgentProfiles();
            AgentProfile blueProfile = Config.GetAgentProfile("blue_team");
            AgentProfile redProfile = Config.GetAgentProfile("red_team");
            AgentProfile socProfile = Config.GetAgentProfile("soc_analyst");

            TabControl agentTabs = new TabControl
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 20)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                Height = 210,
                Margin = new Thickness(0, 0, 0, 8),
                ItemContainerStyle = DdcapUiStyles.CreateDarkTabItemStyle()
            };

            // Blue Team Tab
            TabItem blueTab = new TabItem { Header = "DEFENSE" };
            blueTab.Content = CreateAgentProfilePanel(
                blueProfile, Color.FromRgb(34, 197, 94),
                out _txtBlueApiKey, out _cbBlueModel, out _txtBlueBaseUrl);
            agentTabs.Items.Add(blueTab);

            // Red Team Tab  
            TabItem redTab = new TabItem { Header = "ATTACK" };
            redTab.Content = CreateAgentProfilePanel(
                redProfile, Color.FromRgb(220, 38, 38),
                out _txtRedApiKey, out _cbRedModel, out _txtRedBaseUrl);
            agentTabs.Items.Add(redTab);

            // SOC Analyst Tab
            TabItem socTab = new TabItem { Header = "MONITOR" };
            socTab.Content = CreateAgentProfilePanel(
                socProfile, Color.FromRgb(251, 191, 36),
                out _txtSocApiKey, out _cbSocModel, out _txtSocBaseUrl);
            agentTabs.Items.Add(socTab);

            rightPanel.Children.Add(agentTabs);

            // Global fallback key
            _txtApiKey = new PasswordBox { Password = !string.IsNullOrEmpty(Config.AiApiKey) ? Config.AiApiKey : Config.GeminiApiKey, Margin = new Thickness(0,0,0,4) };
            rightPanel.Children.Add(CreateLabel("GLOBAL FALLBACK API KEY (used when agent key is empty)"));
            rightPanel.Children.Add(_txtApiKey);

            _cbModel = new ComboBox 
            { 
                Margin = new Thickness(0,0,0,4),
                Padding = new Thickness(6),
                FontSize = 13,
                IsEditable = true,
                Visibility = Visibility.Collapsed
            };
            _cbModel.Items.Add("x-ai/grok-4.7");
            _cbModel.Items.Add("openai/gpt-6-luna-pro");
            _cbModel.Items.Add("deepseek/deepseek-v3.2-exp");
            _cbModel.Text = Config.GetEffectiveModel();

            var apiBtns = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,8) };
            
            _btnTestConnection = new Button { Content = "TEST CONNECTION", Margin = new Thickness(0,0,8,0), Padding = new Thickness(10,5,10,5) };
            _btnTestConnection.Click += BtnTestConnection_Click;
            
            Button btnLoadModels = new Button { Content = "LOAD MODELS (POLZA AI)", Padding = new Thickness(10,5,10,5) };
            btnLoadModels.Click += BtnLoadModels_Click;
            
            apiBtns.Children.Add(_btnTestConnection);
            apiBtns.Children.Add(btnLoadModels);
            rightPanel.Children.Add(apiBtns);

            rightPanel.Children.Add(CreateSectionHeader("4. DYNAMIC VULNBOX SERVICES GRID"));
            
            _servicesCollection = new ObservableCollection<ServiceDefinition>(Config.Services);
            _lvServices = new ListView
            {
                ItemsSource = _servicesCollection,
                Height = 150,
                Margin = new Thickness(0,0,0,10)
            };
            DdcapUiStyles.ApplyDarkListViewStyle(_lvServices);
            
            var gridView = new GridView();
            gridView.Columns.Add(new GridViewColumn { Header = "Service Name", DisplayMemberBinding = new Binding("Name"), Width = 110 });
            gridView.Columns.Add(new GridViewColumn { Header = "Port", DisplayMemberBinding = new Binding("ListenPort"), Width = 60 });
            gridView.Columns.Add(new GridViewColumn { Header = "Target URL", DisplayMemberBinding = new Binding("TargetUrl"), Width = 170 });
            gridView.Columns.Add(new GridViewColumn { Header = "Paths", DisplayMemberBinding = new Binding("PathPrefixes"), Width = 110 });
            _lvServices.View = gridView;

            // Context Menu (ПКМ)
            var svcMenu = DdcapUiStyles.CreateDarkContextMenu(Color.FromRgb(0, 255, 150));
            svcMenu.Items.Add(DdcapUiStyles.CreateDarkMenuItem("[+] Редактировать сервис...", new SolidColorBrush(Color.FromRgb(0, 255, 150)), (s, e) => EditSelectedService()));
            svcMenu.Items.Add(DdcapUiStyles.CreateDarkMenuItem("[+] Добавить новый сервис...", Brushes.White, (s, e) => AddNewService()));
            svcMenu.Items.Add(DdcapUiStyles.CreateDarkMenuItem("[-] Удалить выбранный сервис", new SolidColorBrush(Color.FromRgb(239, 68, 68)), (s, e) => DeleteSelectedService()));
            _lvServices.ContextMenu = svcMenu;
            _lvServices.MouseDoubleClick += (s, e) => EditSelectedService();

            rightPanel.Children.Add(_lvServices);

            var serviceBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btnAdd = new Button { Content = "ADD SERVICE", Margin = new Thickness(0,0,8,0), Padding = new Thickness(10,5,10,5) };
            btnAdd.Click += (s, e) => AddNewService();
            var btnEdit = new Button { Content = "EDIT SELECTED (ПКМ)", Margin = new Thickness(0,0,8,0), Padding = new Thickness(10,5,10,5) };
            btnEdit.Click += (s, e) => EditSelectedService();
            var btnDel = new Button { Content = "DELETE SELECTED", Padding = new Thickness(10,5,10,5) };
            btnDel.Click += (s, e) => DeleteSelectedService();
            serviceBtns.Children.Add(btnAdd);
            serviceBtns.Children.Add(btnEdit);
            serviceBtns.Children.Add(btnDel);
            rightPanel.Children.Add(serviceBtns);

            // FOOTER
            var footerPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            
            _btnStartEngine = new Button 
            { 
                Content = _isReconfig ? "SAVE & RESTART ENGINE" : "INITIALIZE & START ENGINE", 
                FontSize = 16, 
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(20, 8, 20, 8),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(1)
            };
            DdcapUiStyles.ApplyDarkButtonStyle(_btnStartEngine, Color.FromRgb(35, 20, 20), Color.FromRgb(220, 38, 38), Color.FromRgb(220, 38, 38));
            _btnStartEngine.Click += BtnStartEngine_Click;
            
            footerPanel.Children.Add(_btnStartEngine);
            mainGrid.Children.Add(footerPanel);
            Grid.SetRow(footerPanel, 2);

        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock 
            { 
                Text = text, 
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)), 
                FontSize = 12, 
                Margin = new Thickness(0, 0, 0, 2) 
            };
        }

        private TextBlock CreateSectionHeader(string text)
        {
            return new TextBlock 
            { 
                Text = text, 
                Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)), 
                FontSize = 16, 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 0, 0, 15) 
            };
        }

        private StackPanel CreateAgentProfilePanel(AgentProfile profile, Color accentColor,
            out PasswordBox apiKeyBox, out ComboBox modelBox, out TextBox baseUrlBox)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(8, 6, 8, 4) };

            sp.Children.Add(CreateLabel("API KEY (empty = use global fallback)"));
            apiKeyBox = new PasswordBox
            {
                Password = profile != null ? (profile.ApiKey ?? "") : "",
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 25)),
                Foreground = new SolidColorBrush(accentColor),
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 70)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 6)
            };
            sp.Children.Add(apiKeyBox);

            sp.Children.Add(CreateLabel("MODEL (e.g. google/gemini-2.5-flash)"));
            modelBox = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(6),
                FontSize = 12,
                IsEditable = true
            };
            modelBox.Items.Add("x-ai/grok-4.7");
            modelBox.Items.Add("openai/gpt-6-luna-pro");
            modelBox.Items.Add("deepseek/deepseek-v3.2-exp");
            string defM = "x-ai/grok-4.7";
            if (profile != null && profile.Role == "red_team") defM = "openai/gpt-6-luna-pro";
            if (profile != null && profile.Role == "soc_analyst") defM = "deepseek/deepseek-v3.2-exp";
            modelBox.Text = profile != null && !string.IsNullOrWhiteSpace(profile.Model) ? profile.Model : defM;
            DdcapUiStyles.ApplyDarkComboBoxStyle(modelBox, Color.FromRgb(20, 20, 25), accentColor);
            sp.Children.Add(modelBox);

            sp.Children.Add(CreateLabel("BASE URL (empty = use global / Polza AI)"));
            baseUrlBox = new TextBox
            {
                Text = profile != null ? (profile.BaseUrl ?? "") : "",
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 25)),
                Foreground = new SolidColorBrush(accentColor),
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 70)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(6),
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 4)
            };
            sp.Children.Add(baseUrlBox);

            return sp;
        }

        private async void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            _btnTestConnection.IsEnabled = false;
            _btnTestConnection.Content = "TESTING...";
            
            string apiKey = _txtApiKey.Password.Trim();
            if (string.IsNullOrWhiteSpace(apiKey)) apiKey = Config.GetEffectiveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                MessageBox.Show("API Key не задан. Укажите ключ в поле ввода или в .env (AI_API_KEY).", "API Key Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                _btnTestConnection.IsEnabled = true;
                _btnTestConnection.Content = "TEST GLOBAL API CONNECTION";
                return;
            }

            string baseUrl = Config.GetEffectiveAiBaseUrl();

            string modelStr = !string.IsNullOrWhiteSpace(_cbModel.Text) 
                ? _cbModel.Text.Trim() 
                : (_cbModel.SelectedItem != null ? _cbModel.SelectedItem.ToString() : "google/gemini-2.5-flash");

            try
            {
                string result = await GoogleGeminiClient.TestConnectionAsync(apiKey, modelStr, baseUrl);
                MessageBox.Show(result, "API Success", MessageBoxButton.OK, MessageBoxImage.Information);
                _btnTestConnection.Background = new SolidColorBrush(Color.FromRgb(0, 150, 50));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к агрегатору: " + ex.Message, "Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                _btnTestConnection.Background = new SolidColorBrush(Color.FromRgb(200, 0, 0));
            }
            finally
            {
                _btnTestConnection.IsEnabled = true;
                _btnTestConnection.Content = "TEST GLOBAL API CONNECTION";
            }
        }

        private async void BtnLoadModels_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            string apiKey = _txtApiKey.Password.Trim();
            if (string.IsNullOrWhiteSpace(apiKey)) apiKey = Config.GetEffectiveApiKey();

            string baseUrl = Config.GetEffectiveAiBaseUrl();

            try
            {
                var models = await AiClient.FetchModelsAsync(apiKey, baseUrl);
                if (models.Count > 0)
                {
                    // UI comboboxes are intentionally kept clean with the 3 required models.
                    // The user can type any custom model name manually since IsEditable = true.
                    MessageBox.Show(string.Format("Загружено {0} моделей из {1}", models.Count, baseUrl), "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Не удалось получить список моделей. Проверьте API ключ и Base URL.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке моделей: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void BtnStartEngine_Click(object sender, RoutedEventArgs e)
        {
            // 1. (Removed target url)


            // 2. Ports update
            int primaryPort;
            if (int.TryParse(_txtListenPortInput.Text.Trim(), out primaryPort) && primaryPort > 0 && primaryPort <= 65535)
            {
                Config.ListenPort = primaryPort;
            }

            // Sync services collection
            Config.Services = _servicesCollection.ToList();

            string[] portsStr = _txtPorts.Text.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (portsStr.Length > 0)
            {
                int p0;
                if (int.TryParse(portsStr[0], out p0) && p0 > 0 && p0 <= 65535)
                {
                    Config.ListenPort = p0;
                }
            }

            for (int i = 0; i < Config.Services.Count; i++)
            {
                int p;
                if (i < portsStr.Length && int.TryParse(portsStr[i], out p) && p > 0 && p <= 65535)
                {
                    Config.Services[i].ListenPort = p;
                }
                else if (i == 0 && Config.ListenPort > 0)
                {
                    Config.Services[0].ListenPort = Config.ListenPort;
                }
            }

            if (Config.Services.Count > 0 && Config.ListenPort > 0)
            {
                Config.Services[0].ListenPort = Config.ListenPort;
            }

            // 3. CTF game scope
            Config.TeamSubnetPattern = _txtTeamSubnet.Text.Trim();
            Config.JuryIps = _txtJuryIps.Text.Trim();
            
            int myTeam;
            if (int.TryParse(_txtMyTeam.Text, out myTeam)) Config.MyTeamNumber = myTeam;
            int totalTeams;
            if (int.TryParse(_txtTotalTeams.Text, out totalTeams)) Config.TeamsCount = totalTeams;
            int rps;
            if (int.TryParse(_txtFlagRps.Text, out rps)) Config.FlagSubmitRps = rps;

            // 4. AI Agent — default role
            string selectedRoleDisplay = _cbAiAgentRole.SelectedItem != null ? _cbAiAgentRole.SelectedItem.ToString() : "Blue Team DevSecOps (Defense)";
            if (selectedRoleDisplay.StartsWith("SOC")) Config.AiAgentRole = "soc_analyst";
            else if (selectedRoleDisplay.StartsWith("Red")) Config.AiAgentRole = "red_team";
            else Config.AiAgentRole = "blue_team";
            Config.AiBaseUrl = Config.GetEffectiveAiBaseUrl();
            Config.AiApiKey = _txtApiKey.Password.Trim();
            Config.GeminiApiKey = Config.AiApiKey;
            Config.AiModel = !string.IsNullOrWhiteSpace(_cbModel.Text) 
                ? _cbModel.Text.Trim() 
                : (_cbModel.SelectedItem != null ? _cbModel.SelectedItem.ToString() : "google/gemini-2.5-flash");
            Config.GeminiModel = Config.AiModel;

            // 5. Per-agent profiles
            SaveAgentProfile("blue_team", _txtBlueApiKey, _cbBlueModel, _txtBlueBaseUrl);
            SaveAgentProfile("red_team", _txtRedApiKey, _cbRedModel, _txtRedBaseUrl);
            SaveAgentProfile("soc_analyst", _txtSocApiKey, _cbSocModel, _txtSocBaseUrl);

            Config.Save();

            if (_isReconfig)
            {
                this.DialogResult = true;
            }
            else
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            
            this.Close();
        }

        private void SaveAgentProfile(string role, PasswordBox apiKeyBox, ComboBox modelBox, TextBox baseUrlBox)
        {
            string key = apiKeyBox != null ? apiKeyBox.Password.Trim() : "";
            string model = "";
            if (modelBox != null)
            {
                model = !string.IsNullOrWhiteSpace(modelBox.Text) 
                    ? modelBox.Text.Trim() 
                    : (modelBox.SelectedItem != null ? modelBox.SelectedItem.ToString() : "");
            }
            string baseUrl = baseUrlBox != null ? baseUrlBox.Text.Trim() : "";

            Config.SetAgentProfile(new AgentProfile(role, key, model, baseUrl));
        }



        private void EditSelectedService()
        {
            var sel = _lvServices.SelectedItem as ServiceDefinition;
            if (sel == null)
            {
                MessageBox.Show("Пожалуйста, выберите сервис из таблицы или нажмите ПКМ по сервису.", "Выбор сервиса", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ShowServiceEditDialog(sel, false);
        }

        private void AddNewService()
        {
            string defaultTarget = "http://127.0.0.1:5000";

            int nextPort = 8000;
            if (_servicesCollection.Count > 0)
            {
                nextPort = _servicesCollection.Max(s => s.ListenPort) + 1;
                if (nextPort > 65535) nextPort = 8000;
            }

            var newSvc = new ServiceDefinition
            {
                Name = "Service_" + (_servicesCollection.Count + 1),
                ListenPort = nextPort,
                TargetUrl = defaultTarget.TrimEnd('/'),
                PathPrefixes = "/api"
            };

            if (ShowServiceEditDialog(newSvc, true))
            {
                _servicesCollection.Add(newSvc);
                _lvServices.SelectedItem = newSvc;
                SyncPortsFromServices();
            }
        }

        private void DeleteSelectedService()
        {
            var sel = _lvServices.SelectedItem as ServiceDefinition;
            if (sel != null)
            {
                _servicesCollection.Remove(sel);
                SyncPortsFromServices();
            }
        }

        private void SyncPortsFromServices()
        {
            if (_servicesCollection != null && _servicesCollection.Count > 0)
            {
                _txtPorts.Text = string.Join(", ", _servicesCollection.Select(s => s.ListenPort).Distinct());
                if (_servicesCollection[0] != null && _servicesCollection[0].ListenPort > 0)
                {
                    _txtListenPortInput.Text = _servicesCollection[0].ListenPort.ToString();
                }
            }
        }

        private bool ShowServiceEditDialog(ServiceDefinition svc, bool isNew)
        {
            if (svc == null) return false;

            var dlg = new Window
            {
                Title = isNew ? "ДОБАВЛЕНИЕ СЕРВИСА // ADD SERVICE" : "ПАРАМЕТРЫ СЕРВИСА // EDIT SERVICE: " + svc.Name,
                Width = 560,
                Height = 440,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 20)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Fields
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            var header = new TextBlock
            {
                Text = isNew ? ">> КОНФИГУРАЦИЯ НОВОГО СЕРВИСА" : ">> РЕДАКТИРОВАНИЕ ПАРАМЕТРОВ СЕРВИСА",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 150)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            grid.Children.Add(header);
            Grid.SetRow(header, 0);

            var spFields = new StackPanel();

            // 1. Service Name
            spFields.Children.Add(CreateLabel("1. ИМЯ СЕРВИСА (Service Name):"));
            var txtName = new TextBox
            {
                Text = svc.Name ?? "",
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 35)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 150)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 60)),
                Padding = new Thickness(8),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            };
            spFields.Children.Add(txtName);

            // 2. Listen Port
            spFields.Children.Add(CreateLabel("2. ПОРТ ПЕРЕХВАТА DDCAP PROXY (Listen Port, e.g. 8000):"));
            var txtPort = new TextBox
            {
                Text = svc.ListenPort > 0 ? svc.ListenPort.ToString() : "8000",
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 35)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 150)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 60)),
                Padding = new Thickness(8),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            };
            spFields.Children.Add(txtPort);

            // 3. Target URL
            string curTarget = !string.IsNullOrWhiteSpace(svc.TargetUrl) ? svc.TargetUrl : "http://127.0.0.1:5000";
            spFields.Children.Add(CreateLabel("3. ЦЕЛЕВОЙ URL VULNBOX (Больше не используется, чисто инфо):"));
            var txtTarget = new TextBox
            {
                Text = curTarget,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 35)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 150)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 60)),
                Padding = new Thickness(8),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            };
            spFields.Children.Add(txtTarget);

            // 4. Path Prefixes
            spFields.Children.Add(CreateLabel("4. ПРЕФИКСЫ ПУТЕЙ URL (Path Prefixes, через запятую, e.g. /api, /auth, /v1):"));
            var txtPaths = new TextBox
            {
                Text = svc.PathPrefixes ?? "",
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 35)),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 150)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 60)),
                Padding = new Thickness(8),
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10)
            };
            spFields.Children.Add(txtPaths);

            grid.Children.Add(spFields);
            Grid.SetRow(spFields, 1);

            // Buttons
            var spButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 15, 0, 0) };

            var btnSave = new Button
            {
                Content = "СОХРАНИТЬ",
                Background = new SolidColorBrush(Color.FromRgb(0, 150, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0, 200, 80)),
                Padding = new Thickness(15, 6, 15, 6),
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            var btnCancel = new Button
            {
                Content = "ОТМЕНА",
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 90)),
                Padding = new Thickness(15, 6, 15, 6),
                Cursor = Cursors.Hand
            };

            bool saved = false;
            btnSave.Click += (s, e) =>
            {
                string name = txtName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Введите имя сервиса.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int port;
                if (!int.TryParse(txtPort.Text.Trim(), out port) || port <= 0 || port > 65535)
                {
                    MessageBox.Show("Введите корректный номер порта (1-65535).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string target = txtTarget.Text.Trim();
                if (string.IsNullOrEmpty(target))
                {
                    MessageBox.Show("Введите целевой URL сервиса.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (!target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    target = "http://" + target;
                }

                svc.Name = name;
                svc.ListenPort = port;
                svc.TargetUrl = target.TrimEnd('/');
                svc.PathPrefixes = txtPaths.Text.Trim();
                svc.CachePrefixes();

                saved = true;
                dlg.DialogResult = true;
                dlg.Close();
            };

            btnCancel.Click += (s, e) =>
            {
                dlg.DialogResult = false;
                dlg.Close();
            };

            spButtons.Children.Add(btnSave);
            spButtons.Children.Add(btnCancel);
            grid.Children.Add(spButtons);
            Grid.SetRow(spButtons, 2);

            dlg.Content = grid;
            dlg.ShowDialog();

            if (saved)
            {
                if (_lvServices != null && _lvServices.Items != null)
                {
                    _lvServices.Items.Refresh();
                }
                SyncPortsFromServices();
            }
            return saved;
        }
    }
}
