using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Media;
using System.Windows.Threading;

namespace DDCAP.AltayCTF
{
    public class TrafficManager
    {
        private readonly MainViewModel _viewModel;
        private readonly ConcurrentQueue<TrafficPacket> _incomingQueue = new ConcurrentQueue<TrafficPacket>();
        private DispatcherTimer _uiBatchTimer;
        
        private const int MAX_IN_MEMORY_PACKETS = 10000;
        private const int MAX_FILTERED_PACKETS = 5000;
        private const int UI_BATCH_LIMIT_PER_TICK = 150;

        private DateTime _lastAlertSoundTime = DateTime.MinValue;

        public event Action<TrafficPacket> OnThreatDetected;
        public event Action<bool> OnBatchProcessed; // true if any filtered packets were added

        public TrafficManager(MainViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Start()
        {
            if (_uiBatchTimer == null)
            {
                _uiBatchTimer = new DispatcherTimer(DispatcherPriority.Normal);
                _uiBatchTimer.Interval = TimeSpan.FromMilliseconds(100);
                _uiBatchTimer.Tick += ProcessIncomingPacketsBatch;
            }
            _uiBatchTimer.Start();
        }

        public void Stop()
        {
            if (_uiBatchTimer != null) _uiBatchTimer.Stop();
        }

        public void EnqueuePacket(TrafficPacket packet)
        {
            if (packet == null) return;
            // Cap queue at 50,000 to prevent unbounded RAM growth under severe DDoS
            if (_incomingQueue.Count < 50000)
            {
                _incomingQueue.Enqueue(packet);
            }
        }

        private void ProcessIncomingPacketsBatch(object sender, EventArgs e)
        {
            try
            {
                if (_incomingQueue.IsEmpty) return;

                // UI Dispatch Policy (Strict Drop): 
                // Drop UI rendering updates if background queue spikes to prevent OOM
                if (_incomingQueue.Count > 1000)
                {
                    TrafficPacket dropped;
                    while (_incomingQueue.Count > UI_BATCH_LIMIT_PER_TICK)
                    {
                        if (_incomingQueue.TryDequeue(out dropped))
                        {
                            StatePersistence.SaveTraffic(dropped);
                        }
                    }
                }

                int processedCount = 0;
                bool anyAddedToFiltered = false;
                TrafficPacket p;

                while (processedCount < UI_BATCH_LIMIT_PER_TICK && _incomingQueue.TryDequeue(out p))
                {
                    _viewModel.AllPackets.Add(p);
                    _viewModel.TotalPacketsCount++;
                    StatePersistence.SaveTraffic(p);

                    if (p.TrafficType == "ЧЕКЕР")
                    {
                        _viewModel.TotalCheckerCount++;
                    }
                    else if (p.TrafficType == "АТАКА" || p.TrafficType == "НОВЫЙ ВЕКТОР" || p.TrafficType == "УТЕЧКА" || p.TrafficType == "ЦЕПОЧКА" || p.TrafficType == "0-DAY")
                    {
                        _viewModel.TotalAttacksCount++;
                        if (OnThreatDetected != null) OnThreatDetected(p);

                        // Sound alert for critical events (max 1 per second to avoid spam)
                        if ((DateTime.Now - _lastAlertSoundTime).TotalMilliseconds > 1000)
                        {
                            if (p.TrafficType == "УТЕЧКА")
                            {
                                try { SystemSounds.Hand.Play(); } catch { }
                                _lastAlertSoundTime = DateTime.Now;
                            }
                            else if (p.TrafficType == "НОВЫЙ ВЕКТОР" || p.TrafficType == "ЦЕПОЧКА")
                            {
                                try { SystemSounds.Exclamation.Play(); } catch { }
                                _lastAlertSoundTime = DateTime.Now;
                            }
                        }
                    }

                    // We use an event instead of direct filter logic since filter logic might depend on UI state or ViewModel properties.
                    // Actually, moving MatchesFilter logic to ViewModel is better.
                    if (_viewModel.MatchesFilter(p))
                    {
                        _viewModel.FilteredPackets.Add(p);
                        anyAddedToFiltered = true;
                    }

                    processedCount++;
                }

                // Ring buffer memory cap: prevent Out Of Memory by bulk pruning oldest records
                int excessAll = _viewModel.AllPackets.Count - MAX_IN_MEMORY_PACKETS;
                if (excessAll > 0)
                {
                    var trimmed = new List<TrafficPacket>(_viewModel.AllPackets.Count - excessAll);
                    for (int i = excessAll; i < _viewModel.AllPackets.Count; i++)
                        trimmed.Add(_viewModel.AllPackets[i]);
                    _viewModel.AllPackets.Clear();
                    for (int i = 0; i < trimmed.Count; i++)
                        _viewModel.AllPackets.Add(trimmed[i]);
                }

                int excessFiltered = _viewModel.FilteredPackets.Count - MAX_FILTERED_PACKETS;
                if (excessFiltered > 0)
                {
                    var trimmed = new List<TrafficPacket>(_viewModel.FilteredPackets.Count - excessFiltered);
                    for (int i = excessFiltered; i < _viewModel.FilteredPackets.Count; i++)
                        trimmed.Add(_viewModel.FilteredPackets[i]);
                    _viewModel.FilteredPackets.Clear();
                    for (int i = 0; i < trimmed.Count; i++)
                        _viewModel.FilteredPackets.Add(trimmed[i]);
                }

                if (processedCount > 0)
                {
                    if (OnBatchProcessed != null) OnBatchProcessed(anyAddedToFiltered);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BatchTimer error: " + ex.Message);
            }
        }
    }
}
