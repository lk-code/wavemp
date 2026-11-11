import { defineStore } from 'pinia'
import { computed, ref, shallowRef, watch } from 'vue'
import {
    PlaybackMethodObject,
    type AudioFormat,
    type Track,
    type TrackPlayback,
} from '@/api/generated/models'
import { statusOf } from '@/api/problems'
import {
    nextRepeatMode,
    type PlaybackState,
    type PlayerMessage,
    type RepeatMode,
    type TransportState,
} from '@/components/shell/transport'
import { getPlayback, recordPlay } from '@/player/api'
import { AudioEngine, type DeckSource, type EngineEvents } from '@/player/engine'
import { browserAudioFormats } from '@/player/formats'
import { PlayHistoryTracker } from '@/player/history'
import { connectMediaSession, updateMediaSession } from '@/player/mediaSession'
import {
    append,
    clearUpcoming,
    currentItem,
    emptyQueue,
    insertNext,
    jumpTo,
    move,
    nextIndex,
    playTracks,
    previousIndex,
    remove,
    setShuffle,
    withIndex,
    type Queue,
    type QueueItem,
} from '@/player/queue'
import { replayGainFactor } from '@/player/replayGain'
import { shouldPreload, shouldRestart } from '@/player/scheduling'
import { clampVolume, loadVolume, saveVolume, volumeToGain } from '@/player/volume'
import { useCoversStore } from './covers'
import { useSessionStore } from './session'

/** What the store needs from the engine; tests replace the engine. */
export type PlayerEngine = Pick<
    AudioEngine,
    | 'load'
    | 'prepare'
    | 'play'
    | 'pause'
    | 'stop'
    | 'seek'
    | 'setVolume'
    | 'setTrackGain'
    | 'hasPrepared'
    | 'position'
>

/** Parts of the browser the player uses; tests replace them. */
export const playerDependencies = {
    createEngine: (events: EngineEvents): PlayerEngine => new AudioEngine(events),
    audioFormats: (): readonly AudioFormat[] => browserAudioFormats(),
    random: (): number => Math.random(),
    mediaSession: true,
}

/** Size of the cover shown by the media session (lock screen, notifications). */
const artworkSize = 300

interface Loaded {
    readonly key: string
    readonly playback: TrackPlayback
}

function seconds(milliseconds: number | null | undefined): number {
    return typeof milliseconds === 'number' && milliseconds > 0 ? milliseconds / 1000 : 0
}

function artistOf(track: Track | undefined): string | undefined {
    return track?.artist?.name ?? undefined
}

/**
 * The audio player: the queue, the playback through the audio engine and the state the transport
 * bar shows. The server decides per track whether the browser plays it directly; tracks that need
 * transcoding (roadmap step 12) or fail are skipped with a notice. Plays count for the history once
 * they were listened to long enough.
 */
