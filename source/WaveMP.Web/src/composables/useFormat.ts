import { useI18n } from 'vue-i18n'
import {
    formatDate,
    formatDecibels,
    formatDuration,
    formatFileSize,
    formatFrequency,
    formatNumber,
    formatPercent,
    formatYear,
} from '@/i18n/format'

/** The formatting functions of src/i18n/format.ts, bound to the current language. */
export function useFormat() {
    const { locale } = useI18n()

    return {
        duration: (seconds: number) => formatDuration(seconds, locale.value),
        year: (year: number) => formatYear(year, locale.value),
        number: (value: number, options?: Intl.NumberFormatOptions) =>
            formatNumber(value, locale.value, options),
        percent: (ratio: number) => formatPercent(ratio, locale.value),
        date: (date: Date | number, options?: Intl.DateTimeFormatOptions) =>
            formatDate(date, locale.value, options),
        fileSize: (bytes: number) => formatFileSize(bytes, locale.value),
        decibels: (value: number) => formatDecibels(value, locale.value),
        frequency: (hertz: number) => formatFrequency(hertz, locale.value),
    }
}
