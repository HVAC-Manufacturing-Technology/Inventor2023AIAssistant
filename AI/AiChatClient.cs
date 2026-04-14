using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Inventor2023AIAssistant
{
    public class ChatMessage
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class AiChatClient
    {
        private readonly string _baseUrl =
            "https://api.groq.com/openai/v1";

        // ─── FIX 3: Fallback model list ───────────────────────────────
        // If primary model hits rate limit (429),
        // automatically tries the next model in the list
        private readonly string[] _models = new[]
        {
            "llama-3.3-70b-versatile",   // Primary
            "llama-3.1-70b-versatile",   // Fallback 1
            "llama-3.1-8b-instant",      // Fallback 2
            "gemma2-9b-it",              // Fallback 3
            "mixtral-8x7b-32768",        // Fallback 4
        };

        public AiChatClient() { }

        private string GetApiKey()
        {
            return System.Environment.GetEnvironmentVariable(
                "GROQ_API_KEY");
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(GetApiKey());
        }

        public async Task<bool> CheckConnectionAsync()
        {
            if (!IsConfigured()) return false;
            try
            {
                var testMessages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        role = "user",
                        content =
                            "Reply with the single word: connected"
                    }
                };
                string result =
                    await GetResponseAsync(testMessages);
                return
                    !string.IsNullOrWhiteSpace(result) &&
                    !result.StartsWith("Groq request failed") &&
                    !result.StartsWith("Failed to parse") &&
                    !result.StartsWith("Groq is not configured") &&
                    !result.StartsWith("Parse issue") &&
                    !result.StartsWith("All models rate limited");
            }
            catch { return false; }
        }

        // ─── Non-streaming with fallback ──────────────────────────────

        public async Task<string> GetResponseAsync(
            List<ChatMessage> messages)
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
                return
                    "Groq is not configured. " +
                    "Set the GROQ_API_KEY environment " +
                    "variable and restart Inventor.";

            string url = _baseUrl + "/chat/completions";
            var serializer = new JavaScriptSerializer();

            var messageList =
                new List<Dictionary<string, string>>();
            foreach (var msg in messages)
                messageList.Add(
                    new Dictionary<string, string>
                    {
                        { "role",    msg.role    ?? "user" },
                        { "content", msg.content ?? ""     }
                    });

            string lastError = "";

            // Try each model in order until one succeeds
            foreach (string model in _models)
            {
                var requestBody =
                    new Dictionary<string, object>
                    {
                        { "model",       model       },
                        { "messages",    messageList },
                        { "temperature", 0.2         },
                        { "max_tokens",  800         }
                    };

                string json =
                    serializer.Serialize(requestBody);

                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout =
                            System.TimeSpan.FromSeconds(30);
                        client.DefaultRequestHeaders.Add(
                            "Authorization",
                            "Bearer " + apiKey);

                        using (var body = new StringContent(
                            json, Encoding.UTF8,
                            "application/json"))
                        {
                            HttpResponseMessage httpResp =
                                await client.PostAsync(
                                    url, body);

                            string responseText =
                                await httpResp.Content
                                    .ReadAsStringAsync();

                            // 429 = rate limited,
                            // try next model
                            if ((int)httpResp.StatusCode
                                == 429)
                            {
                                lastError =
                                    "Rate limited on " +
                                    model;
                                continue;
                            }

                            if (!httpResp.IsSuccessStatusCode)
                            {
                                lastError =
                                    "Groq request failed (" +
                                    (int)httpResp.StatusCode +
                                    "): " + responseText;
                                continue;
                            }

                            return ExtractContentFromJson(
                                responseText);
                        }
                    }
                }
                catch (Exception ex)
                {
                    lastError =
                        "Exception on " + model +
                        ": " + ex.Message;
                    continue;
                }
            }

            // All models exhausted
            return
                "All models rate limited or unavailable. " +
                "Last error: " + lastError +
                System.Environment.NewLine +
                "Please wait a few minutes and try again.";
        }

        // ─── Streaming with fallback ──────────────────────────────────

        public async Task GetStreamingResponseAsync(
            List<ChatMessage> messages,
            Action<string> onToken,
            Action<string> onComplete,
            Action<string> onError)
        {
            string apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                onError?.Invoke(
                    "Groq is not configured. " +
                    "Set the GROQ_API_KEY environment variable.");
                return;
            }

            string url = _baseUrl + "/chat/completions";
            var serializer = new JavaScriptSerializer();

            var messageList =
                new List<Dictionary<string, string>>();
            foreach (var msg in messages)
                messageList.Add(
                    new Dictionary<string, string>
                    {
                        { "role",    msg.role    ?? "user" },
                        { "content", msg.content ?? ""     }
                    });

            string lastError = "";

            // Try each model in order until one succeeds
            foreach (string model in _models)
            {
                var requestBody =
                    new Dictionary<string, object>
                    {
                        { "model",       model       },
                        { "messages",    messageList },
                        { "temperature", 0.2         },
                        { "max_tokens",  800         },
                        { "stream",      true        }
                    };

                string json =
                    serializer.Serialize(requestBody);

                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout =
                            System.TimeSpan.FromSeconds(60);
                        client.DefaultRequestHeaders.Add(
                            "Authorization",
                            "Bearer " + apiKey);

                        var request =
                            new HttpRequestMessage(
                                HttpMethod.Post, url);
                        request.Content =
                            new StringContent(
                                json, Encoding.UTF8,
                                "application/json");

                        var response =
                            await client.SendAsync(
                                request,
                                HttpCompletionOption
                                    .ResponseHeadersRead);

                        // 429 = rate limited,
                        // try next model silently
                        if ((int)response.StatusCode == 429)
                        {
                            lastError =
                                "Rate limited on " + model;
                            continue;
                        }

                        if (!response.IsSuccessStatusCode)
                        {
                            string err =
                                await response.Content
                                    .ReadAsStringAsync();
                            lastError =
                                "Groq streaming failed (" +
                                (int)response.StatusCode +
                                "): " + err;
                            continue;
                        }

                        var fullResponse =
                            new System.Text.StringBuilder();

                        using (var stream =
                            await response.Content
                                .ReadAsStreamAsync())
                        using (var reader =
                            new StreamReader(stream))
                        {
                            while (!reader.EndOfStream)
                            {
                                string line =
                                    await reader
                                        .ReadLineAsync();

                                if (string.IsNullOrWhiteSpace(
                                        line)) continue;
                                if (!line.StartsWith(
                                        "data: ")) continue;

                                string data =
                                    line.Substring(6).Trim();
                                if (data == "[DONE]") break;

                                string token =
                                    ExtractTokenFromChunk(
                                        data);

                                if (!string.IsNullOrEmpty(
                                        token))
                                {
                                    fullResponse.Append(token);
                                    onToken?.Invoke(token);
                                }
                            }
                        }

                        onComplete?.Invoke(
                            fullResponse.ToString());
                        return; // Success — stop trying
                    }
                }
                catch (Exception ex)
                {
                    lastError =
                        "Exception on " + model +
                        ": " + ex.Message;
                    continue;
                }
            }

            // All models exhausted
            onError?.Invoke(
                "All models rate limited or unavailable. " +
                "Last error: " + lastError +
                System.Environment.NewLine +
                "Please wait a few minutes and try again.");
        }

        // ─── Parsers ──────────────────────────────────────────────────

        private string ExtractTokenFromChunk(string json)
        {
            try
            {
                string searchKey = "\"content\":\"";
                int startIndex = json.IndexOf(searchKey);
                if (startIndex == -1) return string.Empty;
                startIndex += searchKey.Length;
                var sb = new System.Text.StringBuilder();
                int i = startIndex;
                while (i < json.Length)
                {
                    char c = json[i];
                    if (c == '\\' && i + 1 < json.Length)
                    {
                        char next = json[i + 1];
                        switch (next)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            default: sb.Append(next); break;
                        }
                        i += 2; continue;
                    }
                    if (c == '"') break;
                    sb.Append(c); i++;
                }
                return sb.ToString();
            }
            catch { return string.Empty; }
        }

        private string ExtractContentFromJson(string json)
        {
            try
            {
                string searchKey = "\"content\":\"";
                int startIndex = json.IndexOf(searchKey);
                if (startIndex == -1)
                {
                    if (json.Contains("\"content\":null"))
                        return
                            "The assistant returned " +
                            "no content.";
                    return
                        "Could not find content " +
                        "in response.";
                }
                startIndex += searchKey.Length;
                var sb = new System.Text.StringBuilder();
                int i = startIndex;
                while (i < json.Length)
                {
                    char c = json[i];
                    if (c == '\\' && i + 1 < json.Length)
                    {
                        char next = json[i + 1];
                        switch (next)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            default: sb.Append(next); break;
                        }
                        i += 2; continue;
                    }
                    if (c == '"') break;
                    sb.Append(c); i++;
                }
                string result = sb.ToString().Trim();
                if (string.IsNullOrWhiteSpace(result))
                    return
                        "The assistant returned " +
                        "an empty response.";
                return result;
            }
            catch (Exception ex)
            {
                return
                    "Failed to extract response: " +
                    ex.Message;
            }
        }
    }
}