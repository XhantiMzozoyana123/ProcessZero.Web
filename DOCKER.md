# Running ProcessZero.Web in Docker

This project is containerized with a multi-stage `Dockerfile` (build with the
.NET 8 SDK, run on the ASP.NET Core 8 runtime). The image also installs
Playwright's Chromium browser and its OS dependencies, since the app references
`Microsoft.Playwright`.

## Prerequisites

- Install **Docker Desktop** (Windows): https://www.docker.com/products/docker-desktop/
- Make sure Docker Desktop is running before building.

## Build & run with Docker

```bash
# From the repo root (c:\Users\Xhanti\source\repos\ProcessZero.Web)
docker build -t processzero-web:latest .

docker run --rm -p 8080:8080 processzero-web:latest
```

Then open: http://localhost:8080

## Build & run with Docker Compose

```bash
docker compose up --build
```

Stop with `Ctrl+C`, then `docker compose down`.

## Configuration / secrets

The app currently reads its MySQL connection string and other secrets from
`ProcessZero.Web/appsettings.json`. You can override any setting at runtime with
environment variables (use `__` for nested keys). For example:

```bash
docker run --rm -p 8080:8080 \
  -e "ConnectionStrings__DefaultConnection=Server=my-db;Port=3306;Database=processzero;User=user;Password=pass;AllowUserVariables=true;UseAffectedRows=false" \
  -e "Jwt__Key=your-secret-key" \
  processzero-web:latest
```

> Security note: secrets are currently committed in `appsettings.json`. For a
> production deployment, prefer passing them as environment variables (as above)
> or via a secrets manager rather than baking them into the image.

## Optional local MySQL

`docker-compose.yml` includes a commented-out `db` (MySQL 8) service. Uncomment
that block plus the related `depends_on` and `ConnectionStrings__DefaultConnection`
lines to run a local database alongside the app instead of the remote one.

## Ports

The container listens on port **8080** (the .NET 8 runtime image default).
Adjust the `-p host:container` mapping as needed.
