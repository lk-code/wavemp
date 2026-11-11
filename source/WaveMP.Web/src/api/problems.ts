// Errors of the API client. Failed requests reject with the Problem Details of the server (with a
// machine-readable code) or, without a body, with a Kiota ApiError; both carry the status code.
// Network errors carry neither.

import {
    ProblemCodeObject,
    type ProblemCode,
    type ValidationProblemDetails,
} from './generated/models'

const problemCodes = new Set<string>(Object.values(ProblemCodeObject))

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

/**
 * The fields of a validation problem (400, request.invalid) that have errors, by their JSON names.
 * Errors of values that could not be read at all are keyed by a JSON path such as "$.name"; the
 * leading "$." is removed.
 */
export function fieldErrorsOf(error: unknown): string[] {
    const errors = (error as ValidationProblemDetails | null | undefined)?.errors
    const fields = errors?.additionalData ? Object.keys(errors.additionalData) : []
    return fields.map((field) => field.replace(/^\$\./, ''))
}
