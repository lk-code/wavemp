import { defineStore } from 'pinia'
import { computed, ref, shallowRef, watch } from 'vue'
import type { EqualizerPreset, UpdateEqualizerSettings } from '@/api/generated/models'
import { getEqualizer } from '@/player/api'
import {
    equalizerOutput,
    fallbackEqualizerBands,
    fallbackMaxGainDb,
    fallbackMinGainDb,
    resetEqualizer,
    toEqualizerDefinition,
    withBand,
    withPreamp,
    withPreset,
    type EqualizerDefinition,
    type EqualizerSettings,
} from '@/player/equalizer'
import { useSessionStore } from './session'

/** Milliseconds after the last change until it is saved, so dragging a slider sends one request. */
export const equalizerSaveDelay = 400

export type EqualizerLoadState = 'idle' | 'loading' | 'loaded' | 'failed'

/**
 * The equalizer of the profile: the definition of the server (bands, presets) and a local working
 * copy of the settings. Changes apply at once (the player follows output) and are saved shortly
 * after the last one; flush saves at once, e.g. when the view closes. If saving fails, the settings
 * of the profile apply again.
 */
export const useEqualizerStore = defineStore('equalizer', () => {
    const session = useSessionStore()

    const definition = shallowRef<EqualizerDefinition | null>(null)
    const loadState = ref<EqualizerLoadState>('idle')
    /** Local changes not yet confirmed by the server. */
    const draft = shallowRef<EqualizerSettings | null>(null)
    const saveError = ref(false)

    const settings = computed<EqualizerSettings>(() => draft.value ?? session.equalizer)
    /** What the audio engine applies. */
    const output = computed(() => equalizerOutput(settings.value, definition.value))
    /** The bands of the definition, or those of the fallback before it is loaded. */
    const layout = computed(() => definition.value?.bands ?? fallbackEqualizerBands)
    const minGainDb = computed(() => definition.value?.minGainDb ?? fallbackMinGainDb)
    const maxGainDb = computed(() => definition.value?.maxGainDb ?? fallbackMaxGainDb)

    let pending: UpdateEqualizerSettings | null = null
    let timer: ReturnType<typeof setTimeout> | null = null
    let inFlight = 0
    /** Increases with every profile, so answers of the previous one are ignored. */
    let generation = 0

    /** Loads the definition once; a failed load is tried again on the next call. */
    async function ensureDefinition(): Promise<void> {
        if (loadState.value === 'loaded' || loadState.value === 'loading') {
            return
        }

        loadState.value = 'loading'
        try {
            const result = toEqualizerDefinition(await getEqualizer())
            definition.value = result
            loadState.value = result ? 'loaded' : 'failed'
        } catch {
            loadState.value = 'failed'
        }
    }

    function clearTimer(): void {
        if (timer !== null) {
            clearTimeout(timer)
            timer = null
        }
    }

    function change(next: EqualizerSettings, patch: UpdateEqualizerSettings): void {
        draft.value = next
        saveError.value = false
        pending = { ...pending, ...patch }
        clearTimer()
        timer = setTimeout(() => {
            timer = null
            void flush()
        }, equalizerSaveDelay)
    }

    /** Saves the changes at once. */
    async function flush(): Promise<void> {
        clearTimer()
        if (!pending) {
            return
        }

        const patch = pending
        const token = generation
        pending = null
        inFlight++
        try {
            await session.updateEqualizer(patch)
        } catch {
            if (token === generation) {
                // The settings of the profile apply again; later changes are dropped with them.
                saveError.value = true
                pending = null
                clearTimer()
                draft.value = null
            }

            return
        } finally {
            inFlight--
        }

        if (token === generation && pending === null && timer === null && inFlight === 0) {
            draft.value = null
        }
    }

    function setEnabled(enabled: boolean): void {
        change({ ...settings.value, enabled }, { enabled })
    }

    function selectPreset(preset: EqualizerPreset): void {
        change(withPreset(settings.value, preset), { preset })
    }

    /** Moves a band; a preset becomes the own curve (Custom). */
    function setBand(index: number, db: number): void {
        const next = withBand(settings.value, index, db, definition.value)
        change(next, { preset: next.preset, customGainsDb: [...next.customGainsDb] })
    }

    function setPreamp(db: number): void {
        const next = withPreamp(settings.value, db, definition.value)
        change(next, { preampDb: next.preampDb })
    }

    /** Back to Flat without a preamp; the own curve stays. */
    function reset(): void {
        const next = resetEqualizer(settings.value)
        change(next, { preset: next.preset, preampDb: next.preampDb })
    }

    function dismissError(): void {
        saveError.value = false
    }

    // Another profile: local changes of the previous one are forgotten.
    watch(
        () => session.token,
        () => {
            generation++
            pending = null
            clearTimer()
            draft.value = null
            saveError.value = false
        },
    )

    if (typeof document !== 'undefined') {
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'hidden') {
                void flush()
            }
        })
    }

    return {
        definition,
        loadState,
        settings,
        output,
        layout,
        minGainDb,
        maxGainDb,
        saveError,
        ensureDefinition,
        setEnabled,
        selectPreset,
        setBand,
        setPreamp,
        reset,
        flush,
        dismissError,
    }
})
