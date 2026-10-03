# SLA & Patch Assistant Logic (Deferred Feature)

## Концепция
В классическом A/D CTF системе защиты часто требуется быстро проверять работоспособность патча (закрывает ли он уязвимость и не ломает ли чекеры).
Так как DDCAP переведен на пассивную L2/L3 архитектуру, он не может блокировать или изменять боевой трафик. 

Вместо этого мы используем подход "Локального реплея":
1. На машине мониторинга поднимается локальная копия уязвимого сервиса (например, через Docker или Host-Only клон виртуалки).
2. DDCAP захватывает боевой эксплойт из сети.
3. Инженер пишет патч на локальной копии сервиса.
4. В DDCAP (во вкладке Patch Assistant) вводится URL локальной копии (например, `http://127.0.0.1:5000`).
5. Инженер выбирает захваченный вредоносный пакет и нажимает кнопку "Проверить локально" (Replay).
6. DDCAP реконструирует HTTP-запрос (с сохранением метода, заголовков и Payload) и отправляет его на локальную копию, показывая ответ сервера.
7. Если эксплойт не сработал (патч помог), а сервис отдает 200 OK — патч можно заливать на боевой сервер.

## Архивный код PatchTabControl.cs
Ниже сохранен исходный код контрола для интеграции в будущем.

```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DDCAP.AltayCTF
{
    public class PatchTabControl : UserControl
    {
        private AppConfig _config;
        private TrafficPacket _currentPacket;
        private TextBox _txtLocalVulnboxUrl;
        private TextBox _txtResponseViewer;

        public PatchTabControl(AppConfig config)
        {
            _config = config;
            InitializeLayout();
        }

        private void InitializeLayout()
        {
            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var spTop = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            spTop.Children.Add(new TextBlock
            {
                Text = "LOCAL VULNBOX (SLA TEST): ",
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                VerticalAlignment = VerticalAlignment.Center
            });
            _txtLocalVulnboxUrl = new TextBox
            {
                Text = "http://127.0.0.1:5000",
                Width = 200,
                Margin = new Thickness(10, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(20, 20, 25)),
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas")
            };
            spTop.Children.Add(_txtLocalVulnboxUrl);

            var btnTest = new Button
            {
                Content = "[ ЗАПУСТИТЬ ТЕСТ (REPLAY) ]",
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(10, 4, 10, 4),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            DdcapUiStyles.ApplyDarkButtonStyle(btnTest, Color.FromRgb(20, 35, 25), Color.FromRgb(0, 255, 150), Color.FromRgb(0, 255, 150));
            btnTest.Click += BtnTest_Click;
            spTop.Children.Add(btnTest);

            grid.Children.Add(spTop);
            Grid.SetRow(spTop, 0);

            _txtResponseViewer = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 15)),
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontFamily = new FontFamily("Consolas"),
                Margin = new Thickness(0, 10, 0, 0)
            };
            grid.Children.Add(_txtResponseViewer);
            Grid.SetRow(_txtResponseViewer, 2);

            Content = grid;
        }

        public void UpdatePacketView(TrafficPacket packet)
        {
            _currentPacket = packet;
        }

        public void RunLocalTestForPacket(TrafficPacket p)
        {
            _currentPacket = p;
            BtnTest_Click(null, null);
        }

        private async void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPacket == null)
            {
                MessageBox.Show("Сначала выберите пакет в таблице Инспектора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string targetUrl = _txtLocalVulnboxUrl.Text.Trim();
            if (string.IsNullOrEmpty(targetUrl)) return;

            string fullUrl = targetUrl.TrimEnd('/') + _currentPacket.Uri;

            _txtResponseViewer.Text = "Отправка запроса на " + fullUrl + "...\r\n\r\n";

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    
                    var method = new HttpMethod(_currentPacket.Method);
                    var request = new HttpRequestMessage(method, fullUrl);
                    
                    try {
                        if (!string.IsNullOrEmpty(_currentPacket.ReqHeaders))
                        {
                            using (var reader = new StringReader(_currentPacket.ReqHeaders))
                            {
                                string line;
                                bool headers = true;
                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (string.IsNullOrWhiteSpace(line)) { headers = false; continue; }
                                    if (headers)
                                    {
                                        int sep = line.IndexOf(':');
                                        if (sep > 0)
                                        {
                                            string key = line.Substring(0, sep).Trim();
                                            string val = line.Substring(sep + 1).Trim();
                                            if (key.ToLower() != "host" && key.ToLower() != "content-length")
                                            {
                                                request.Headers.TryAddWithoutValidation(key, val);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    } catch { }

                    if (!string.IsNullOrEmpty(_currentPacket.ReqBody))
                    {
                        request.Content = new StringContent(_currentPacket.ReqBody, Encoding.UTF8);
                    }

                    var response = await client.SendAsync(request);
                    string respBody = await response.Content.ReadAsStringAsync();
                    
                    _txtResponseViewer.Text += $"{(int)response.StatusCode} {response.ReasonPhrase}\r\n";
                    _txtResponseViewer.Text += response.Headers.ToString() + "\r\n";
                    _txtResponseViewer.Text += respBody;
                }
            }
            catch (Exception ex)
            {
                _txtResponseViewer.Text += "ОШИБКА: " + ex.Message;
            }
        }
    }
}
```

## Как восстановить
1. Создать файл `src/UI/MainWindow/PatchTabControl.cs` со скриптом выше.
2. В `MainWindow.cs` добавить:
   ```csharp
   private PatchTabControl _patchTab;
   ...
   _patchTab = new PatchTabControl(_config);
   tc.Items.Add(new TabItem { Header = "[05] ЛОКАЛЬНЫЙ ТЕСТ ПАТЧЕЙ", Content = _patchTab });
   ```
   И в `OnPacketCaptured` или `SelectedPacket`:
   ```csharp
   if (_patchTab != null) _patchTab.UpdatePacketView(p);
   ```
3. В `MainWindow.PacketsGrid.cs` вернуть контекстное меню:
   ```csharp
   cm.Items.Add(createMenuItem("[*] Проверить локально на VulnBox (SLA TEST)", new SolidColorBrush(Color.FromRgb(212, 212, 212)), (s, e) => {
       var sel = dg.SelectedItem as TrafficPacket;
       if (sel != null && _patchTab != null) {
           _patchTab.RunLocalTestForPacket(sel);
       }
   }));
   ```
