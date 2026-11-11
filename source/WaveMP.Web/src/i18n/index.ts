import { createI18n } from 'vue-i18n'
import en from '@/locales/en.json'
import de from '@/locales/de.json'
import { defaultLocale, type SupportedLocale } from './locales'

/** Structure of all messages; English is the source language. */
export type MessageSchema = typeof en

export function createWaveMpI18n(locale: SupportedLocale = defaultLocale) {
    return createI18n<[MessageSchema], SupportedLocale, false>({
        legacy: false,
        locale,
        fallbackLocale: defaultLocale,
        messages: { en, de },
    })
}

export type WaveMpI18n = ReturnType<typeof createWaveMpI18n>

/** The global composer (i18n.global, or useI18n({ useScope: 'global' }) in components). */
export interface LocaleTarget {
    locale: { value: string }
}

/**
 * Switches the language of the web app and of the document. useLocaleSync calls it with the
 * language of the selected profile, or with the browser language before a profile is selected.
 */
export function setLocale(
    composer: LocaleTarget,
    locale: SupportedLocale,
    doc: Document = document,
): void {
    composer.locale.value = locale
    doc.documentElement.lang = locale
}
