import { beforeEach, describe, expect, it } from 'vitest'
import { themeApiVersion } from '../parts'
import { applyTheme, defaultThemeId, resolveThemeId, themeLinkId, themes } from '../themes'

describe('resolveThemeId', () => {
    it('returns known themes', () => {
        expect(resolveThemeId('midnight')).toBe('midnight')
    })

    it('falls back to the default theme', () => {
        expect(defaultThemeId).toBe('midnight')
        expect(resolveThemeId('unknown')).toBe('midnight')
        expect(resolveThemeId('')).toBe('midnight')
        expect(resolveThemeId(null)).toBe('midnight')
        expect(resolveThemeId(undefined)).toBe('midnight')
    })

    it('knows a stylesheet for every theme', () => {
        for (const theme of themes) {
            expect(theme.stylesheet).toBe(`/themes/${theme.id}/theme.css`)
        }
    })
})

describe('applyTheme', () => {
    let doc: Document

    beforeEach(() => {
        doc = document.implementation.createHTMLDocument('test')
    })

    function themeLinks(): HTMLLinkElement[] {
        return [...doc.querySelectorAll<HTMLLinkElement>(`link#${themeLinkId}`)]
    }

    it('sets the theme and the theme API version on the root element', () => {
        applyTheme('midnight', doc)

        expect(doc.documentElement.dataset.theme).toBe('midnight')
        expect(doc.documentElement.dataset.themeApi).toBe(String(themeApiVersion))
    })

    it('creates the theme link if it is missing', () => {
        applyTheme('midnight', doc)

        const links = themeLinks()
        expect(links).toHaveLength(1)
        expect(links[0]?.rel).toBe('stylesheet')
        expect(links[0]?.getAttribute('href')).toBe('/themes/midnight/theme.css')
    })

    it('updates an existing link and keeps exactly one', () => {
        for (const href of ['/themes/old/theme.css', '/themes/other/theme.css']) {
            const link = doc.createElement('link')
            link.id = themeLinkId
            link.rel = 'stylesheet'
            link.href = href
            doc.head.append(link)
        }

        applyTheme('midnight', doc)

        const links = themeLinks()
        expect(links).toHaveLength(1)
        expect(links[0]?.getAttribute('href')).toBe('/themes/midnight/theme.css')
    })
})
