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
    public class App : Application
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

        public static void EnsureNpcapLoaded()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                
                // 1. Try local application directory
                string localPacket = Path.Combine(baseDir, "Packet.dll");
                string localWpcap = Path.Combine(baseDir, "wpcap.dll");
                if (File.Exists(localWpcap))
                {
                    SetDllDirectory(baseDir);
                    if (File.Exists(localPacket)) LoadLibrary(localPacket);
                    LoadLibrary(localWpcap);
                    return;
                }

                // 2. Try System32\Npcap (standard Npcap location on 64-bit OS)
                string sysDir = Environment.SystemDirectory;
                string npcapSys = Path.Combine(sysDir, "Npcap");
                if (Directory.Exists(npcapSys))
                {
                    SetDllDirectory(npcapSys);
                    LoadLibrary(Path.Combine(npcapSys, "Packet.dll"));
                    LoadLibrary(Path.Combine(npcapSys, "wpcap.dll"));
                    return;
                }

                // 3. Try SysWOW64\Npcap (if running in 32-bit subsystem)
                string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                string sysWowNpcap = Path.Combine(winDir, "SysWOW64", "Npcap");
                if (Directory.Exists(sysWowNpcap))
                {
                    SetDllDirectory(sysWowNpcap);
                    LoadLibrary(Path.Combine(sysWowNpcap, "Packet.dll"));
                    LoadLibrary(Path.Combine(sysWowNpcap, "wpcap.dll"));
                    return;
                }

                // 4. Try Program Files\Npcap
                string pfNpcap = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Npcap");
                if (Directory.Exists(pfNpcap))
                {
                    SetDllDirectory(pfNpcap);
                    LoadLibrary(Path.Combine(pfNpcap, "Packet.dll"));
                    LoadLibrary(Path.Combine(pfNpcap, "wpcap.dll"));
                    return;
                }

                // 5. Try default DLL search (System32 / PATH)
                LoadLibrary("Packet.dll");
                LoadLibrary("wpcap.dll");
            }
            catch { }
        }

        [STAThread]
        public static void Main()
        {
            EnsureNpcapLoaded();

            AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) =>
            {
                try
                {
                    var requestedName = new System.Reflection.AssemblyName(resolveArgs.Name);
                    foreach (var loaded in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (loaded.GetName().Name.Equals(requestedName.Name, StringComparison.OrdinalIgnoreCase))
                            return loaded;
                    }
                    string candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, requestedName.Name + ".dll");
                    if (File.Exists(candidate))
                        return System.Reflection.Assembly.LoadFrom(candidate);

                    string libCandidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lib", requestedName.Name + ".dll");
                    if (File.Exists(libCandidate))
                        return System.Reflection.Assembly.LoadFrom(libCandidate);
                }
                catch { }
                return null;
            };

            try
            {
                var app = new App();
                var startup = new StartupWindow(null, false);

                app.DispatcherUnhandledException += (s, args) =>
                {
                    try
                    {
                        string crashLog = string.Format("[{0}] CRASH: {1}\n{2}",
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            args.Exception.Message,
                            args.Exception.StackTrace);
                        File.AppendAllText("ddcap_crash.log", crashLog + "\n\n");
                        MessageBox.Show("Произошла критическая ошибка. Подробности сохранены в ddcap_crash.log.\n\n" + args.Exception.Message, "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch { }
                    args.Handled = true;
                };

                startup.Show();
                startup.Activate();
                startup.Focus();
                app.Run();
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(AppPaths.CrashLog, ex.ToString()); } catch { }
            }
        }
    }
}
