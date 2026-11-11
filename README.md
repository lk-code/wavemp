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
- **Tag editor.** Admins correct title, artist, album, album artist, year, track and disc number and genre right in WaveMP. Changes are written into the audio file, never into videos, and the previous tags are kept in the database.
- **Music and music videos together.** Switch seamlessly between the audio track, its music video, its cover and a visualization.
- **Fast library.** Covers are scaled to the size they are shown in and cached, so lists and tiles load quickly.
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
      # Data of WaveMP that is not in the database, e.g. the cache of scaled covers.
      - wavemp-data:/data
      # Append :ro to keep WaveMP from changing your files; the metadata editor is then off.
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

Open `http://<your-server>:8080` and select the **Admin** profile to set up your library. WaveMP scans your media folder one minute after the start and then every six hours; changes are also picked up within seconds by watching the folder.

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
| `ADMIN_PIN` | Protects the admin profile with a PIN (planned, not available yet) | *(none)* |
| `MEDIA_PATH` | Media folder inside the container; only change it if you mount your media elsewhere | `/media` |
| `DATA_PATH` | Data folder inside the container (cache and, later, uploads and backups); only change it if you mount the volume elsewhere | `/data` |

### Hardware transcoding (optional)

If your server has an Intel CPU with Quick Sync, pass the GPU device into the container:

```yaml
    devices:
      - /dev/dri:/dev/dri
```

WaveMP falls back to software transcoding when no hardware acceleration is available.

### Media folder access

- Mount the folder **read-only** (`/path/to/your/media:/media:ro`) if WaveMP should never change your files. The metadata editor then shows its fields greyed out with a hint.
- Mount it **writable** if you want to edit tags from within WaveMP. Changes are only written to audio files, never to videos, and file names stay as they are.
- The container runs as user `1654` (`app`); your files and folders must be readable for it. To edit tags, the audio files must also be writable for it, for example through a group with write permission that you add to the container with `group_add` in the compose file. No rescan is needed after changing the mount or the permissions: the editor checks the file each time it opens.

### Data folder and cache

WaveMP keeps everything that is not in the database in the volume mounted at `/data`. Its folder `cache` holds the covers scaled to 96, 300 and 1000 pixels as WebP, so lists and tiles load quickly. They are created the first time a cover is shown; after every scan WaveMP also prepares the two smaller sizes of every cover and removes those of changed or deleted pictures (job **Prepare covers** under **Administration > Jobs**). The cache is limited to 2 GB by default: when it grows larger, the files used least recently are removed. Change the limit or clear the cache under **Administration > Cache**.

Everything in the cache is created again when it is needed, so it does not have to be backed up. The cache also has room for transcoded media, which a later version uses for formats that browsers cannot play. If `/data` cannot be written, WaveMP still works and shows the covers unscaled; the cache page and the log tell you so.

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

Files are grouped without regard to case, so `Song.MP3` and `song.jpg` belong together. Which extensions count as audio, video and pictures can be changed under **Administration > Library**.

**Metadata.** WaveMP reads the tags of your files first (title, artist, album, album artist, track and disc number, year, genre). Whatever the tags leave open is taken from the folder and file names, for example:

| Path | Result |
|---|---|
| `Artist/Album/01 - Title.mp3` | artist, album, track 1, title |
| `Artist/Album/CD2/03 Title.mp3` | also disc 2 (`CD1`, `Disc 2` and similar folders are discs of one album) |
| `Artist - Album/02. Title.mp3` | artist and album from the folder |
| `Artist/2001 - Album/Title.mp3` | also the year (`(2001) Album`, `[2001] Album` and `Album (2001)` work too) |
| `Artist/Title.mp3`, `Artist - Title.mp3` | artist and title |
| `weird.mp3` | title `weird` |

A number at the start of a file name is only a track number inside an album folder or with a leading zero, so `50 Cent - Song.mp3` keeps its artist. If most file names of a folder share a pattern, for example all files in the root named `Artist - Title` or `01 - Artist - Title`, WaveMP uses it for all of them. Albums without an album artist tag get the artist of their tracks; tracks of several artists in one album folder become a compilation.

**Covers.** A picture named `cover`, `folder`, `front`, `album` or `albumart` (in this order, any picture format) is the cover of the album in its folder, and for disc folders such as `CD1` also in the folder above. A picture with the name of a track (`Song.jpg`) is the cover of that track, otherwise a picture embedded in its files is used. By default, the album cover is shown before a track's own cover.

