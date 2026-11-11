// Guard for the theme API: parts, variables, icon slots, the breakpoint and the theme Midnight
// must match THEMING.md, the base CSS, the components and the files under /assets/web.
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { desktopMediaQuery, mobileMediaQuery } from '../breakpoints'
import { iconSlotNames, iconSlots } from '../iconSlots'
import { devPartPrefix, parts, themeApiVersion } from '../parts'
import { themes } from '../themes'

// Not new URL(..., import.meta.url): Vite rewrites that pattern into an asset URL.
const sourceDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..')
const repositoryRoot = path.resolve(sourceDirectory, '../../..')
const webAssets = path.join(repositoryRoot, 'assets', 'web')
const stylesDirectory = path.join(sourceDirectory, 'styles')

const runtimeVariables = [
    '--wmp-slider-value',
    '--wmp-tree-level',
    '--wmp-menu-x',
    '--wmp-menu-y',
    '--wmp-profile-color',
    '--wmp-swatch-color',
    '--wmp-progress-value',
]

function read(file: string): string {
    return readFileSync(file, 'utf8')
}

/** CSS without comments, so that only rules are checked. */
function readCss(file: string): string {
    return read(file).replace(/\/\*[\s\S]*?\*\//g, '')
}

function filesIn(directory: string, extension: string): string[] {
    return readdirSync(directory).flatMap((name) => {
        const file = path.join(directory, name)
        if (statSync(file).isDirectory()) {
            return name === '__tests__' ? [] : filesIn(file, extension)
        }
        return name.endsWith(extension) ? [file] : []
    })
}

/** Content of a "## Heading" section of a Markdown document, up to the next "## " heading. */
function section(markdown: string, heading: string): string {
    const start = markdown.indexOf(`\n## ${heading}\n`)
    expect(start, `section ## ${heading}`).toBeGreaterThanOrEqual(0)
    const end = markdown.indexOf('\n## ', start + 1)
    return markdown.slice(start, end < 0 ? undefined : end)
}

/** Names in backticks in the first column of the table rows of a Markdown text. */
function firstColumnNames(markdown: string): string[] {
    return [...markdown.matchAll(/^\| `([^`]+)` \|/gm)].map((match) => match[1] ?? '')
}

function unique(values: Iterable<string>): string[] {
    return [...new Set(values)].sort()
}

const theming = read(path.join(repositoryRoot, 'THEMING.md'))
const tokens = readCss(path.join(stylesDirectory, 'tokens.css'))
const iconsCss = readCss(path.join(stylesDirectory, 'icons.css'))
const baseCssFiles = filesIn(stylesDirectory, '.css').filter((file) => !file.endsWith('dev.css'))
const declaredVariables = unique(
    [...tokens.matchAll(/^\s*(--wmp-[a-z0-9-]+)\s*:/gm)].map((match) => match[1] ?? ''),
)

describe('THEMING.md', () => {
    it('documents the current version of the theme API', () => {
        expect(theming).toContain(`\nVersion ${themeApiVersion}\n`)
        expect(read(path.join(sourceDirectory, '..', 'index.html'))).toContain(
            `data-theme-api="${themeApiVersion}"`,
        )
    })

    it('documents exactly the parts of the theme API', () => {
        const documented = unique(firstColumnNames(section(theming, 'Parts')))
        expect(documented).toEqual(unique(parts))
    })

    it('documents every variable of the base and only existing ones', () => {
        const known = new Set([...declaredVariables, ...runtimeVariables])
        const mentioned = unique(
            [...theming.matchAll(/--wmp-[a-z0-9<>-]*[a-z0-9>]/g)]
                .map((match) => match[0])
                .filter((name) => !name.includes('<')),
        )

        expect(declaredVariables.filter((name) => !mentioned.includes(name))).toEqual([])
        expect(runtimeVariables.filter((name) => !mentioned.includes(name))).toEqual([])
        expect(mentioned.filter((name) => !known.has(name))).toEqual([])
    })

    it('documents every icon slot with its default file', () => {
        const rows = section(theming, 'Icons')
        for (const slot of iconSlotNames) {
            expect(rows, slot).toContain(
                `| \`${slot}\` | \`--wmp-icon-${slot}\` | \`${iconSlots[slot].file}\` |`,
            )
        }
        expect(unique(firstColumnNames(rows))).toEqual(unique(iconSlotNames))
    })
})

describe('parts in the code', () => {
    it('are all part of the theme API', () => {
        const known = new Set<string>(parts)
        const used = new Map<string, string>()
        for (const file of filesIn(sourceDirectory, '.vue')) {
            for (const match of read(file).matchAll(
                /(?<![:\w-])(?:data-part|part)="([a-z0-9-]+)"/g,
            )) {
                used.set(match[1] ?? '', path.relative(sourceDirectory, file))
            }
        }
        for (const file of baseCssFiles) {
            for (const match of readCss(file).matchAll(/\[data-part='([a-z0-9-]+)'\]/g)) {
                used.set(match[1] ?? '', path.relative(sourceDirectory, file))
            }
        }

        const unknown = [...used]
            .filter(([name]) => !known.has(name) && !name.startsWith(devPartPrefix))
            .map(([name, file]) => `${name} (${file})`)
        expect(unknown).toEqual([])
    })
})

