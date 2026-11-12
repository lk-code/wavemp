# syntax=docker/dockerfile:1@sha256:4edf897a3ffa55b89f906fc8cc78afdb3f1834cc9c7083565e611a8a7d5fe99e

# Multi-arch build (linux/amd64, linux/arm64): the web app and the .NET service are built on the
# build machine's own platform ($BUILDPLATFORM). The service is cross-compiled for the target
# architecture ($TARGETARCH), so only the runtime stage runs for the target platform.
#
# All foreign images (including the Dockerfile frontend above) are pinned by the digest of their
# multi-arch index (image:tag@sha256:...); the tag only documents the version. To update an image,
# resolve the new index digest with
#   docker buildx imagetools inspect <image>:<tag>
# and replace tag and digest together.

# --- Web app (platform independent output) -------------------------------------
FROM --platform=$BUILDPLATFORM node:24-alpine@sha256:61cf3202592df80f42ff3a595636e97039b351d1bd2a1c475f897f328e4870ed AS web
WORKDIR /src/source/WaveMP.Web
COPY source/WaveMP.Web/package.json source/WaveMP.Web/package-lock.json ./
RUN npm ci
# Graphics, icons and themes, served unchanged by the web app (Vite publicDir).
COPY assets/web/ /src/assets/web/
COPY source/WaveMP.Web/ ./
RUN npm run build

# --- Service (cross-compiled on the build machine for the target architecture) --
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:0294f285d062afacc8b5f6d7febb0cd4ee879320e5585fa15f2f4ebf1feb9ba7 AS build
ARG TARGETARCH
WORKDIR /src
COPY .editorconfig global.json Directory.Build.props Directory.Packages.props ./
COPY source/WaveMP.Core.Caching/WaveMP.Core.Caching.csproj source/WaveMP.Core.Caching/
COPY source/WaveMP.Core.Collection/WaveMP.Core.Collection.csproj source/WaveMP.Core.Collection/
COPY source/WaveMP.Core.Jobs/WaveMP.Core.Jobs.csproj source/WaveMP.Core.Jobs/
COPY source/WaveMP.Core.Library/WaveMP.Core.Library.csproj source/WaveMP.Core.Library/
COPY source/WaveMP.Core.Metadata/WaveMP.Core.Metadata.csproj source/WaveMP.Core.Metadata/
COPY source/WaveMP.Core.Playlists/WaveMP.Core.Playlists.csproj source/WaveMP.Core.Playlists/
COPY source/WaveMP.Core.Scanning/WaveMP.Core.Scanning.csproj source/WaveMP.Core.Scanning/
COPY source/WaveMP.Core.Settings/WaveMP.Core.Settings.csproj source/WaveMP.Core.Settings/
COPY source/WaveMP.Core.Streaming/WaveMP.Core.Streaming.csproj source/WaveMP.Core.Streaming/
COPY source/WaveMP.Core.Users/WaveMP.Core.Users.csproj source/WaveMP.Core.Users/
COPY source/WaveMP.Data/WaveMP.Data.csproj source/WaveMP.Data/
COPY source/WaveMP.Service/WaveMP.Service.csproj source/WaveMP.Service/
RUN dotnet restore source/WaveMP.Service/WaveMP.Service.csproj -a "$TARGETARCH"
COPY source/WaveMP.Core.Caching/ source/WaveMP.Core.Caching/
COPY source/WaveMP.Core.Collection/ source/WaveMP.Core.Collection/
COPY source/WaveMP.Core.Jobs/ source/WaveMP.Core.Jobs/
COPY source/WaveMP.Core.Library/ source/WaveMP.Core.Library/
COPY source/WaveMP.Core.Metadata/ source/WaveMP.Core.Metadata/
COPY source/WaveMP.Core.Playlists/ source/WaveMP.Core.Playlists/
COPY source/WaveMP.Core.Scanning/ source/WaveMP.Core.Scanning/
COPY source/WaveMP.Core.Settings/ source/WaveMP.Core.Settings/
COPY source/WaveMP.Core.Streaming/ source/WaveMP.Core.Streaming/
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine@sha256:21c3f9798416cba4c0576a947c286231cb71ea2884896cb968253f5c9668f930 AS runtime

# The Alpine base image runs .NET in invariant globalization mode, without ICU. WaveMP needs ICU
# for Unicode normalization (profile names, tags) and culture-aware comparisons, so ICU with the
# data of all cultures is installed and the invariant mode is turned off (the service also sets
# InvariantGlobalization=false, so it fails at start instead of running without ICU).
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# ffmpeg and ffprobe are used as external processes only (never linked); the cache scales covers
# to WebP with the libwebp encoder of the Alpine ffmpeg package.
RUN apk add --no-cache ffmpeg icu-data-full icu-libs \
    && mkdir -p /licenses/ffmpeg /licenses/icu /licenses/phosphor-icons /licenses/taglibsharp /licenses/wavemp /data /media \
    && ffmpeg -hide_banner -version > /licenses/ffmpeg/VERSION \
    && chown "$APP_UID" /data

COPY LICENSE /licenses/wavemp/LICENSE
COPY assets/licenses/ffmpeg/ /licenses/ffmpeg/
COPY assets/licenses/icu/ /licenses/icu/
COPY assets/licenses/taglibsharp/ /licenses/taglibsharp/
COPY assets/web/icons/phosphor/LICENSE /licenses/phosphor-icons/LICENSE

WORKDIR /app
COPY --from=build /app ./

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WaveMP.Service.dll"]