**Duplicates.** If there are several audio files with the same name (e.g. `Song.mp3` and `Song.flac`), the one with the highest quality plays: lossless before lossy, then the higher bit depth, sample rate and bitrate. For videos the higher resolution wins, then the bitrate. All files are kept in the library.

**Scans.** Scans are incremental: only files that are new or have a new size or change time are read again, and files that are gone are removed. Hidden folders (starting with a dot) and the system folders of NAS devices and operating systems (`@eaDir`, `#recycle`, `#snapshot`, `$RECYCLE.BIN`, `System Volume Information`, `lost+found`) are skipped. If the media folder is suddenly empty while the library has files, for example because a network share is not mounted, the scan stops with an error and removes nothing. Watching the folder for changes is not reliable on network shares (NFS, SMB); the periodic scan therefore always stays active.

## Usage

1. **Select a profile.** WaveMP starts with a profile selection instead of a login. On the first start there is only the built-in **Admin** profile. The selection is remembered on the device, so the next start goes straight on.
2. **Create profiles.** As admin, open the profile menu (your initials at the top right), choose **Administration** and then **Profiles** to add, edit or delete profiles: name, initials (created from the name if left empty), color and role (admin or user). The built-in admin profile can be renamed, but it cannot be deleted and always stays an admin. Once there are other profiles, it is shown less prominently in the selection.
3. **Switch profiles** with **Switch profile** in the profile menu.
4. **Choose your language** under **Settings** in the profile menu. It is stored with the profile and applies on all devices; before a profile is selected, WaveMP uses the language of the browser (English or German, otherwise English).
5. **Scan your library.** As admin, open **Administration > Library**: **Scan now** starts a scan of your media folder, shows its progress and then its result (files found, added, updated and removed, tracks, albums and artists, and files with problems). Below are the history of scans and the settings: how often the library is scanned, whether the media folder is watched for changes, the file extensions of audio, video and pictures and the names of album covers. **Administration > Jobs** lists all background jobs with their last and next run and runs them on demand; **Prepare covers** runs after every scan. **Administration > Cache** shows how much space the cache of scaled covers uses, clears it and sets its maximum size.
6. **Browse your library.** **Library** in the sidebar (the drawer behind the menu button on smartphones) has the sections **Recently Added** (newest first), **Artist**, **Album**, **Songs** (grouped by album with cover), **Genre** and **Year**. Albums, artists, genres and years open their own page; the details of a song (cover, artist, album, length, audio and video) open from its context menu: right click, the context menu key or a long press on touch screens. Admins also see all files of a song there, with their technical details.
7. **Edit metadata.** As admin, choose **Edit metadata** on the page of a song or in its context menu. Change title, artist, album, album artist, year, track and disc number or genre and save: WaveMP writes the changes into the audio file the song plays, keeps the previous tags in the database and reads the song again, so the library shows the new values at once. An empty field removes the value from the tags; WaveMP then takes it from the folder and file name again. Songs with a video only and files that cannot be written show the editor greyed out with a hint.
8. **Search.** Type into the search field at the top: WaveMP searches artists, albums and songs while you type, and small typos are fine. **Show all** opens the full list filtered by your text. On the desktop, the sort button next to the search sorts the current list; sort order and search text are part of the address, so you can bookmark or share them.
9. **Play.** Build playlists and switch between cover, video and visualization while playing.

Profiles are not protected yet: everyone who can open WaveMP can select any profile, including the admin profile. An optional PIN is planned. Do not expose WaveMP to the internet without protection, for example a reverse proxy with authentication or a VPN.

## Platforms

| Platform | Status |
|---|---|
| Docker (linux/amd64, linux/arm64) | Preview images (`edge`), first release planned |
| TrueNAS, ZimaOS and other app catalogs | Planned |
| Android, iOS, Windows, macOS | Planned |

## Building from Source

