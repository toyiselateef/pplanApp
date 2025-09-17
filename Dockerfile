FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install dependencies including LibreOffice
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libx11-dev \
    fonts-liberation \
    libreoffice \
    && rm -rf /var/lib/apt/lists/*

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
COPY --from=publish /app/publish .

# Verify LibreOffice installation
RUN which libreoffice && libreoffice --version

ENTRYPOINT ["dotnet", "MfgDocs.Api.dll"]