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
using System.Diagnostics;
using System.Web.Script.Serialization;

namespace DDCAP.AltayCTF
{
    // =========================================================================
    // AI AGGREGATOR CLIENT (OPENAI-COMPATIBLE / OPENROUTER / PROXY-API / GEMINI)
    // =========================================================================
    public static class GoogleGeminiClient
    {
        public static async Task<string> GeneratePatchAsync(string prompt, string apiKey, string model, string baseUrl = null, string agentRole = null)
        {
            var cfg = AppConfig.Load();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = cfg != null ? cfg.GetEffectiveApiKey() : "";
            }
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API Key агрегатора не задан. Укажите ключ в Настройках или через .env (AI_API_KEY).", "apiKey");
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                model = cfg != null ? cfg.GetEffectiveModel() : "google/gemini-2.5-flash";
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = cfg != null ? cfg.GetEffectiveAiBaseUrl() : "https://polza.ai/api/v1";
            }

            if (string.IsNullOrWhiteSpace(agentRole))
            {
                agentRole = cfg != null ? cfg.GetEffectiveAiAgentRole() : "blue_team";
            }
            string systemPrompt = AppConfig.GetAiSystemPrompt(agentRole);

            string cleanBase = baseUrl.Trim().TrimEnd('/');
            bool isGeminiNative = cleanBase.Contains("generativelanguage.googleapis.com") || cleanBase.Contains(":generateContent");

