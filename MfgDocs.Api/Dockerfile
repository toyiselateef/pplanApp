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

# Install the correct native dependencies for FreeSpire.XLS and SkiaSharp
# These libraries are essential for graphics and font rendering.
# The 'fonts-liberation' package is a good addition for common fonts.
RUN apt-get update && apt-get install -y \
    libc6-dev \
    libgdiplus \
    libfontconfig1 \
    libx11-dev \
    fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

# The LibreOffice installation is not needed and significantly bloats the image.
# LibreOffice is not a dependency of Spire.Xls.

# Copy the published application from the build stage
COPY --from=build /app/publish .

# The `Spire.XLS` library can sometimes have issues with its native assets
# on Linux. The following command attempts to clear the NuGet cache to
# ensure the correct SkiaSharp.NativeAssets.Linux.NoDependencies package
# is used. However, it's often more reliable to simply ensure your
# Dockerfile installs the necessary system-level dependencies.
# This line is optional but can sometimes help.
# RUN rm -rf /root/.nuget/packages/skiasharp.nativeassets.linux*

# Set the environment variable to ensure the app listens on the correct port
# This is crucial for Heroku.
ENV ASPNETCORE_URLS=http://*:$PORT

ENTRYPOINT ["dotnet", "MfgDocs.Api.dll"]