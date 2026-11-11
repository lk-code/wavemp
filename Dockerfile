# syntax=docker/dockerfile:1

# Multi-arch build (linux/amd64, linux/arm64): the web app and the .NET service are built on the
# build machine's own platform ($BUILDPLATFORM). The service is cross-compiled for the target
# architecture ($TARGETARCH), so only the runtime stage runs for the target platform.

# --- Web app (platform independent output) -------------------------------------
FROM --platform=$BUILDPLATFORM node:24-alpine AS web
WORKDIR /src/source/WaveMP.Web
COPY source/WaveMP.Web/package.json source/WaveMP.Web/package-lock.json ./
RUN npm ci
# Graphics, icons and themes, served unchanged by the web app (Vite publicDir).
COPY assets/web/ /src/assets/web/
COPY source/WaveMP.Web/ ./
RUN npm run build

# --- Service (cross-compiled on the build machine for the target architecture) --
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG TARGETARCH
WORKDIR /src
COPY .editorconfig global.json Directory.Build.props Directory.Packages.props ./
COPY source/WaveMP.Core.Caching/WaveMP.Core.Caching.csproj source/WaveMP.Core.Caching/
COPY source/WaveMP.Core.Jobs/WaveMP.Core.Jobs.csproj source/WaveMP.Core.Jobs/
COPY source/WaveMP.Core.Library/WaveMP.Core.Library.csproj source/WaveMP.Core.Library/
COPY source/WaveMP.Core.Metadata/WaveMP.Core.Metadata.csproj source/WaveMP.Core.Metadata/
COPY source/WaveMP.Core.Scanning/WaveMP.Core.Scanning.csproj source/WaveMP.Core.Scanning/
COPY source/WaveMP.Core.Settings/WaveMP.Core.Settings.csproj source/WaveMP.Core.Settings/
COPY source/WaveMP.Core.Users/WaveMP.Core.Users.csproj source/WaveMP.Core.Users/
COPY source/WaveMP.Data/WaveMP.Data.csproj source/WaveMP.Data/
COPY source/WaveMP.Service/WaveMP.Service.csproj source/WaveMP.Service/
RUN dotnet restore source/WaveMP.Service/WaveMP.Service.csproj -a "$TARGETARCH"
COPY source/WaveMP.Core.Caching/ source/WaveMP.Core.Caching/
COPY source/WaveMP.Core.Jobs/ source/WaveMP.Core.Jobs/
COPY source/WaveMP.Core.Library/ source/WaveMP.Core.Library/
COPY source/WaveMP.Core.Metadata/ source/WaveMP.Core.Metadata/
COPY source/WaveMP.Core.Scanning/ source/WaveMP.Core.Scanning/
COPY source/WaveMP.Core.Settings/ source/WaveMP.Core.Settings/
COPY source/WaveMP.Core.Users/ source/WaveMP.Core.Users/
COPY source/WaveMP.Data/ source/WaveMP.Data/
COPY source/WaveMP.Service/ source/WaveMP.Service/
# The built web app becomes part of the service's static assets.
COPY --from=web /src/source/WaveMP.Web/dist/ source/WaveMP.Service/wwwroot/
# Version of release images (e.g. 1.2.3); empty keeps the version from Directory.Build.props.
ARG VERSION
RUN dotnet publish source/WaveMP.Service/WaveMP.Service.csproj -c Release -a "$TARGETARCH" \
    --no-self-contained --no-restore -o /app ${VERSION:+-p:Version=$VERSION}

# --- Runtime --------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# ffmpeg and ffprobe are used as external processes only (never linked).
RUN apk add --no-cache ffmpeg \
    && mkdir -p /licenses/ffmpeg /licenses/phosphor-icons /licenses/taglibsharp /licenses/wavemp /data /media \
    && ffmpeg -hide_banner -version > /licenses/ffmpeg/VERSION \
    && chown "$APP_UID" /data

COPY LICENSE /licenses/wavemp/LICENSE
COPY assets/licenses/ffmpeg/ /licenses/ffmpeg/
COPY assets/licenses/taglibsharp/ /licenses/taglibsharp/
COPY assets/web/icons/phosphor/LICENSE /licenses/phosphor-icons/LICENSE

WORKDIR /app
COPY --from=build /app ./

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WaveMP.Service.dll"]
