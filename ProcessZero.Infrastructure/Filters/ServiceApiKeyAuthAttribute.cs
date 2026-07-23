using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace ProcessZero.Infrastructure.Filters
{
    /// <summary>
    /// Validates service-to-service calls using a shared API key.
    /// Used by the TimerService to call credit endpoints on the main API.
    /// </summary>
    public class ServiceApiKeyAuthAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private const string ApiKeyHeaderName = "X-Timer-Api-Key";

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var logger = context.HttpContext.RequestServices
                .GetService(typeof(ILogger<ServiceApiKeyAuthAttribute>)) as ILogger<ServiceApiKeyAuthAttribute>;

            var configuration = context.HttpContext.RequestServices
                .GetService(typeof(IConfiguration)) as IConfiguration;

            var expectedApiKey = configuration?["TimerService:ApiKey"];
            
            if (string.IsNullOrEmpty(expectedApiKey))
            {
                logger?.LogWarning("TimerService:ApiKey is not configured");
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedApiKey))
            {
                logger?.LogWarning("Missing X-Timer-Api-Key header");
                context.Result = new UnauthorizedResult();
                return;
            }

            if (!string.Equals(providedApiKey.ToString(), expectedApiKey, StringComparison.Ordinal))
            {
                logger?.LogWarning("Invalid API key provided");
                context.Result = new UnauthorizedResult();
                return;
            }

            // API key is valid - bypass JWT authorization requirement
            context.Result = null;
            logger?.LogInformation("Service-to-service authentication successful via API key");
            await Task.CompletedTask;
        }
    }
}
