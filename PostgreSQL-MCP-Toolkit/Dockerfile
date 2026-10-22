# Use the official .NET 10.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV TIMEOUT=1024

# Use the official .NET 10.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only project files first for better caching (dependencies change less frequently than code)
COPY ["PostgreSQL-MCP-Toolkit/PostgreSQL-MCP-Toolkit.csproj", "PostgreSQL-MCP-Toolkit/"]
COPY ["Directory.Build.props", "./"]

# Restore dependencies in a separate layer (cached unless project file changes)
RUN dotnet restore "PostgreSQL-MCP-Toolkit/PostgreSQL-MCP-Toolkit.csproj"

# Now copy the rest of the source code
COPY PostgreSQL-MCP-Toolkit/ PostgreSQL-MCP-Toolkit/

# Build the application
RUN dotnet build "PostgreSQL-MCP-Toolkit/PostgreSQL-MCP-Toolkit.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish the application
FROM build AS publish
WORKDIR "/src"
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PostgreSQL-MCP-Toolkit/PostgreSQL-MCP-Toolkit.csproj" -c $BUILD_CONFIGURATION -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:SelfContained=false \
    /p:PublishSelfContained=false \
    /p:PublishSingleFile=false

# Final stage - runtime image
FROM base AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application
COPY --from=publish /app/publish .

# Run as the built-in non-root user
USER $APP_UID

# Add health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1


# Set the entry point
ENTRYPOINT ["dotnet", "PostgreSQL-MCP-Toolkit.dll"]

# ============================