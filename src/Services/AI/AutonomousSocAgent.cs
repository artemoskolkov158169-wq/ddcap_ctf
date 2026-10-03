using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DDCAP.AltayCTF
{
    public class AutonomousSocAgent
    {
        private readonly AsyncBoundedQueue<TrafficPacket> _queue = new AsyncBoundedQueue<TrafficPacket>(500);
        private volatile bool _isRunning;
        private AppConfig _config;

        public AutonomousSocAgent(AppConfig config)
        {
            _config = config;
        }

        public void Start()
        {
            if (_isRunning) return;
            _isRunning = true;
            Task.Run(new Func<Task>(ProcessQueueAsync));
        }

        public void Stop()
        {
            _isRunning = false;
            _queue.Complete();
        }

        public void EnqueueSuspiciousPacket(TrafficPacket packet)
        {
            if (!_isRunning) return;
            if (_queue.Count < 400)
            {
                _queue.TryWrite(packet);
            }
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (_isRunning || _queue.Count > 0)
                {
                    // Накапливаем контекст в течение 15 секунд (временное окно)
                    await Task.Delay(15000).ConfigureAwait(false);
                    
                    List<TrafficPacket> batch = new List<TrafficPacket>();
                    while (_queue.Count > 0)
                    {
                        var packet = await _queue.ReadAsync(100).ConfigureAwait(false);
                        if (packet != null) batch.Add(packet);
                    }

                    if (batch.Count > 0)
                    {
                        // Группируем по IP атакующего, чтобы нейросеть видела паттерн
                        var grouped = batch.GroupBy(p => p.SrcIp);
                        foreach (var group in grouped)
                        {
                            await AnalyzeBatchAsync(group.Key, group.ToList()).ConfigureAwait(false);
                            // Небольшая задержка между запросами к API, чтобы не ловить Rate Limit
                            await Task.Delay(5000).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private async Task AnalyzeBatchAsync(string ip, List<TrafficPacket> packets)
        {
            try
            {
                var profile = _config.GetAgentProfile("soc_analyst");
                if (profile == null || string.IsNullOrEmpty(profile.ApiKey))
                {
                    AsyncTrafficLogger.Log("[!] Autonomous SOC Agent пропущен: не настроен ключ API.");
                    return;
                }

                if (packets.Count == 1)
                {
                    var packet = packets[0];
                    string prompt = AltayProxySniffer.GenerateAiPrompt(
                        packet.Method, packet.Uri, packet.SrcIp, packet.VulnType, packet.DecodedPayload,
                        packet.ReqHeaders, packet.ReqBody, packet.ServiceName);

                    string response = await GoogleGeminiClient.GeneratePatchAsync(prompt, profile.ApiKey, profile.Model, profile.BaseUrl, "soc_analyst");
                    
                    packet.VulnerabilityAnalysis = response;
                    packet.CorrelationNotes += " [AI AUTOSCAN: COMPLETED]";

                    AsyncTrafficLogger.Log(string.Format("[*] AI AUTOSCAN завершен для IP {0} (1 пакет): {1}...", ip, response.Substring(0, Math.Min(response.Length, 60)).Replace("\n", " ")));
                }
                else
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("# ======================================================================================");
                    sb.AppendLine("# DDCAP ALTAY 2026 // AI MULTI-PACKET CONTEXT ANALYSIS");
                    sb.AppendLine("# ======================================================================================");
                    sb.AppendLine("Ты ведущий SOC-аналитик. Проанализируй серию подозрительных запросов от одного IP.");
                    sb.AppendLine("IP Атакующего: " + ip);
                    sb.AppendLine("Количество пакетов в окне (15 сек): " + packets.Count);
                    sb.AppendLine("\nСПИСОК ЗАПРОСОВ (сжато для экономии токенов):");
                    
                    foreach(var p in packets)
                    {
                        sb.AppendLine(string.Format("[{0}] {1} {2} | Уязвимость: {3}", p.Time, p.Method, p.Uri, p.VulnType));
                        if (!string.IsNullOrEmpty(p.DecodedPayload))
                        {
                            sb.AppendLine("Нагрузка: " + p.DecodedPayload.Substring(0, Math.Min(p.DecodedPayload.Length, 200)));
                        }
                    }
                    
                    sb.AppendLine("\nЗАДАЧА:");
                    sb.AppendLine("1. Выяви общий паттерн атаки (например, брутфорс, сканирование, IDOR, автоматизированный сбор флагов).");
                    sb.AppendLine("2. Напиши краткий вывод и рекомендации по защите на уровне бизнес-логики.");
                    sb.AppendLine("Отвечай лаконично и по делу, без воды.");

                    string response = await GoogleGeminiClient.GeneratePatchAsync(sb.ToString(), profile.ApiKey, profile.Model, profile.BaseUrl, "soc_analyst");
                    
                    foreach(var p in packets)
                    {
                        p.VulnerabilityAnalysis = "Групповой анализ для IP " + ip + ":\n" + response;
                        p.CorrelationNotes += " [AI BATCH AUTOSCAN: COMPLETED]";
                    }
                    AsyncTrafficLogger.Log(string.Format("[*] AI BATCH AUTOSCAN завершен для IP {0} ({1} пакетов): {2}...", ip, packets.Count, response.Substring(0, Math.Min(response.Length, 60)).Replace("\n", " ")));
                }
            }
            catch (Exception ex)
            {
                AsyncTrafficLogger.Log("[!] Ошибка Autonomous SOC Agent: " + ex.Message);
            }
        }
    }
}
