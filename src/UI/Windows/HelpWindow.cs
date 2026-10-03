using System;
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
    public class HelpWindow : Window
    {
        public HelpWindow()
        {
            Title = "[::] DDCAP // БОЕВАЯ ПАМЯТКА ОПЕРАТОРА [F1]";
            Width = 820;
            Height = 640;
            MinWidth = 720;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(10, 10, 10));
            Foreground = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            FontFamily = new FontFamily("Consolas");
            ResizeMode = ResizeMode.CanResizeWithGrip;

            PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape || e.Key == Key.F1)
                {
                    Close();
                    e.Handled = true;
                }
            };

            InitializeLayout();
        }

        private void InitializeLayout()
        {
            DockPanel dpRoot = new DockPanel { LastChildFill = true, Margin = new Thickness(14) };

            // Header
            StackPanel spHeader = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };
            TextBlock tbTitle = new TextBlock
            {
                Text = "[::] DDCAP DEFENSE MATRIX // БОЕВАЯ ПАМЯТКА ОПЕРАТОРА (ALTAY 2026)",
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68))
            };
            TextBlock tbSub = new TextBlock
            {
                Text = ":: Тактическое руководство Blue Team, горячие клавиши и маршрутизация сервисов",
                FontSize = 10.5,
                FontFamily = new FontFamily("Consolas"),
                Foreground = new SolidColorBrush(Color.FromRgb(163, 163, 163)),
                Margin = new Thickness(0, 2, 0, 0)
            };
            spHeader.Children.Add(tbTitle);
            spHeader.Children.Add(tbSub);
            DockPanel.SetDock(spHeader, Dock.Top);
            dpRoot.Children.Add(spHeader);

            // Close Button at bottom
            Button btnClose = new Button
            {
                Content = "[ ЗАКРЫТЬ (ESC) ]",
                Background = new SolidColorBrush(Color.FromRgb(28, 11, 11)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                FontFamily = new FontFamily("Consolas"),
                FontWeight = FontWeights.Bold,
                Padding = new Thickness(22, 6, 22, 6),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 10, 0, 0)
            };
            btnClose.Click += (s, e) => Close();
            DockPanel.SetDock(btnClose, Dock.Bottom);
            dpRoot.Children.Add(btnClose);

            // Scrollable Content
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = Brushes.Transparent
            };

            StackPanel spContent = new StackPanel();

            // BLOCK 1: HOTKEYS
            spContent.Children.Add(CreateSectionBox("[ ГОРЯЧИЕ КЛАВИШИ УПРАВЛЕНИЯ ]",
                "F1          :: Открыть / скрыть данную боевую памятку оператора\n" +
                "F5          :: Быстрый запуск / остановка сниффера перехвата трафика\n" +
                "Ctrl+Space  :: Альтернативный триггер переключения сниффера (Start / Stop)\n" +
                "Ctrl+F      :: Мгновенный фокус в строку быстрого поиска трафика с выделением\n" +
                "Ctrl+L      :: Очистить экран от отображаемых пакетов (без сброса сниффера)\n" +
                "Ctrl+1      :: Переключить на вкладку [01] ИНСПЕКТОР ТРАФИКА\n" +
                "Ctrl+2      :: Переключить на вкладку [02] РЕЗЕРВНЫЙ РЕПЛЕЕР\n" +
                "Ctrl+3      :: Переключить на вкладку [03] БАНК ФЛАГОВ И ЖЮРИ\n" +
                "Ctrl+4      :: Переключить на вкладку [04] МАТРИЦА УГРОЗ\n" +
                "Ctrl+5      :: Переключить на вкладку [05] AI AGENTS\n",
                Color.FromRgb(239, 68, 68)));

            // BLOCK 2: SLA RULES
            spContent.Children.Add(CreateSectionBox("[ ЗОЛОТЫЕ ПРАВИЛА SLA BLUE TEAM ]",
                "1. НИКОГДА НЕ ОТДАВАТЬ 403 FORBIDDEN ЧЕКЕРАМ ЖЮРИ:\n" +
                "   Любая блокировка запроса чекера ведет к статусу SERVICE DOWN / CORRUPT,\n" +
                "   обнулению очков за SLA и потере защищаемого сервиса в раунде.\n\n" +
                "2. ЛЮБОЙ ПАТЧ ОБЯЗАН ВОЗВРАЩАТЬ ВАЛИДНЫЙ 200 OK ДЛЯ PUTFLAG / GETFLAG:\n" +
                "   Патч должен фильтровать вредоносные пейлоады (SQLi, SSTI, RCE, Path Traversal),\n" +
                "   но легитимные данные флагов чекера обязаны успешно сохраняться и читаться.\n\n" +
                "3. РЕЗАТЬ ЭКСПЛОЙТЫ ТОЧЕЧНО ПО СИГНАТУРАМ, НЕ БЛОКИРУЯ IP ЦЕЛИКОМ:\n" +
                "   Чекер жюри и атакующие могут использовать одинаковые подсети или прокси.\n" +
                "   Блокировка по IP недопустима — нейтрализуйте только деобфусцированную нагрузку.\n\n" +
                "4. ТЕСТИРУЙТЕ ПАТЧИ ЛОКАЛЬНО ПЕРЕД ВЫКАТКОЙ:\n" +
                "   Используйте кнопку [* Проверить локально на VulnBox] во вкладке [02].\n",
                Color.FromRgb(251, 191, 36)));

            // BLOCK 3: ROUTING & FLAGS
            spContent.Children.Add(CreateSectionBox("[ МУЛЬТИПОРТОВАЯ ТАБЛИЦА МАРШРУТИЗАЦИИ И ФЛАГИ ]",
                "ТАБЛИЦА ПОРТОВ (PORT FORWARDING -> VULNBOX 127.0.0.1:5000):\n" +
                "  • ПОРТ 8000 :: Web Shop       -> /api/v1/products, /checker/putflag\n" +
                "  • ПОРТ 8001 :: Auth Service   -> /api/v1/auth\n" +
                "  • ПОРТ 8002 :: Notes Service  -> /notes\n" +
                "  • ПОРТ 8003 :: System Service -> /service, /api/upload, /uploads, /api/debug\n\n" +
                "ФОРМАТЫ ФЛАГОВ СОРЕВНОВАНИЯ ALTAYCTF:\n" +
                "  • Regex 1 : (?i)(flag|ddcap|altay|ructf)\\{[^ \\r\\n\\t\\}]{6,80}\\}\n" +
                "  • Regex 2 : \\b[A-Z0-9]{31}=\\b (Base64-подобный токен жюри)\n" +
                "  • Regex 3 : \\b[a-f0-9]{32}\\b (MD5-хэш токена)\n\n" +
                "ШАБЛОН ПОДСЕТЕЙ КОМАНД (FARM ATTACK):\n" +
                "  • Шаблон : http://10.60.{team}.2:8000/<route>\n",
                Color.FromRgb(59, 130, 246)));

            sv.Content = spContent;
            dpRoot.Children.Add(sv);
            Content = dpRoot;
        }

        private Border CreateSectionBox(string header, string text, Color accentColor)
        {
            Border b = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(14, 14, 14)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(38, 38, 38)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };

            StackPanel sp = new StackPanel();
            TextBlock tbHead = new TextBlock
            {
                Text = header,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(accentColor),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 6)
            };
            TextBlock tbBody = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10.5,
                LineHeight = 16
            };
            sp.Children.Add(tbHead);
            sp.Children.Add(tbBody);
            b.Child = sp;
            return b;
        }
    }
}
