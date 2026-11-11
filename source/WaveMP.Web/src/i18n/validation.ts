// Messages of the validation errors of the API. The server only sends a code per field plus its
// limits (min, max); the web app translates them. Forms may replace the message of a code with a
// text of their own that uses the same placeholders.

import { ValidationErrorCodeObject, type ValidationErrorCode } from '@/api/generated/models'
import { memberOf, type FieldError } from '@/api/problems'

/** Message key of every validation code. */
export const validationMessageKeys: Readonly<Record<ValidationErrorCode, string>> = {
    required: 'validation.required',
    min_length: 'validation.minLength',
    max_length: 'validation.maxLength',
    min_items: 'validation.minItems',
    max_items: 'validation.maxItems',
    out_of_range: 'validation.outOfRange',
    invalid_format: 'validation.invalidFormat',
    invalid_value: 'validation.invalidValue',
    duplicate: 'validation.duplicate',
}

/** The limits a code needs in its message; without them the error is shown as invalid_value. */
const neededLimits: Readonly<Partial<Record<ValidationErrorCode, readonly ('min' | 'max')[]>>> = {
    min_length: ['min'],
    max_length: ['max'],
    min_items: ['min'],
    max_items: ['max'],
    out_of_range: ['min', 'max'],
}

/** Translates a message key with named parameters, e.g. t of vue-i18n. */
export type Translate = (key: string, parameters: Record<string, string>) => string

export interface ValidationMessageOptions {
    /** Message keys that replace the general ones for single codes. */
    readonly keys?: Readonly<Partial<Record<ValidationErrorCode, string>>>
    /** Formats the limits, e.g. as localized number or file size; String by default. */
    readonly formatLimit?: (value: number) => string
}

/** The message of a validation error with its limits as placeholders {min} and {max}. */
export function validationMessage(
    error: FieldError,
    translate: Translate,
    options: ValidationMessageOptions = {},
): string {
    const limits = neededLimits[error.code] ?? []
    if (limits.some((limit) => error[limit] === undefined)) {
        return translate(validationMessageKeys[ValidationErrorCodeObject.Invalid_value], {})
    }

    const format = options.formatLimit ?? String
    const parameters: Record<string, string> = {}
    if (error.min !== undefined) {
        parameters.min = format(error.min)
    }
    if (error.max !== undefined) {
        parameters.max = format(error.max)
    }

    return translate(options.keys?.[error.code] ?? validationMessageKeys[error.code], parameters)
}

/**
 * The first message of each top-level member that has errors, keyed by the name the form uses.
 * Members the form does not know (e.g. errors of the body as a whole) are left out.
 */
export function validationMessagesByField<Field extends string>(
    errors: readonly FieldError[],
    fields: Readonly<Record<string, Field>>,
    translate: Translate,
    options: (field: Field) => ValidationMessageOptions = () => ({}),
): Partial<Record<Field, string>> {
    const messages: Partial<Record<Field, string>> = {}
    for (const error of errors) {
        const member = memberOf(error.field)
        const field = Object.hasOwn(fields, member) ? fields[member] : undefined
        if (field !== undefined && messages[field] === undefined) {
            messages[field] = validationMessage(error, translate, options(field))
        }
    }
    return messages
}
