// Copies the Phosphor icons used by WaveMP from @phosphor-icons/core into /assets/web/icons,
// unchanged except for LF line endings and a final newline (.editorconfig), together with the
// license.
// Icons that are no longer listed are removed, so only the icons in use are shipped.
//
// Usage: npm run icons:sync (after adding an icon to the list below)

import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

type IconStyle = 'fill' | 'duotone' | 'regular'

// Phosphor icon names per style; the file is <name>-<style>.svg (regular: <name>.svg).
// Dark areas and toolbars use "fill", the sidebar "duotone", small hints, menus and dialogs
// "regular". The icon slots in src/themes/iconSlots.ts refer to these files.
const icons: Record<IconStyle, string[]> = {
    fill: [
        'arrow-left',
        'arrow-right',
        'books',
        'caret-down',
        'caret-right',
        'corners-in',
        'corners-out',
        'list',
        'magnifying-glass',
        'music-notes',
        'pause',
        'play',
        'playlist',
        'repeat',
        'repeat-once',
        'rows',
        'shuffle',
        'skip-back',
        'skip-forward',
        'sort-ascending',
        'speaker-high',
        'speaker-x',
        'squares-four',
        'stop',
        'x',
    ],
    duotone: [
        'books',
        'calendar-blank',
        'clock-counter-clockwise',
        'guitar',
        'heart',
        'list-plus',
        'microphone-stage',
        'music-note',
        'playlist',
        'vinyl-record',
    ],
    regular: [
        'arrows-clockwise',
        'clock-clockwise',
        'film-strip',
        'folder-open',
        'gear-six',
        'image',
        'info',
        'minus-square',
        'pencil-simple',
        'play',
        'plus',
        'plus-square',
        'trash',
        'user-circle',
        'user-switch',
        'users',
        'video-camera',
        'waveform',
        'wrench',
        'x',
    ],
}

const webDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..')
const packageDirectory = path.join(webDirectory, 'node_modules', '@phosphor-icons', 'core')
const targetDirectory = path.resolve(webDirectory, '..', '..', 'assets', 'web', 'icons', 'phosphor')

function fileName(name: string, style: IconStyle): string {
    return style === 'regular' ? `${name}.svg` : `${name}-${style}.svg`
}

function normalized(content: string): string {
    const lf = content.replaceAll('\r\n', '\n')
    return lf.endsWith('\n') ? lf : `${lf}\n`
}

if (!existsSync(packageDirectory)) {
    throw new Error('@phosphor-icons/core is missing, run npm install first.')
}

let copied = 0
let removed = 0
for (const [style, names] of Object.entries(icons) as [IconStyle, string[]][]) {
    const styleDirectory = path.join(targetDirectory, style)
    mkdirSync(styleDirectory, { recursive: true })

    const wanted = new Set(names.map((name) => fileName(name, style)))
    for (const file of wanted) {
        const source = path.join(packageDirectory, 'assets', style, file)
        writeFileSync(path.join(styleDirectory, file), normalized(readFileSync(source, 'utf8')))
        copied++
    }

    for (const file of readdirSync(styleDirectory)) {
        if (!wanted.has(file)) {
            rmSync(path.join(styleDirectory, file))
            removed++
        }
    }
}

const license = readFileSync(path.join(packageDirectory, 'LICENSE'), 'utf8')
writeFileSync(path.join(targetDirectory, 'LICENSE'), normalized(license))

console.log(`Phosphor icons: ${copied} copied, ${removed} removed (${targetDirectory})`)
