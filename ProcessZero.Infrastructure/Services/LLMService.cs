using Microsoft.Extensions.Configuration;
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

        public LLMService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string> GenerateTextAsync(string prompt)
        {
            try
            {
                var request = new OllamaRequest
                {
                    Model = _configuration["LLM:Model"] ?? "llama3:latest",
                    Prompt = prompt,
                    Stream = false
                };

                var json = JsonSerializer.Serialize(request);

                var response = await _httpClient.PostAsync(
                    $"https://llm.processzero.xyz/api/generate",
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
    }
}
