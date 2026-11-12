import { createPinia, setActivePinia } from 'pinia'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises } from '@vue/test-utils'
import type { Equalizer, UpdateEqualizerSettings } from '@/api/generated/models'
import { getEqualizer } from '@/player/api'
import { fallbackEqualizerBands } from '@/player/equalizer'
import { equalizerSaveDelay, useEqualizerStore } from '../equalizer'
import { useSessionStore } from '../session'

vi.mock('@/player/api', () => ({
    getEqualizer: vi.fn(),
}))

const rock = [5, 4, 3, 1, -1, -1, 1, 3, 4, 5]
const pop = [-1, 0, 2, 4, 5, 4, 2, 0, -1, -1]
const own = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]

const definition: Equalizer = {
    minGainDb: -12,
    maxGainDb: 12,
    bands: fallbackEqualizerBands.map((band) => ({
        frequencyHz: band.frequency,
        filterType: band.type,
        q: band.q,
    })),
    presets: [
        { preset: 'Rock', gainsDb: rock },
        { preset: 'Pop', gainsDb: pop },
    ],
}

describe('equalizer store', () => {
    let updateEqualizer: ReturnType<
        typeof vi.fn<(change: UpdateEqualizerSettings) => Promise<void>>
    >

    beforeEach(() => {
        vi.useFakeTimers()
        setActivePinia(createPinia())
        vi.mocked(getEqualizer).mockReset().mockResolvedValue(definition)
        const session = useSessionStore()
        session.token = 'token'
        session.equalizer = {
            enabled: false,
            preset: 'Flat',
            preampDb: 0,
            gainsDb: [0, 0, 0, 0, 0, 0, 0, 0, 0, 0],
            customGainsDb: own,
        }
        updateEqualizer = vi.fn((_change: UpdateEqualizerSettings) => Promise.resolve())
        session.updateEqualizer = updateEqualizer
    })

    afterEach(() => {
        vi.useRealTimers()
    })

    it('loads the definition once', async () => {
        const store = useEqualizerStore()

        await store.ensureDefinition()
        await store.ensureDefinition()

        expect(getEqualizer).toHaveBeenCalledOnce()
        expect(store.loadState).toBe('loaded')
        expect(store.layout).toEqual(fallbackEqualizerBands)
    })

    it('keeps the fallback bands when the definition fails and tries again later', async () => {
        vi.mocked(getEqualizer).mockRejectedValueOnce(new TypeError('Failed to fetch'))
        const store = useEqualizerStore()

        await store.ensureDefinition()
        expect(store.loadState).toBe('failed')
        expect(store.layout).toBe(fallbackEqualizerBands)

        await store.ensureDefinition()
        expect(store.loadState).toBe('loaded')
    })

    it('applies a preset at once and saves it after a short delay', async () => {
        const store = useEqualizerStore()
        await store.ensureDefinition()

        store.setEnabled(true)
        store.selectPreset('Rock')

        expect(store.output).toEqual({ enabled: true, preampDb: 0, gainsDb: rock })
        expect(updateEqualizer).not.toHaveBeenCalled()
        vi.advanceTimersByTime(equalizerSaveDelay)
        expect(updateEqualizer).toHaveBeenCalledOnce()
        expect(updateEqualizer).toHaveBeenCalledWith({ enabled: true, preset: 'Rock' })
    })

    it('turns a moved band into the own curve', async () => {
        const store = useEqualizerStore()
        await store.ensureDefinition()
        store.selectPreset('Rock')

        store.setBand(5, 2)
        expect(store.settings.preset).toBe('Custom')
        await store.flush()

        expect(updateEqualizer).toHaveBeenCalledWith({
            preset: 'Custom',
            customGainsDb: [5, 4, 3, 1, -1, 2, 1, 3, 4, 5],
        })
    })

    it('saves preamp and reset', async () => {
        const store = useEqualizerStore()
        store.setPreamp(-3.2)
        expect(store.settings.preampDb).toBe(-3)

        store.reset()
        await store.flush()

        expect(updateEqualizer).toHaveBeenCalledWith({ preampDb: 0, preset: 'Flat' })
    })

    it('restores the own curve when Custom is selected again', async () => {
        const store = useEqualizerStore()
        await store.ensureDefinition()

        store.selectPreset('Pop')
        expect(store.output.gainsDb).toEqual(pop)
        store.selectPreset('Custom')

        expect(store.output.gainsDb).toEqual(own)
    })

    it('falls back to the settings of the profile when saving fails', async () => {
        updateEqualizer.mockRejectedValueOnce(new TypeError('Failed to fetch'))
        const store = useEqualizerStore()

        store.setEnabled(true)
        expect(store.output.enabled).toBe(true)
        await store.flush()

        expect(store.saveError).toBe(true)
        expect(store.output.enabled).toBe(false)
    })

    it('keeps local changes until the last save is done', async () => {
        let finish: () => void = () => undefined
        updateEqualizer.mockImplementationOnce(
            () =>
                new Promise<void>((resolve) => {
                    finish = resolve
                }),
        )
        const store = useEqualizerStore()

        store.setEnabled(true)
        const saving = store.flush()
        store.setPreamp(4)
        finish()
        await saving

        expect(store.settings).toMatchObject({ enabled: true, preampDb: 4 })
        await store.flush()
        await flushPromises()
        // Confirmed: the settings of the profile apply again (the session took over the answer).
        expect(store.settings).toBe(useSessionStore().equalizer)
    })

    it('forgets local changes for another profile', () => {
        const store = useEqualizerStore()
        const session = useSessionStore()
        store.setEnabled(true)

        session.token = 'other'
        return flushPromises().then(() => {
            vi.advanceTimersByTime(equalizerSaveDelay)
            expect(store.output.enabled).toBe(false)
            expect(updateEqualizer).not.toHaveBeenCalled()
        })
    })
})
