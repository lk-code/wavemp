# WaveMP

**A nostalgic media player for your self-hosted music and music videos.**

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
| Docker | Planned (first release) |
| TrueNAS, ZimaOS and other app catalogs | Planned |
| Android, iOS, Windows, macOS | Planned |

## Building from Source

Requirements: .NET 10 SDK, Node.js 24, Docker.

```bash
git clone https://github.com/lk-code/wavemp.git
cd wavemp
docker compose -f docker-compose.dev.yml up --build
```

## License

WaveMP is licensed under the [MIT License](LICENSE).
It uses [ffmpeg](https://ffmpeg.org), which is licensed separately; see the notices shipped with the image.