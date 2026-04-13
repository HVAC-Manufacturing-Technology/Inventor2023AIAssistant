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

        private readonly string _model =
            "llama-3.3-70b-versatile";

        public AiChatClient()
        {
        }

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
            if (!IsConfigured())
                return false;

            try
            {
                var testMessages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        role = "user",
                        content = "Reply with the single word: connected"
                    }
                };

                string result = await GetResponseAsync(testMessages);

                return
                    !string.IsNullOrWhiteSpace(result) &&
                    !result.StartsWith("Groq request failed") &&
                    !result.StartsWith("Failed to parse") &&
                    !result.StartsWith("Groq is not configured") &&
                    !result.StartsWith("Parse issue");
            }
            catch
            {
                return false;
            }
        }

        // ─── Non-streaming response ───────────────────────────────────

        public async Task<string> GetResponseAsync(
            List<ChatMessage> messages)
        {
            string apiKey = GetApiKey();

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return
                    "Groq is not configured. " +
                    "Set the GROQ_API_KEY environment variable " +
                    "and restart Inventor.";
            }

            string url = _baseUrl + "/chat/completions";

            var serializer = new JavaScriptSerializer();

            var messageList =
                new List<Dictionary<string, string>>();

            foreach (var msg in messages)
            {
                messageList.Add(
                    new Dictionary<string, string>
                    {
                        { "role", msg.role ?? "user" },
                        { "content", msg.content ?? "" }
                    });
            }

            var requestBody = new Dictionary<string, object>
            {
                { "model", _model },
                { "messages", messageList },
                { "temperature", 0.2 },
                { "max_tokens", 800 }
            };

            string json = serializer.Serialize(requestBody);

            using (var client = new HttpClient())
            {
                client.Timeout =
                    System.TimeSpan.FromSeconds(30);

                client.DefaultRequestHeaders.Add(
                    "Authorization", "Bearer " + apiKey);

                using (var body = new StringContent(
                    json, Encoding.UTF8, "application/json"))
                {
                    HttpResponseMessage httpResponse =
                        await client.PostAsync(url, body);

                    string responseText =
                        await httpResponse.Content
                                          .ReadAsStringAsync();

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        return
                            "Groq request failed (" +
                            (int)httpResponse.StatusCode +
                            "):" +
                            System.Environment.NewLine +
                            responseText;
                    }

                    return ExtractContentFromJson(responseText);
                }
            }
        }

        // ─── Streaming response ───────────────────────────────────────

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
            {
                messageList.Add(
                    new Dictionary<string, string>
                    {
                        { "role", msg.role ?? "user" },
                        { "content", msg.content ?? "" }
                    });
            }

            var requestBody = new Dictionary<string, object>
            {
                { "model", _model },
                { "messages", messageList },
                { "temperature", 0.2 },
                { "max_tokens", 800 },
                { "stream", true }
            };

            string json = serializer.Serialize(requestBody);

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout =
                        System.TimeSpan.FromSeconds(60);

                    client.DefaultRequestHeaders.Add(
                        "Authorization", "Bearer " + apiKey);

                    var request = new HttpRequestMessage(
                        HttpMethod.Post, url);

                    request.Content = new StringContent(
                        json, Encoding.UTF8, "application/json");

                    var response = await client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseHeadersRead);

                    if (!response.IsSuccessStatusCode)
                    {
                        string err =
                            await response.Content
                                          .ReadAsStringAsync();
                        onError?.Invoke(
                            "Groq streaming failed (" +
                            (int)response.StatusCode + "): " +
                            err);
                        return;
                    }

                    var fullResponse =
                        new System.Text.StringBuilder();

                    using (var stream =
                        await response.Content.ReadAsStreamAsync())
                    using (var reader =
                        new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            string line =
                                await reader.ReadLineAsync();

                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            if (!line.StartsWith("data: "))
                                continue;

                            string data = line.Substring(6).Trim();

                            if (data == "[DONE]")
                                break;

                            string token =
                                ExtractTokenFromChunk(data);

                            if (!string.IsNullOrEmpty(token))
                            {
                                fullResponse.Append(token);
                                onToken?.Invoke(token);
                            }
                        }
                    }

                    onComplete?.Invoke(fullResponse.ToString());
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(
                    "Streaming error: " + ex.Message);
            }
        }

        // ─── Parsers ──────────────────────────────────────────────────

        private string ExtractTokenFromChunk(string json)
        {
            try
            {
                string searchKey = "\"content\":\"";
                int startIndex = json.IndexOf(searchKey);

                if (startIndex == -1)
                    return string.Empty;

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

                        i += 2;
                        continue;
                    }

                    if (c == '"') break;

                    sb.Append(c);
                    i++;
                }

                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
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
                        return "The assistant returned no content.";

                    return "Could not find content in response.";
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

                        i += 2;
                        continue;
                    }

                    if (c == '"') break;

                    sb.Append(c);
                    i++;
                }

                string result = sb.ToString().Trim();

                if (string.IsNullOrWhiteSpace(result))
                    return "The assistant returned an empty response.";

                return result;
            }
            catch (Exception ex)
            {
                return "Failed to extract response: " + ex.Message;
            }
        }
    }
}