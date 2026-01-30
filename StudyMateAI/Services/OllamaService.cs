using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;

namespace StudyMateAI.Services
{
    public class OllamaService
    {
        private readonly HttpClient _httpClient;
        public static string DocumentModel { get; private set; } = "glm-4.7:cloud";
        public static string ChatModel { get; private set; } = "glm-4.7:cloud";
        public static string VideoModel { get; private set; } = "glm-4.7:cloud";
        public static string GeneralModel { get; private set; } = "glm-4.7:cloud";
        
        // Backwards compatibility property (returns GeneralModel or sets all)
        public static string CurrentModel 
        { 
            get => GeneralModel; 
            set 
            {
                GeneralModel = value;
                DocumentModel = value;
                ChatModel = value;
                VideoModel = value;
                SaveConfig();
            }
        }

        private readonly string _baseUrl = "http://localhost:11434";
        private static readonly string _configPath = "app_config.json";

        public enum ModelCategory { General, Document, Chat, Video }

        public OllamaService()
        {
            LoadConfig();
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };
        }

        private static void LoadConfig()
        {
            try
            {
                if (System.IO.File.Exists(_configPath))
                {
                    var json = System.IO.File.ReadAllText(_configPath);
                    var config = JObject.Parse(json);
                    
                    // Load individual settings if they exist, otherwise fallback to legacy "OllamaModel" or default
                    if (config["DocumentModel"] != null) DocumentModel = config["DocumentModel"].ToString();
                    else if (config["OllamaModel"] != null) DocumentModel = config["OllamaModel"].ToString();

                    if (config["ChatModel"] != null) ChatModel = config["ChatModel"].ToString();
                    else if (config["OllamaModel"] != null) ChatModel = config["OllamaModel"].ToString();

                    if (config["VideoModel"] != null) VideoModel = config["VideoModel"].ToString();
                    else if (config["OllamaModel"] != null) VideoModel = config["OllamaModel"].ToString();

                    if (config["GeneralModel"] != null) GeneralModel = config["GeneralModel"].ToString();
                    else if (config["OllamaModel"] != null) GeneralModel = config["OllamaModel"].ToString();
                }
            }
            catch { }
        }

        public static void SetModels(string document, string chat, string video, string general)
        {
            DocumentModel = document;
            ChatModel = chat;
            VideoModel = video;
            GeneralModel = general;
            SaveConfig();
        }

        private static void SaveConfig()
        {
            try
            {
                var config = new JObject();
                if (System.IO.File.Exists(_configPath))
                {
                    try { config = JObject.Parse(System.IO.File.ReadAllText(_configPath)); } catch { }
                }
                
                config["DocumentModel"] = DocumentModel;
                config["ChatModel"] = ChatModel;
                config["VideoModel"] = VideoModel;
                config["GeneralModel"] = GeneralModel;
                
                // Legacy support
                config["OllamaModel"] = GeneralModel;

                System.IO.File.WriteAllText(_configPath, config.ToString());
            }
            catch { }
        }

        private string GetModelForCategory(ModelCategory category)
        {
            return category switch
            {
                ModelCategory.Document => DocumentModel,
                ModelCategory.Chat => ChatModel,
                ModelCategory.Video => VideoModel,
                _ => GeneralModel
            };
        }

        public async Task<bool> IsOllamaRunningAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async IAsyncEnumerable<string> SendMessageStreamAsync(string message, string context = null, string base64Image = null, ModelCategory category = ModelCategory.General)
        {
            var payload = new JObject
            {
                ["model"] = GetModelForCategory(category),
                ["prompt"] = message,
                ["stream"] = true
            };

            if (!string.IsNullOrEmpty(context))
            {
                payload["system"] = context;
            }

            if (!string.IsNullOrEmpty(base64Image))
            {
                payload["images"] = new JArray(base64Image);
            }

            var content = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate") { Content = content };

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null) break;

