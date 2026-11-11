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

/**
 * Switches the language of the web app and of the document.
 * Prepared for step 4: after selecting a profile, the app calls this with the language stored
 * for the user. Until then, the browser language applies.
 */
export function setLocale(
    i18n: WaveMpI18n,
    locale: SupportedLocale,
    doc: Document = document,
): void {
    i18n.global.locale.value = locale
    doc.documentElement.lang = locale
}
