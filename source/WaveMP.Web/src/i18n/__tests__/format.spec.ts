import { describe, expect, it } from 'vitest'
import {
    formatDate,
    formatDuration,
    formatFileSize,
    formatNumber,
    formatPercent,
    formatYear,
} from '../format'

describe('formatDuration', () => {
    it('formats minutes and seconds', () => {
        expect(formatDuration(0, 'en')).toBe('0:00')
        expect(formatDuration(7, 'en')).toBe('0:07')
        expect(formatDuration(65, 'de')).toBe('1:05')
        expect(formatDuration(599.9, 'en')).toBe('9:59')
        expect(formatDuration(3599, 'en')).toBe('59:59')
    })

    it('adds hours from one hour on', () => {
        expect(formatDuration(3600, 'en')).toBe('1:00:00')
        expect(formatDuration(3723, 'de')).toBe('1:02:03')
    })

    it('shows invalid and negative values as zero', () => {
        expect(formatDuration(-5, 'en')).toBe('0:00')
        expect(formatDuration(Number.NaN, 'en')).toBe('0:00')
        expect(formatDuration(Number.POSITIVE_INFINITY, 'en')).toBe('0:00')
    })
})

describe('formatYear', () => {
    it('never groups the digits', () => {
        expect(formatYear(2007, 'en')).toBe('2007')
        expect(formatYear(2007, 'de')).toBe('2007')
    })
})

describe('formatNumber', () => {
    it('uses the separators of the language', () => {
        expect(formatNumber(12345, 'en')).toBe('12,345')
        expect(formatNumber(12345, 'de')).toBe('12.345')
        expect(formatNumber(1.5, 'de')).toBe('1,5')
    })
})

describe('formatPercent', () => {
    it('formats ratios as whole percentages', () => {
        expect(formatPercent(0.8, 'en')).toBe('80%')
        // German puts a (non-breaking) space before the percent sign.
        expect(formatPercent(0.8, 'de')).toMatch(/^80\s%$/u)
        expect(formatPercent(0.333, 'en')).toBe('33%')
    })
})

describe('formatDate', () => {
    const date = new Date(Date.UTC(2007, 0, 30, 12))

    it('contains the year in the medium style', () => {
        expect(formatDate(date, 'en')).toContain('2007')
        expect(formatDate(date, 'de')).toContain('2007')
    })

    it('follows the language', () => {
        expect(formatDate(date, 'de')).not.toBe(formatDate(date, 'en'))
        expect(formatDate(date, 'de', { month: 'long', timeZone: 'UTC' })).toBe('Januar')
        expect(formatDate(date, 'en', { month: 'long', timeZone: 'UTC' })).toBe('January')
    })
})

describe('formatFileSize', () => {
    it('uses the largest fitting unit', () => {
        expect(formatFileSize(0, 'en')).toBe('0 byte')
        expect(formatFileSize(999, 'en')).toBe('999 byte')
        expect(formatFileSize(1500, 'en')).toBe('1.5 kB')
        expect(formatFileSize(4_200_000, 'de')).toBe('4,2 MB')
        expect(formatFileSize(3_000_000_000, 'en')).toBe('3 GB')
    })

    it('shows invalid and negative values as zero', () => {
        expect(formatFileSize(-1, 'en')).toBe('0 byte')
        expect(formatFileSize(Number.NaN, 'en')).toBe('0 byte')
    })
})
