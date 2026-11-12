// The equalizer in the Web Audio graph: a preamp and one biquad filter per band in a row. The chain
// stays connected all the time; turned off, the preamp is 1 and every band 0 dB, which lets the signal
// through unchanged. Rewiring during playback could click.

import { dbToGain, type EqualizerBandLayout, type EqualizerState } from './equalizer'

/** Time constant of the changes in seconds: fast enough to follow a slider, smooth enough not to click. */
export const equalizerSmoothing = 0.02

export class EqualizerChain {
    readonly input: GainNode
    readonly output: AudioNode
    private readonly filters: BiquadFilterNode[]

    constructor(
        private readonly context: BaseAudioContext,
        layout: readonly EqualizerBandLayout[],
    ) {
        this.input = context.createGain()
        this.filters = layout.map((band) => {
            const filter = context.createBiquadFilter()
            filter.type =
                band.type === 'LowShelf'
                    ? 'lowshelf'
                    : band.type === 'HighShelf'
                      ? 'highshelf'
                      : 'peaking'
            filter.frequency.value = band.frequency
            filter.Q.value = band.q
            filter.gain.value = 0
            return filter
        })

        let previous: AudioNode = this.input
        for (const filter of this.filters) {
            previous.connect(filter)
            previous = filter
        }

        this.output = previous
    }

    /** Number of filters, one per band. */
    get bandCount(): number {
        return this.filters.length
    }

    /** Applies the settings smoothly; turned off, the chain passes the signal unchanged. */
    apply(state: EqualizerState, layout?: readonly EqualizerBandLayout[]): void {
        const now = this.context.currentTime
        this.input.gain.setTargetAtTime(
            state.enabled ? dbToGain(state.preampDb) : 1,
            now,
            equalizerSmoothing,
        )
        this.filters.forEach((filter, index) => {
            const band = layout?.[index]
            if (band && layout.length === this.filters.length) {
                filter.frequency.setTargetAtTime(band.frequency, now, equalizerSmoothing)
                filter.Q.setTargetAtTime(band.q, now, equalizerSmoothing)
            }

            const gain = state.enabled ? (state.gainsDb[index] ?? 0) : 0
            filter.gain.setTargetAtTime(gain, now, equalizerSmoothing)
        })
    }
}
