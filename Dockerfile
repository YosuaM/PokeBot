# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

# Publish the Discord bot project explicitly
WORKDIR /src/Back/PokeBotDiscord/PokeBotDiscord
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# Create non-root user
RUN useradd -m botuser
USER botuser

COPY --from=build /app/publish .

# If you use appsettings.json inside image, keep it read-only.
ENV DOTNET_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "PokeBotDiscord.dll"]
