using System;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DDCAP.AltayCTF
{
    public class InspectorTabControl : UserControl
    {
        private TextBlock _tbDecodedPreview;
        private TextBox _txtReqHeaders;
        private TextBox _txtReqBody;
        private TextBox _txtResHeaders;
        private TextBox _txtResBody;

        public InspectorTabControl()
        {
            Content = CreateInspectorTab();
        }

        public void UpdatePacketView(TrafficPacket p)
        {
            // Теперь логика находится в InspectorViewModel, этот метод больше не нужен
            // Но мы оставляем его пустным для обратной совместимости, если где-то еще вызывается (до полного рефакторинга)
        }

        private Grid CreateInspectorTab()
        {
            Grid g = new Grid { Margin = new Thickness(8), Background = new SolidColorBrush(Color.FromRgb(5, 5, 5)) };
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28, GridUnitType.Pixel) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Top preview strip
            Border bDecoded = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(13, 13, 13)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 0, 0, 4)
            };
            _tbDecodedPreview = new TextBlock
            {
                Text = "[ ДЕОБФУСЦИРОВАННАЯ ПОЛЕЗНАЯ НАГРУЗКА ]: выберите пакет в таблице для анализа...",
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _tbDecodedPreview.SetBinding(TextBlock.TextProperty, new Binding("DecodedPreview"));
            bDecoded.Child = _tbDecodedPreview;
            Grid.SetRow(bDecoded, 0);
            g.Children.Add(bDecoded);

            // Bottom split view
            Grid gSplit = new Grid();
            gSplit.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gSplit.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6, GridUnitType.Pixel) });
            gSplit.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Left: Request
            GroupBox gbReq = new GroupBox
            {
                Header = "[+] ВХОДЯЩИЙ HTTP ЗАПРОС (СЕТЕВОЙ ПАКЕТ)",
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold
            };
            DockPanel dpReq = new DockPanel();

            _txtReqHeaders = new TextBox
            {
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                Height = 80
            };
            _txtReqHeaders.SetBinding(TextBox.TextProperty, new Binding("ReqHeaders") { Mode = BindingMode.OneWay });
            DockPanel.SetDock(_txtReqHeaders, Dock.Top);
            dpReq.Children.Add(_txtReqHeaders);

            _txtReqBody = new TextBox
            {
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                Foreground = new SolidColorBrush(Color.FromRgb(229, 229, 229)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 4, 0, 0)
            };
            _txtReqBody.SetBinding(TextBox.TextProperty, new Binding("ReqBody") { Mode = BindingMode.OneWay });
            dpReq.Children.Add(_txtReqBody);
            gbReq.Content = dpReq;
            Grid.SetColumn(gbReq, 0);
            gSplit.Children.Add(gbReq);

            GridSplitter splitV = new GridSplitter
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Stretch,
                Width = 6,
                Background = new SolidColorBrush(Color.FromRgb(38, 38, 38))
            };
            Grid.SetColumn(splitV, 1);
            gSplit.Children.Add(splitV);

            // Right: Response
            GroupBox gbRes = new GroupBox
            {
                Header = "[=] ИСХОДЯЩИЙ HTTP ОТВЕТ (ИЗ VULNBOX В ЧЕКЕР / КЛИЕНТУ)",
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold
            };
            DockPanel dpRes = new DockPanel();

            _txtResHeaders = new TextBox
            {
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                Height = 80
            };
            _txtResHeaders.SetBinding(TextBox.TextProperty, new Binding("ResHeaders") { Mode = BindingMode.OneWay });
            DockPanel.SetDock(_txtResHeaders, Dock.Top);
            dpRes.Children.Add(_txtResHeaders);

            _txtResBody = new TextBox
            {
                IsReadOnly = true,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                Foreground = new SolidColorBrush(Color.FromRgb(229, 229, 229)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 4, 0, 0)
            };
            _txtResBody.SetBinding(TextBox.TextProperty, new Binding("ResBody") { Mode = BindingMode.OneWay });
            dpRes.Children.Add(_txtResBody);
            gbRes.Content = dpRes;
            Grid.SetColumn(gbRes, 2);
            gSplit.Children.Add(gbRes);

            Grid.SetRow(gSplit, 1);
            g.Children.Add(gSplit);

            return g;
        }
    }
}
