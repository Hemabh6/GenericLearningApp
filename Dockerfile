# Builds on the machine it runs on, so it is arm64 on the Oracle A1 target and x64 on a laptop.
# The output is a portable framework-dependent app, so there is deliberately no runtime identifier
# (-a / -r): a runtime-specific publish silently leaves out the framework's blazor.web.js, and every
# interactive page then loads dead with a 404 for /_framework/blazor.web.js.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.slnx ./
COPY src/GenericLearningApp.Domain/*.csproj src/GenericLearningApp.Domain/
COPY src/GenericLearningApp.Application/*.csproj src/GenericLearningApp.Application/
COPY src/GenericLearningApp.Infrastructure/*.csproj src/GenericLearningApp.Infrastructure/
COPY src/GenericLearningApp.Web/*.csproj src/GenericLearningApp.Web/
# Restore from the project files alone first, so the NuGet packages sit in a cached layer.
RUN dotnet restore src/GenericLearningApp.Web/GenericLearningApp.Web.csproj

COPY . .
# Restore again now the sources are here. Publishing from the csproj-only restore leaves the framework's
# static assets (blazor.web.js) out of the output; this second, cheap restore is what puts them back.
RUN dotnet restore src/GenericLearningApp.Web/GenericLearningApp.Web.csproj
RUN dotnet publish src/GenericLearningApp.Web/GenericLearningApp.Web.csproj \
    -c Release --no-restore -o /app

# Fail the build, not the launch, if the Blazor script didn't make it into the published assets.
RUN grep -q 'blazor.web' /app/GenericLearningApp.Web.staticwebassets.endpoints.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# The runtime image ships neither curl nor wget, and the healthcheck below needs one.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Kestrel sits behind Caddy, which terminates TLS.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# A Blazor Server circuit is a long-lived WebSocket, so the container must not be killed
# for being idle; health is measured by the app answering, not by traffic.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "GenericLearningApp.Web.dll"]
