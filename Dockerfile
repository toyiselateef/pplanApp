FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#USER $APP_UID
# Install dependencies for FreeSpire.XLS 
#RUN apt-get update && apt-get install -y \
#    libgdiplus \
#    libc6-dev \
#    libx11-dev \
#    fontconfig \
#    && rm -rf /var/lib/apt/lists/*
RUN apt-get update && apt-get install -y \
    fontconfig \
    fonts-dejavu \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*
    
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
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MfgDocs.Api.dll"]
