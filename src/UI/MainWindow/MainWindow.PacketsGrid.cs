using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DDCAP.AltayCTF
{
    public partial class MainWindow : Window
    {
        // =====================================================================
        // PACKETS DATAGRID (DDCAP BLACK / WHITE / RED ASCII)
        // =====================================================================
        private DataGrid CreatePacketsDataGrid()
        {
            DataGrid dg = new DataGrid
            {
                ItemsSource = _filteredPackets,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                SelectionMode = DataGridSelectionMode.Single,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                Background = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                RowBackground = new SolidColorBrush(Color.FromRgb(8, 8, 8)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(14, 14, 14)),
                BorderThickness = new Thickness(0),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                CanUserResizeRows = false,
                EnableRowVirtualization = true,
                EnableColumnVirtualization = false,
                ColumnHeaderStyle = DdcapUiStyles.CreateDarkColumnHeaderStyle(),
                CellStyle = DdcapUiStyles.CreateDarkCellStyle(),
                RowStyle = DdcapUiStyles.CreateDarkRowStyle()
            };

            VirtualizingPanel.SetIsVirtualizing(dg, true);
            VirtualizingPanel.SetVirtualizationMode(dg, VirtualizationMode.Recycling);
            ScrollViewer.SetIsDeferredScrollingEnabled(dg, false);

            dg.Columns.Add(new DataGridTextColumn { Header = "#", Binding = new Binding("Id"), Width = 45 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Время", Binding = new Binding("Time"), Width = 85 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Метод", Binding = new Binding("Method"), Width = 55 });
            dg.Columns.Add(new DataGridTextColumn { Header = "IP Источника", Binding = new Binding("SrcIp"), Width = 110 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Порт", Binding = new Binding("DstPort"), Width = 55 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Сервис", Binding = new Binding("ServiceName"), Width = 110 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Маршрут (URI)", Binding = new Binding("Uri"), Width = new DataGridLength(1.5, DataGridLengthUnitType.Star) });
            dg.Columns.Add(new DataGridTextColumn { Header = "Код", Binding = new Binding("Status"), Width = 45 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Классификация", Binding = new Binding("TrafficType"), Width = 130 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Уязвимость / Атака", Binding = new Binding("VulnType"), Width = new DataGridLength(1.5, DataGridLengthUnitType.Star) });
            dg.Columns.Add(new DataGridTextColumn { Header = "Энтропия", Binding = new Binding("Entropy"), Width = 65 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Корреляция / Цепочка", Binding = new Binding("CorrelationNotes"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            dg.Columns.Add(new DataGridTextColumn { Header = "Токен флага", Binding = new Binding("FlagToken"), Width = 120 });

            // Visual Row Styling (DDCAP Black / White / Red)
            dg.LoadingRow += (s, e) =>
            {
                var item = e.Row.Item as TrafficPacket;
                if (item != null)
                {
                    if (item.IsFirstSeen || item.TrafficType == "НОВЫЙ ВЕКТОР")
                    {
                        e.Row.Background = FrozenBrushNewVectorBg;
                        e.Row.Foreground = Brushes.White;
                        e.Row.FontWeight = FontWeights.Bold;
                    }
                    else if (item.TrafficType == "УТЕЧКА")
                    {
                        e.Row.Background = FrozenBrushLeakBg;
                        e.Row.Foreground = FrozenBrushLeakFg;
                        e.Row.FontWeight = FontWeights.Bold;
                    }
                    else if (item.TrafficType == "ЦЕПОЧКА")
                    {
                        e.Row.Background = FrozenBrushChainBg;
                        e.Row.Foreground = FrozenBrushChainFg;
                        e.Row.FontWeight = FontWeights.Bold;
                    }
                    else if (item.TrafficType == "0-DAY")
                    {
                        e.Row.Background = FrozenBrushZeroDayBg;
                        e.Row.Foreground = FrozenBrushZeroDayFg;
                        e.Row.FontWeight = FontWeights.Bold;
                    }
                    else if (item.TrafficType == "АТАКА")
                    {
                        e.Row.Background = FrozenBrushAttackBg;
                        e.Row.Foreground = FrozenBrushAttackFg;
                        e.Row.FontWeight = FontWeights.Bold;
                    }
                    else if (item.TrafficType == "ЧЕКЕР")
                    {
                        e.Row.Background = FrozenBrushCheckerBg;
                        e.Row.Foreground = Brushes.White;
                        e.Row.FontWeight = FontWeights.Normal;
                    }
                    // Обязательный сброс фона для обычного трафика (устраняет баг ресайклинга WPF)
                    else
                    {
                        e.Row.Background = (e.Row.GetIndex() % 2 == 0)
                            ? FrozenBrushNormalEvenBg
                            : FrozenBrushNormalOddBg;
                        e.Row.Foreground = FrozenBrushNormalFg;
                        e.Row.FontWeight = FontWeights.Normal;
                    }
                }
            };

            dg.SelectionChanged += OnPacketSelected;

            // Context Menu (DDCAP Dark Terminal style, custom template to kill Aero gutter & black text)
            var cmTemplate = new ControlTemplate(typeof(ContextMenu));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(18, 18, 18)));
            borderFactory.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(220, 38, 38))); // Тонкая красная рамка DDCAP
            borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(3));

            var itemsPresenterFactory = new FrameworkElementFactory(typeof(ItemsPresenter));
            borderFactory.AppendChild(itemsPresenterFactory);
            cmTemplate.VisualTree = borderFactory;

            ContextMenu cm = new ContextMenu
            {
                Template = cmTemplate,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12
            };

            // Custom MenuItem template to eliminate default Aero icon gutter and hover text inversion
            var miTemplate = new ControlTemplate(typeof(MenuItem));
            var miBorder = new FrameworkElementFactory(typeof(Border));
            miBorder.Name = "ItemBorder";
            miBorder.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(18, 18, 18)));
            miBorder.SetValue(Border.PaddingProperty, new Thickness(12, 6, 12, 6));

            var miPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            miPresenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
            miPresenter.SetValue(ContentPresenter.RecognizesAccessKeyProperty, true);
            miBorder.AppendChild(miPresenter);
            miTemplate.VisualTree = miBorder;

            var trig = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
            trig.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 14, 14)), "ItemBorder"));
            miTemplate.Triggers.Add(trig);

            // Хелпер создания пункта меню с гарантированно видимым светлым текстом:
            Func<string, Brush, RoutedEventHandler, MenuItem> createMenuItem = (header, fgBrush, onClick) =>
            {
                var mi = new MenuItem
                {
                    Header = header,
                    Foreground = fgBrush ?? new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    Background = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(10, 6, 10, 6),
                    FontFamily = new FontFamily("Consolas"),
                    Template = miTemplate,
                    Cursor = Cursors.Hand
                };
                if (onClick != null) mi.Click += onClick;
                return mi;
            };

            // Заполнение пунктов БЕЗ ЭМОДЗИ, со строгим ASCII и четкими цветами:
            cm.Items.Add(createMenuItem("[>>] Зеркальный удар (Инвертировать эксплойт на все команды)", new SolidColorBrush(Color.FromRgb(239, 68, 68)), (s, e) => {
                var sel = dg.SelectedItem as TrafficPacket;
                if (sel != null) 
                {
                    ViewModel.Farm.InvertExploit(sel, _config);
                    if (_bottomTabs != null) _bottomTabs.SelectedIndex = 2;
                }
            }));

            cm.Items.Add(createMenuItem("[+] Отправить в резервный реплеер (проверка)", Brushes.White, (s, e) => {
                var sel = dg.SelectedItem as TrafficPacket;
                if (sel != null) ViewModel.Farm.LoadFromPacket(sel, _config);
            }));

            cm.Items.Add(createMenuItem("[F] Копировать токен флага", new SolidColorBrush(Color.FromRgb(248, 113, 113)), (s, e) => {
                var sel = dg.SelectedItem as TrafficPacket;
                if (sel != null && !string.IsNullOrEmpty(sel.FlagToken))
                {
                    try { Clipboard.SetText(sel.FlagToken); } catch { }
                    MessageBox.Show("Флаг скопирован: " + sel.FlagToken, "Банк флагов", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }));

            cm.Items.Add(createMenuItem("[?] Копировать AI-промпт для анализа", new SolidColorBrush(Color.FromRgb(163, 163, 163)), (s, e) => {
                var sel = dg.SelectedItem as TrafficPacket;
                if (sel != null)
                {
                    sel.EnsureReportsGenerated();
                    if (!string.IsNullOrEmpty(sel.AiPromptTemplate))
                    {
                        try { Clipboard.SetText(sel.AiPromptTemplate); } catch { }
                        MessageBox.Show("AI-промпт скопирован в буфер обмена.", "AI Patch Advisor", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }));

            dg.ContextMenu = cm;
            return dg;
        }
    }
}
