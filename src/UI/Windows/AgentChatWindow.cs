using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Documents;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // AGENT CHAT WINDOW — Dedicated per-agent interaction with file upload
    // =========================================================================
    public class AgentChatWindow : Window
    {
        private AgentProfile _profile;
        private AppConfig _config;

        // Chat history
        private readonly List<ChatMessage> _chatHistory = new List<ChatMessage>();
        private readonly List<FileAttachment> _attachedFiles = new List<FileAttachment>();

        // UI Elements
        private ScrollViewer _chatScroll;
        private StackPanel _chatPanel;
        private TextBox _txtUserInput;
        private Button _btnSend;
        private Button _btnAttachFile;
        private Button _btnAttachFolder;
        private Button _btnClearContext;
        private Button _btnClearChat;
        private TextBlock _tbStatus;
        private StackPanel _attachmentPanel;
        private TextBlock _tbAttachmentCount;

        // Project Explorer
        private Grid _mainGrid;
        private ColumnDefinition _colProject;
        private TreeView _tvProject;
        private TreeView _tvNetwork;
        private RichTextBox _rtbCodeEditor;
        private string _currentProjectRoot = null;
        private string _activeFilePath = null;
        private string _serviceSkeleton = null;
        private TextBox _txtSearch;

        // State
        private bool _isProcessing;

        public AgentChatWindow(AgentProfile profile, AppConfig config)
        {
            _profile = profile != null ? profile : new AgentProfile();
            _config = config != null ? config : AppConfig.Load();

            InitializeLayout();
            
            this.Resources.Add(typeof(TextBox), DdcapUiStyles.CreateDarkTextBoxStyle());
            this.Resources.Add(typeof(CheckBox), DdcapUiStyles.CreateDarkCheckBoxStyle());
            DdcapUiStyles.ApplyGlobalDarkScrollBarStyle(this);

            AllowDrop = true;
            Drop += OnFileDrop;
            DragOver += OnFileDragOver;

            AltayProxySniffer.GlobalPacketCaptured += OnGlobalPacketCaptured;
            
            if (_profile.Role == "red_team")
            {
                MainWindow.FlagSubmitResultReceived += OnFlagSubmitResult;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            AltayProxySniffer.GlobalPacketCaptured -= OnGlobalPacketCaptured;
            if (_profile.Role == "red_team")
            {
                MainWindow.FlagSubmitResultReceived -= OnFlagSubmitResult;
            }
            base.OnClosed(e);
        }

        private void OnFlagSubmitResult(string flag, string result)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AddSystemMessage(string.Format("[ JURY FEEDBACK ]\nFlag: {0}\nResult: {1}", flag, result));
            }));
        }

        private void OnGlobalPacketCaptured(TrafficPacket p)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_tvNetwork == null) return;
                
                string key = p.SrcIp;
                TreeViewItem groupNode = null;
                foreach (TreeViewItem item in _tvNetwork.Items)
                {
                    if (item.Header.ToString() == "[IP] " + key)
                    {
                        groupNode = item;
                        break;
                    }
                }
                
                if (groupNode == null)
                {
                    groupNode = new TreeViewItem { Header = "[IP] " + key, IsExpanded = false };
                    _tvNetwork.Items.Insert(0, groupNode); // Add new groups to top
                }
                
                TreeViewItem node = new TreeViewItem 
                { 
                    Header = string.Format("  [{0}] {1} {2}", p.Time, p.Method, p.Uri),
                    Tag = p 
                };
                if (p.TrafficType == "АТАКА") node.Foreground = Brushes.Red;
                else if (p.TrafficType == "УТЕЧКА") node.Foreground = Brushes.Magenta;
                else if (p.TrafficType == "АНОМАЛИЯ") node.Foreground = Brushes.Orange;
                
                groupNode.Items.Insert(0, node); // Add newest packets to top of group
            }));
        }

        private void InitializeLayout()
        {
            string roleName = _profile.GetDisplayName();
            string shortName = _profile.GetShortName();
            string modelName = _profile.GetEffectiveModel();

            Title = string.Format("[::] DDCAP // {0} AGENT CHAT // {1}", shortName, modelName);
            Width = 900;
            Height = 700;
            MinWidth = 700;
            MinHeight = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Background = new SolidColorBrush(Color.FromRgb(10, 10, 14));
            Foreground = new SolidColorBrush(Color.FromRgb(245, 245, 245));
            FontFamily = new FontFamily("Consolas");

            Color accentColor = GetRoleAccentColor(_profile.Role);

            DockPanel root = new DockPanel { LastChildFill = true };

            // === TOP HEADER BAR ===
            Border topBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(8, 8, 12)),
                BorderBrush = new SolidColorBrush(accentColor),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Padding = new Thickness(12, 8, 12, 8)
            };

            DockPanel dpTop = new DockPanel { LastChildFill = true };

            TextBlock tbTitle = new TextBlock
            {
                Text = string.Format("[{0}] {1}", shortName, roleName),
                Foreground = new SolidColorBrush(accentColor),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(tbTitle, Dock.Left);
            dpTop.Children.Add(tbTitle);

            StackPanel spTopRight = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

            TextBlock tbModel = new TextBlock
            {
                Text = string.Format("MODEL: {0}", modelName),
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };
            spTopRight.Children.Add(tbModel);

            _btnClearChat = CreateActionButton("CLEAR CHAT", Color.FromRgb(100, 100, 110), OnClearChatClick);
            spTopRight.Children.Add(_btnClearChat);

            dpTop.Children.Add(spTopRight);
            topBar.Child = dpTop;
            DockPanel.SetDock(topBar, Dock.Top);
            root.Children.Add(topBar);

            // === ATTACHMENT BAR ===
            Border attachBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 14, 20)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 48)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 5, 10, 5)
            };

            DockPanel dpAttach = new DockPanel { LastChildFill = true };

            StackPanel spAttachButtons = new StackPanel { Orientation = Orientation.Horizontal };

            _btnAttachFile = CreateActionButton("[+] FILE", Color.FromRgb(60, 130, 200), OnAttachFileClick);
            _btnAttachFile.Margin = new Thickness(0, 0, 6, 0);
            spAttachButtons.Children.Add(_btnAttachFile);

            _btnAttachFolder = CreateActionButton("[+] FOLDER / SERVICE", Color.FromRgb(60, 130, 200), OnAttachFolderClick);
            _btnAttachFolder.Margin = new Thickness(0, 0, 6, 0);
            spAttachButtons.Children.Add(_btnAttachFolder);

            _btnClearContext = CreateActionButton("[x] CLEAR FILES", Color.FromRgb(200, 80, 80), OnClearContextClick);
            spAttachButtons.Children.Add(_btnClearContext);

            DockPanel.SetDock(spAttachButtons, Dock.Left);
            dpAttach.Children.Add(spAttachButtons);

            _tbAttachmentCount = new TextBlock
            {
                Text = "No files attached. Drag & drop files/folders here.",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 120)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10, 0, 0, 0)
            };
            dpAttach.Children.Add(_tbAttachmentCount);

            attachBar.Child = dpAttach;
            DockPanel.SetDock(attachBar, Dock.Top);
            root.Children.Add(attachBar);

            // === ATTACHMENTS LIST PANEL (collapsible) ===
            _attachmentPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(12, 12, 18)),
                Margin = new Thickness(0)
            };
            Border attachListBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 48)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = _attachmentPanel,
                MaxHeight = 120
            };
            DockPanel.SetDock(attachListBorder, Dock.Top);
            root.Children.Add(attachListBorder);

            // === BOTTOM INPUT BAR ===
            Border inputBar = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(12, 12, 18)),
                BorderBrush = new SolidColorBrush(accentColor),
                BorderThickness = new Thickness(0, 2, 0, 0),
                Padding = new Thickness(10, 8, 10, 8)
            };

            Grid gInput = new Grid();
            gInput.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gInput.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _txtUserInput = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 28)),
                Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                CaretBrush = new SolidColorBrush(accentColor),
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 65)),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                Padding = new Thickness(10, 8, 10, 8),
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                MaxHeight = 120,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            _txtUserInput.KeyDown += OnInputKeyDown;
            Grid.SetColumn(_txtUserInput, 0);
            gInput.Children.Add(_txtUserInput);

            StackPanel spSendArea = new StackPanel { Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            _btnSend = new Button
            {
                Content = "SEND >>",
                Background = new SolidColorBrush(accentColor),
                Foreground = Brushes.Black,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Padding = new Thickness(18, 10, 18, 10),
                Cursor = Cursors.Hand,
                BorderThickness = new Thickness(0)
            };
            _btnSend.Click += OnSendClick;
            spSendArea.Children.Add(_btnSend);

            _tbStatus = new TextBlock
            {
                Text = "READY",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 110)),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0)
            };
            spSendArea.Children.Add(_tbStatus);

            Grid.SetColumn(spSendArea, 1);
            gInput.Children.Add(spSendArea);

            inputBar.Child = gInput;
            DockPanel.SetDock(inputBar, Dock.Bottom);
            root.Children.Add(inputBar);

            // === MAIN CHAT AREA & PROJECT EXPLORER ===
            _mainGrid = new Grid();
            _colProject = new ColumnDefinition { Width = new GridLength(300) }; // Shown by default
            _mainGrid.ColumnDefinitions.Add(_colProject);
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Pixel) }); // Splitter
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left Panel (Project Explorer)
            Grid gLeft = new Grid();
            gLeft.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Search
            gLeft.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Tree
            gLeft.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Pixel) }); // Splitter
            gLeft.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1.5, GridUnitType.Star) }); // Editor

            TabControl tcLeft = new TabControl { Background = Brushes.Transparent, BorderThickness = new Thickness(0) };
            tcLeft.Resources[SystemColors.ControlBrushKey] = new SolidColorBrush(Color.FromRgb(20, 20, 25));
            tcLeft.Resources[SystemColors.ControlTextBrushKey] = Brushes.Gray;

            _txtSearch = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 25)),
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 50)),
                Text = "Search files...",
                Padding = new Thickness(4)
            };
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Search files...") { _txtSearch.Text = ""; _txtSearch.Foreground = Brushes.White; } };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = "Search files..."; _txtSearch.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)); } };
            _txtSearch.TextChanged += (s, e) => {
                if (_txtSearch.Text != "Search files...") {
                    if (tcLeft.SelectedIndex == 0) FilterTreeView(_tvProject.Items, _txtSearch.Text.ToLower());
                    else if (_tvNetwork != null) FilterTreeView(_tvNetwork.Items, _txtSearch.Text.ToLower());
                }
            };
            Grid.SetRow(_txtSearch, 0);
            gLeft.Children.Add(_txtSearch);

            _tvProject = new TreeView
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 20)),
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            };
            
            // Override default Windows highlight colors for TreeView
            _tvProject.Resources[SystemColors.HighlightBrushKey] = new SolidColorBrush(Color.FromRgb(37, 58, 45));
            _tvProject.Resources[SystemColors.HighlightTextBrushKey] = new SolidColorBrush(Color.FromRgb(0, 255, 150));
            _tvProject.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(Color.FromRgb(26, 40, 31));
            _tvProject.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = new SolidColorBrush(Color.FromRgb(0, 220, 130));

            _tvProject.SelectedItemChanged += OnProjectTreeSelectionChanged;
            
            ContextMenu cm = DdcapUiStyles.CreateDarkContextMenu(null);
            cm.Items.Add(DdcapUiStyles.CreateDarkMenuItem("Copy Path", Brushes.White, (s, e) => {
                TreeViewItem tvi = _tvProject.SelectedItem as TreeViewItem;
                if (tvi != null && tvi.Tag != null) Clipboard.SetText(tvi.Tag.ToString());
            }, null));
            _tvProject.ContextMenu = cm;

            TabItem tiProject = new TabItem { Header = "Files", Background = Brushes.Transparent, Foreground = Brushes.LightGray, Content = _tvProject };
            tcLeft.Items.Add(tiProject);

            _tvNetwork = new TreeView
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 20)),
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            };
            _tvNetwork.Resources[SystemColors.HighlightBrushKey] = new SolidColorBrush(Color.FromRgb(37, 58, 45));
            _tvNetwork.Resources[SystemColors.HighlightTextBrushKey] = new SolidColorBrush(Color.FromRgb(0, 255, 150));
            _tvNetwork.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = new SolidColorBrush(Color.FromRgb(26, 40, 31));
            _tvNetwork.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = new SolidColorBrush(Color.FromRgb(0, 220, 130));
            
            _tvNetwork.SelectedItemChanged += OnNetworkTreeSelectionChanged;
            
            ContextMenu cmNet = DdcapUiStyles.CreateDarkContextMenu(null);
            cmNet.Items.Add(DdcapUiStyles.CreateDarkMenuItem("Refresh Packets", Brushes.White, (s, e) => RefreshNetworkPackets(), null));
            cmNet.Items.Add(DdcapUiStyles.CreateDarkMenuItem("Ask Agent to Patch", Brushes.Cyan, (s, e) => {
                TreeViewItem node = _tvNetwork.SelectedItem as TreeViewItem;
                if (node != null)
                {
                    TrafficPacket p = node.Tag as TrafficPacket;
                    if (p != null)
                    {
                        string prompt = string.Format("Analyze this malicious {0} request to {1} (from {2}) and provide a patch for the {3} vulnerability.\nPayload:\n{4}",
                            p.Method, p.Uri, p.SrcIp, p.VulnType, string.IsNullOrEmpty(p.DecodedPayload) ? p.ReqBody : p.DecodedPayload);
                        if (_txtUserInput != null)
                        {
                            _txtUserInput.Text = prompt;
                            if (_btnSend != null) _btnSend.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        }
                    }
                }
            }, null));
            _tvNetwork.ContextMenu = cmNet;

            TabItem tiNetwork = new TabItem { Header = "Network", Background = Brushes.Transparent, Foreground = Brushes.LightGray, Content = _tvNetwork };
            tcLeft.Items.Add(tiNetwork);
            tcLeft.SelectionChanged += (s, e) => { if (tcLeft.SelectedIndex == 1) RefreshNetworkPackets(); };

            Grid.SetRow(tcLeft, 1);
            gLeft.Children.Add(tcLeft);

            GridSplitter gsV = new GridSplitter 
            { 
                Height = 3, 
                HorizontalAlignment = HorizontalAlignment.Stretch, 
                VerticalAlignment = VerticalAlignment.Center,
                ResizeDirection = GridResizeDirection.Rows,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 40)) 
            };
            Grid.SetRow(gsV, 2);
            gLeft.Children.Add(gsV);

            _rtbCodeEditor = new RichTextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 25)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 40, 50)),
                BorderThickness = new Thickness(0, 2, 0, 0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                IsReadOnly = false, // Editable!
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(5)
            };
            


            _rtbCodeEditor.AllowDrop = true;
            _rtbCodeEditor.PreviewDragOver += (s, e) => { e.Effects = DragDropEffects.Copy; e.Handled = true; };
            _rtbCodeEditor.PreviewDrop += (s, e) => {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files.Length > 0 && File.Exists(files[0]))
                    {
                        _activeFilePath = files[0];
                        HighlightCodeToEditor(File.ReadAllText(files[0], Encoding.UTF8));
                    }
                }
                e.Handled = true;
            };

            _rtbCodeEditor.PreviewKeyDown += (s, e) => {
                if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) {
                    if (!string.IsNullOrEmpty(_activeFilePath)) {
                        string code = new TextRange(_rtbCodeEditor.Document.ContentStart, _rtbCodeEditor.Document.ContentEnd).Text;
                        File.WriteAllText(_activeFilePath, code, Encoding.UTF8);
                        AddSystemMessage("Saved " + Path.GetFileName(_activeFilePath));
                    }
                    e.Handled = true;
                }
            };

            Grid.SetRow(_rtbCodeEditor, 3);
            gLeft.Children.Add(_rtbCodeEditor);

            Grid.SetColumn(gLeft, 0);
            _mainGrid.Children.Add(gLeft);

            GridSplitter gsH = new GridSplitter 
            { 
                Width = 3, 
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch, 
                ResizeDirection = GridResizeDirection.Columns,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 40)) 
            };
            Grid.SetColumn(gsH, 1);
            _mainGrid.Children.Add(gsH);

            // Chat area
            _chatScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 14))
            };

            _chatPanel = new StackPanel { Margin = new Thickness(10) };
            _chatScroll.Content = _chatPanel;
            Grid.SetColumn(_chatScroll, 2);
            _mainGrid.Children.Add(_chatScroll);

            // Welcome message
            AddSystemMessage(string.Format(
                "DDCAP {0} Agent initialized.\nModel: {1}\nDrag & drop files or entire service directories to include in context.\nType your message and press Ctrl+Enter or click SEND.",
                _profile.GetDisplayName(), _profile.GetEffectiveModel()));

            root.Children.Add(_mainGrid);
            Content = root;
        }

        // =====================================================================
        // CHAT MESSAGE RENDERING
        // =====================================================================

        private void AddSystemMessage(string text)
        {
            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(40, 4, 40, 4),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 150)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            };

            bubble.Child = tb;
            _chatPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private void AddUserMessage(string text)
        {
            Color accent = GetRoleAccentColor(_profile.Role);

            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(18, 25, 38)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(40, 70, 110)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(100, 6, 10, 6),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = 650
            };

            StackPanel sp = new StackPanel();

            TextBlock tbLabel = new TextBlock
            {
                Text = "YOU",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 140, 200)),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 3)
            };
            sp.Children.Add(tbLabel);

            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 225, 235)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(tb);

            bubble.Child = sp;
            _chatPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private void AddAgentMessage(string text)
        {
            Color accent = GetRoleAccentColor(_profile.Role);

            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 14, 20)),
                BorderBrush = new SolidColorBrush(accent),
                BorderThickness = new Thickness(2, 0, 0, 0),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(10, 6, 100, 6),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = 700
            };

            StackPanel sp = new StackPanel();

            TextBlock tbLabel = new TextBlock
            {
                Text = string.Format("[{0}] AGENT", _profile.GetShortName()),
                Foreground = new SolidColorBrush(accent),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 3)
            };
            sp.Children.Add(tbLabel);

            TextBox tbContent = new TextBox
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                IsReadOnly = true,
                Padding = new Thickness(0)
            };
            sp.Children.Add(tbContent);

            // Copy button
            Button btnCopy = CreateActionButton("[COPY]", Color.FromRgb(100, 100, 120), null);
            btnCopy.Margin = new Thickness(0, 6, 0, 0);
            btnCopy.HorizontalAlignment = HorizontalAlignment.Left;
            string textCopy = text;
            btnCopy.Click += (s, e) =>
            {
                try { Clipboard.SetText(textCopy); } catch { }
            };
            sp.Children.Add(btnCopy);

            bubble.Child = sp;
            _chatPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private void AddErrorMessage(string text)
        {
            Border bubble = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 10, 10)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(10, 4, 10, 4)
            };

            TextBlock tb = new TextBlock
            {
                Text = "[ERROR] " + text,
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap
            };

            bubble.Child = tb;
            _chatPanel.Children.Add(bubble);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (_chatScroll != null)
                    _chatScroll.ScrollToEnd();
            }));
        }

        // =====================================================================
        // FILE ATTACHMENT HANDLING
        // =====================================================================

        private void OnAttachFileClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Attach files to agent context",
                Filter = "All files (*.*)|*.*|Source code (*.py;*.go;*.php;*.js;*.cs;*.java;*.rb;*.rs)|*.py;*.go;*.php;*.js;*.cs;*.java;*.rb;*.rs",
                Multiselect = true
            };

            bool result = dlg.ShowDialog() == true;
            if (result)
            {
                for (int i = 0; i < dlg.FileNames.Length; i++)
                {
                    AttachFile(dlg.FileNames[i]);
                }
                RefreshAttachmentUI();
            }
        }

        private void OnAttachFolderClick(object sender, RoutedEventArgs e)
        {
            // Use FolderBrowserDialog via WinForms interop
            var dlg = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select service directory to include in agent context",
                ShowNewFolderButton = false
            };

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AttachDirectory(dlg.SelectedPath);
                RefreshAttachmentUI();
            }
        }

        private void OnClearContextClick(object sender, RoutedEventArgs e)
        {
            _attachedFiles.Clear();
            _currentProjectRoot = null;
            _activeFilePath = null;
            _serviceSkeleton = null;
            if (_tvProject != null) _tvProject.Items.Clear();
            if (_rtbCodeEditor != null) _rtbCodeEditor.Document.Blocks.Clear();
            // We no longer collapse the panel because it has the Network tab
            // if (_colProject != null) _colProject.Width = new GridLength(0);
            RefreshAttachmentUI();
        }

        private void OnFileDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (paths == null) return;

            for (int i = 0; i < paths.Length; i++)
            {
                if (Directory.Exists(paths[i]))
                {
                    AttachDirectory(paths[i]);
                }
                else if (File.Exists(paths[i]))
                {
                    AttachFile(paths[i]);
                }
            }
            RefreshAttachmentUI();
        }

        private void AttachFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;

            // Skip duplicates
            for (int i = 0; i < _attachedFiles.Count; i++)
            {
                if (string.Equals(_attachedFiles[i].FilePath, filePath, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            try
            {
                FileInfo fi = new FileInfo(filePath);
                // Skip files > 500KB
                if (fi.Length > 512000)
                {
                    AddSystemMessage(string.Format("Skipped {0} (too large: {1} KB)", fi.Name, fi.Length / 1024));
                    return;
                }

                string content = File.ReadAllText(filePath, Encoding.UTF8);
                _attachedFiles.Add(new FileAttachment
                {
                    FilePath = filePath,
                    FileName = fi.Name,
                    Content = content,
                    SizeBytes = fi.Length
                });
            }
            catch (Exception ex)
            {
                AddSystemMessage(string.Format("Error reading {0}: {1}", Path.GetFileName(filePath), ex.Message));
            }
        }

        private void AttachDirectory(string dirPath)
        {
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return;
            
            _currentProjectRoot = dirPath;
            _tvProject.Items.Clear();
            _activeFilePath = null;
            _rtbCodeEditor.Document.Blocks.Clear();

            TreeViewItem rootNode = new TreeViewItem { Header = "[+] " + Path.GetFileName(dirPath), IsExpanded = true, Tag = dirPath };
            PopulateTreeView(dirPath, rootNode);
            _tvProject.Items.Add(rootNode);

            // Generate Service Skeleton Map for LLM context
            try
            {
                _serviceSkeleton = ServiceMapGenerator.GenerateProjectMap(dirPath);
            }
            catch (Exception ex)
            {
                _serviceSkeleton = "Error generating service map: " + ex.Message;
            }

            _colProject.Width = new GridLength(250); // Uncollapse the panel
            AddSystemMessage(string.Format("Project loaded: {0}. Select a file to view it in the editor. Only the currently active file will be sent to the agent to save tokens.", Path.GetFileName(dirPath)));
        }

        private void PopulateTreeView(string dir, TreeViewItem parentNode)
        {
            try
            {
                string[] dirs = Directory.GetDirectories(dir);
                for (int i = 0; i < dirs.Length; i++)
                {
                    if (Path.GetFileName(dirs[i]).StartsWith(".")) continue; // Skip hidden
                    TreeViewItem node = new TreeViewItem { Header = "[+] " + Path.GetFileName(dirs[i]), Tag = dirs[i] };
                    PopulateTreeView(dirs[i], node);
                    if (node.Items.Count > 0)
                        parentNode.Items.Add(node);
                }
                
                string[] files = Directory.GetFiles(dir);
                for (int i = 0; i < files.Length; i++)
                {
                    string ext = Path.GetExtension(files[i]).ToLowerInvariant();
                    if (ext == ".exe" || ext == ".dll" || ext == ".png" || ext == ".jpg" || ext == ".zip" || ext == ".pdb") continue;
                    TreeViewItem node = new TreeViewItem { Header = "  " + Path.GetFileName(files[i]), Tag = files[i] };
                    parentNode.Items.Add(node);
                }
            }
            catch { }
        }

        private bool FilterTreeView(ItemCollection items, string filter)
        {
            bool anyVisible = false;
            foreach (TreeViewItem item in items)
            {
                bool isMatch = item.Header.ToString().ToLower().Contains(filter);
                bool childVisible = false;
                if (item.Items.Count > 0)
                {
                    childVisible = FilterTreeView(item.Items, filter);
                }
                
                if (isMatch || childVisible)
                {
                    item.Visibility = Visibility.Visible;
                    if (childVisible && !string.IsNullOrEmpty(filter)) item.IsExpanded = true;
                    anyVisible = true;
                }
                else
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }
            return anyVisible;
        }

        private void HighlightCodeToEditor(string code)
        {
            if (_rtbCodeEditor == null) return;
            _rtbCodeEditor.Document.Blocks.Clear();
            Paragraph p = new Paragraph { Margin = new Thickness(0) };

            if (code.Length > 50000)
            {
                p.Inlines.Add(new Run(code) { Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)) });
                _rtbCodeEditor.Document.Blocks.Add(p);
                return;
            }

            string[] lines = code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            SolidColorBrush commentBrush = new SolidColorBrush(Color.FromRgb(87, 166, 74));
            SolidColorBrush keywordBrush = new SolidColorBrush(Color.FromRgb(86, 156, 214));
            SolidColorBrush stringBrush = new SolidColorBrush(Color.FromRgb(214, 157, 133));
            SolidColorBrush normalBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220));

            Regex rxTokens = new Regex(@"("".*?""|'.*?')|\b(public|private|protected|class|void|string|int|bool|return|if|else|for|while|new|using|namespace|function|var|let|const|import|from|export|default|async|await|def|func|fn|struct|interface|enum|static)\b", RegexOptions.Compiled);

            foreach (string line in lines)
            {
                string trimmed = line.TrimStart();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("#") || trimmed.StartsWith("/*") || trimmed.StartsWith("*"))
                {
                    p.Inlines.Add(new Run(line + "\n") { Foreground = commentBrush });
                    continue;
                }

                int lastIdx = 0;
                MatchCollection matches = rxTokens.Matches(line);
                foreach (Match m in matches)
                {
                    if (m.Index > lastIdx)
                        p.Inlines.Add(new Run(line.Substring(lastIdx, m.Index - lastIdx)) { Foreground = normalBrush });
                    
                    Brush b = (m.Value.StartsWith("\"") || m.Value.StartsWith("'")) ? stringBrush : keywordBrush;
                    p.Inlines.Add(new Run(m.Value) { Foreground = b });
                    lastIdx = m.Index + m.Length;
                }
                
                if (lastIdx < line.Length)
                    p.Inlines.Add(new Run(line.Substring(lastIdx)) { Foreground = normalBrush });
                
                p.Inlines.Add(new Run("\n"));
            }
            _rtbCodeEditor.Document.Blocks.Add(p);
        }

        private void RefreshNetworkPackets()
        {
            if (_tvNetwork == null) return;
            _tvNetwork.Items.Clear();

            var packets = AltayProxySniffer.GlobalRecentPackets.ToArray();
            var grouped = new Dictionary<string, TreeViewItem>();

            for (int i = packets.Length - 1; i >= 0; i--) // newer first
            {
                var p = packets[i];
                string key = p.SrcIp;
                if (!grouped.ContainsKey(key))
                {
                    grouped[key] = new TreeViewItem { Header = "[IP] " + key, IsExpanded = false };
                    _tvNetwork.Items.Add(grouped[key]);
                }
                
                TreeViewItem node = new TreeViewItem 
                { 
                    Header = string.Format("  [{0}] {1} {2}", p.Time, p.Method, p.Uri),
                    Tag = p 
                };
                if (p.TrafficType == "АТАКА") node.Foreground = Brushes.Red;
                else if (p.TrafficType == "УТЕЧКА") node.Foreground = Brushes.Magenta;
                else if (p.TrafficType == "АНОМАЛИЯ") node.Foreground = Brushes.Orange;
                
                grouped[key].Items.Add(node);
            }
        }

        private void OnNetworkTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem node = _tvNetwork.SelectedItem as TreeViewItem;
            if (node != null && node.Tag != null && node.Tag.GetType().Name == "TrafficPacket")
            {
                dynamic p = node.Tag;
                string txt = string.Format("ID: {0} | IP: {1} | Port: {2} -> {3} | Type: {4}\n\n", p.Id, p.SrcIp, p.SrcPort, p.DstPort, p.TrafficType) +
                             "=== REQUEST ===\n" + p.ReqHeaders + "\n\n" + p.ReqBody + "\n\n" +
                             "=== RESPONSE ===\n" + p.ResHeaders + "\n\n" + p.ResBody;
                
                _activeFilePath = "packet_" + p.Id + ".http"; // Virtual path for context
                HighlightCodeToEditor(txt);
            }
        }

        private void OnProjectTreeSelectionChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem node = _tvProject.SelectedItem as TreeViewItem;
            if (node != null)
            {
                string path = node.Tag as string;
                if (path != null && File.Exists(path))
                {
                    try
                    {
                        FileInfo fi = new FileInfo(path);
                        if (fi.Length > 204800) // 200KB limit
                        {
                            HighlightCodeToEditor("// File too large to preview.");
                            _activeFilePath = null;
                        }
                        else
                        {
                            HighlightCodeToEditor(File.ReadAllText(path, Encoding.UTF8));
                            _activeFilePath = path;
                        }
                    }
                    catch (Exception ex)
                    {
                        HighlightCodeToEditor("// Error reading file: " + ex.Message);
                    }
                }
            }
        }

        public void SetNetworkFilter(string ip)
        {
            if (_txtSearch != null)
            {
                _txtSearch.Text = ip;
                _txtSearch.Foreground = Brushes.White;
                
                // Select the Network tab directly using UI tree traversal if needed, 
                // but since we know it's index 1 (0 is Files, 1 is Network)
                Grid g = _txtSearch.Parent as Grid;
                if (g != null)
                {
                    foreach (var child in g.Children)
                    {
                        TabControl tc = child as TabControl;
                        if (tc != null && tc.Items.Count > 1)
                        {
                            tc.SelectedIndex = 1;
                            break;
                        }
                    }
                }
            }
        }

        private void RefreshAttachmentUI()
        {
            _attachmentPanel.Children.Clear();

            if (_attachedFiles.Count == 0)
            {
                _tbAttachmentCount.Text = "No files attached. Drag & drop files/folders here.";
                return;
            }

            long totalSize = 0;
            for (int i = 0; i < _attachedFiles.Count; i++)
            {
                totalSize += _attachedFiles[i].SizeBytes;

                DockPanel dp = new DockPanel { Margin = new Thickness(10, 1, 10, 1) };

                Button btnRemove = new Button
                {
                    Content = "x",
                    Foreground = new SolidColorBrush(Color.FromRgb(200, 80, 80)),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    FontSize = 10,
                    Cursor = Cursors.Hand,
                    Padding = new Thickness(4, 0, 4, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                int idx = i;
                btnRemove.Click += (s, e) =>
                {
                    if (idx < _attachedFiles.Count)
                    {
                        _attachedFiles.RemoveAt(idx);
                        RefreshAttachmentUI();
                    }
                };
                DockPanel.SetDock(btnRemove, Dock.Right);
                dp.Children.Add(btnRemove);

                TextBlock tbSize = new TextBlock
                {
                    Text = string.Format("{0} KB", _attachedFiles[i].SizeBytes / 1024),
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 120)),
                    FontSize = 10,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 8, 0)
                };
                DockPanel.SetDock(tbSize, Dock.Right);
                dp.Children.Add(tbSize);

                TextBlock tbName = new TextBlock
                {
                    Text = _attachedFiles[i].FileName,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255)),
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas"),
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                dp.Children.Add(tbName);

                _attachmentPanel.Children.Add(dp);
            }

            _tbAttachmentCount.Text = string.Format("{0} files attached ({1} KB total)", _attachedFiles.Count, totalSize / 1024);
        }

        // =====================================================================
        // SEND MESSAGE & AI INTERACTION
        // =====================================================================

        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Enter to send
            if (e.Key == Key.Return && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                OnSendClick(sender, e);
                e.Handled = true;
            }
        }

        private async void OnSendClick(object sender, RoutedEventArgs e)
        {
            if (_isProcessing) return;
            string userText = _txtUserInput != null ? _txtUserInput.Text.Trim() : "";
            if (string.IsNullOrEmpty(userText)) return;

            // Add user message to chat
            AddUserMessage(userText);
            _chatHistory.Add(new ChatMessage { Role = "user", Content = userText });
            _txtUserInput.Text = "";

            _isProcessing = true;
            _btnSend.IsEnabled = false;
            _tbStatus.Text = "PROCESSING...";
            _tbStatus.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));

            try
            {
                // Build prompt with file context
                string fullPrompt = BuildPromptWithContext(userText);

                string apiKey = _profile.GetEffectiveApiKey(_config);
                string model = _profile.GetEffectiveModel();
                string baseUrl = _profile.GetEffectiveBaseUrl(_config);
                string role = _profile.Role;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    AddErrorMessage("API Key not configured for this agent. Set it in Settings.");
                    return;
                }

                string response = await GoogleGeminiClient.GeneratePatchAsync(fullPrompt, apiKey, model, baseUrl, role);

                AddAgentMessage(response);
                _chatHistory.Add(new ChatMessage { Role = "assistant", Content = response });

                _tbStatus.Text = "READY";
                _tbStatus.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            }
            catch (Exception ex)
            {
                AddErrorMessage(ex.Message);
                _tbStatus.Text = "ERROR";
                _tbStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
            finally
            {
                _isProcessing = false;
                _btnSend.IsEnabled = true;
            }
        }

        private string BuildPromptWithContext(string userMessage)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(_currentProjectRoot))
            {
                sb.AppendLine("=== PROJECT WORKSPACE CONTEXT ===");
                sb.AppendLine("Workspace Root: " + _currentProjectRoot);
                
                if (!string.IsNullOrEmpty(_serviceSkeleton))
                {
                    sb.AppendLine(_serviceSkeleton);
                }

                if (!string.IsNullOrEmpty(_activeFilePath))
                {
                    sb.AppendLine("Currently Active File in Editor: " + _activeFilePath);
                    sb.AppendLine("--- FILE CONTENT ---");
                    string content = _rtbCodeEditor != null ? new TextRange(_rtbCodeEditor.Document.ContentStart, _rtbCodeEditor.Document.ContentEnd).Text : "";
                    if (content.Length > 8000)
                    {
                        sb.AppendLine(content.Substring(0, 8000));
                        sb.AppendLine("... (truncated)");
                    }
                    else
                    {
                        sb.AppendLine(content);
                    }
                    sb.AppendLine("--- END FILE CONTENT ---");
                }
                else
                {
                    sb.AppendLine("No file is currently open in the editor.");
                }
                sb.AppendLine("=== END PROJECT WORKSPACE ===");
                sb.AppendLine();
            }

            // Add file context if any
            if (_attachedFiles.Count > 0)
            {
                sb.AppendLine("=== ATTACHED SERVICE/FILE CONTEXT ===");
                sb.AppendLine(string.Format("({0} files attached)", _attachedFiles.Count));
                sb.AppendLine();

                for (int i = 0; i < _attachedFiles.Count; i++)
                {
                    FileAttachment fa = _attachedFiles[i];
                    sb.AppendLine(string.Format("--- FILE: {0} ---", fa.FileName));
                    // Truncate very long files
                    if (fa.Content != null && fa.Content.Length > 8000)
                    {
                        sb.AppendLine(fa.Content.Substring(0, 8000));
                        sb.AppendLine(string.Format("... (truncated, full file is {0} bytes)", fa.Content.Length));
                    }
                    else
                    {
                        sb.AppendLine(fa.Content ?? "");
                    }
                    sb.AppendLine("--- END FILE ---");
                    sb.AppendLine();
                }

                sb.AppendLine("=== END OF ATTACHED CONTEXT ===");
                sb.AppendLine();
            }

            // Add recent chat history (last 4 exchanges for context)
            if (_chatHistory.Count > 0)
            {
                int startIdx = _chatHistory.Count > 8 ? _chatHistory.Count - 8 : 0;
                if (startIdx > 0)
                {
                    sb.AppendLine("=== PREVIOUS CONVERSATION CONTEXT ===");
                    for (int i = startIdx; i < _chatHistory.Count; i++)
                    {
                        ChatMessage msg = _chatHistory[i];
                        string label = msg.Role == "user" ? "USER" : "AGENT";
                        string content = msg.Content;
                        if (content != null && content.Length > 2000)
                        {
                            content = content.Substring(0, 2000) + "... (truncated)";
                        }
                        sb.AppendLine(string.Format("[{0}]: {1}", label, content));
                        sb.AppendLine();
                    }
                    sb.AppendLine("=== END PREVIOUS CONTEXT ===");
                    sb.AppendLine();
                }
            }

            sb.AppendLine(userMessage);
            return sb.ToString();
        }

        // =====================================================================
        // UI ACTIONS
        // =====================================================================

        private void OnClearChatClick(object sender, RoutedEventArgs e)
        {
            _chatHistory.Clear();
            _chatPanel.Children.Clear();
            AddSystemMessage(string.Format(
                "Chat cleared. {0} Agent ready.\nModel: {1}",
                _profile.GetDisplayName(), _profile.GetEffectiveModel()));
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private static Color GetRoleAccentColor(string role)
        {
            if (role == "red_team") return Color.FromRgb(220, 38, 38);     // Red
            if (role == "soc_analyst") return Color.FromRgb(251, 191, 36); // Amber
            return Color.FromRgb(34, 197, 94);                             // Green (blue_team)
        }

        private static Button CreateActionButton(string text, Color borderColor, RoutedEventHandler onClick)
        {
            Button btn = new Button
            {
                Content = text,
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 28)),
                Foreground = new SolidColorBrush(borderColor),
                BorderBrush = new SolidColorBrush(borderColor),
                BorderThickness = new Thickness(1),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = Cursors.Hand
            };
            if (onClick != null) btn.Click += onClick;
            return btn;
        }

        // =====================================================================
        // INTERNAL DATA CLASSES
        // =====================================================================
        private class ChatMessage
        {
            public string Role { get; set; }
            public string Content { get; set; }
        }

        private class FileAttachment
        {
            public string FilePath { get; set; }
            public string FileName { get; set; }
            public string Content { get; set; }
            public long SizeBytes { get; set; }
        }
    }
}
