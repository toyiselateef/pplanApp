FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MfgDocs.Api/MfgDocs.Api.csproj", "MfgDocs.Api/"]
RUN dotnet restore "MfgDocs.Api/MfgDocs.Api.csproj"
COPY . .
WORKDIR "/src/MfgDocs.Api"
RUN dotnet build "./MfgDocs.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MfgDocs.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app

# Install all dependencies in the final stage
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    fonts-liberation \
    libreoffice \
    && rm -rf /var/lib/apt/lists/*

# Debug: List what LibreOffice binaries are available
RUN echo "=== Debugging LibreOffice installation ===" && \
    which libreoffice || echo "libreoffice not found in PATH" && \
    ls -la /usr/bin/libre* || echo "No libre* binaries found" && \
    ls -la /usr/bin/soffice* || echo "No soffice* binaries found" && \
    find /usr -name "*libre*" -type f 2>/dev/null | head -10 || echo "No libre files found" && \
    echo "=== End debugging ==="

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MfgDocs.Api.dll"]