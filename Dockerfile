# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MfgDocs.Api/MfgDocs.Api.csproj", "MfgDocs.Api/"]
RUN dotnet restore "MfgDocs.Api/MfgDocs.Api.csproj"
COPY . .
WORKDIR "/src/MfgDocs.Api"
RUN dotnet publish "./MfgDocs.Api.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Create the final production image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install essential dependencies, excluding the problematic font installer
RUN apt-get update && apt-get install -y --no-install-recommends \
    libc6-dev \
    libgdiplus \
    libfontconfig1 \
    libx11-dev \
    fonts-liberation \
    wget \
    unzip \
    cabextract \
    xfonts-utils \
    && rm -rf /var/lib/apt/lists/*

# Manually install the Microsoft Core Fonts
# This section downloads the fonts directly and extracts them.
RUN mkdir -p /usr/share/fonts/truetype