            string endpoint;
            if (isGeminiNative)
            {
                string cleanModel = model.Trim();
                if (cleanModel.StartsWith("models/")) cleanModel = cleanModel.Substring(7);
                endpoint = string.Format("{0}/models/{1}:generateContent?key={2}", cleanBase, cleanModel, Uri.EscapeDataString(apiKey.Trim()));
            }
            else
            {
                endpoint = cleanBase.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) 
                    ? cleanBase 
                    : cleanBase + "/chat/completions";
            }

            string jsonBody;
            var serializer = new JavaScriptSerializer();

            if (isGeminiNative)
            {
                var geminiReq = new Dictionary<string, object>
                {
                    {
                        "systemInstruction", new Dictionary<string, object>
                        {
                            {
                                "parts", new object[]
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "text", systemPrompt }
                                    }
                                }
                            }
                        }
                    },
                    {
                        "contents", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "parts", new object[] { new Dictionary<string, object> { { "text", prompt ?? "" } } } }
                            }
                        }
                    },
                    {
                        "generationConfig", new Dictionary<string, object>
                        {
                            { "temperature", 0.1 },
                            { "maxOutputTokens", 2048 }
                        }
                    }
                };
                jsonBody = serializer.Serialize(geminiReq);
            }
            else
            {
                var openAiReq = new Dictionary<string, object>
                {
                    { "model", model.Trim() },
                    {
                        "messages", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "role", "system" },
                                { "content", systemPrompt }
                            },
                            new Dictionary<string, object>
                            {
                                { "role", "user" },
                                { "content", prompt ?? "" }
                            }
                        }
                    },
                    { "temperature", 0.1 },
                    { "max_tokens", 2048 }
                };
                jsonBody = serializer.Serialize(openAiReq);
            }

            HttpClient client = SharedHttpClient.Instance;
            using (var req = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                if (!isGeminiNative)
                {
                    req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey.Trim());
                    req.Headers.TryAddWithoutValidation("HTTP-Referer", "https://ddcap.altayctf");
                    req.Headers.TryAddWithoutValidation("X-Title", "DDCAP Defense Matrix");
                }

                req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                HttpResponseMessage response = null;
                try
                {
                    response = await client.SendAsync(req);
                }
                catch (Exception reqEx)
                {
                    throw new Exception("Ошибка подключения к AI агрегатору (" + endpoint + "): " + reqEx.Message, reqEx);
                }

                using (response)
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        string parsedErr = ExtractApiErrorMessage(responseText);
                        if (!string.IsNullOrEmpty(parsedErr))
                        {
                            throw new Exception(string.Format("HTTP {0}: {1}", (int)response.StatusCode, parsedErr));
                        }
                        throw new Exception(string.Format("HTTP {0}: {1}", (int)response.StatusCode, responseText));
                    }

                    string resultText = "";
                    try
                    {
                        var root = serializer.Deserialize<Dictionary<string, object>>(responseText);
                        
                        // 1. OpenAI-compatible format: choices[0].message.content
                        if (root != null && root.ContainsKey("choices"))
                        {
                            var choices = root["choices"] as System.Collections.IList;
                            if (choices != null && choices.Count > 0)
                            {
                                var firstChoice = choices[0] as Dictionary<string, object>;
                                if (firstChoice != null && firstChoice.ContainsKey("message"))
                                {
                                    var msg = firstChoice["message"] as Dictionary<string, object>;
                                    if (msg != null && msg.ContainsKey("content"))
                                    {
                                        object cVal = msg["content"];
                                        if (cVal is string)
                                        {
                                            resultText = (string)cVal;
                                        }
                                    }
                                }
                            }
                        }

                        // 2. Gemini native format: candidates[0].content.parts[0].text
                        if (string.IsNullOrEmpty(resultText) && root != null && root.ContainsKey("candidates"))
                        {
                            var candidates = root["candidates"] as System.Collections.IList;
                            if (candidates != null && candidates.Count > 0)
                            {
                                var firstCand = candidates[0] as Dictionary<string, object>;
                                if (firstCand != null && firstCand.ContainsKey("content"))
                                {
                                    var contentDict = firstCand["content"] as Dictionary<string, object>;
                                    if (contentDict != null && contentDict.ContainsKey("parts"))
                                    {
                                        var parts = contentDict["parts"] as System.Collections.IList;
                                        if (parts != null && parts.Count > 0)
                                        {
                                            for (int pi = parts.Count - 1; pi >= 0; pi--)
                                            {
                                                var part = parts[pi] as Dictionary<string, object>;
                                                if (part == null) continue;
                                                object thoughtVal;
                                                if (part.TryGetValue("thought", out thoughtVal) && thoughtVal is bool && (bool)thoughtVal)
                                                    continue;
                                                object textVal;
                                                if (part.TryGetValue("text", out textVal) && textVal is string)
                                                {
                                                    resultText = (string)textVal;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        resultText = "";
                    }

                    // Fallback regex parsing
                    if (string.IsNullOrEmpty(resultText))
                    {
                        Match match = Regex.Match(responseText, "\"content\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Singleline);
                        if (!match.Success)
                        {
                            match = Regex.Match(responseText, "\"text\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Singleline);
                        }
                        if (match.Success)
                        {
                            resultText = UnescapeJsonString(match.Groups[1].Value);
                        }
                    }

                    if (string.IsNullOrEmpty(resultText))
                    {
                        throw new Exception("Ответ от AI агрегатора получен, но текст ответа не найден в JSON структуре:\n" + responseText);
                    }

                    return resultText;
                }
            }
        }

        public static async Task<string> TestConnectionAsync(string apiKey, string model, string baseUrl = null)
        {
            var sw = Stopwatch.StartNew();
            string reply = await GeneratePatchAsync("DDCAP Health Check ping. Respond with 'DDCAP OK'.", apiKey, model, baseUrl, null);
            sw.Stop();
            string clean = reply.Trim().Replace("\r", " ").Replace("\n", " ");
            if (clean.Length > 80) clean = clean.Substring(0, 80) + "...";
            return string.Format("Подключение к агрегатору успешно (RTT: {0} ms): {1}", sw.ElapsedMilliseconds, clean);
        }

        public static async Task<List<string>> FetchModelsAsync(string apiKey, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "https://polza.ai/api/v1";
            string endpoint = baseUrl.Trim().TrimEnd('/') + "/models";

            HttpClient client = SharedHttpClient.Instance;
            using (var req = new HttpRequestMessage(HttpMethod.Get, endpoint))
            {
                if (!string.IsNullOrWhiteSpace(apiKey))
                    req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey.Trim());

                var response = await client.SendAsync(req);
                if (!response.IsSuccessStatusCode)
                    return new List<string>();

                string json = await response.Content.ReadAsStringAsync();
                var ser = new JavaScriptSerializer();
                var root = ser.Deserialize<Dictionary<string, object>>(json);
                
                var result = new List<string>();
                if (root != null && root.ContainsKey("data"))
                {
                    var data = root["data"] as System.Collections.IList;
                    if (data != null)
                    {
                        foreach (Dictionary<string, object> model in data)
                        {
                            string id = model.ContainsKey("id") ? Convert.ToString(model["id"]) : "";
                            string name = model.ContainsKey("name") ? Convert.ToString(model["name"]) : id;
                            string priceStr = "";
                            
                            if (model.ContainsKey("top_provider"))
                            {
                                var provider = model["top_provider"] as Dictionary<string, object>;
                                if (provider != null && provider.ContainsKey("pricing"))
                                {
                                    var pricing = provider["pricing"] as Dictionary<string, object>;
                                    if (pricing != null)
                                    {
                                        string pPrice = pricing.ContainsKey("prompt_per_million") ? Convert.ToString(pricing["prompt_per_million"]) : "0";
                                        string cPrice = pricing.ContainsKey("completion_per_million") ? Convert.ToString(pricing["completion_per_million"]) : "0";
                                        string curr = pricing.ContainsKey("currency") ? Convert.ToString(pricing["currency"]) : "RUB";
                                        priceStr = string.Format(" [{0} / {1} {2} per 1M]", pPrice, cPrice, curr);
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(id))
                            {
                                result.Add(id + priceStr);
                            }
                        }
                    }
                }
                return result;
            }
        }

        private static string UnescapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\/", "/")
                    .Replace("\\\\", "\\");
        }

        private static string ExtractApiErrorMessage(string responseJson)
        {
            if (string.IsNullOrEmpty(responseJson)) return "";
            try
            {
                var ser = new JavaScriptSerializer();
                var root = ser.Deserialize<Dictionary<string, object>>(responseJson);
                if (root != null && root.ContainsKey("error"))
                {
                    object errObj = root["error"];
                    if (errObj is string) return (string)errObj;
                    var errDict = errObj as Dictionary<string, object>;
                    if (errDict != null)
                    {
                        string msg = errDict.ContainsKey("message") ? Convert.ToString(errDict["message"]) : "";
                        string status = errDict.ContainsKey("code") ? Convert.ToString(errDict["code"]) : "";
                        if (!string.IsNullOrEmpty(status) && !string.IsNullOrEmpty(msg))
                        {
                            return string.Format("{0} (Code: {1})", msg, status);
                        }
                        if (!string.IsNullOrEmpty(msg)) return msg;
                        if (!string.IsNullOrEmpty(status)) return status;
                    }
                }
            }
            catch { }

            string regexMsg = "";
            Match mMsg = Regex.Match(responseJson, "\"message\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            if (mMsg.Success) regexMsg = UnescapeJsonString(mMsg.Groups[1].Value);

            if (!string.IsNullOrEmpty(regexMsg)) return regexMsg;

            return "";
        }
    }

    public static class AiClient
    {
        public static Task<string> GeneratePatchAsync(string prompt, string apiKey, string model, string baseUrl = null, string agentRole = null)
        {
            return GoogleGeminiClient.GeneratePatchAsync(prompt, apiKey, model, baseUrl, agentRole);
        }
        public static Task<string> TestConnectionAsync(string apiKey, string model, string baseUrl = null)
        {
            return GoogleGeminiClient.TestConnectionAsync(apiKey, model, baseUrl);
        }
        public static Task<List<string>> FetchModelsAsync(string apiKey, string baseUrl)
        {
            return GoogleGeminiClient.FetchModelsAsync(apiKey, baseUrl);
        }
    }
}
