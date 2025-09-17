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

# Add the 'contrib' repository to access non-free packages like ttf-mscorefonts-installer
RUN echo "deb http://deb.debian.org/debian/ bookworm main contrib" >> /etc/apt/sources.list.d/sources.list

# Install essential dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libc6-dev \
    libgdiplus \
    libfontconfig1 \
    libx11-dev \
    fonts-liberation \
    ttf-mscorefonts-installer \
    && rm -rf /var/lib/apt/lists/*

# Rebuild the font cache
RUN fc-cache -f -v

# Copy the published application
COPY --from=build /app/publish .

# Set the environment variable for Heroku
ENV ASPNETCORE_URLS=http://*:$PORT

ENTRYPOINT ["dotnet", "MfgDocs.Api.dll"]