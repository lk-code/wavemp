# WaveMP

**A nostalgic media player for your self-hosted music and music videos.**

[![Main](https://github.com/lk-code/wavemp/actions/workflows/main.yml/badge.svg?branch=main)](https://github.com/lk-code/wavemp/actions/workflows/main.yml)
[![Release](https://github.com/lk-code/wavemp/actions/workflows/release.yml/badge.svg)](https://github.com/lk-code/wavemp/actions/workflows/release.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

> **Status:** WaveMP is in early development. Features listed below are planned and will be added step by step.

## About

WaveMP brings back the glossy, glassy look of the classic desktop media players from the Frutiger Aero era, and combines it with what you expect from a modern music app. Point it at your media folder, and it builds a library you can play from any browser on your network.

## Features

- **Your library, your files.** WaveMP scans a folder and organizes your music. It reads tags first and falls back to your folder structure.
- **Music and music videos together.** Switch seamlessly between the audio track, its music video, its cover and a visualization.
- **Profiles.** Pick your profile, no login required. Each profile has its own playlists and history.
- **Playlists, queue, shuffle and repeat.**
- **Visualizations.** Classic Milkdrop presets plus WaveMP's own styles, *A-Force* and *BlackCap*.
- **Plays almost anything.** Formats your browser supports are streamed directly; everything else is transcoded on the fly with ffmpeg. Intel Quick Sync is supported for hardware acceleration.
- **Easy setup.** One compose file, one password, your media folder. That's it.
- **Open API.** A documented REST API (OpenAPI) for building your own clients. Native apps are planned.
- **Installable as an app (PWA)** on desktop and mobile devices.
- **English and German.** More languages will follow through community translations.
- **Themes.** The look is a theme: one CSS file plus graphics, from colors to the complete desktop layout. WaveMP ships with the dark glass theme *Midnight*; see [THEMING.md](THEMING.md) for writing your own.

## Quick Start

WaveMP runs as two containers: the app and a PostgreSQL database.

Create a `.env` file with a database password of your choice:

```env
DB_PASSWORD=change-me-to-a-long-random-password
```

Create a `docker-compose.yml` next to it:

```yaml
services:
  wavemp:
    image: ghcr.io/lk-code/wavemp:latest
    container_name: wavemp
    ports:
      - "8080:8080"
    environment:
      DB_HOST: wavemp-db
      DB_PASSWORD: ${DB_PASSWORD:?Please set DB_PASSWORD in .env}
    volumes:
      - wavemp-data:/data
      - /path/to/your/media:/media
    depends_on:
      wavemp-db:
        condition: service_healthy
    restart: unless-stopped

  wavemp-db:
    image: postgres:18
    container_name: wavemp-db
    environment:
      POSTGRES_USER: wavemp
      POSTGRES_DB: wavemp
      POSTGRES_PASSWORD: ${DB_PASSWORD:?Please set DB_PASSWORD in .env}
    volumes:
      - wavemp-db:/var/lib/postgresql
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U wavemp -d wavemp"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

volumes:
  wavemp-data:
  wavemp-db:
```

The database is not exposed outside the Docker network. The app image is also available on Docker Hub as `lkcode/wavemp`.

Start it:

```bash
docker compose up -d
```

Open `http://<your-server>:8080` and select the **Admin** profile to set up your library.

WaveMP can be installed as an app (PWA). Browsers require HTTPS for that, except on `localhost`, for example behind a reverse proxy with TLS.

### Image tags

Images are built for `linux/amd64` and `linux/arm64` and published as `ghcr.io/lk-code/wavemp` and on Docker Hub as `lkcode/wavemp`, both with the same tags:

| Tag | Content |
|---|---|
| `latest` | Latest stable version |
| `1.2.3` | A specific version |
| `1.2` | Latest patch of a minor version |
| `edge` | Current state of `main`, may be unstable |
| `sha-<commit>` | A specific commit on `main` (short hash) |

Until the first release, only `edge` and `sha-<commit>` are available.

### Settings

| Variable | Description | Default |
|---|---|---|
| `DB_PASSWORD` | Database password, used by both containers | *(required)* |
| `ADMIN_PIN` | Protects the admin profile with a PIN | *(none)* |

### Hardware transcoding (optional)

If your server has an Intel CPU with Quick Sync, pass the GPU device into the container:

```yaml
    devices:
      - /dev/dri:/dev/dri
```

WaveMP falls back to software transcoding when no hardware acceleration is available.

### Media folder access

- Mount the folder **read-only** (`/path/to/your/media:/media:ro`) if WaveMP should never change your files.
- Mount it **writable** if you want to edit tags from within WaveMP. Changes are only written to audio files, never to videos.

## Organizing Your Media

All files that belong to one track must share the **same name** in the same folder. Only the extension differs:

```
Artist/
  Album/
    cover.jpg            ← album cover
    Song.flac            ← audio
    Song.mkv             ← music video
    Song.jpg             ← track cover
```

Any folder structure works, for example:

```
Artist/Album/Song.mp3
Artist - Song.mp3
Song.mp3
```

WaveMP reads metadata from the file tags first. If tags are missing, it tries to detect artist, album and title from your folder and file names.

**Covers:** By default, the album cover is used before a track's own cover.
**Duplicates:** If there are several audio files with the same name (e.g. `Song.mp3` and `Song.flac`), the one with the higher quality is used.

## Usage

1. **Select a profile.** The admin profile can create additional profiles.
2. **Scan your library.** As admin, start a scan of your media folder.
3. **Play.** Browse by artist, album or track, build playlists and switch between cover, video and visualization while playing.

## Platforms

| Platform | Status |
|---|---|
| Docker (linux/amd64, linux/arm64) | Preview images (`edge`), first release planned |
| TrueNAS, ZimaOS and other app catalogs | Planned |
| Android, iOS, Windows, macOS | Planned |

## Building from Source

Requirements: .NET 10 SDK, Node.js 24, Docker.

```bash
git clone https://github.com/lk-code/wavemp.git
cd wavemp
cp .env.example .env    # then set DB_PASSWORD
docker compose -f docker-compose.dev.yml up --build
```

This builds the image from source and starts WaveMP on `http://localhost:8080`. Media files go into `./media`. If port 8080 or 5432 is already in use on your machine, set `WAVEMP_PORT` or `DB_PORT` in `.env`.

### Project layout

| Path | Content |
|---|---|
| `source/WaveMP.Service` | ASP.NET Core host (REST API, serves the web app) |
| `source/WaveMP.Data` | EF Core with PostgreSQL (DbContext, migrations) |
| `source/WaveMP.Web` | Vue 3 web app |
| `tests/` | Unit and integration tests (NUnit), E2E tests (Playwright) |
| `assets/` | Graphics, icons, themes and license texts; `assets/web` is served unchanged by the web app |
| `.github/` | GitHub Actions workflows, license check script and policy |
| `THEMING.md` | Theme API for theme authors |

### Local development

Start only the database, then the service and the web app:

```bash
docker compose -f docker-compose.dev.yml up -d wavemp-db
dotnet tool restore
dotnet user-secrets set DB_PASSWORD "<your DB_PASSWORD>" --project source/WaveMP.Service
dotnet run --project source/WaveMP.Service        # http://localhost:5080

cd source/WaveMP.Web
npm install
npm run dev                                       # http://localhost:5173, proxies /api to the service
```

The development server also serves an internal component overview at `http://localhost:5173/dev/components`: all parts of the theme API in all states and the app at mobile and desktop size. It is not part of the production build.

The Phosphor icons in `assets/web/icons` are copied from the npm package `@phosphor-icons/core`. To add an icon, add it to the list in `source/WaveMP.Web/scripts/sync-icons.ts` and run `npm run icons:sync` in `source/WaveMP.Web`.

If you set `DB_PORT` in `.env`, also set it for the service: `dotnet user-secrets set DB_PORT <port> --project source/WaveMP.Service`.

The service applies database migrations on start. To add a migration:

```bash
dotnet ef migrations add <Name> --project source/WaveMP.Data
```

Migrations are generated code: every file is marked with `// <auto-generated />`, so the build and `dotnet format` leave them as they are. Do not reformat or edit them by hand. Commands that need the database, such as `dotnet ef migrations remove`, read `DB_HOST`, `DB_PORT` and `DB_PASSWORD` from the environment (use `DB_HOST=127.0.0.1` for the dev database).

### API client

The OpenAPI document is the only contract between server and clients. Building `WaveMP.Service` writes it to `source/WaveMP.Service/openapi.json`; at runtime it is served at `/openapi/v1.json`. The web app uses a client generated from it with [Kiota](https://learn.microsoft.com/openapi/kiota/):

```bash
cd source/WaveMP.Web
npm run generate:api
```

Commit the updated `openapi.json` and the generated client together with API changes.

Errors are returned as Problem Details ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)) with a machine-readable `code`, for example `request.not_found` or `health.unhealthy`. All codes are listed in the `ProblemCode` enum of the OpenAPI document. The API never returns display texts; clients translate the codes.

### Tests

Run the commands from the repository root:

```bash
# Unit and integration tests (integration tests need the dev database)
docker compose -f docker-compose.dev.yml up -d wavemp-db
dotnet test

# Web app checks
(cd source/WaveMP.Web && npm run lint:check && npm run type-check && npm run format:check && npm run test:unit)

# E2E tests against a running instance
docker compose -f docker-compose.dev.yml up --build -d
cd tests/WaveMP.E2E
npm install
npx playwright install chromium
npm test                                          # E2E_BASE_URL defaults to http://localhost:8080
```

The integration tests host the service in memory with `WebApplicationFactory` and its real startup, including the configuration check and the database migrations. They use their own database `wavemp_tests` and read `DB_PASSWORD` and `DB_PORT` from the environment or from `.env`.

If you set `WAVEMP_PORT` in `.env`, point the E2E tests at it, for example `E2E_BASE_URL=http://localhost:8081 npm test`. `npm run test:unit` runs the unit tests of the web app (Vitest), including checks that every text comes from the translation files and that `THEMING.md` matches the code.

`npm run lint:check` only reports problems, like the CI does; `npm run lint` fixes what it can. The E2E tests check the version reported by `/api/v1/health` exactly if `E2E_EXPECTED_VERSION` is set (the release workflow sets it), otherwise any version.

### License check

All NuGet and npm dependencies must have a license listed in `.github/license-policy.json`: MIT-compatible licenses only, never GPL or AGPL. This covers the NuGet packages of all projects in `WaveMP.slnx` (including transitive ones), the local .NET tools in `dotnet-tools.json` and the npm packages of every npm project, including the license check's own tools in `.github/scripts`. The CI runs the same commands:

```bash
node .github/scripts/check-licenses.mjs
node --test .github/scripts/check-licenses.test.mjs   # tests of the check itself
```

It restores the .NET tools and npm packages it needs by itself and fails if a package has a license that is not allowed or no license information. It also fails if a .NET project is not part of `WaveMP.slnx` or a tool manifest or npm project is missing from the policy (`nuget.toolManifests`, `npm.projects`), so new projects cannot be skipped. Exceptions for single packages are only added to the policy after a manual license review, npm exceptions with their exact version.

### Continuous integration

GitHub Actions run `.github/workflows/test.yml` for every pull request (`pull-request.yml`) and every push to `main` (`main.yml`): the .NET build with unit tests and integration tests against a PostgreSQL 18 service container, lint, type check, unit tests (Vitest) and build of the web app, the license check, and the E2E tests against the Docker image. After the tests pass, `main.yml` publishes the images `edge` and `sha-<commit>`. A version tag such as `v1.2.3` (`release.yml`) runs the tests, publishes `1.2.3`, `1.2` and `latest` and creates a GitHub release with generated release notes.

## License

WaveMP is licensed under the [MIT License](LICENSE).
It uses [ffmpeg](https://ffmpeg.org), which is licensed separately; see the notices shipped with the image.
