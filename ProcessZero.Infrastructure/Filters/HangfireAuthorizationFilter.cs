using Hangfire.Dashboard;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ProcessZero.Infrastructure.Filters
{
    /// <summary>
    /// Restricts access to the Hangfire dashboard to authenticated users with the Admin role.
    /// Without this filter the dashboard is publicly accessible.
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Require authenticated user in Admin role
            return httpContext.User?.Identity?.IsAuthenticated == true
                && httpContext.User.IsInRole("Admin");
        }
    }
}
