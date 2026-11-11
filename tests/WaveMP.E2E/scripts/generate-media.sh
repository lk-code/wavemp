#!/bin/sh
# Creates the synthetic test media of the E2E tests with ffmpeg: sine tones, test pictures and
# tags written with -metadata, no files from other sources. The result are 4 tracks:
#   E2E Artist/E2E Album/01 - First Song.mp3          tagged audio (genre E2E Genre, 2026)
#   E2E Artist/E2E Album/02 - Second Song.flac + .mp4  audio with music video
#   E2E Artist/E2E Album/cover.jpg                     album cover
#   E2E Artist/Video Only.mp4                          music video without audio file
#   Loose Artist - Loose Song.mp3                      audio without tags in the root
#
# Usage: generate-media.sh <target folder>
# With the ffmpeg of the WaveMP image (see README):
#   docker run --rm -v "$PWD/media:/media" -v "$PWD/tests/WaveMP.E2E/scripts:/scripts:ro" \
#     --entrypoint sh wavemp:dev /scripts/generate-media.sh /media
set -eu

target="${1:?Usage: generate-media.sh <target folder>}"
artist="$target/E2E Artist"
album="$artist/E2E Album"
mkdir -p "$album"

ff() {
    ffmpeg -hide_banner -loglevel error -nostdin -y "$@"
}

audio() {
    # $1 frequency, $2 output file, further arguments: codec options and tags
    frequency="$1"
    output="$2"
    shift 2
    ff -f lavfi -i "sine=frequency=$frequency:duration=2" -ac 2 "$@" "$output"
}

video() {
    # $1 size, $2 output file
    ff -f lavfi -i "testsrc=size=$1:rate=10:duration=2" -f lavfi -i "sine=frequency=330:duration=2" \
        -c:v mpeg4 -q:v 5 -pix_fmt yuv420p -c:a aac -b:a 64k -shortest "$2"
}

audio 440 "$album/01 - First Song.mp3" -c:a libmp3lame -b:a 128k \
    -metadata title="First Song" -metadata artist="E2E Artist" \
    -metadata album="E2E Album" -metadata track=1 -metadata date=2026 \
    -metadata genre="E2E Genre"
audio 550 "$album/02 - Second Song.flac" -c:a flac
video 320x240 "$album/02 - Second Song.mp4"
ff -f lavfi -i "testsrc=size=300x300:rate=1" -frames:v 1 -update 1 "$album/cover.jpg"
video 320x240 "$artist/Video Only.mp4"
audio 660 "$target/Loose Artist - Loose Song.mp3" -c:a libmp3lame -b:a 128k

echo "Test media created in $target"
