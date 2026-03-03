using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Systekna.Application.Services;

/// <summary>
/// Interface para o serviço de IA (geração de imagens e otimização de layout).
/// </summary>
public interface IAIService
{
    Task<(bool success, byte[]? data, string? error)> GeneratePixelArtAsync(string apiKey, string prompt, int seed);
    Task<(bool success, string? elementsJson, string? error)> OptimizeLayoutAsync(string apiKey, string elementsJson, string screenIntent);
}

/// <summary>
/// Serviço de IA para geração de pixel art e otimização de layouts usando Gemini.
/// </summary>
public class AIService : IAIService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AIService(HttpClient httpClient, ILogger<AIService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<(bool success, byte[]? data, string? error)> GeneratePixelArtAsync(string apiKey, string prompt, int seed)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Tentativa de gerar imagem sem API key configurada");
            return (false, null, "Gemini API key not configured");
        }

        string finalPrompt = prompt;
        try
        {
            finalPrompt = await ImprovePromptWithGemini(apiKey, prompt);
            _logger.LogDebug("Prompt melhorado: {FinalPrompt}", finalPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao melhorar prompt com Gemini, usando prompt original");
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

        try
        {
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API retornou erro {StatusCode}: {Error}", response.StatusCode, errorBody);
                return (false, null, errorBody);
            }

            var responseText = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseText);
            
            if (TryExtractImageBytes(doc.RootElement, out var bytes))
            {
                _logger.LogInformation("Imagem gerada com sucesso ({Size} bytes)", bytes.Length);
                return (true, bytes, null);
            }

            _logger.LogWarning("Resposta do Gemini não contém bytes de imagem");
            return (false, null, "No image bytes found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar resposta do Gemini");
            return (false, null, "Failed to parse response");
        }
    }

    private async Task<string> ImprovePromptWithGemini(string apiKey, string prompt)
    {
        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = $"Transform this description into a professional pixel art prompt for a 240x240 display. Return ONLY the final prompt text. Description: {prompt}" } } }
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(requestBody, options: _jsonOptions)
        };
        request.Headers.Add("x-goog-api-key", apiKey);

        var response = await _httpClient.SendAsync(request);
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

    public async Task<(bool success, string? elementsJson, string? error)> OptimizeLayoutAsync(string apiKey, string elementsJson, string screenIntent)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("Tentativa de otimizar layout sem API key configurada");
            return (false, null, "Gemini API key not configured");
        }

        var systemPrompt = @"Você é um Senior UI/UX Designer especializado em sistemas embarcados e displays TFT de baixa resolução (240x240 pixels).
                            Sua tarefa é reorganizar uma lista de elementos UI (JSON) para que fiquem harmoniosos, respeitando:
                            1. Grid de 8 pixels (coordenadas x, y devem ser preferencialmente múltiplas de 8).
                            2. Não sobrepor elementos importantes a menos que seja intencional (ex: texto sobre fundo).
                            3. Hierarquia visual: elementos principais centralizados ou em destaque.
                            4. Margens seguras de pelo menos 8 pixels das bordas da tela (0,0 a 240,240).
                            5. Otimização para legibilidade em telas pequenas.

                            Você deve retornar APENAS o JSON da lista de elementos atualizados, mantendo os IDs originais, mas alterando X, Y, W, H conforme necessário para um design superior.
                            Não adicione explicações, retorne apenas o JSON puro.";

        var userPrompt = $"Aqui está a lista de elementos atuais (JSON):\n{elementsJson}\n\nIntenção da Tela: {screenIntent}\n\nPor favor, otimize o layout:";

        var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent";
        var requestBody = new
        {
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = $"{systemPrompt}\n\n{userPrompt}" } } }
            }
        };

        try
        {
            _logger.LogDebug("Enviando requisição de otimização de layout para Gemini");
            
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(requestBody, options: _jsonOptions)
            };
            request.Headers.Add("x-goog-api-key", apiKey);
            
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API retornou erro na otimização: {Error}", errorBody);
                return (false, null, $"Gemini API Error: {errorBody}");
            }

            var data = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
            var resultText = data.GetProperty("candidates")[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text").GetString()?.Trim() ?? string.Empty;

            // Clean up Markdown code blocks if AI included them
            if (resultText.StartsWith("```json"))
            {
                resultText = resultText.Substring(7);
                if (resultText.EndsWith("```")) resultText = resultText.Substring(0, resultText.Length - 3);
            }
            else if (resultText.StartsWith("```"))
            {
                resultText = resultText.Substring(3);
                if (resultText.EndsWith("```")) resultText = resultText.Substring(0, resultText.Length - 3);
            }

            _logger.LogInformation("Layout otimizado com sucesso");
            return (true, resultText.Trim(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao otimizar layout com Gemini");
            return (false, null, ex.Message);
        }
    }
}
