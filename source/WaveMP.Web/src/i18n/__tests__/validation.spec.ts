import { readFileSync } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { ValidationErrorCodeObject } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import {
    validationMessage,
    validationMessageKeys,
    validationMessagesByField,
    type Translate,
} from '../validation'

const localesDirectory = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../locales')

function translator(locale: 'en' | 'de'): Translate {
    const { t } = createWaveMpI18n(locale).global
    return (key, parameters) => t(key, parameters)
}

const en = translator('en')
const de = translator('de')

describe('validationMessageKeys', () => {
    it('have a message for every code of the API in every language', () => {
        expect(Object.keys(validationMessageKeys).sort()).toEqual(
            Object.values(ValidationErrorCodeObject).sort(),
        )
        for (const locale of ['en', 'de']) {
            const messages = JSON.parse(
                readFileSync(path.join(localesDirectory, `${locale}.json`), 'utf8'),
            )
            for (const key of Object.values(validationMessageKeys)) {
                const [, name] = key.split('.')
                expect(messages.validation[name ?? ''], `${locale}: ${key}`).toBeTypeOf('string')
            }
        }
    })
})

describe('validationMessage', () => {
    it('translates codes with their limits', () => {
        expect(validationMessage({ field: 'name', code: 'max_length', max: 50 }, en)).toBe(
            'This text is too long (maximum length: 50).',
        )
        expect(
            validationMessage({ field: 'year', code: 'out_of_range', min: 1, max: 9999 }, de),
        ).toBe('Gib einen Wert von 1 bis 9999 ein.')
        expect(validationMessage({ field: 'name', code: 'required' }, de)).toBe(
            'Dieses Feld ist erforderlich.',
        )
        expect(validationMessage({ field: 'q', code: 'min_length', min: 1 }, de)).toBe(
            'Dieser Text ist zu kurz (mindestens 1 Zeichen).',
        )
    })

    it('formats the limits and uses the texts of the form', () => {
        const message = validationMessage(
            { field: 'scanIntervalMinutes', code: 'out_of_range', min: 15, max: 1440 },
            en,
            {
                keys: { out_of_range: 'adminLibrary.errors.interval' },
                formatLimit: (value) => value.toLocaleString('en'),
            },
        )

        expect(message).toBe('Choose an interval between 15 and 1,440 minutes.')
    })

    it('shows an error without its needed limits as invalid value', () => {
        expect(validationMessage({ field: 'size', code: 'out_of_range', min: 1 }, en)).toBe(
            'This value is not valid.',
        )
    })
})

describe('validationMessagesByField', () => {
    it('keeps the first message of every known member', () => {
        const messages = validationMessagesByField(
            [
                { field: 'audioExtensions[3]', code: 'invalid_format' },
                { field: 'audioExtensions', code: 'max_items', max: 50 },
                { field: 'videoExtensions', code: 'duplicate' },
                { field: '', code: 'required' },
                { field: 'unknown', code: 'required' },
            ],
            { audioExtensions: 'audio', videoExtensions: 'video' },
            en,
        )

        expect(messages).toEqual({
            audio: 'This value has an invalid format.',
            video: 'This value is already used in another field.',
        })
    })

    it('ignores members inherited from Object', () => {
        const messages = validationMessagesByField(
            [{ field: 'constructor', code: 'required' }],
            { name: 'name' },
            en,
        )

        expect(messages).toEqual({})
    })
})
