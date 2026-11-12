# Third-Party Assets

Files under `/assets` that come from third parties, with source and license.

| Path | Source | License |
|---|---|---|
| `licenses/ffmpeg/COPYING.GPLv2`, `licenses/ffmpeg/COPYING.LGPLv2.1`, `licenses/ffmpeg/LICENSE.md` | FFmpeg source repository, tag `n8.1.3` (https://github.com/FFmpeg/FFmpeg) | License texts of FFmpeg (GPL-2.0, LGPL-2.1); shipped in the Docker image under `/licenses/ffmpeg` |
| `licenses/icu/LICENSE` | ICU source repository, tag `release-78.1` (https://github.com/unicode-org/icu), the version of the Alpine packages `icu-libs` and `icu-data-full` in the Docker image | License text of ICU (Unicode-3.0, with the notices of the third-party parts of ICU); shipped in the Docker image under `/licenses/icu` |
| `licenses/taglibsharp/COPYING` | License text of TagLib# (LGPL-2.1), identical to `licenses/ffmpeg/COPYING.LGPLv2.1`; `licenses/taglibsharp/NOTICE.md` written for this project | LGPL-2.1 license text; shipped in the Docker image under `/licenses/taglibsharp` |
| `licenses/intel-media-driver/LICENSE.md` | Intel Media Driver for VA-API, tag `intel-media-26.2.1` (https://github.com/intel/media-driver), the version of the Alpine package `intel-media-driver` in the Docker image (amd64) | MIT and BSD-3-Clause; shipped in the Docker image under `/licenses/intel-media-driver` |
| `licenses/libva-intel-driver/COPYING` | Intel VA-API driver for older GPUs, tag `2.4.1` (https://github.com/intel/intel-vaapi-driver), the version of the Alpine package `libva-intel-driver` (amd64) | MIT; shipped in the Docker image under `/licenses/libva-intel-driver` |
| `licenses/onevpl-intel-gpu/LICENSE` | Intel VPL GPU runtime, tag `intel-onevpl-25.4.6` (https://github.com/intel/vpl-gpu-rt), the version of the Alpine package `onevpl-intel-gpu` (amd64) | MIT; shipped in the Docker image under `/licenses/onevpl-intel-gpu` |
| `licenses/intel-gmmlib/LICENSE.md` | Intel Graphics Memory Management Library, tag `intel-gmmlib-22.10.1` (https://github.com/intel/gmmlib), a dependency of the media driver (amd64) | MIT; shipped in the Docker image under `/licenses/intel-gmmlib` |
| `web/icons/phosphor/**` | Phosphor Icons, npm package `@phosphor-icons/core` version 2.1.1 (https://github.com/phosphor-icons/core); unchanged, only the icons in use, copied with `npm run icons:sync` in `source/WaveMP.Web` | MIT; license text in `web/icons/phosphor/LICENSE`, shipped in the Docker image under `/licenses/phosphor-icons` |

All other files under `web/` (`brand`, `themes`) are graphics created for this project and are licensed under the MIT license of WaveMP. The logo and the app icons in `web/brand` are placeholders.
