# syntax=docker/dockerfile:1

# ----------------------------------------------------------------------------
# ProcessZero.Web - Multi-stage Dockerfile
# .NET 8 ASP.NET Core MVC app (uses Pomelo MySQL, Hangfire, Microsoft.Playwright)
# ----------------------------------------------------------------------------

# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + project files first to leverage Docker layer caching for restore
COPY ProcessZero.Web.slnx ./
COPY ProcessZero.Web/ProcessZero.Web.csproj ProcessZero.Web/
COPY ProcessZero.Application/ProcessZero.Application.csproj ProcessZero.Application/
COPY ProcessZero.Domain/ProcessZero.Domain.csproj ProcessZero.Domain/
COPY ProcessZero.Infrastructure/ProcessZero.Infrastructure.csproj ProcessZero.Infrastructure/

# Restore only the web project (pulls in referenced projects)
RUN dotnet restore ProcessZero.Web/ProcessZero.Web.csproj

# Copy the rest of the source and publish
COPY . .
RUN dotnet publish ProcessZero.Web/ProcessZero.Web.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# ASP.NET Core listens on port 8081 in the .NET 8 runtime image
ENV ASPNETCORE_URLS=http://+:8081 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    PLAYWRIGHT_BROWSERS_PATH=/ms-playwright

# Copy the published app
COPY --from=build /app/publish ./

# Install Playwright's Chromium browser + required OS dependencies.
# Microsoft.Playwright ships a bundled node + CLI under ./.playwright, so we can
# invoke it directly without needing PowerShell installed in the image.
RUN if [ -f ./.playwright/package/cli.js ]; then \
        chmod +x ./.playwright/node/linux-x64/node && \
        ./.playwright/node/linux-x64/node ./.playwright/package/cli.js install --with-deps chromium && \
        rm -rf /var/lib/apt/lists/*; \
    else \
        echo "Playwright CLI not found in publish output - skipping browser install"; \
    fi

EXPOSE 8081

ENTRYPOINT ["dotnet", "ProcessZero.Web.dll"]
