// Availability of the back and forward buttons. Vue Router keeps the neighboring entries of the
// browser history in history.state.back and history.state.forward.

export interface HistoryAvailability {
    readonly back: boolean
    readonly forward: boolean
}

export function historyAvailability(state: unknown): HistoryAvailability {
    const entries = (typeof state === 'object' && state !== null ? state : {}) as {
        back?: unknown
        forward?: unknown
    }

    return {
        back: typeof entries.back === 'string' && entries.back !== '',
        forward: typeof entries.forward === 'string' && entries.forward !== '',
    }
}