describe('variables in the base CSS', () => {
    it('are declared in tokens.css or set at runtime', () => {
        const known = new Set([...declaredVariables, ...runtimeVariables])
        const used = unique(
            baseCssFiles.flatMap((file) =>
                [...readCss(file).matchAll(/var\((--wmp-[a-z0-9-]+)/g)].map(
                    (match) => match[1] ?? '',
                ),
            ),
        )
        expect(used.filter((name) => !known.has(name))).toEqual([])
    })
})

describe('icon slots', () => {
    it('are mapped in icons.css and have their default in tokens.css', () => {
        for (const slot of iconSlotNames) {
            expect(iconsCss, slot).toContain(
                `[data-icon='${slot}'] {\n        --_icon: var(--wmp-icon-${slot});`,
            )
            expect(tokens, slot).toContain(`--wmp-icon-${slot}: url('${iconSlots[slot].file}');`)
        }
    })

    it('refer to existing files', () => {
        for (const slot of iconSlotNames) {
            expect(existsSync(path.join(webAssets, iconSlots[slot].file)), slot).toBe(true)
        }
    })

    it('use every shipped icon file', () => {
        const referenced = new Set(iconSlotNames.map((slot) => iconSlots[slot].file))
        const phosphor = path.join(webAssets, 'icons', 'phosphor')
        const shipped = filesIn(phosphor, '.svg').map(
            (file) => `/${path.relative(webAssets, file).replaceAll('\\', '/')}`,
        )

        expect(shipped.length).toBeGreaterThan(0)
        expect(shipped.filter((file) => !referenced.has(file))).toEqual([])
    })
})

describe('breakpoint', () => {
    it('is the same in the code and in the base CSS', () => {
        expect(desktopMediaQuery).toBe('(width >= 64rem)')
        expect(mobileMediaQuery).toBe('(width < 64rem)')

        const layout = readCss(path.join(stylesDirectory, 'layout.css'))
        const mobile = readCss(path.join(stylesDirectory, 'mobile.css'))
        expect(layout).toContain(`@media ${desktopMediaQuery} {`)
        expect(mobile).toContain(`@media ${mobileMediaQuery} {`)

        // No other width breakpoints in the base CSS.
        const widthQueries = baseCssFiles.flatMap((file) =>
            [...readCss(file).matchAll(/@media ([^{]*width[^{]*)\{/g)].map((match) =>
                (match[1] ?? '').trim(),
            ),
        )
        expect(
            unique(widthQueries).filter(
                (query) => query !== desktopMediaQuery && query !== mobileMediaQuery,
            ),
        ).toEqual([])
    })
})

describe('shipped themes', () => {
    for (const theme of themes) {
        describe(theme.id, () => {
            const file = path.join(webAssets, theme.stylesheet)
            const css = readCss(file)

            it('exists as one CSS file in its own folder', () => {
                expect(theme.stylesheet).toBe(`/themes/${theme.id}/theme.css`)
                expect(existsSync(file)).toBe(true)
            })

            it('has no layers, no imports and no external URLs', () => {
                expect(css).not.toMatch(/@layer\b/)
                expect(css).not.toMatch(/@import\b/)
                expect(css).not.toMatch(/url\(\s*['"]?(?:[a-z]+:|\/\/)/i)
            })

            it('refers only to existing files with root-absolute URLs', () => {
                const urls = [...css.matchAll(/url\(\s*['"]?([^'")]+)['"]?\s*\)/g)].map(
                    (match) => match[1] ?? '',
                )
                expect(urls.length).toBeGreaterThan(0)
                for (const url of urls) {
                    expect(url, url).toMatch(new RegExp(`^/themes/${theme.id}/images/`))
                    expect(existsSync(path.join(webAssets, url)), url).toBe(true)
                }
            })

            it('uses only existing variables and the desktop breakpoint', () => {
                const known = new Set(declaredVariables)
                const variables = unique(
                    [...css.matchAll(/(--wmp-[a-z0-9-]+)\s*:/g)].map((match) => match[1] ?? ''),
                )
                expect(variables.filter((name) => !known.has(name))).toEqual([])

                const queries = unique(
                    [...css.matchAll(/@media ([^{]*)\{/g)].map((match) => (match[1] ?? '').trim()),
                )
                expect(queries.filter((query) => query !== desktopMediaQuery)).toEqual([])
            })

            it('uses only parts of the theme API', () => {
                const known = new Set<string>(parts)
                const used = unique(
                    [...css.matchAll(/\[data-part='([a-z0-9-]+)'\]/g)].map(
                        (match) => match[1] ?? '',
                    ),
                )
                expect(used.filter((name) => !known.has(name))).toEqual([])
            })
        })
    }
})
