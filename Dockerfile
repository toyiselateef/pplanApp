# --- Base runtime image ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Install runtime dependencies (fonts, SkiaSharp, FreeSpire, etc.)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libc6 \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    libharfbuzz0b \
    libfreetype6 \
    libfontconfig1 \
    libpangocairo-1.0-0 \
    libgtk-3-0 \
    fontconfig \
    fonts-dejavu \
    fonts-liberation \
    poppler-utils \
    && rm -rf /var/lib/apt/lists/*

# Copy Calibri TTF fonts from repo into system fonts and refresh cache
COPY MfgDocs.Api/Assets/Fonts/Calibri /usr/share/fonts/truetype/calibri
RUN fc-cache -fv

WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# --- Build stage ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MfgDocs.Api/MfgDocs.Api.csproj", "MfgDocs.Api/"]
RUN dotnet restore "MfgDocs.Api/MfgDocs.Api.csproj"
COPY . .
WORKDIR "/src/MfgDocs.Api"
RUN dotnet build "./MfgDocs.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# --- Publish stage ---
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MfgDocs.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# --- Final runtime image ---
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MfgDocs.Api.dll"]
