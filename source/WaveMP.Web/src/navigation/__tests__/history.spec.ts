import { describe, expect, it } from 'vitest'
import { historyAvailability } from '../history'

describe('historyAvailability', () => {
    it('reads the neighbors that the router keeps in history.state', () => {
        expect(historyAvailability({ back: '/library/songs', forward: null })).toEqual({
            back: true,
            forward: false,
        })
        expect(historyAvailability({ back: null, forward: '/playlists' })).toEqual({
            back: false,
            forward: true,
        })
    })

    it('treats a missing state as no history', () => {
        expect(historyAvailability(null)).toEqual({ back: false, forward: false })
        expect(historyAvailability(undefined)).toEqual({ back: false, forward: false })
        expect(historyAvailability('state')).toEqual({ back: false, forward: false })
        expect(historyAvailability({ back: '' })).toEqual({ back: false, forward: false })
    })
})
