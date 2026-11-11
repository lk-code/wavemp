import { describe, expect, it } from 'vitest'
import type { TrackMetadata } from '@/api/generated/models'
import {
    metadataChanges,
    normalizeText,
    parseMetadataNumber,
    toMetadataForm,
    validateMetadataForm,
    type MetadataForm,
} from '../metadata'

const metadata: TrackMetadata = {
    trackId: 'song',
    status: 'Editable',
    audioFilePath: 'Band/Album/01 - Song.mp3',
    title: 'Song',
    artist: 'Band',
    album: 'Album',
    albumArtist: null,
    year: 2001,
    trackNumber: 1,
    discNumber: null,
    genre: 'Rock',
}

function form(values: Partial<MetadataForm> = {}): MetadataForm {
    return { ...toMetadataForm(metadata), ...values }
}

describe('toMetadataForm', () => {
    it('turns every value into text and missing values into empty fields', () => {
        expect(toMetadataForm(metadata)).toEqual({
            title: 'Song',
            artist: 'Band',
            album: 'Album',
            albumArtist: '',
            year: '2001',
            trackNumber: '1',
            discNumber: '',
            genre: 'Rock',
        })
    })
})

describe('normalizeText', () => {
    it('trims and joins white space like the server', () => {
        expect(normalizeText('  New \t  Title \n')).toBe('New Title')
        expect(normalizeText('   ')).toBe('')
    })
})

describe('parseMetadataNumber', () => {
    it.each([
        ['', null],
        ['  ', null],
        ['1', 1],
        [' 2001 ', 2001],
        ['9999', 9999],
        ['0', undefined],
        ['10000', undefined],
        ['1.5', undefined],
        ['-3', undefined],
        ['abc', undefined],
        ['1e3', undefined],
    ])('reads "%s" as %s', (value, expected) => {
        expect(parseMetadataNumber(value)).toBe(expected)
    })
})

describe('validateMetadataForm', () => {
    it('accepts empty numbers and whole numbers from 1 to 9999', () => {
        expect(
            validateMetadataForm(form({ year: '', trackNumber: '9999', discNumber: '1' })),
        ).toEqual({})
    })

    it('marks every number field with an invalid value', () => {
        expect(
            validateMetadataForm(form({ year: '0', trackNumber: '10000', discNumber: 'abc' })),
        ).toEqual({
            year: 'number',
            trackNumber: 'number',
            discNumber: 'number',
        })
        expect(validateMetadataForm(form({ year: '1.5' }))).toEqual({ year: 'number' })
    })
})

describe('metadataChanges', () => {
    it('is empty without changes', () => {
        expect(metadataChanges(form(), form())).toEqual({})
    })

    it('ignores changes of white space only', () => {
        expect(metadataChanges(form(), form({ title: '  Song ', genre: 'Rock  ' }))).toEqual({})
    })

    it('sends normalized texts and numbers as numbers', () => {
        expect(
            metadataChanges(
                form(),
                form({
                    title: ' New   Title ',
                    albumArtist: 'Band',
                    year: '1999',
                    discNumber: '2',
                }),
            ),
        ).toEqual({ title: 'New Title', albumArtist: 'Band', year: 1999, discNumber: 2 })
    })

    it('removes cleared values with null', () => {
        expect(metadataChanges(form(), form({ genre: '  ', year: '', trackNumber: '' }))).toEqual({
            genre: null,
            year: null,
            trackNumber: null,
        })
    })

    it('leaves out invalid numbers', () => {
        expect(metadataChanges(form(), form({ year: 'abc' }))).toEqual({})
    })
})
