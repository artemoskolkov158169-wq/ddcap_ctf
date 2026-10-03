using System;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // APPLICATION CONFIGURATION
    // =========================================================================
    public class AppConfig
    {
        public int ListenPort { get; set; }
        public string TargetBaseUrl { get; set; }
        public string JuryIps { get; set; }
        public string TeamSubnetPattern { get; set; }
        public int MyTeamNumber { get; set; }
        public int TeamsCount { get; set; }
        public int FlagSubmitRps { get; set; }
        public string GeminiApiKey { get; set; }
        public string GeminiModel { get; set; }
        public string GeminiProxyUrl { get; set; }
        public string AiBaseUrl { get; set; }
        public string AiApiKey { get; set; }
        public string AiModel { get; set; }
        public string AiAgentRole { get; set; }
        public List<ServiceDefinition> Services { get; set; }
        public List<AgentProfile> AgentProfiles { get; set; }

        public AppConfig()
        {
            ListenPort = 8000;
            TargetBaseUrl = "http://127.0.0.1:5000";
            JuryIps = "127.0.0.1, 10.60.0.1, 10.0.2.2";
            TeamSubnetPattern = "10.60.{team}.2";
            MyTeamNumber = 5;
            TeamsCount = 15;
            FlagSubmitRps = 5;
            GeminiApiKey = "";
            GeminiModel = "openai/gpt-4o";
            GeminiProxyUrl = "";
            AiBaseUrl = "https://polza.ai/api/v1";
            AiApiKey = "";
            AiModel = "openai/gpt-4o";
            AiAgentRole = "blue_team";
            Services = new List<ServiceDefinition>
            {
                new ServiceDefinition { Name = "Web Shop", ListenPort = 8000, TargetUrl = "http://127.0.0.1:5000", PathPrefixes = "/api/v1/products, /checker/putflag", Enabled = true },
                new ServiceDefinition { Name = "Auth Service", ListenPort = 8001, TargetUrl = "http://127.0.0.1:5000", PathPrefixes = "/api/v1/auth", Enabled = true },
                new ServiceDefinition { Name = "Notes Service", ListenPort = 8002, TargetUrl = "http://127.0.0.1:5000", PathPrefixes = "/notes", Enabled = true },
                new ServiceDefinition { Name = "System Service", ListenPort = 8003, TargetUrl = "http://127.0.0.1:5000", PathPrefixes = "/service, /api/upload, /uploads, /api/debug", Enabled = true }
            };
            AgentProfiles = new List<AgentProfile>
            {
                new AgentProfile("blue_team", "", "x-ai/grok-4.7", "https://polza.ai/api/v1"),
                new AgentProfile("red_team", "", "openai/gpt-6-luna-pro", "https://polza.ai/api/v1"),
                new AgentProfile("soc_analyst", "", "deepseek/deepseek-v3.2-exp", "https://polza.ai/api/v1")
            };
        }

        public string GetEffectiveAiBaseUrl()
        {
            if (!string.IsNullOrWhiteSpace(AiBaseUrl))
                return AiBaseUrl.Trim();

            string env = Environment.GetEnvironmentVariable("AI_BASE_URL");
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();

            string envOpenAi = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
            if (!string.IsNullOrWhiteSpace(envOpenAi))
                return envOpenAi.Trim();

            try
            {
                string envPath = AppPaths.EnvFile;
                if (File.Exists(envPath))
                {
                    string[] lines = File.ReadAllLines(envPath);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("AI_BASE_URL=", StringComparison.OrdinalIgnoreCase))
                        {
                            string val = trimmed.Substring("AI_BASE_URL=".Length).Trim().Trim('"', '\'');
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                        if (trimmed.StartsWith("OPENAI_BASE_URL=", StringComparison.OrdinalIgnoreCase))
                        {
                            string val = trimmed.Substring("OPENAI_BASE_URL=".Length).Trim().Trim('"', '\'');
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                    }
                }
            }
            catch { }

            return "https://polza.ai/api/v1";
        }

        public string GetEffectiveApiKey(string guiInput = null)
        {
            // Priority 1: GUI input
            if (!string.IsNullOrWhiteSpace(guiInput))
                return guiInput.Trim();

            // Priority 2: Config properties
            if (!string.IsNullOrWhiteSpace(AiApiKey))
                return AiApiKey.Trim();

            if (!string.IsNullOrWhiteSpace(GeminiApiKey))
                return GeminiApiKey.Trim();

            // Priority 3: Environment Variables
            string envAi = Environment.GetEnvironmentVariable("AI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envAi))
                return envAi.Trim();

            string envPolza = Environment.GetEnvironmentVariable("POLZA_AI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envPolza))
                return envPolza.Trim();

            string envOpenAi = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envOpenAi))
                return envOpenAi.Trim();

            string envGemini = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (!string.IsNullOrWhiteSpace(envGemini))
                return envGemini.Trim();

            // Priority 4: .env file
            try
            {
                string envPath = AppPaths.EnvFile;
                if (File.Exists(envPath))
                {
                    string[] lines = File.ReadAllLines(envPath);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("AI_API_KEY=", StringComparison.OrdinalIgnoreCase))
                        {
                            string val = trimmed.Substring("AI_API_KEY=".Length).Trim().Trim('"', '\'');
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                        if (trimmed.StartsWith("POLZA_AI_API_KEY=", StringComparison.OrdinalIgnoreCase))
                        {
                            string val = trimmed.Substring("POLZA_AI_API_KEY=".Length).Trim().Trim('"', '\'');
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                        if (trimmed.StartsWith("OPENAI_API_KEY=", StringComparison.OrdinalIgnoreCase))
                        {
                            string val = trimmed.Substring("OPENAI_API_KEY=".Length).Trim().Trim('"', '\'');
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                        if (trimmed.StartsWith("GEMINI_API_KEY=", StringComparison.OrdinalIgnoreCase))
                        {
                            string val = trimmed.Substring("GEMINI_API_KEY=".Length).Trim().Trim('"', '\'');
                            if (!string.IsNullOrWhiteSpace(val)) return val;
                        }
                    }
                }
            }
            catch { }

            return "";
        }

        public string GetEffectiveModel()
        {
            if (!string.IsNullOrWhiteSpace(AiModel))
                return AiModel.Trim();
            if (!string.IsNullOrWhiteSpace(GeminiModel))
                return GeminiModel.Trim();
            return "google/gemini-2.5-flash";
        }

        public string GetEffectiveAiAgentRole()
        {
            if (!string.IsNullOrWhiteSpace(AiAgentRole))
                return AiAgentRole.Trim();
            return "blue_team";
        }

        public static string GetAiSystemPrompt(string role)
        {
            if (string.IsNullOrEmpty(role)) role = "blue_team";

            if (role == "soc_analyst")
            {
                return "Ты ведущий SOC-аналитик (Security Operations Center) команды DDCAP на соревновании AltayCTF (Attack-Defense). "
                    + "Твоя задача: мониторинг, обнаружение и классификация угроз в реальном времени. "
                    + "Анализируй сетевой трафик, выявляй индикаторы компрометации (IoC), определяй TTPs атакующих по MITRE ATT&CK. "
                    + "Классифицируй угрозы по критичности: CRITICAL / HIGH / MEDIUM / LOW / INFO. "
                    + "Указывай конкретные CVE, если возможно. Выдавай структурированный отчет с рекомендациями по реагированию. "
                    + "Формат ответа: 1) Тип атаки 2) Вектор 3) Критичность 4) IoC 5) Рекомендации по митигации.";
            }

            if (role == "red_team")
            {
                return "Ты ведущий пентестер и Red Team оператор команды DDCAP на соревновании AltayCTF (Attack-Defense). "
                    + "Твоя задача: анализ перехваченных атак противника и создание эксплойтов для массового применения по командам. "
                    + "На основе перехваченного трафика: 1) Реверси логику эксплойта 2) Адаптируй payload для массовой рассылки "
                    + "3) Напиши скрипт атаки (Python/curl/Go) с параметризацией IP-команд "
                    + "4) Укажи как извлечь флаг из ответа сервера. "
                    + "ВАЖНО: эксплойт должен быть надежным, с таймаутами и обработкой ошибок. "
                    + "Выдавай готовый к запуску код с шаблоном {TEAM_IP} для подстановки адресов.";
            }

            // blue_team (default)
            return "Ты ведущий Blue Team специалист и DevSecOps инженер команды DDCAP на соревновании AltayCTF (Attack-Defense). "
                + "Твоя цель: ликвидировать уязвимость в коде сервиса, строго соблюдая SLA. "
                + "КАТЕГОРИЧЕСКИ ЗАПРЕЩЕНО использовать WAF, блокировки по IP или HTTP 403 Forbidden. "
                + "Чекер жюри проверяет сервис (putflag/getflag) и должен получать корректные 200 OK ответы. "
                + "Выдавай минимальный, точный патч (Python, PHP, Go). "
                + "Используй строгую валидацию, параметризацию (Prepared Statements), санитизацию входных данных. "
                + "Формат: 1) Анализ уязвимости 2) БЫЛО -> СТАЛО (diff) 3) Почему чекер продолжит работать.";
        }

        public static string GetAiAgentRoleDisplayName(string role)
        {
            if (string.IsNullOrEmpty(role)) role = "blue_team";
            if (role == "soc_analyst") return "SOC Analyst (Monitoring)";
            if (role == "red_team") return "Red Team Pentester (Exploits)";
            return "Blue Team DevSecOps (Defense)";
        }

        public AgentProfile GetAgentProfile(string role)
        {
            if (string.IsNullOrEmpty(role)) role = "blue_team";
            if (AgentProfiles != null)
            {
                for (int i = 0; i < AgentProfiles.Count; i++)
                {
                    if (AgentProfiles[i] != null && AgentProfiles[i].Role == role)
                        return AgentProfiles[i];
                }
            }
            // Return a fallback profile using global settings
            return new AgentProfile(role, GetEffectiveApiKey(), GetEffectiveModel(), GetEffectiveAiBaseUrl());
        }

        public void SetAgentProfile(AgentProfile profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.Role)) return;
            if (AgentProfiles == null)
                AgentProfiles = new List<AgentProfile>();

            for (int i = 0; i < AgentProfiles.Count; i++)
            {
                if (AgentProfiles[i] != null && AgentProfiles[i].Role == profile.Role)
                {
                    AgentProfiles[i] = profile;
                    return;
                }
            }
            AgentProfiles.Add(profile);
        }

        public void EnsureAgentProfiles()
        {
            if (AgentProfiles == null)
                AgentProfiles = new List<AgentProfile>();

            string[] roles = new string[] { "blue_team", "red_team", "soc_analyst" };
            for (int r = 0; r < roles.Length; r++)
            {
                bool found = false;
                for (int i = 0; i < AgentProfiles.Count; i++)
                {
                    if (AgentProfiles[i] != null && AgentProfiles[i].Role == roles[r])
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    string defModel = "x-ai/grok-4.7";
                    if (roles[r] == "red_team") defModel = "openai/gpt-6-luna-pro";
                    if (roles[r] == "soc_analyst") defModel = "deepseek/deepseek-v3.2-exp";
                    AgentProfiles.Add(new AgentProfile(roles[r], "", defModel, ""));
                }
            }
        }

        public string GetEffectiveProxyUrl()
        {
            if (!string.IsNullOrWhiteSpace(GeminiProxyUrl))
                return GeminiProxyUrl.Trim();

            string env = Environment.GetEnvironmentVariable("GEMINI_PROXY");
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();

            string httpsProxy = Environment.GetEnvironmentVariable("HTTPS_PROXY");
            if (!string.IsNullOrWhiteSpace(httpsProxy))
                return httpsProxy.Trim();

            try
            {
                string envPath = AppPaths.EnvFile;
                if (File.Exists(envPath))
                {
                    string[] lines = File.ReadAllLines(envPath);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("GEMINI_PROXY=", StringComparison.OrdinalIgnoreCase))
                        {
                            string val = trimmed.Substring("GEMINI_PROXY=".Length).Trim().Trim('"', '\'');
                            if (!string.IsNullOrWhiteSpace(val))
                                return val;
                        }
                    }
                }
            }
            catch { }

            return "";
        }

        public string ResolveService(string uri, int localPort = 0)
        {
            if (Services != null)
            {
                if (localPort > 0)
                {
                    foreach (var svc in Services)
                    {
                        if (svc.Enabled && svc.ListenPort == localPort) return svc.Name;
                    }
                }

                foreach (var svc in Services)
                {
                    if (svc.Enabled && svc.MatchesUri(uri)) return svc.Name;
                }
            }
            return "Основной";
        }

        public string ResolveTargetUrl(string uri, int localPort = 0)
        {
            if (Services != null)
            {
                if (localPort > 0)
                {
                    foreach (var svc in Services)
                    {
                        if (svc.Enabled && svc.ListenPort == localPort && !string.IsNullOrEmpty(svc.TargetUrl))
                            return svc.TargetUrl;
                    }
                }

                foreach (var svc in Services)
                {
                    if (svc.Enabled && svc.MatchesUri(uri) && !string.IsNullOrEmpty(svc.TargetUrl))
                        return svc.TargetUrl;
                }
            }
            return TargetBaseUrl;
        }

        private HashSet<string> _juryIpSet;
        private static readonly object _saveLock = new object();

        public void RebuildJuryIpSet()
        {
            _juryIpSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(JuryIps))
            {
                string[] parts = JuryIps.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                    _juryIpSet.Add(parts[i].Trim());
            }
        }

        public bool IsJuryIp(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            if (_juryIpSet == null) RebuildJuryIpSet();
            return _juryIpSet.Contains(ip.Trim());
        }

        public static AppConfig Load()
        {
            try
            {
                string path = AppPaths.ConfigFile;
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var ser = new JavaScriptSerializer();
                    var cfg = ser.Deserialize<AppConfig>(json);
                    if (cfg != null && cfg.Services != null)
                    {
                        if (cfg.AiBaseUrl == null)
                            cfg.AiBaseUrl = "";
                        if (string.IsNullOrEmpty(cfg.AiAgentRole))
                            cfg.AiAgentRole = "blue_team";
                        if (string.IsNullOrEmpty(cfg.AiModel))
                            cfg.AiModel = !string.IsNullOrEmpty(cfg.GeminiModel) ? cfg.GeminiModel : "google/gemini-2.5-flash";
                        if (cfg.AiApiKey == null)
                            cfg.AiApiKey = cfg.GeminiApiKey ?? "";
                        if (string.IsNullOrEmpty(cfg.GeminiModel))
                            cfg.GeminiModel = cfg.AiModel;
                        if (cfg.ListenPort <= 0)
                            cfg.ListenPort = (cfg.Services.Count > 0 && cfg.Services[0].ListenPort > 0) ? cfg.Services[0].ListenPort : 8000;
                        if (cfg.FlagSubmitRps <= 0)
                            cfg.FlagSubmitRps = 5;
                        if (cfg.GeminiApiKey == null)
                            cfg.GeminiApiKey = "";
                        if (cfg.GeminiProxyUrl == null)
                            cfg.GeminiProxyUrl = "";
                        for (int i = 0; i < cfg.Services.Count; i++)
                        {
                            var s = cfg.Services[i];
                            if (s.ListenPort <= 0)
                            {
                                s.ListenPort = cfg.ListenPort + i;
                            }
                        }
                        cfg.EnsureAgentProfiles();
                        cfg.RebuildJuryIpSet();
                        cfg.AutoConfigureFromNetwork();
                        return cfg;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[DDCAP] AppConfig.Load failed: " + ex.Message);
            }
            var defCfg = new AppConfig();
            defCfg.RebuildJuryIpSet();
            defCfg.AutoConfigureFromNetwork();
            return defCfg;
        }

        public void AutoConfigureFromNetwork()
        {
            try
            {
                string localIp = null;
                using (var s = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0))
                {
                    s.Connect("8.8.8.8", 80);
                    localIp = (s.LocalEndPoint as System.Net.IPEndPoint)?.Address.ToString();
                }

                if (!string.IsNullOrEmpty(localIp))
                {
                    string envPath = AppPaths.EnvFile;
                    string[] lines = new string[0];
                    if (File.Exists(envPath)) lines = File.ReadAllLines(envPath);
                    
                    bool foundIp = false;
                    var newLines = new List<string>();
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("DDCAP_LOCAL_IP=")) { newLines.Add("DDCAP_LOCAL_IP=" + localIp); foundIp = true; }
                        else { newLines.Add(line); }
                    }
                    if (!foundIp) newLines.Add("DDCAP_LOCAL_IP=" + localIp);
                    File.WriteAllLines(envPath, newLines);

                    if (!string.IsNullOrEmpty(TeamSubnetPattern) && TeamSubnetPattern.Contains("{team}"))
                    {
                        string regexPattern = "^" + TeamSubnetPattern.Replace(".", @"\.").Replace("{team}", @"(\d+)") + "$";
                        var match = System.Text.RegularExpressions.Regex.Match(localIp, regexPattern);
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int teamId))
                        {
                            MyTeamNumber = teamId;
                        }
                        else
                        {
                            var fallbackMatch = System.Text.RegularExpressions.Regex.Match(localIp, @"^10\.\d+\.(\d+)\.\d+$");
                            if (fallbackMatch.Success && int.TryParse(fallbackMatch.Groups[1].Value, out int fallbackTeamId))
                            {
                                MyTeamNumber = fallbackTeamId;
                            }
                        }
                    }
                }
            }
            catch { }
        }

        public void Save()
        {
            lock (_saveLock)
            {
                try
                {
                    RebuildJuryIpSet();
                    string path = AppPaths.ConfigFile;
                    string tmpPath = path + ".tmp";
                    var ser = new JavaScriptSerializer();
                    string json = ser.Serialize(this);
                    File.WriteAllText(tmpPath, json);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    File.Move(tmpPath, path);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("[DDCAP] AppConfig.Save failed: " + ex.Message);
                }
            }
        }
    }
}
