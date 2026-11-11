import { reactive } from 'vue'
import { nextRepeatMode, type TransportState } from '@/components/shell/transport'

/**
 * Placeholder until step 9 (audio player): local UI state of the transport bar without playback.
 * Nothing plays, so play, stop, previous, next and seeking stay disabled; shuffle, repeat, mute
 * and volume can be used as UI state. Step 9 replaces this source of the state with the player.
 */
export function usePlaceholderTransport() {
    const state = reactive<TransportState>({
        playback: 'idle',
        position: 0,
        duration: 0,
        shuffle: false,
        repeat: 'off',
        volume: 0.8,
        muted: false,
    })

    return {
        state,
        toggleShuffle: () => {
            state.shuffle = !state.shuffle
        },
        cycleRepeat: () => {
            state.repeat = nextRepeatMode(state.repeat)
        },
        toggleMute: () => {
            state.muted = !state.muted
        },
        setVolume: (volume: number) => {
            state.volume = volume
            if (volume > 0) {
                state.muted = false
            }
        },
    }
}
