// Form logic of the metadata editor: the form holds every field as text, the request contains
// only the fields the admin changed. Empty fields remove the value from the tags (null).

import type { TrackMetadata, UpdateTrackMetadata } from '@/api/generated/models'

export type MetadataTextField = 'title' | 'artist' | 'album' | 'albumArtist' | 'genre'
export type MetadataNumberField = 'year' | 'trackNumber' | 'discNumber'
export type MetadataField = MetadataTextField | MetadataNumberField

/** The values of the form, all as entered. */
export type MetadataForm = Record<MetadataField, string>

/** Errors of the form by field; "number" stands for a value that is no number from 1 to the maximum. */
export type MetadataFormErrors = Partial<Record<MetadataField, 'number'>>

/** Limits of the API (UpdateTrackMetadata). */
export const metadataLimits = {
    text: 512,
    genre: 128,
    numberDigits: 4,
    minNumber: 1,
    maxNumber: 9999,
} as const

export const metadataTextFields: readonly MetadataTextField[] = [
    'title',
    'artist',
    'album',
    'albumArtist',
    'genre',
]

export const metadataNumberFields: readonly MetadataNumberField[] = [
    'year',
    'trackNumber',
    'discNumber',
]

/** data-field of each field in the theme API. */
export const metadataDataFields: Readonly<Record<MetadataField, string>> = {
    title: 'title',
    artist: 'artist',
    album: 'album',
    albumArtist: 'album-artist',
    year: 'year',
    trackNumber: 'number',
    discNumber: 'disc',
    genre: 'genre',
}

export function emptyMetadataForm(): MetadataForm {
    return {
        title: '',
        artist: '',
        album: '',
        albumArtist: '',
        year: '',
        trackNumber: '',
        discNumber: '',
        genre: '',
    }
}

/** The form of the metadata of a track. */
export function toMetadataForm(metadata: TrackMetadata): MetadataForm {
    return {
        title: metadata.title ?? '',
        artist: metadata.artist ?? '',
        album: metadata.album ?? '',
        albumArtist: metadata.albumArtist ?? '',
        year: numberText(metadata.year),
        trackNumber: numberText(metadata.trackNumber),
        discNumber: numberText(metadata.discNumber),
        genre: metadata.genre ?? '',
    }
}

/** A text as the server stores it: trimmed, white space joined to single spaces. */
export function normalizeText(value: string): string {
    return value.trim().replace(/\s+/g, ' ')
}

/** The number of a field: null if empty, undefined if it is no whole number in the limits. */
export function parseMetadataNumber(value: string): number | null | undefined {
    const text = value.trim()
    if (text === '') {
        return null
    }
    if (!/^\d+$/.test(text)) {
        return undefined
    }
    const number = Number(text)
    return number >= metadataLimits.minNumber && number <= metadataLimits.maxNumber
        ? number
        : undefined
}

/** The fields whose values cannot be sent. */
export function validateMetadataForm(form: MetadataForm): MetadataFormErrors {
    const errors: MetadataFormErrors = {}
    for (const field of metadataNumberFields) {
        if (parseMetadataNumber(form[field]) === undefined) {
            errors[field] = 'number'
        }
    }
    return errors
}

/**
 * The changed fields of the form compared with the loaded values; empty fields become null.
 * Unchanged fields are left out, so values the admin did not touch are never written.
 */
export function metadataChanges(original: MetadataForm, form: MetadataForm): UpdateTrackMetadata {
    const changes: UpdateTrackMetadata = {}
    for (const field of metadataTextFields) {
        const value = normalizeText(form[field])
        if (value !== normalizeText(original[field])) {
            changes[field] = value === '' ? null : value
        }
    }
    for (const field of metadataNumberFields) {
        const value = parseMetadataNumber(form[field])
        if (value !== undefined && value !== parseMetadataNumber(original[field])) {
            changes[field] = value
        }
    }
    return changes
}

function numberText(value: number | null | undefined): string {
    return value === null || value === undefined ? '' : String(value)
}
