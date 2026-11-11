// Locale-aware formatting of numbers, durations and dates. Everything goes through Intl, so the
// formats follow the language of the user and no format strings are needed in the messages.

function digits(value: number, locale: string, minimumIntegerDigits = 1): string {
    return new Intl.NumberFormat(locale, { useGrouping: false, minimumIntegerDigits }).format(value)
}

/**
 * Formats a duration in seconds as m:ss, or h:mm:ss from one hour on.
 * Negative or invalid values are shown as 0:00.
 */
export function formatDuration(seconds: number, locale: string): string {
    const total = Number.isFinite(seconds) && seconds > 0 ? Math.floor(seconds) : 0
    const hours = Math.floor(total / 3600)
    const minutes = Math.floor((total % 3600) / 60)
    const rest = total % 60

    if (hours > 0) {
        return `${digits(hours, locale)}:${digits(minutes, locale, 2)}:${digits(rest, locale, 2)}`
    }

    return `${digits(minutes, locale)}:${digits(rest, locale, 2)}`
}

/** Formats a year without digit grouping (2007, not 2,007). */
export function formatYear(year: number, locale: string): string {
    return digits(year, locale)
}

/** Formats a number with the grouping and decimal separators of the language. */
export function formatNumber(
    value: number,
    locale: string,
    options?: Intl.NumberFormatOptions,
): string {
    return new Intl.NumberFormat(locale, options).format(value)
}

/** Formats a ratio between 0 and 1 as a percentage (0.8 is 80 %). */
export function formatPercent(ratio: number, locale: string): string {
    return new Intl.NumberFormat(locale, { style: 'percent', maximumFractionDigits: 0 }).format(
        ratio,
    )
}

/** Formats a date, by default in the medium style of the language. */
export function formatDate(
    date: Date | number,
    locale: string,
    options: Intl.DateTimeFormatOptions = { dateStyle: 'medium' },
): string {
    return new Intl.DateTimeFormat(locale, options).format(date)
}
