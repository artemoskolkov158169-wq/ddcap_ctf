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

namespace DDCAP.AltayCTF
{
    public class JuryFlagSubmitter
    {
        public static async Task<string> SubmitFlagAsync(string submitUrl, string token, string flag)
        {
            if (string.IsNullOrEmpty(submitUrl) || string.IsNullOrEmpty(flag)) return "Ошибка: пустой URL или флаг";
            try
            {
                HttpClient client = SharedHttpClient.Instance;
                using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Post, submitUrl))
                {
                    if (!string.IsNullOrEmpty(token))
                    {
                        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            req.Headers.TryAddWithoutValidation("Authorization", token);
                        else
                            req.Headers.TryAddWithoutValidation("X-Team-Token", token);
                    }

                    string json;
                    if (submitUrl.IndexOf("format=array", StringComparison.OrdinalIgnoreCase) >= 0)
                        json = new JavaScriptSerializer().Serialize(new[] { new { flag = flag } });
                    else if (submitUrl.IndexOf("format=raw", StringComparison.OrdinalIgnoreCase) >= 0)
                        json = "\"" + flag + "\"";
                    else
                        json = new JavaScriptSerializer().Serialize(new { flag = flag });

                    using (StringContent sc = new StringContent(json, Encoding.UTF8, "application/json"))
                    {
                        req.Content = sc;

                        using (HttpResponseMessage res = await client.SendAsync(req))
                        {
                            string reply = await res.Content.ReadAsStringAsync();
                            return string.Format("[{0}] {1}", (int)res.StatusCode, reply.Length > 80 ? reply.Substring(0, 80) : reply);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return "Ошибка отправки: " + ex.Message;
            }
        }
    }
}
