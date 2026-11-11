// Errors of the API client. Failed requests reject with the Problem Details of the server (with a
// machine-readable code) or, without a body, with a Kiota ApiError; both carry the status code.
// Network errors carry neither.

import {
    ProblemCodeObject,
    ValidationErrorCodeObject,
    type ProblemCode,
    type ValidationError,
    type ValidationErrorCode,
    type ValidationProblem,
} from './generated/models'

const problemCodes = new Set<string>(Object.values(ProblemCodeObject))
const validationErrorCodes = new Set<string>(Object.values(ValidationErrorCodeObject))

/** Machine-readable code of a Problem Details error, or undefined. */
export function problemCodeOf(error: unknown): ProblemCode | undefined {
    const code = (error as { code?: unknown } | null | undefined)?.code
    return typeof code === 'string' && problemCodes.has(code) ? (code as ProblemCode) : undefined
}

/** HTTP status code of a failed request, or undefined for network errors. */
export function statusOf(error: unknown): number | undefined {
    const status = (error as { responseStatusCode?: unknown } | null | undefined)
        ?.responseStatusCode
    return typeof status === 'number' ? status : undefined
}

/** True if the server rejected the session token. */
export function isUnauthorized(error: unknown): boolean {
    return statusOf(error) === 401 || problemCodeOf(error) === ProblemCodeObject.AuthUnauthorized
}

/** A field error of a validation problem with a known code; unknown codes become invalid_value. */
export interface FieldError {
    /** JSON name of the member or name of the query parameter; empty for the body as a whole. */
    readonly field: string
    readonly code: ValidationErrorCode
    readonly min?: number
    readonly max?: number
}

/**
 * The errors of a validation problem (400, request.invalid): field, code and limits. The server
 * sends no texts; validationMessage (src/i18n/validation.ts) translates the codes.
 */
export function validationErrorsOf(error: unknown): FieldError[] {
    const errors = (error as ValidationProblem | null | undefined)?.errors
    if (!Array.isArray(errors)) {
        return []
    }

    return errors.filter(isFieldError).map((item) => ({
        field: item.field,
        code:
            typeof item.code === 'string' && validationErrorCodes.has(item.code)
                ? item.code
                : ValidationErrorCodeObject.Invalid_value,
        ...(typeof item.min === 'number' ? { min: item.min } : {}),
        ...(typeof item.max === 'number' ? { max: item.max } : {}),
    }))
}

/**
 * The top-level member of a field, e.g. "audioExtensions" for "audioExtensions[2]" or "name" for
 * "name.first".
 */
export function memberOf(field: string): string {
    return field.split(/[.[]/, 1)[0] ?? ''
}

/** The top-level members of a validation problem that have errors, each once. */
export function fieldErrorsOf(error: unknown): string[] {
    return [...new Set(validationErrorsOf(error).map((item) => memberOf(item.field)))]
}

function isFieldError(item: unknown): item is ValidationError & { field: string } {
    return typeof (item as ValidationError | null | undefined)?.field === 'string'
}
