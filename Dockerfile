# syntax=docker/dockerfile:1

# --- Web app ------------------------------------------------------------------
FROM node:24-alpine AS web
WORKDIR /src/source/WaveMP.Web
COPY source/WaveMP.Web/package.json source/WaveMP.Web/package-lock.json ./
RUN npm ci
COPY source/WaveMP.Web/ ./
RUN npm run build

# --- Service ------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY .editorconfig global.json Directory.Build.props Directory.Packages.props ./
COPY source/WaveMP.Data/WaveMP.Data.csproj source/WaveMP.Data/
COPY source/WaveMP.Service/WaveMP.Service.csproj source/WaveMP.Service/
RUN dotnet restore source/WaveMP.Service/WaveMP.Service.csproj
COPY source/WaveMP.Data/ source/WaveMP.Data/
COPY source/WaveMP.Service/ source/WaveMP.Service/
# The built web app becomes part of the service's static assets.
COPY --from=web /src/source/WaveMP.Web/dist/ source/WaveMP.Service/wwwroot/
RUN dotnet publish source/WaveMP.Service/WaveMP.Service.csproj -c Release -o /app --no-restore

# --- Runtime ------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime

# ffmpeg is used as an external process only (never linked).
RUN apk add --no-cache ffmpeg \
    && mkdir -p /licenses/ffmpeg /licenses/wavemp /data /media \
    && ffmpeg -hide_banner -version > /licenses/ffmpeg/VERSION \
    && chown "$APP_UID" /data

COPY LICENSE /licenses/wavemp/LICENSE
COPY assets/licenses/ffmpeg/ /licenses/ffmpeg/

WORKDIR /app
COPY --from=build /app ./

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "WaveMP.Service.dll"]
