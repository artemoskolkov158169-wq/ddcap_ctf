using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // 3.5 HIGH-LOAD ASYNC LOG ENGINE (LOCK-FREE BATCH WRITER)
    // =========================================================================
    public static class AsyncTrafficLogger
    {
        private static readonly AsyncBoundedQueue<string> _logQueue = new AsyncBoundedQueue<string>(100000);
        private static readonly Task _writerTask;
        private static readonly string _logPath = AppPaths.TrafficLog;
        private static volatile bool _isRunning = true;

        static AsyncTrafficLogger()
        {
            _writerTask = Task.Run(new Func<Task>(WriterLoopAsync));
        }

        public static void Log(string line)
        {
            if (string.IsNullOrEmpty(line) || !_isRunning) return;
            try
            {
                _logQueue.TryWrite(line);
            }
            catch { }
        }

        public static void Shutdown()
        {
            _isRunning = false;
            try { _logQueue.Complete(); } catch { }
            try { if (_writerTask != null) _writerTask.Wait(2000); } catch { }
        }

        private static readonly object _rotationSync = new object();
        private static volatile bool _rotateRequested = false;
        private static volatile bool _rotationComplete = true;

        public static string RotateLog()
        {
            lock (_rotationSync)
            {
                _rotationComplete = false;
                _rotateRequested = true;

                // Wait briefly for the writer loop to flush and release the file handle
                int waited = 0;
                while (_rotateRequested && waited < 1000)
                {
                    Thread.Sleep(25);
                    waited += 25;
                }

                string archiveName = string.Format("traffic_captured_round_{0:yyyyMMdd_HHmmss}.log", DateTime.Now);
                string archivePath = Path.Combine(Path.GetDirectoryName(_logPath) ?? AppPaths.BaseDir, archiveName);

                try
                {
                    if (File.Exists(_logPath))
                    {
                        File.Move(_logPath, archivePath);
                    }
                    return archiveName;
                }
                catch (Exception ex)
                {
                    return "Ошибка ротации: " + ex.Message;
                }
                finally
                {
                    _rotationComplete = true;
                }
            }
        }

        private static async Task WriterLoopAsync()
        {
            while (_isRunning || _logQueue.Count > 0)
            {
                bool delayNeeded = false;
                StreamWriter writer = null;
                FileStream fs = null;
                try
                {
                    string dir = Path.GetDirectoryName(_logPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    fs = new FileStream(_logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 65536, true);
                    writer = new StreamWriter(fs, Encoding.UTF8, 65536);

                    DateTime lastFlush = DateTime.UtcNow;
                    int batchCount = 0;

                    while (_isRunning || _logQueue.Count > 0)
                    {
                        if (_rotateRequested)
                        {
                            if (writer != null) { await writer.FlushAsync().ConfigureAwait(false); writer.Dispose(); writer = null; }
                            if (fs != null) { fs.Dispose(); fs = null; }
                            _rotateRequested = false;
                            while (!_rotationComplete && _isRunning)
                            {
                                await Task.Delay(25).ConfigureAwait(false);
                            }
                            break;
                        }

                        string item = await _logQueue.ReadAsync(150).ConfigureAwait(false);
                        bool taken = !string.IsNullOrEmpty(item);

                        if (taken)
                        {
                            await writer.WriteLineAsync(item).ConfigureAwait(false);
                            batchCount++;
                        }
                        else
                        {
                            if (batchCount > 0)
                            {
                                await writer.FlushAsync().ConfigureAwait(false);
                                batchCount = 0;
                                lastFlush = DateTime.UtcNow;
                            }
                        }

                        if (batchCount > 0 && ((DateTime.UtcNow - lastFlush).TotalMilliseconds >= 1000 || _logQueue.Count == 0))
                        {
                            await writer.FlushAsync().ConfigureAwait(false);
                            batchCount = 0;
                            lastFlush = DateTime.UtcNow;
                        }
                    }
                }
                catch (Exception)
                {
                    delayNeeded = true;
                }
                
                if (writer != null)
                {
                    try { writer.Flush(); } catch { }
                    try { writer.Dispose(); } catch { }
                }
                else if (fs != null)
                {
                    try { fs.Dispose(); } catch { }
                }
                
                if (delayNeeded)
                {
                    await Task.Delay(250).ConfigureAwait(false);
                }
            }
        }
    }
}
