using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static ProcessZero.Application.Dtos.OllamaDto;

namespace ProcessZero.Infrastructure.Services
{
    public class LLMService : ILLMService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LLMService> _logger;

        public LLMService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<LLMService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> GenerateTextAsync(string prompt)
        {
            try
            {
                var llmUrl = _configuration["LLM:Url"];

                var request = new OllamaRequest
                {
                    Model = _configuration["LLM:Model"] ?? "llama3:latest",
                    Prompt = prompt,
                    Stream = false
                };

                var json = JsonSerializer.Serialize(request);

                var response = await _httpClient.PostAsync(
                    $"{llmUrl}/api/generate",
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"));

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<OllamaResponse>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return result?.Response ?? "No response returned.";
            }
            catch (Exception ex)
            {
                return $"LLM Error: {ex.Message}";
            }
        }

        public async Task<bool> VerifyPaymentProofAsync(string base64Image, decimal expectedAmount, string currency)
        {
            try
            {
                var llmUrl = _configuration["LLM:Url"] ?? "http://46.202.170.203:11434";
                // Use a vision-capable model for image analysis
                var visionModel = _configuration["LLM:VisionModel"];

                var prompt = $@"You are a payment verification AI. Analyze this image and determine if it shows a valid payment confirmation screenshot from a banking app.

Expected payment details:
- Amount: {expectedAmount:N2} {currency}
- Payment method: PayShap

Please analyze the image and answer ONLY with 'true' or 'false':
- 'true' if this appears to be a legitimate payment confirmation screenshot showing the correct amount
- 'false' if this does not appear to be a payment confirmation, shows a different amount, or appears to be fraudulent

Respond with ONLY 'true' or 'false' - no other text.";

                // Ensure base64 data is clean (strip data URL prefix if present)
                var base64Data = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;

                // Build Ollama vision API request
                var visionRequest = new
                {
                    model = visionModel,
                    prompt = prompt,
                    images = new[] { base64Data },
                    stream = false
                };

                var json = JsonSerializer.Serialize(visionRequest);

                _logger.LogInformation("Sending payment proof to LLM for verification. Model: {Model}, URL: {Url}", visionModel, llmUrl);

                var response = await _httpClient.PostAsync(
                    $"{llmUrl}/api/generate",
                    new StringContent(
                        json,
                        Encoding.UTF8,
                        "application/json"));

                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("LLM response status: {StatusCode}, content length: {Length}", response.StatusCode, responseContent.Length);

                response.EnsureSuccessStatusCode();

                var result = JsonSerializer.Deserialize<OllamaResponse>(
                    responseContent,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                var answer = result?.Response?.Trim().ToLower() ?? "false";
                
                _logger.LogInformation("LLM verification result: {Answer}", answer);
                
                return answer == "true";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM payment verification failed - defaulting to verification failed");
                // Default to false on error - credits must not be added unless verification is explicitly successful
                return false;
            }
        }
    }
}
