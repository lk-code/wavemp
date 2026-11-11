/** Languages of the web app. Every language has one JSON file in src/locales. */
export const supportedLocales = ['en', 'de'] as const

export type SupportedLocale = (typeof supportedLocales)[number]

export const defaultLocale: SupportedLocale = 'en'

export function isSupportedLocale(value: string): value is SupportedLocale {
    return (supportedLocales as readonly string[]).includes(value)
}

/**
 * Picks the first supported language from a list of preferred language tags (e.g. "de-AT"),
 * matching the exact tag or its primary subtag, case-insensitive. Falls back to English.
 */
export function resolveLocale(preferred: readonly string[]): SupportedLocale {
    for (const tag of preferred) {
        const normalized = tag.trim().toLowerCase()
        if (isSupportedLocale(normalized)) {
            return normalized
        }

        const primary = normalized.split(/[-_]/)[0] ?? ''
        if (isSupportedLocale(primary)) {
            return primary
        }
    }

    return defaultLocale
}

/** Preferred languages of the browser; until a profile is selected, they decide the language. */
export function browserLocales(
    nav: Pick<Navigator, 'languages' | 'language'> = navigator,
): string[] {
    if (nav.languages && nav.languages.length > 0) {
        return [...nav.languages]
    }

    return nav.language ? [nav.language] : []
}
