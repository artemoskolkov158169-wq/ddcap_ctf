using System;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // PER-AGENT API PROFILE
    // Each agent role has its own independent API key, model, and base URL.
    // =========================================================================
    public class AgentProfile
    {
        public string Role { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
        public string BaseUrl { get; set; }

        public AgentProfile()
        {
            Role = "blue_team";
            ApiKey = "";
            Model = "";
            BaseUrl = "";
        }

        public AgentProfile(string role, string apiKey, string model, string baseUrl)
        {
            Role = role ?? "blue_team";
            ApiKey = apiKey ?? "";
            Model = model ?? "";
            BaseUrl = baseUrl ?? "";
        }

        public string GetDisplayName()
        {
            if (string.IsNullOrEmpty(Role)) return "Blue Team DevSecOps (Defense)";
            if (Role == "soc_analyst") return "SOC Analyst (Monitoring)";
            if (Role == "red_team") return "Red Team Pentester (Attack)";
            return "Blue Team DevSecOps (Defense)";
        }

        public string GetShortName()
        {
            if (string.IsNullOrEmpty(Role)) return "DEFENSE";
            if (Role == "soc_analyst") return "MONITOR";
            if (Role == "red_team") return "ATTACK";
            return "DEFENSE";
        }

        public string GetEffectiveApiKey(AppConfig fallbackCfg)
        {
            if (!string.IsNullOrWhiteSpace(ApiKey))
                return ApiKey.Trim();

            // Fallback to global config
            if (fallbackCfg != null)
                return fallbackCfg.GetEffectiveApiKey();

            return "";
        }

        public string GetEffectiveModel()
        {
            if (!string.IsNullOrWhiteSpace(Model))
                return Model.Trim();
            return "google/gemini-2.5-flash";
        }

        public string GetEffectiveBaseUrl(AppConfig fallbackCfg)
        {
            if (!string.IsNullOrWhiteSpace(BaseUrl))
                return BaseUrl.Trim();

            // Fallback to global config
            if (fallbackCfg != null)
                return fallbackCfg.GetEffectiveAiBaseUrl();

            return "https://polza.ai/api/v1";
        }

        public AgentProfile Clone()
        {
            return new AgentProfile(Role, ApiKey, Model, BaseUrl);
        }
    }
}
