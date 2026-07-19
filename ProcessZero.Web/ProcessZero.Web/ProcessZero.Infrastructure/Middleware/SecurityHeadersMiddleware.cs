using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Middleware
{
    /// <summary>
    /// Adds recommended security headers to every HTTP response.
    /// Protects against XSS, clickjacking, MIME-sniffing, and information leakage.
    /// </summary>
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var headers = context.Response.Headers;

            // Prevent MIME-type sniffing
            headers["X-Content-Type-Options"] = "nosniff";

            // Prevent clickjacking by disallowing framing
            headers["X-Frame-Options"] = "DENY";

            // Enable browser XSS filter (legacy browsers)
            headers["X-XSS-Protection"] = "1; mode=block";

            // Control referrer information leakage
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Restrict browser features/permissions
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            // Prevent caching of authenticated responses
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
                headers["Pragma"] = "no-cache";
            }

            // Remove server identification header
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            await _next(context);
        }
    }
}
