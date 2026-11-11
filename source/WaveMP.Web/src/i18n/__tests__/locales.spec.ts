import { describe, expect, it } from 'vitest'
import { LanguageObject } from '@/api/generated/models'
import { createWaveMpI18n, setLocale } from '../index'
import { browserLocales, defaultLocale, resolveLocale, supportedLocales } from '../locales'

describe('resolveLocale', () => {
    it('supports English and German with English as default', () => {
        expect(supportedLocales).toEqual(['en', 'de'])
        expect(defaultLocale).toBe('en')
    })

    it('are exactly the languages of the API', () => {
        expect([...supportedLocales].sort()).toEqual(Object.values(LanguageObject).sort())
    })

    it('matches exact tags case-insensitively', () => {
        expect(resolveLocale(['DE'])).toBe('de')
        expect(resolveLocale(['en'])).toBe('en')
    })

    it('matches the primary subtag of regional tags', () => {
        expect(resolveLocale(['de-AT'])).toBe('de')
        expect(resolveLocale(['en-GB'])).toBe('en')
        expect(resolveLocale(['de_CH'])).toBe('de')
    })

    it('takes the first supported language in order of preference', () => {
        expect(resolveLocale(['fr-FR', 'de-DE', 'en-US'])).toBe('de')
        expect(resolveLocale(['en-US', 'de-DE'])).toBe('en')
    })

    it('falls back to English', () => {
        expect(resolveLocale(['fr-FR', 'ja'])).toBe('en')
        expect(resolveLocale([])).toBe('en')
        expect(resolveLocale([''])).toBe('en')
    })
})

describe('browserLocales', () => {
    it('prefers the list of languages', () => {
        expect(browserLocales({ languages: ['de-DE', 'en'], language: 'fr' })).toEqual([
            'de-DE',
            'en',
        ])
    })

    it('falls back to the single language', () => {
        expect(browserLocales({ languages: [], language: 'de' })).toEqual(['de'])
        expect(browserLocales({ languages: [], language: '' })).toEqual([])
    })
})

describe('setLocale', () => {
    it('switches the messages and the document language', () => {
        const i18n = createWaveMpI18n('en')
        const doc = document.implementation.createHTMLDocument('test')

        expect(i18n.global.t('nav.library')).toBe('Library')

        setLocale(i18n.global, 'de', doc)

        expect(i18n.global.locale.value).toBe('de')
        expect(i18n.global.t('nav.library')).toBe('Bibliothek')
        expect(doc.documentElement.lang).toBe('de')
    })

    it('formats named placeholders', () => {
        const i18n = createWaveMpI18n('de')

        expect(i18n.global.t('placeholders.album', { number: 2 })).toBe('Platzhalter-Album 2')
    })
})
