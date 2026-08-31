# Multi-stage build for the FinTrack API.
# Build context is the repository root (see docker-compose.yml).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the central build/package files first so restore is cached across source changes.
COPY backend/Directory.Build.props backend/Directory.Packages.props backend/
COPY backend/src/FinTrack.Api/FinTrack.Api.csproj backend/src/FinTrack.Api/
COPY backend/src/FinTrack.Application/FinTrack.Application.csproj backend/src/FinTrack.Application/
COPY backend/src/FinTrack.Domain/FinTrack.Domain.csproj backend/src/FinTrack.Domain/
COPY backend/src/FinTrack.Infrastructure/FinTrack.Infrastructure.csproj backend/src/FinTrack.Infrastructure/
RUN dotnet restore backend/src/FinTrack.Api/FinTrack.Api.csproj

# Copy the rest of the source and publish.
COPY backend/ backend/
RUN dotnet publish backend/src/FinTrack.Api/FinTrack.Api.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FinTrack.Api.dll"]