export const usePlayerStore = defineStore('player', () => {
    const session = useSessionStore()
    const covers = useCoversStore()

    const queue = shallowRef<Queue>(emptyQueue)
    const playback = ref<PlaybackState>('idle')
    const loading = ref(false)
    const position = ref(0)
    const duration = ref(0)
    const repeat = ref<RepeatMode>('off')
    const message = ref<PlayerMessage | null>(null)
    const storedVolume = loadVolume()
    const volume = ref(storedVolume.volume)
    const muted = ref(storedVolume.muted)
    const artwork = ref<string | null>(null)

    const current = computed<QueueItem | undefined>(() => currentItem(queue.value))
    const shuffle = computed(() => queue.value.shuffle)

    let engine: PlayerEngine | null = null
    /** The loaded track and its playback; null while loading or idle. */
    let loaded: Loaded | null = null
    /** The track prepared on the other deck. */
    let prepared: Loaded | null = null
    let preparing: string | null = null
    /** Increases with every new track, so late answers of older requests are ignored. */
    let generation = 0
    /** Tracks skipped in a row; stops when the whole queue failed. */
    let skipped = 0
    let retried: string | null = null
    let artworkHeld: string | null = null

    const history = new PlayHistoryTracker((trackId, playedAt) => recordPlay(trackId, playedAt))

    function getEngine(): PlayerEngine {
        engine ??= playerDependencies.createEngine({
            timeupdate: onTimeUpdate,
            playing: () => {
                playback.value = 'playing'
                loading.value = false
                skipped = 0
            },
            paused: () => {
                // Loading a new source pauses the element as well; that is no pause of the user.
                if (playback.value === 'playing' && !loading.value) {
                    playback.value = 'paused'
                }
            },
            waiting: () => {
                loading.value = true
            },
            ended: onEnded,
            advanced: onAdvanced,
            error: onError,
        })
        engine.setVolume(volumeToGain({ volume: volume.value, muted: muted.value }))
        return engine
    }

    function gainOf(value: TrackPlayback | null | undefined): number {
        return replayGainFactor(session.replayGain, value?.replayGain)
    }

    function sourceOf(value: TrackPlayback): DeckSource | null {
        return value.method === PlaybackMethodObject.DirectPlay && value.streamUrl
            ? { url: value.streamUrl, gain: gainOf(value) }
            : null
    }

    async function fetchPlayback(item: QueueItem): Promise<TrackPlayback | PlayerMessage> {
        try {
            const result = await getPlayback(item.track.id ?? '', playerDependencies.audioFormats())
            return result && sourceOf(result) ? result : 'unsupported'
        } catch (error) {
            const status = statusOf(error)
            return status === undefined || status >= 500 ? 'failed' : 'unavailable'
        }
    }

    function forgetPrepared(): void {
        prepared = null
        preparing = null
        engine?.prepare(null)
    }

    /** Loads the current entry and plays it (autoplay) or waits paused at the position. */
    async function startCurrent(autoplay = true, startPosition = 0): Promise<void> {
        const item = current.value
        const token = ++generation
        forgetPrepared()
        loaded = null
        history.stop()
        if (!item) {
            reset()
            return
        }

        loading.value = true
        position.value = startPosition
        duration.value = seconds(item.track.durationMilliseconds)
        playback.value = autoplay ? 'playing' : 'paused'

        const result = await fetchPlayback(item)
        if (token !== generation) {
            return
        }

        // Paused while loading: the track waits.
        const shouldPlay = autoplay && playback.value === 'playing'
        if (typeof result === 'string') {
            skip(result, shouldPlay)
            return
        }

        const source = sourceOf(result)!
        loaded = { key: item.key, playback: result }
        duration.value = seconds(result.durationMilliseconds) || duration.value
        const player = getEngine()
        player.load(source, startPosition)
        history.start(item.track.id ?? '', startPosition)
        if (shouldPlay) {
            playback.value = 'playing'
            await player.play()
        } else {
            playback.value = 'paused'
            loading.value = false
        }
    }

    /** Shows the notice and moves on to the next entry, unless every entry failed. */
    function skip(notice: PlayerMessage, autoplay: boolean): void {
        message.value = notice
        loading.value = false
        skipped++
        const next = nextIndex(queue.value, repeat.value, false)
        if (next === null || skipped >= queue.value.items.length) {
            skipped = 0
            stopPlayback()
            return
        }

        queue.value = withIndex(queue.value, next)
        void startCurrent(autoplay)
    }

    function onTimeUpdate(time: number, length: number): void {
        position.value = time
        if (length > 0) {
            duration.value = length
        }

        history.progress(time, duration.value)
        if (shouldPreload(time, duration.value)) {
            void prepareNext()
        }
    }

    /** Loads the next entry on the other deck, so it starts without a gap. */
    async function prepareNext(): Promise<void> {
        const index = nextIndex(queue.value, repeat.value, true)
        const item = index === null ? undefined : queue.value.items[index]
        if (!item || !loaded || prepared?.key === item.key || preparing === item.key) {
            return
        }

        preparing = item.key
        const token = generation
        const result = await fetchPlayback(item)
        if (token !== generation || preparing !== item.key) {
            return
        }

        preparing = null
        if (typeof result === 'string') {
            // It fails again when it is due and is skipped then.
            return
        }

        prepared = { key: item.key, playback: result }
        getEngine().prepare(sourceOf(result))
    }

    function onAdvanced(): void {
        const next = prepared
        prepared = null
        const index = next ? queue.value.items.findIndex((item) => item.key === next.key) : -1
        if (!next || index < 0) {
            return
        }

        generation++
        queue.value = withIndex(queue.value, index)
        loaded = next
        retried = null
        message.value = null
        position.value = 0
        duration.value = seconds(next.playback.durationMilliseconds)
        history.start(queue.value.items[index]?.track.id ?? '')
    }

    function onEnded(): void {
        const index = nextIndex(queue.value, repeat.value, true)
        if (index === null) {
            // The end of the queue: the last track stays, play starts it again.
            history.stop()
            playback.value = 'paused'
            loading.value = false
            return
        }

        queue.value = withIndex(queue.value, index)
        void startCurrent(true)
    }

    function onError(error: unknown): void {
        const item = current.value
        const wasPlaying = playback.value === 'playing'
        if (error instanceof DOMException && error.name === 'NotAllowedError') {
            // The browser blocked the start without a user gesture; play starts it.
            playback.value = 'paused'
            loading.value = false
            return
        }

        // A stream URL may have expired while paused: load the track once more at its position.
        if (item && retried !== item.key) {
            retried = item.key
            void startCurrent(wasPlaying, position.value)
            return
        }

        retried = null
        skip('failed', wasPlaying)
    }

    function reset(): void {
        loading.value = false
        playback.value = 'idle'
        position.value = 0
        duration.value = current.value ? seconds(current.value.track.durationMilliseconds) : 0
    }

    function stopPlayback(): void {
        generation++
        forgetPrepared()
        loaded = null
        history.stop()
        engine?.stop()
        reset()
    }

    /** Replaces the queue with the tracks and plays the one at start. */
    function playTrackList(
        tracks: readonly Track[],
        start = 0,
        options: { shuffle?: boolean } = {},
    ): void {
        const shuffled = options.shuffle ?? queue.value.shuffle
        queue.value = playTracks(tracks, start, shuffled, playerDependencies.random)
        message.value = null
        skipped = 0
        retried = null
        void startCurrent(true)
    }

    /** Without a current track, the first new entry becomes current, ready to play. */
    function afterAdding(): void {
        if (!current.value && queue.value.items.length > 0) {
            queue.value = withIndex(queue.value, 0)
            void startCurrent(false)
        } else {
            forgetPrepared()
        }
    }

    function playNext(tracks: readonly Track[]): void {
        queue.value = insertNext(queue.value, tracks)
        afterAdding()
    }

    function addToQueue(tracks: readonly Track[]): void {
        queue.value = append(queue.value, tracks)
        afterAdding()
    }

    function playItem(key: string): void {
        queue.value = jumpTo(queue.value, key)
        retried = null
        void startCurrent(true)
    }

    function removeItem(key: string): void {
        queue.value = remove(queue.value, key)
        forgetPrepared()
    }

    function moveItem(key: string, offset: -1 | 1): void {
        queue.value = move(queue.value, key, offset)
        forgetPrepared()
    }

    function clearQueue(): void {
        queue.value = clearUpcoming(queue.value)
        forgetPrepared()
    }

    function play(): void {
        if (!loaded) {
            void startCurrent(true, position.value)
            return
        }

        playback.value = 'playing'
        void getEngine().play()
    }

    function pause(): void {
        engine?.pause()
        if (playback.value === 'playing') {
            playback.value = 'paused'
        }
    }

    function togglePlay(): void {
        if (playback.value === 'playing') {
            pause()
        } else {
            play()
        }
    }

    function stop(): void {
        stopPlayback()
    }

    function goTo(index: number | null): void {
        if (index === null) {
            stopPlayback()
            return
        }

        const autoplay = playback.value !== 'paused'
        queue.value = withIndex(queue.value, index)
        retried = null
        message.value = null
        void startCurrent(autoplay)
    }

    function next(): void {
        if (current.value) {
            goTo(nextIndex(queue.value, repeat.value, false))
        }
    }

    function previous(): void {
        if (!current.value) {
            return
        }

        const index = previousIndex(queue.value, repeat.value)
        if (shouldRestart(position.value) || index === null) {
            seek(0)
        } else {
            goTo(index)
        }
    }

    function seek(to: number): void {
        const target = Math.max(0, duration.value > 0 ? Math.min(to, duration.value) : to)
        position.value = target
        history.seek(target)
        if (loaded) {
            engine?.seek(target)
        }
    }

    function toggleShuffle(): void {
        queue.value = setShuffle(queue.value, !queue.value.shuffle, playerDependencies.random)
        forgetPrepared()
    }

    function cycleRepeat(): void {
        repeat.value = nextRepeatMode(repeat.value)
        forgetPrepared()
    }

    function applyVolume(): void {
        saveVolume({ volume: volume.value, muted: muted.value })
        engine?.setVolume(volumeToGain({ volume: volume.value, muted: muted.value }))
    }

    function toggleMute(): void {
        muted.value = !muted.value
        applyVolume()
    }

    function setVolume(value: number): void {
        volume.value = clampVolume(value)
        if (volume.value > 0) {
            muted.value = false
        }

        applyVolume()
    }

    /** Stops and empties the queue, e.g. when the profile changes. */
    function clear(): void {
        stopPlayback()
        queue.value = emptyQueue
        message.value = null
        reset()
    }

    function dismissMessage(): void {
        message.value = null
    }

    // A new ReplayGain mode applies to the current track at once and to the next when it loads.
    watch(
        () => session.replayGain,
        () => {
            if (loaded) {
                engine?.setTrackGain(gainOf(loaded.playback))
            }

            forgetPrepared()
        },
    )

    // Another profile (or none) gets an empty player: stream URLs and history belong to the session.
    watch(
        () => session.token,
        () => clear(),
    )

    // The cover of the current track for the media session.
    watch(
        () => (current.value?.track.hasCover ? (current.value.track.id ?? null) : null),
        async (id) => {
            if (artworkHeld) {
                covers.release('track', artworkHeld, artworkSize)
                artworkHeld = null
            }

            artwork.value = null
            if (id && playerDependencies.mediaSession) {
                artworkHeld = id
                const url = await covers.acquire('track', id, artworkSize)
                if (artworkHeld === id) {
                    artwork.value = url
                }
            }
        },
    )

    const state = computed<TransportState>(() => {
        const track = current.value?.track
        return {
            playback: playback.value,
            loading: loading.value,
            trackId: track?.id ?? undefined,
            hasCover: track?.hasCover ?? false,
            title: track?.title ?? undefined,
            artist: artistOf(track),
            position: position.value,
            duration: duration.value,
            shuffle: shuffle.value,
            repeat: repeat.value,
            volume: volume.value,
            muted: muted.value,
            message: message.value,
        }
    })

    if (playerDependencies.mediaSession) {
        connectMediaSession({
            play,
            pause,
            stop,
            previous,
            next,
            seekTo: seek,
        })
        watch(
            () => ({ ...state.value, artwork: artwork.value }),
            (value) =>
                updateMediaSession({
                    playback: value.playback,
                    title: value.title,
                    artist: value.artist,
                    album: current.value?.track.album?.title ?? undefined,
                    artwork: value.artwork,
                    position: value.position,
                    duration: value.duration,
                }),
        )
    }

    if (typeof window !== 'undefined') {
        window.addEventListener('online', () => void history.flush())
    }

    return {
        queue,
        current,
        state,
        playback,
        loading,
        position,
        duration,
        repeat,
        shuffle,
        volume,
        muted,
        message,
        playTracks: playTrackList,
        playNext,
        addToQueue,
        playItem,
        removeItem,
        moveItem,
        clearQueue,
        play,
        pause,
        togglePlay,
        stop,
        next,
        previous,
        seek,
        toggleShuffle,
        cycleRepeat,
        toggleMute,
        setVolume,
        clear,
        dismissMessage,
    }
})
