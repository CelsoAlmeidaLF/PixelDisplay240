using System.Text.Json;
using System.Net.Http.Json;
using PixelDisplay240Api.Models;

namespace PixelDisplay240Api.Services;

public class AIService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        // Timeout longo para geração de imagem
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<(bool success, byte[]? data, string? error)> GeneratePixelArtAsync(string apiKey, string prompt, int seed)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, null, "Gemini API key not configured");
        }

        string finalPrompt = prompt;
        try
        {
            finalPrompt = await ImprovePromptWithGemini(apiKey, prompt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI] Gemini prompt enhancement failed: {ex.Message}");
        }

        var pixelPrompt = $"pixel art, 1:1 square, low resolution, limited color palette, crisp edges, no gradients, {finalPrompt}";
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = pixelPrompt }
                    }
                }
            }
        };

        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(requestBody, options: _jsonOptions)
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[AI] Gemini error {response.StatusCode}: {errorBody}");
            return (false, null, errorBody);
        }

        var responseText = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            if (TryExtractImageBytes(doc.RootElement, out var bytes))
            {
                return (true, bytes, null);
            }

            Console.WriteLine("[AI] No image bytes found in response:");
            Console.WriteLine(responseText);
            return (false, null, "No image bytes found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AI] Failed to parse response: {ex.Message}");
            Console.WriteLine(responseText);
            return (false, null, "Failed to parse response");
        }
    }

    private async Task<string> ImprovePromptWithGemini(string apiKey, string prompt)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}";
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = $"Transform this description into a professional pixel art prompt for a 240x240 display. Return ONLY the final prompt text. Description: {prompt}" } } }
            }
        };

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, _jsonOptions);
        if (!response.IsSuccessStatusCode) return prompt;

        var data = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        try
        {
            return data.GetProperty("candidates")[0]
                       .GetProperty("content")
                       .GetProperty("parts")[0]
                       .GetProperty("text").GetString()?.Trim() ?? prompt;
        }
        catch { return prompt; }
    }

    private bool TryExtractImageBytes(JsonElement root, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();

        // Check 'candidates' array (Gemini standard)
        if (root.TryGetProperty("candidates", out var candidates) &&
            candidates.ValueKind == JsonValueKind.Array &&
            candidates.GetArrayLength() > 0)
        {
            var first = candidates[0];
            if (TryReadBase64(first, out bytes)) return true;
        }

        // Check 'predictions' array (Vertex AI Imagen)
        if (root.TryGetProperty("predictions", out var predictions) &&
            predictions.ValueKind == JsonValueKind.Array &&
            predictions.GetArrayLength() > 0)
        {
            var first = predictions[0];
            if (TryReadBase64(first, out bytes)) return true;
        }
        
        // Check 'generatedImages' (Newer Imagen)
        if (root.TryGetProperty("generatedImages", out var generatedImages) &&
            generatedImages.ValueKind == JsonValueKind.Array &&
            generatedImages.GetArrayLength() > 0)
        {
            var first = generatedImages[0];
             if (TryReadBase64(first, out bytes)) return true;
        }

        // Check 'images' array
        if (root.TryGetProperty("images", out var images) &&
            images.ValueKind == JsonValueKind.Array &&
            images.GetArrayLength() > 0)
        {
            var first = images[0];
            if (TryReadBase64(first, out bytes)) return true;
        }

        return false;
    }

    private bool TryReadBase64(JsonElement element, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();

        // 1. Check content.parts[].inlineData
        if (element.TryGetProperty("content", out var content) &&
            content.TryGetProperty("parts", out var parts) &&
            parts.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("inlineData", out var inlineData))
                {
                    if (TryReadInlineData(inlineData, out bytes)) return true;
                }
            }
        }

        // 2. Check direct bytesBase64Encoded (Vertex AI style)
        if (element.TryGetProperty("bytesBase64Encoded", out var bytesBase64Encoded) &&
            bytesBase64Encoded.ValueKind == JsonValueKind.String)
        {
            return TryDecode(bytesBase64Encoded.GetString(), out bytes);
        }

        // 3. Check image.imageBytes
        if (element.TryGetProperty("image", out var image) &&
            image.TryGetProperty("imageBytes", out var imageBytes) &&
            imageBytes.ValueKind == JsonValueKind.String)
        {
            return TryDecode(imageBytes.GetString(), out bytes);
        }

        // 4. Check imageBytes directly
        if (element.TryGetProperty("imageBytes", out var imageBytesFlat) &&
            imageBytesFlat.ValueKind == JsonValueKind.String)
        {
             return TryDecode(imageBytesFlat.GetString(), out bytes);
        }
        
        // 5. Check data field (Generic base64)
        if (element.TryGetProperty("data", out var data) && 
            data.ValueKind == JsonValueKind.String)
        {
            return TryDecode(data.GetString(), out bytes);
        }

        return false;
    }

    private bool TryReadInlineData(JsonElement inlineData, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();

        if (inlineData.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.String)
        {
            return TryDecode(data.GetString(), out bytes);
        }

        if (inlineData.TryGetProperty("bytesBase64Encoded", out var bytesBase64Encoded) &&
            bytesBase64Encoded.ValueKind == JsonValueKind.String)
        {
            return TryDecode(bytesBase64Encoded.GetString(), out bytes);
        }

        return false;
    }

    private bool TryDecode(string? base64, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(base64)) return false;

        try
        {
            bytes = Convert.FromBase64String(base64);
            return bytes.Length > 0;
        }
        catch
        {
            return false;
        }
    }
}
