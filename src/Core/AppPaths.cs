using System;
using System.IO;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // APPLICATION PATHS (PORTABLE - NO HARDCODED DIRECTORIES)
    // =========================================================================
    internal static class AppPaths
    {
        private static readonly string _baseDir;

        static AppPaths()
        {
            _baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
        }

        public static string BaseDir { get { return _baseDir; } }
        public static string ConfigFile { get { return Path.Combine(_baseDir, "ddcap_config.json"); } }
        public static string EnvFile { get { return Path.Combine(_baseDir, ".env"); } }
        public static string TrafficLog { get { return Path.Combine(_baseDir, "traffic_captured.log"); } }
        public static string CrashLog { get { return Path.Combine(_baseDir, "crash.log"); } }
        public static string FlagsExport { get { return Path.Combine(_baseDir, "flags_exported.txt"); } }
    }
}
