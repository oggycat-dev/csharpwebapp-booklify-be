using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.DTOs.BookAI;
using Booklify.Infrastructure.Models;

namespace Booklify.Infrastructure.Services;

/// <summary>
/// Gemini AI service implementation
/// </summary>
public class GeminiService : ITextAIService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Override with environment variables if available
        OverrideWithEnvironmentVariables();
    }

    private void OverrideWithEnvironmentVariables()
    {
        var envApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? 
                       Environment.GetEnvironmentVariable("GEMINI__APIKEY");
        if (!string.IsNullOrEmpty(envApiKey))
        {
            _options.ApiKey = envApiKey;
        }

        var envBaseUrl = Environment.GetEnvironmentVariable("GEMINI_BASE_URL") ?? 
                        Environment.GetEnvironmentVariable("GEMINI__BASEURL");
        if (!string.IsNullOrEmpty(envBaseUrl))
        {
            _options.BaseUrl = envBaseUrl;
        }

        var envModel = Environment.GetEnvironmentVariable("GEMINI_MODEL") ?? 
                      Environment.GetEnvironmentVariable("GEMINI__MODEL");
        if (!string.IsNullOrEmpty(envModel))
        {
            _options.Model = envModel;
        }

        var envIsEnabled = Environment.GetEnvironmentVariable("GEMINI_IS_ENABLED") ?? 
                          Environment.GetEnvironmentVariable("GEMINI__ISENABLED");
        if (!string.IsNullOrEmpty(envIsEnabled) && bool.TryParse(envIsEnabled, out var isEnabled))
        {
            _options.IsEnabled = isEnabled;
        }

        _logger.LogInformation("Gemini configuration loaded - Enabled: {IsEnabled}, Model: {Model}", 
            _options.IsEnabled, _options.Model);
    }

    private async Task<string> GenerateAsync(string prompt)
    {
        if (!_options.IsEnabled)
        {
            throw new InvalidOperationException("Gemini service is disabled");
        }

        try
        {
            var body = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = _options.Temperature,
                    maxOutputTokens = _options.MaxTokens
                }
            };

            // Use correct Gemini API endpoint
            var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_options.ApiKey}";
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to Gemini API: {Url}", apiUrl.Replace(_options.ApiKey, "***"));

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, content);
                throw new HttpRequestException($"Gemini API error: {response.StatusCode}");
            }

            var json = JObject.Parse(content);
            var result = json["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";

            _logger.LogDebug("Gemini API response received, length: {Length}", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            throw;
        }
    }

    public async Task<string> SummarizeAsync(string chapterText)
    {
        var prompt = $@"Please summarize the following chapter content in English in a concise and easy-to-understand manner:

{chapterText}

Requirements:
- Summarize in English
- Length should be around 3-5 sentences
- Focus on main ideas and important points
- Write in a clear and engaging way";

        return await GenerateAsync(prompt);
    }

    public async Task<string> TranslateAsync(string chapterText)
    {
        var prompt = $@"Hãy dịch đoạn văn sau sang tiếng Việt một cách tự nhiên và dễ hiểu:

{chapterText}

Yêu cầu:
- Dịch chính xác ý nghĩa
- Sử dụng tiếng Việt tự nhiên và dễ đọc
- Giữ nguyên cấu trúc đoạn văn
- Phù hợp với văn phong sách/truyện";

        return await GenerateAsync(prompt);
    }

    public async Task<List<string>> ExtractKeywordsAsync(string chapterText)
    {
        var prompt = $@"Trích xuất 10 từ vựng tiếng Anh quan trọng nhất từ đoạn văn sau, mỗi từ một dòng:

{chapterText}

Yêu cầu:
- Chỉ trả về danh sách từ vựng tiếng Anh
- Mỗi từ một dòng
- Không giải thích thêm
- Ưu tiên từ vựng có tần suất sử dụng cao";

        var response = await GenerateAsync(prompt);
        return response.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                      .Where(x => !string.IsNullOrWhiteSpace(x))
                      .Select(x => x.Trim().Trim('-', '*', '•', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '.', ' '))
                      .Where(x => !string.IsNullOrWhiteSpace(x))
                      .Take(10)
                      .ToList();
    }

    public async Task<List<FlashcardDto>> GenerateFlashcardsAsync(string chapterText)
    {
        var prompt = $@"Tạo 8 flashcard từ vựng tiếng Anh từ đoạn văn sau, trả về dưới dạng JSON với các trường: word, meaning, example, definition, partOfSpeech, difficultyLevel:

{chapterText}

Yêu cầu:
- Trả về JSON array hợp lệ
- Chọn những từ vựng hữu ích cho người học tiếng Anh
- meaning: nghĩa tiếng Việt
- example: ví dụ câu tiếng Anh
- definition: định nghĩa tiếng Anh
- partOfSpeech: từ loại (noun/verb/adjective/adverb)
- difficultyLevel: mức độ khó từ 1-5 (5 là khó nhất)

Ví dụ format:
[
  {{
    ""word"": ""example"",
    ""meaning"": ""ví dụ"",
    ""example"": ""This is an example sentence."",
    ""definition"": ""A thing characteristic of its kind or illustrating a general rule"",
    ""partOfSpeech"": ""noun"",
    ""difficultyLevel"": 2
  }}
]";

        var response = await GenerateAsync(prompt);
        
        try
        {
            // Try to find JSON in the response
            var startIndex = response.IndexOf('[');
            var endIndex = response.LastIndexOf(']');
            
            if (startIndex >= 0 && endIndex > startIndex)
            {
                var jsonContent = response.Substring(startIndex, endIndex - startIndex + 1);
                var flashcards = JsonConvert.DeserializeObject<List<FlashcardDto>>(jsonContent);
                return flashcards ?? new List<FlashcardDto>();
            }
            
            // If no JSON found, return empty list
            _logger.LogWarning("No valid JSON found in Gemini response for flashcards");
            return new List<FlashcardDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing flashcard JSON from Gemini response: {Response}", response);
            return new List<FlashcardDto>();
        }
    }
} 