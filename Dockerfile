##### Stage: build #####
# The SDK image (~800MB) only ever exists in intermediate layers; it never
# reaches the final image. That's the entire point of a multi-stage build.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy just the project file first and restore before copying the rest of the
# source. Docker caches each layer by the hash of its inputs: as long as the
# .csproj (and therefore the package references) hasn't changed, this RUN
# layer is reused on every subsequent build even if application code changed,
# so `dotnet restore` (the slow, network-bound step) only reruns when a
# package reference actually changes.
COPY src/UrlShortener/UrlShortener.csproj src/UrlShortener/
RUN dotnet restore src/UrlShortener/UrlShortener.csproj

COPY src/UrlShortener/ src/UrlShortener/
RUN dotnet publish src/UrlShortener/UrlShortener.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

##### Stage: migrations bundle #####
# `dotnet ef migrations bundle` produces a small self-contained native
# executable that applies migrations and then exits. It is built once here so
# the runtime image never needs the SDK or the `dotnet ef` tool installed.
#
# This is run as its own one-shot step (see the `migrate` service in
# compose.yaml), never inside Program.cs on startup. Once the API scales to N
# replicas (Phase 5), "run migrations on startup" means N containers racing to
# ALTER TABLE the same schema at once - a single, separate migration step
# avoids that entirely.
FROM build AS migrations
RUN dotnet tool install --global dotnet-ef --version 8.0.31
ENV PATH="${PATH}:/root/.dotnet/tools"

# `migrations bundle` builds the app's DI host (Program.cs) as well as using
# AppDbContextFactory, to check for other registered DbContexts. Program.cs
# deliberately throws when ConnectionStrings__Postgres is unset, so without a
# value here that host build fails and takes the whole bundle step down with
# it - even though only the model is read, no connection is ever opened.
# Real values come from compose.yaml/.env at container runtime; this one is
# never dialed and is discarded with the rest of this stage.
ENV ConnectionStrings__Postgres="Host=localhost;Port=5432;Database=urlshortener;Username=postgres;Password=postgres"
RUN dotnet ef migrations bundle \
    --project src/UrlShortener/UrlShortener.csproj \
    --configuration Release \
    --self-contained \
    --runtime linux-x64 \
    --output /app/efbundle

##### Stage: runtime #####
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# curl exists only so Compose/an orchestrator can probe /health/live and
# /health/ready from outside the process. The aspnet base image is a minimal
# Debian slim with nothing that can make an HTTP call on its own - this is a
# deliberate few-MB size trade-off in exchange for a working HEALTHCHECK.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY --from=migrations /app/efbundle ./efbundle

# The aspnet:8.0 base image already ships a non-root "app" user and exposes
# its UID via $APP_UID - use it instead of running as root inside the
# container. A container escape or dependency RCE then can't write outside
# whatever this user already owns.
USER $APP_UID

# .NET 8's ASP.NET container images listen on 8080 by default (not 80, unlike
# earlier versions) - this matches API_HOST_PORT=8080 in .env.
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=3s --start-period=10s --retries=5 \
    CMD curl -f http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "UrlShortener.dll"]