Requirements: .NET 10 SDK, Node.js 24, Docker, and ffmpeg for running the service and the tests outside of Docker (Windows: `winget install --id Gyan.FFmpeg -e`, Debian/Ubuntu: `sudo apt install ffmpeg`, macOS: `brew install ffmpeg`). The Docker image brings its own ffmpeg.

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
| `source/WaveMP.Service` | ASP.NET Core host (REST API, authentication, serves the web app) |
| `source/WaveMP.Core.Users` | User profiles, sessions and user settings |
| `source/WaveMP.Core.Settings` | Server settings, stored as JSON per group (for example the library settings) |
| `source/WaveMP.Core.Jobs` | Background jobs with Quartz.NET (in memory) and their run history |
| `source/WaveMP.Core.Metadata` | Reading and writing tags (TagLib#), technical information (ffprobe) and metadata from folder and file names |
| `source/WaveMP.Core.Scanning` | Library scan, grouping of files, the scan job, the file watcher and the metadata editor |
| `source/WaveMP.Core.Library` | Library queries: artists, albums, tracks, genres, years, the search and the covers |
| `source/WaveMP.Core.Caching` | Cache under `/data/cache`: scaled covers (WebP with ffmpeg), their preparation after scans, size limit and clean-up of the files used least recently |
| `source/WaveMP.Data` | EF Core with PostgreSQL (DbContext, entities, repositories, migrations) |
| `source/WaveMP.Web` | Vue 3 web app |
| `tests/` | Unit and integration tests (NUnit), E2E tests (Playwright); `tests/WaveMP.Testing` holds helpers shared by the test projects (test databases, temporary folders, synthetic media) |
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

`dotnet run` uses the folder `./media` of the repository as media folder and `./data` as data folder with the cache (`MEDIA_PATH` and `DATA_PATH` in `Properties/launchSettings.json`); relative paths are resolved against the project folder. The image uses `/media` and `/data`; both folders are ignored by Git. To fill `./media` with synthetic test media (4 tracks, created with ffmpeg), build the image and run:

```bash
docker compose -f docker-compose.dev.yml build
docker run --rm -v "$PWD/media:/media" -v "$PWD/tests/WaveMP.E2E/scripts:/scripts:ro" \
  --entrypoint sh wavemp:dev /scripts/generate-media.sh /media
```

In Git Bash on Windows, prefix the command with `MSYS_NO_PATHCONV=1` and use `$(pwd -W)` instead of `$PWD`.

The development server also serves an internal component overview at `http://localhost:5173/dev/components`: all parts of the theme API in all states and the app at mobile and desktop size. It is not part of the production build.

The Phosphor icons in `assets/web/icons` are copied from the npm package `@phosphor-icons/core`. To add an icon, add it to the list in `source/WaveMP.Web/scripts/sync-icons.ts` and run `npm run icons:sync` in `source/WaveMP.Web`.

If you set `DB_PORT` in `.env`, also set it for the service: `dotnet user-secrets set DB_PORT <port> --project source/WaveMP.Service`.

The service applies database migrations on start. To add a migration:

```bash
dotnet ef migrations add <Name> --project source/WaveMP.Data
```

Migrations are generated code: every file is marked with `// <auto-generated />`, so the build and `dotnet format` leave them as they are. Do not reformat or edit them by hand; only remove the byte order mark that `dotnet ef` writes (`.editorconfig` asks for UTF-8 without it). Tables, columns, keys and indexes use snake_case names; the migrations history table keeps its standard names. Commands that need the database, such as `dotnet ef migrations remove`, read `DB_HOST`, `DB_PORT` and `DB_PASSWORD` from the environment (use `DB_HOST=127.0.0.1` for the dev database).

### API client

The OpenAPI document is the only contract between server and clients. Building `WaveMP.Service` writes it to `source/WaveMP.Service/openapi.json`; at runtime it is served at `/openapi/v1.json`. The web app uses a client generated from it with [Kiota](https://learn.microsoft.com/openapi/kiota/):

```bash
cd source/WaveMP.Web
npm run generate:api
```

Commit the updated `openapi.json` and the generated client together with API changes.

Errors are returned as Problem Details ([RFC 9457](https://www.rfc-editor.org/rfc/rfc9457)) with a machine-readable `code`, for example `request.not_found` or `user.name_taken`. All codes are listed in the `ProblemCode` enum of the OpenAPI document. The API never returns display texts; clients translate the codes. Invalid requests get `400` (`request.invalid`) with the affected fields under `errors`, keyed by their JSON names (for example `name`, or a JSON path such as `$.role` if a value cannot be read at all). Enums are accepted as strings only, as the OpenAPI document describes them; a number is an invalid request.

Authentication is token-based. `GET /api/v1/profiles` lists the profiles without a token; selecting one with `POST /api/v1/sessions` and `{ "profileId": "<id>" }` returns a session token (JWT) that every further request sends as `Authorization: Bearer <token>`. `DELETE /api/v1/sessions/current` ends the session, for example when switching profiles. Requests without a valid token get `401` (`auth.unauthorized`), profiles without permission `403` (`auth.forbidden`); `/api/v1/users` is for admins only. The key that signs the tokens is created on the first start and kept in the database, so sessions survive restarts.

Collections are paged with the query parameters `offset` (from 0) and `limit` (1 to 100, default 50); the answer contains `items`, `offset`, `limit` and `total`.

Covers are scaled with `?size=` (1 to 4096): the server rounds the size up to 96, 300 or 1000 pixels (larger sizes give 1000) and delivers the cover as WebP, never enlarged; without `size` it delivers the original picture. Every cover carries a strong `ETag` of its version and size, `Last-Modified` and `Cache-Control: private, no-cache`, so clients keep it and ask again with `If-None-Match`, which returns `304 Not Modified` while the picture is unchanged. A changed picture gets a new ETag at once.

Admins start a scan with `POST /api/v1/scans`; the answer is `202 Accepted` with the queued scan and its `Location`. `GET /api/v1/scans/current` returns the running scan with its progress (`404`, `scan.not_running`, while none runs), `GET /api/v1/scans` the history. A second start while a scan runs gets `409` (`scan.already_running`). All background jobs are listed with `GET /api/v1/jobs` and started with `POST /api/v1/jobs/{id}/runs` (for example `library-scan`); `GET /api/v1/jobs/{id}/runs` returns their run history (30 days, at most 100 runs per job). Errors of runs and problems of files are codes such as `scan.media_folder_empty` or `file.probe_failed`. The library settings are read and changed with `GET` and `PATCH /api/v1/settings/library`.

After every full scan, and after a partial scan that changed files, the scan starts the job `cover-cache` (trigger `LibraryScan`); it prepares the covers and removes those of changed pictures from the cache, and fails with `cache.ffmpeg_unavailable` or `cache.folder_unwritable`. Admins read the usage of the cache with `GET /api/v1/cache` (total, covers, transcodes, limit and whether the cache folder can be written) and clear it with `DELETE /api/v1/cache` (`204`). The size limit is read and changed with `GET` and `PATCH /api/v1/settings/cache` (`maxSizeMegabytes`, 100 to 1,000,000; 1 MB = 1,000,000 bytes).

The library is available to every session: `GET /api/v1/artists`, `/api/v1/albums` and `/api/v1/tracks` with their items (`/{id}`), the albums of an artist (`/api/v1/artists/{id}/albums`), the tracks of an album (`/api/v1/albums/{id}/tracks`) and the lists of genres and years (`/api/v1/genres`, `/api/v1/years`). Lists are sorted with `sort` and `direction` (the defaults are documented per operation) and filtered with `q` (fault-tolerant search text), `artistId`, `albumId`, `genre`, `year`, `hasAudio` and `hasVideo` where they apply. Tracks tell with `hasAudio`, `hasVideo` and `hasCover` what they offer. `GET /api/v1/search?q=` returns the best artists, albums and tracks at once (`limit` up to 50 per group); it finds texts that contain the search text or are similar to it, using the PostgreSQL extension `pg_trgm`, which the database migration creates. `GET /api/v1/albums/{id}/cover` and `/api/v1/tracks/{id}/cover` return the cover, scaled with `size` (see above) or as the original picture (JPEG, PNG, WebP, GIF or BMP): by default the album cover (a picture such as `cover.jpg` in the album folder) before the cover of the track (a picture with its base name or embedded in its file), each falling back to the other and finally to the cover of another track of the album. Admins list all files of a track with `GET /api/v1/tracks/{id}/files`. Missing items get `404` with `artist.not_found`, `album.not_found`, `track.not_found` or `cover.not_found`.

Admins edit the metadata of a track with `GET` and `PATCH /api/v1/tracks/{id}/metadata`. `GET` returns the values the editor shows (the tags of the audio file, completed from the library), its `status` (`Editable`, `NoAudio` or `NotWritable`) and the `audioFilePath` it writes. `PATCH` writes `title`, `artist`, `album`, `albumArtist`, `year`, `trackNumber`, `discNumber` and `genre` into the tags of the audio file the track plays: members that are missing stay unchanged, `null` or an empty text removes the value. The previous tags are saved in the database first, and the answer holds the values after the track was read again. A track with a video only gets `409` (`track.no_audio`), an audio file that cannot be written `409` (`track.not_writable`).

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

The tests create their media at runtime with ffmpeg (sine tones, test pictures, tags), so ffmpeg and ffprobe must be in the `PATH`, or in the folder named by `FFMPEG_PATH`; without them the media and scan tests fail with a hint. Tests in the category `Media` need ffmpeg only, tests in the category `Integration` the dev database as well (`dotnet test --filter "TestCategory!=Integration"` runs everything else).

The integration tests host the service in memory with `WebApplicationFactory` and its real startup, including the configuration check, the database migrations and the creation of the built-in admin profile. They use their own databases `wavemp_tests` and `wavemp_tests_*` (created like a new installation and dropped afterwards) and a temporary data folder for the cache, and read `DB_PASSWORD` and `DB_PORT` from the environment or from `.env`.

The E2E tests first select the built-in admin profile once (project `setup`) and store the browser state in `tests/WaveMP.E2E/playwright/.auth/` (not committed); most tests start with this session. Tests that switch profiles create their own profiles through the API and delete them afterwards.

If you set `WAVEMP_PORT` in `.env`, point the E2E tests at it, for example `E2E_BASE_URL=http://localhost:8081 npm test`. `npm run test:unit` runs the unit tests of the web app (Vitest), including checks that every text comes from the translation files and that `THEMING.md` matches the code.

`npm run lint:check` only reports problems, like the CI does; `npm run lint` fixes what it can. The E2E tests check the version reported by `/api/v1/health` exactly if `E2E_EXPECTED_VERSION` is set (the release workflow sets it), otherwise any version. They scan the media folder of the running instance before the tests (project `setup`) and need the test media of `tests/WaveMP.E2E/scripts/generate-media.sh` in it (see [Local development](#local-development)): the library tests browse and search exactly these songs. With `E2E_EXPECTED_TRACKS=4` they also check the number of songs, as the CI does. The test of the metadata editor writes genre and year of "Loose Song" and removes them again afterwards, so the media folder must be writable for the container (`generate-media.sh` makes the files it creates writable for everyone).

### License check

All NuGet and npm dependencies must have a license listed in `.github/license-policy.json`: MIT-compatible licenses only, never GPL or AGPL. This covers the NuGet packages of all projects in `WaveMP.slnx` (including transitive ones), the local .NET tools in `dotnet-tools.json` and the npm packages of every npm project, including the license check's own tools in `.github/scripts`. The CI runs the same commands:

```bash
node .github/scripts/check-licenses.mjs
node --test .github/scripts/check-licenses.test.mjs   # tests of the check itself
```

It restores the .NET tools and npm packages it needs by itself and fails if a package has a license that is not allowed or no license information. It also fails if a .NET project is not part of `WaveMP.slnx` or a tool manifest or npm project is missing from the policy (`nuget.toolManifests`, `npm.projects`), so new projects cannot be skipped. Exceptions for single packages are only added to the policy after a manual license review, npm exceptions with their exact version.

### Continuous integration

GitHub Actions run `.github/workflows/test.yml` for every pull request (`pull-request.yml`) and every push to `main` (`main.yml`): the .NET build with unit and media tests (with ffmpeg) and integration tests against a PostgreSQL 18 service container, lint, type check, unit tests (Vitest) and build of the web app, the license check, and the E2E tests against the Docker image with synthetic test media. After the tests pass, `main.yml` publishes the images `edge` and `sha-<commit>`. A version tag such as `v1.2.3` (`release.yml`) runs the tests, publishes `1.2.3`, `1.2` and `latest` and creates a GitHub release with generated release notes.

All foreign Docker images in the `Dockerfile`, in `docker-compose.dev.yml` and in the workflows (base images, PostgreSQL, the static ffmpeg build, the Playwright image, BuildKit and QEMU) are pinned by the digest of their multi-arch index, for example `postgres:18@sha256:...`; the tag only shows the version. To update an image, change the tag if needed and replace the digest with the one printed by `docker buildx imagetools inspect <image>:<tag>`. The Playwright image in `test.yml` has to be updated together with `@playwright/test` in `tests/WaveMP.E2E`; the E2E job fails if the two versions differ. The `docker-compose.yml` for users keeps the plain tags, so PostgreSQL patch releases arrive with `docker compose pull`.

## License

WaveMP is licensed under the [MIT License](LICENSE).
It uses [ffmpeg](https://ffmpeg.org) as a separate process and [TagLib#](https://github.com/mono/taglib-sharp) (LGPL-2.1, used unchanged as a separate assembly), which are licensed separately; see the notices shipped with the image under `/licenses`.
