# Third-Party Assets

Files under `/assets` that come from third parties, with source and license.

| Path | Source | License |
|---|---|---|
| `licenses/ffmpeg/COPYING.GPLv2`, `licenses/ffmpeg/COPYING.LGPLv2.1`, `licenses/ffmpeg/LICENSE.md` | FFmpeg source repository, tag `n8.1.3` (https://github.com/FFmpeg/FFmpeg) | License texts of FFmpeg (GPL-2.0, LGPL-2.1); shipped in the Docker image under `/licenses/ffmpeg` |
| `web/icons/phosphor/**` | Phosphor Icons, npm package `@phosphor-icons/core` version 2.1.1 (https://github.com/phosphor-icons/core); unchanged, only the icons in use, copied with `npm run icons:sync` in `source/WaveMP.Web` | MIT; license text in `web/icons/phosphor/LICENSE`, shipped in the Docker image under `/licenses/phosphor-icons` |

All other files under `web/` (`brand`, `themes`) are graphics created for this project and are licensed under the MIT license of WaveMP. The logo and the app icons in `web/brand` are placeholders.
