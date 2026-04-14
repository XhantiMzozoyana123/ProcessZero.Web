using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using ProcessZero.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace ProcessZero.Infrastructure.Middleware
{
    public class CheckBannedUserMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Cache key prefix used for ban status entries.
        /// Public so that other services (e.g. UserService) can build the
        /// same key to invalidate the cache on ban/unban.
        /// </summary>
        public const string BanCacheKeyPrefix = "ban:";

        /// <summary>How long a ban-check result is cached before re-querying the DB.</summary>
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

        public CheckBannedUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        var cache = context.RequestServices.GetService(typeof(IMemoryCache)) as IMemoryCache;
                        var cacheKey = BanCacheKeyPrefix + userId;

                        bool isBanned;

                        if (cache != null && cache.TryGetValue(cacheKey, out bool cached))
                        {
                            isBanned = cached;
                        }
                        else
                        {
                            // Cache miss — query DB via UserService
                            var userService = context.RequestServices.GetService(typeof(IUserService)) as IUserService;
                            isBanned = userService != null && await userService.IsUserBannedAsync(userId);

                            // Store result in cache
                            cache?.Set(cacheKey, isBanned, CacheDuration);
                        }

                        if (isBanned)
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            await context.Response.WriteAsJsonAsync(new { error = "Account is banned" });
                            return;
                        }
                    }
                }
            }
            catch
            {
                // If ban check fails for any reason, allow request to proceed so as not to block the app
            }

            await _next(context);
        }
    }

}