                if (!string.IsNullOrEmpty(line))
                {
                    var json = JObject.Parse(line);
                    if (json["response"] != null)
                    {
                        yield return json["response"].ToString();
                    }
                    if (json["done"]?.Value<bool>() == true)
                    {
                        yield break;
                    }
                }
            }
        }

        public async Task<string> SendMessageAsync(string message, string context = null, string base64Image = null, ModelCategory category = ModelCategory.General)
        {
            try
            {
                string prompt = message;
                if (!string.IsNullOrEmpty(context))
                {
                    prompt = $"Context: {context}\n\nQuestion: {message}";
                }

                string selectedModel = GetModelForCategory(category);

                var payload = new System.Collections.Generic.Dictionary<string, object>
                {
                    { "model", selectedModel },
                    { "prompt", prompt },
                    { "stream", false }
                };

                if (!string.IsNullOrEmpty(base64Image))
                {
                    payload["images"] = new[] { base64Image };
                }

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        return $"Hata: '{selectedModel}' modeli bulunamadı. Lütfen terminali açıp 'ollama pull {selectedModel}' komutunu çalıştırın.";
                    }
                    return $"Hata: Ollama API yanıt vermedi ({response.StatusCode})";
                }

                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JObject.Parse(responseString);
                
                return responseJson["response"]?.ToString() ?? "No response from AI";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        public async Task<string> GenerateTopicExplanationAsync(string documentContent, string topic = null)
        {
            string prompt = string.IsNullOrEmpty(topic)
                ? $"Aşağıdaki dökümanı özetleyerek ana konuları açıkla. Öğrenci dostu bir dille anlat:\n\n{documentContent}"
                : $"Aşağıdaki dökümandan '{topic}' konusunu detaylı bir şekilde açıkla:\n\n{documentContent}";

            return await SendMessageAsync(prompt, category: ModelCategory.Document);
        }

        public async Task<string> GenerateQuestionsAsync(string topic, int count = 5)
        {
            string prompt = $"'{topic}' konusu hakkında {count} adet çoktan seçmeli soru oluştur. Her sorunun 4 şıkkı olsun ve doğru cevabı belirt.";
            return await SendMessageAsync(prompt, category: ModelCategory.Chat);
        }

        public async Task<string> ExplainConceptAsync(string concept, string context = null)
        {
            string prompt = $"'{concept}' kavramını basit ve anlaşılır bir şekilde açıkla.";
            return await SendMessageAsync(prompt, context, category: ModelCategory.Chat);
        }

        public async Task<string> GenerateStudyPlanAsync(string topics, string context = null)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine("Bir öğrenci için ders çalışma planı oluştur.");
            promptBuilder.AppendLine($"Konular: {topics}");
            if (!string.IsNullOrEmpty(context))
            {
                promptBuilder.AppendLine($"Ek Bağlam/Döküman İçeriği: {context.Substring(0, Math.Min(context.Length, 2000))}..."); // Context limit
            }
            promptBuilder.AppendLine("\nLütfen çıktıyı tam olarak aşağıdaki JSON formatında ver (yorum veya markdown bloğu olmadan, sadece JSON):");
            promptBuilder.AppendLine("[");
            promptBuilder.AppendLine("  { \"Topic\": \"Konu Başlığı\", \"EstimatedHours\": 2.5 }");
            promptBuilder.AppendLine("]");
            promptBuilder.AppendLine("Her bir alt konu için tahmini çalışma süresi ver (saat cinsinden, örn: 1.5).");

            return await SendMessageAsync(promptBuilder.ToString(), category: ModelCategory.General);
        }

        public async Task<string> GenerateQuizFromContentAsync(string content, int count, string difficulty, string type)
        {
            var promptBuilder = new StringBuilder();
            promptBuilder.AppendLine($"Aşağıdaki içerikten {count} adet '{difficulty}' seviyesinde {type} (Sınav) sorusu oluştur.");
            
            if (type == "Test")
            {
                promptBuilder.AppendLine("Format: Çoktan Seçmeli (4 şık).");
                promptBuilder.AppendLine("Çıktı Formatı (SADECE JSON):");
                promptBuilder.AppendLine("[");
                promptBuilder.AppendLine("  { \"Question\": \"Soru metni\", \"Options\": [\"A\", \"B\", \"C\", \"D\"], \"CorrectAnswer\": 0, \"Explanation\": \"Açıklama\" }");
                promptBuilder.AppendLine("]");
            }
            else // Klasik / Open Ended
            {
                promptBuilder.AppendLine("Format: Açık Uçlu (Klasik).");
                promptBuilder.AppendLine("Çıktı Formatı (SADECE JSON):");
                promptBuilder.AppendLine("[");
                promptBuilder.AppendLine("  { \"Question\": \"Soru metni\", \"ModelAnswer\": \"İdeal cevap metni\" }");
                promptBuilder.AppendLine("]");
            }

            promptBuilder.AppendLine($"\nİçerik:\n{content.Substring(0, Math.Min(content.Length, 15000))}..."); // Limit content

            return await SendMessageAsync(promptBuilder.ToString(), category: ModelCategory.Document);
        }

        public async Task<string> EvaluateAnswerAsync(string question, string userAnswer, string modelAnswer)
        {
            var prompt = $@"
Soru: {question}
İdeal Cevap: {modelAnswer}
Öğrenci Cevabı: {userAnswer}

Lütfen öğrencinin cevabını 0-100 arasında puanla ve kısa bir geri bildirim ver.
Çıktı Formatı (SADECE JSON):
{{
  ""Score"": 85,
  ""Feedback"": ""Cevabın genel olarak doğru ancak ... konusuna değinmemişsin.""
}}";
            return await SendMessageAsync(prompt, category: ModelCategory.General);
        }

        public async Task<string> GenerateExamReportAsync(string examSummary)
        {
            var prompt = $@"
Aşağıdaki sınav sonuçlarını analiz et ve öğrenciye çalışma tavsiyeleri ver.
Sınav Özeti:
{examSummary}

Çıktı Formatı (SADECE JSON):
{{
  ""Score"": 75,
  ""OverallAssessment"": ""İyi bir performans ancak fizik konularında eksiklik var."",
  ""WeakTopics"": [""Kuvvet"", ""Hareket""],
  ""Recommendations"": [""Newton'un yasalarını tekrar et"", ""Sürtünme kuvveti üzerine soru çöz""]
}}";
            return await SendMessageAsync(prompt, category: ModelCategory.General);
        }
        public async Task<string> GenerateSummaryAsync(string content)
        {
            var prompt = $"Aşağıdaki metni Türkçe olarak, önemli noktaları vurgulayarak özetle (Maksimum 3-4 paragraf):\n\n{content.Substring(0, Math.Min(content.Length, 15000))}";
            return await SendMessageAsync(prompt, category: ModelCategory.Document);
        }

        public async Task<string> ExtractKeywordsAsync(string content)
        {
            var prompt = $"Aşağıdaki metinden en önemli 10-15 anahtar kelimeyi veya kavramı çıkar. Çıktıyı SADECE virgülle ayrılmış bir liste olarak ver (örn: Fizik, Kuvvet, Newton, İvme):\n\n{content.Substring(0, Math.Min(content.Length, 10000))}";
            return await SendMessageAsync(prompt, category: ModelCategory.Document);
        }

        public async Task<string> GenerateMindMapAsync(string content)
        {
            var prompt = $@"
Aşağıdaki metni analiz et ve kavramlar arasındaki ilişkiyi gösteren bir Zihin Haritası (Mind Map) oluştur.
Çıktı formatı SADECE Mermaid.js 'graph TD' (veya 'graph LR') sözdizimi olmalıdır.
Markdown bloğu (```mermaid) kullanmana gerek yok, sadece kodu ver.
Örnek format:
graph TD
    A[Ana Konu] --> B(Alt Konu 1)
    A --> C(Alt Konu 2)
    B --> D[Detay 1]

İçerik:
{content.Substring(0, Math.Min(content.Length, 15000))}";
            return await SendMessageAsync(prompt, category: ModelCategory.Document);
        }

        public async Task<bool> CheckModelExistsAsync(string modelName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JObject.Parse(json);
                    var models = data["models"] as JArray;

                    if (models != null)
                    {
                        foreach (var model in models)
                        {
                            var name = model["name"]?.ToString();
                            // Check for exact match or match with :latest implied? 
                            // Ollama user usually provides full tag. Let's check contains to be lenient or exact match.
                            // User input: "qwen2.5-vl" -> Tag: "qwen2.5-vl:latest"
                            if (name != null && (name.Equals(modelName, StringComparison.OrdinalIgnoreCase) || name.StartsWith(modelName + ":", StringComparison.OrdinalIgnoreCase)))
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
