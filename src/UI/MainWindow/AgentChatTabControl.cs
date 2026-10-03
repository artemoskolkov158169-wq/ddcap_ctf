using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DDCAP.AltayCTF
{
    public class AgentChatTabControl : UserControl
    {
        public AgentChatTabControl()
        {
            Content = CreateAgentChatLauncherTab();
        }

        private Grid CreateAgentChatLauncherTab()
        {
            Grid g = new Grid { Margin = new Thickness(8), Background = new SolidColorBrush(Color.FromRgb(5, 5, 5)) };

            StackPanel sp = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock tbHeader = new TextBlock
            {
                Text = "AI AGENT COMMAND CENTER",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 0)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            sp.Children.Add(tbHeader);

            TextBlock tbSubheader = new TextBlock
            {
                Text = "Each agent has its own API, model, and chat window with file upload.",
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 140)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 25)
            };
            sp.Children.Add(tbSubheader);

            StackPanel spButtons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            // Defense Agent Button
            Button btnDefense = CreateAgentLaunchButton(
                "[DEFENSE AGENT]",
                "Blue Team DevSecOps",
                "Patching, hardening, SLA compliance",
                Color.FromRgb(34, 197, 94),
                "blue_team");
            btnDefense.Margin = new Thickness(0, 0, 15, 0);
            spButtons.Children.Add(btnDefense);

            // Attack Agent Button
            Button btnAttack = CreateAgentLaunchButton(
                "[ATTACK AGENT]",
                "Red Team Pentester",
                "Exploit analysis, payload crafting",
                Color.FromRgb(220, 38, 38),
                "red_team");
            btnAttack.Margin = new Thickness(0, 0, 15, 0);
            spButtons.Children.Add(btnAttack);

            // Monitor Agent Button
            Button btnMonitor = CreateAgentLaunchButton(
                "[MONITOR AGENT]",
                "SOC Analyst",
                "Threat detection, IoC, MITRE ATT&CK",
                Color.FromRgb(251, 191, 36),
                "soc_analyst");
            spButtons.Children.Add(btnMonitor);

            sp.Children.Add(spButtons);

            TextBlock tbHint = new TextBlock
            {
                Text = "TIP: You can drag & drop files or entire service directories into the agent chat window.",
                Foreground = new SolidColorBrush(Color.FromRgb(90, 90, 100)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 25, 0, 0)
            };
            sp.Children.Add(tbHint);

            g.Children.Add(sp);
            return g;
        }

        private Button CreateAgentLaunchButton(string title, string subtitle, string description, Color accentColor, string role)
        {
            StackPanel sp = new StackPanel { Margin = new Thickness(15, 12, 15, 12) };

            TextBlock tbTitle = new TextBlock
            {
                Text = title,
                Foreground = new SolidColorBrush(accentColor),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 5)
            };
            sp.Children.Add(tbTitle);

            TextBlock tbSubtitle = new TextBlock
            {
                Text = subtitle,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 3)
            };
            sp.Children.Add(tbSubtitle);

            TextBlock tbDesc = new TextBlock
            {
                Text = description,
                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 140)),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(tbDesc);

            Button btn = new Button
            {
                Content = sp,
                Background = new SolidColorBrush(Color.FromRgb(14, 14, 20)),
                BorderBrush = new SolidColorBrush(accentColor),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(0),
                Cursor = Cursors.Hand,
                Width = 240,
                Height = 110
            };

            string agentRole = role;
            btn.Click += (s, e) =>
            {
                try
                {
                    AppConfig cfg = AppConfig.Load();
                    AgentProfile profile = cfg.GetAgentProfile(agentRole);
                    var chatWindow = new AgentChatWindow(profile, cfg);
                    chatWindow.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error opening agent chat: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            return btn;
        }
    }
}
