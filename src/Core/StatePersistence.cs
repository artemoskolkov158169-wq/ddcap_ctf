using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace DDCAP.AltayCTF
{
    public static class StatePersistence
    {
        private static readonly ConcurrentQueue<TrafficPacket> _trafficQueue = new ConcurrentQueue<TrafficPacket>();
        private static readonly ConcurrentQueue<CapturedFlagItem> _flagQueue = new ConcurrentQueue<CapturedFlagItem>();
        private static bool _isRunning = true;
        private static readonly JavaScriptSerializer _serializer = new JavaScriptSerializer { MaxJsonLength = 10000000 };

        public static string TrafficDbPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "traffic.jsonl"); } }
        public static string FlagsDbPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "flags.jsonl"); } }

        static StatePersistence()
        {
            Task.Run(() => WriterLoop());
        }

        public static void SaveTraffic(TrafficPacket p)
        {
            if (p != null) _trafficQueue.Enqueue(p);
        }

        public static void SaveFlag(CapturedFlagItem f)
        {
            if (f != null) _flagQueue.Enqueue(f);
        }

        private static void WriterLoop()
        {
            while (_isRunning)
            {
                bool wroteAnything = false;
                
                try
                {
                    if (!_trafficQueue.IsEmpty)
                    {
                        using (var fs = new StreamWriter(TrafficDbPath, true))
                        {
                            TrafficPacket p;
                            while (_trafficQueue.TryDequeue(out p))
                            {
                                fs.WriteLine(_serializer.Serialize(p));
                                wroteAnything = true;
                            }
                        }
                    }

                    if (!_flagQueue.IsEmpty)
                    {
                        using (var fs = new StreamWriter(FlagsDbPath, true))
                        {
                            CapturedFlagItem f;
                            while (_flagQueue.TryDequeue(out f))
                            {
                                fs.WriteLine(_serializer.Serialize(f));
                                wroteAnything = true;
                            }
                        }
                    }
                }
                catch { }

                if (!wroteAnything)
                    Thread.Sleep(500);
            }
        }

        public static void LoadState(MainViewModel vm)
        {
            try
            {
                if (File.Exists(TrafficDbPath))
                {
                    // Load last 5000 packets to avoid UI freeze and RAM usage
                    var lines = File.ReadAllLines(TrafficDbPath);
                    int start = Math.Max(0, lines.Length - 5000);
                    for (int i = start; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        try
                        {
                            var p = _serializer.Deserialize<TrafficPacket>(lines[i]);
                            vm.AllPackets.Add(p);
                            vm.TotalPacketsCount++;
                            if (p.TrafficType == "ЧЕКЕР") vm.TotalCheckerCount++;
                            else if (p.TrafficType == "АТАКА" || p.TrafficType == "0-DAY" || p.TrafficType == "УТЕЧКА") vm.TotalAttacksCount++;
                            
                            if (vm.MatchesFilter(p)) vm.FilteredPackets.Add(p);
                        }
                        catch { }
                    }
                }

                if (File.Exists(FlagsDbPath))
                {
                    var lines = File.ReadAllLines(FlagsDbPath);
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            var f = _serializer.Deserialize<CapturedFlagItem>(line);
                            vm.FlagVault.Add(f);
                            vm.TotalFlagsCount++;
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }
    }
}
