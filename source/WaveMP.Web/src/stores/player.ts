import { defineStore } from 'pinia'
import { computed, ref, shallowRef, watch } from 'vue'
import {
    PlaybackMethodObject,
    PlayerViewModeObject,
    StreamProtocolObject,
    type AudioFormat,
    type PlayerViewMode,
    type Track,
    type TrackPlayback,
    type TrackVideoPlayback,
} from '@/api/generated/models'
import { statusOf } from '@/api/problems'
import {
    nextRepeatMode,
    type PlaybackState,
    type PlayerMessage,
    type RepeatMode,
    type TransportState,
} from '@/components/shell/transport'
import { getPlayback, getVideoPlayback, recordPlay } from '@/player/api'
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
import {
    VideoFollower,
    type FollowerClock,
    type VideoFollowerEvents,
    type VideoFollowerSource,
} from '@/player/videoFollower'
import { browserVideoFormats, type VideoFormats } from '@/player/videoFormats'
import { stageViewOf, type StageView, type VideoState } from '@/player/videoSync'
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

/** What the store needs from the video follower; tests replace it. */
export type PlayerVideoFollower = Pick<
    VideoFollower,
    'load' | 'unload' | 'setActive' | 'setOffset' | 'sync' | 'dispose'
>

/** Parts of the browser the player uses; tests replace them. */
export const playerDependencies = {
    createEngine: (events: EngineEvents): PlayerEngine => new AudioEngine(events),
    createVideoFollower: (
        element: HTMLVideoElement,
        clock: () => FollowerClock,
        events: VideoFollowerEvents,
    ): PlayerVideoFollower => new VideoFollower(element, clock, events),
    audioFormats: (): readonly AudioFormat[] => browserAudioFormats(),
    videoFormats: (): VideoFormats => browserVideoFormats(),
    random: (): number => Math.random(),
    mediaSession: true,
}

/** Size of the cover shown by the media session (lock screen, notifications). */
const artworkSize = 300

interface Loaded {
    readonly key: string
    readonly playback: TrackPlayback
}

function isHls(value: TrackPlayback | TrackVideoPlayback): boolean {
    return (
        value.method === PlaybackMethodObject.Transcode ||
        value.protocol === StreamProtocolObject.Hls
    )
}

function seconds(milliseconds: number | null | undefined): number {
    return typeof milliseconds === 'number' && milliseconds > 0 ? milliseconds / 1000 : 0
}

function artistOf(track: Track | undefined): string | undefined {
    return track?.artist?.name ?? undefined
}

/**
 * The player: the queue, the playback through the audio engine and the state the transport bar
 * shows. The server decides per track whether the browser plays the file directly or a transcoded
 * HLS stream; tracks that cannot be played or fail are skipped with a notice. Plays count for the
 * history once they were listened to long enough.
 *
 * The audio always leads, also for tracks with a video: the Now Playing stage shows the cover, the
 * video or the visualization of the current track, and switching between them never touches the
 * audio. The video plays muted and follows the position of the audio plus the offset of the track;
 * it is only loaded while the stage shows it.
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
    /** The video element of the Now Playing stage, while the stage is open. */
    const videoElement = shallowRef<HTMLVideoElement | null>(null)
    /** State of the video of the current track. */
    const videoState = ref<VideoState>('idle')
    /** Whether the page is visible; a hidden page shows no video. */
    const pageVisible = ref(
        typeof document === 'undefined' || document.visibilityState !== 'hidden',
    )

    const current = computed<QueueItem | undefined>(() => currentItem(queue.value))
    const shuffle = computed(() => queue.value.shuffle)
    const hasVideo = computed(() => current.value?.track.hasVideo === true)
    /** The view mode chosen by the profile. */
    const viewMode = computed<PlayerViewMode>(() => session.playerViewMode)
    /** What the stage shows: the video only once it is ready at the right position. */
    const stageView = computed<StageView>(() =>
        stageViewOf(viewMode.value, hasVideo.value, videoState.value),
    )
    /** Whether the video of the current track is to be shown. */
    const videoWanted = computed(
        () =>
            videoElement.value !== null &&
            hasVideo.value &&
            viewMode.value === PlayerViewModeObject.Video &&
            pageVisible.value,
    )

    let engine: PlayerEngine | null = null
    let follower: PlayerVideoFollower | null = null
    /** The queue entry whose video is loaded or requested. */
    let videoKey: string | null = null
    /** Increases with every request of a video, so late answers are ignored. */
    let videoGeneration = 0
    /** The entry whose failed video was already requested once more. */
    let videoRetried: string | null = null
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

    const engineEvents: EngineEvents = {
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
            // Only a playing track waits for data; a paused one does not load.
            if (playback.value === 'playing') {
                loading.value = true
            }
        },
        ended: onEnded,
        advanced: onAdvanced,
        error: onError,
    }

    function outputGain(): number {
        return volumeToGain({ volume: volume.value, muted: muted.value })
    }

    function getEngine(): PlayerEngine {
        engine ??= playerDependencies.createEngine(engineEvents)
        engine.setVolume(outputGain())
        return engine
    }

    function gainOf(value: TrackPlayback | null | undefined): number {
        return replayGainFactor(session.replayGain, value?.replayGain)
    }

    function sourceOf(value: TrackPlayback): DeckSource | null {
        if (!value.streamUrl) {
            return null
        }

        const source = { url: value.streamUrl, gain: gainOf(value) }
        return isHls(value) ? { ...source, hls: true } : source
    }

    function videoSourceOf(value: TrackVideoPlayback | undefined): VideoFollowerSource | null {
        if (!value?.streamUrl) {
            return null
        }

        return isHls(value) ? { url: value.streamUrl, hls: true } : { url: value.streamUrl }
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
        // A loaded track that waited for data stops waiting; one that is still being requested
        // shows loading until startCurrent has it.
        if (loaded) {
            loading.value = false
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
            getEngine().seek(target)
        }

        follower?.sync()
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
        engine?.setVolume(outputGain())
    }

    /** The audio the video follows. */
    function clock(): FollowerClock {
        return {
            position: loaded && engine ? engine.position : position.value,
            playing: playback.value === 'playing' && !loading.value && loaded !== null,
        }
    }

    /** Chooses what the stage shows; the audio is never touched. */
    function selectViewMode(mode: PlayerViewMode): void {
        void session.updatePlayerViewMode(mode)
    }

    /** The stage shows its video element; it shows the video of tracks with one from now on. */
    function attachVideo(element: HTMLVideoElement): void {
        if (videoElement.value === element) {
            return
        }

        detachVideo()
        follower = playerDependencies.createVideoFollower(element, clock, {
            stateChange: onVideoState,
            // The own controls of the video (e.g. full screen on the iPhone) control the audio.
            userPlay: play,
            userPause: pause,
        })
        videoElement.value = element
    }

    /** The stage closes; the audio simply goes on. */
    function detachVideo(element?: HTMLVideoElement): void {
        if (!videoElement.value || (element && element !== videoElement.value)) {
            return
        }

        videoGeneration++
        videoKey = null
        videoElement.value = null
        follower?.dispose()
        follower = null
        videoState.value = 'idle'
    }

    /** Applies a new offset to the video of a track at once, if it is the current one. */
    function applyVideoOffset(trackId: string, offsetMilliseconds: number): void {
        if (current.value?.track.id === trackId) {
            follower?.setOffset(offsetMilliseconds)
            follower?.sync()
        }
    }

    function onVideoState(state: VideoState): void {
        videoState.value = state
        const item = current.value
        // A signed URL may have expired while the video waited: request it once more.
        if (state === 'failed' && item && videoKey === item.key && videoRetried !== item.key) {
            videoRetried = item.key
            void loadVideo(item)
        }
    }

    /** Requests the video of the entry and loads it into the follower. */
    async function loadVideo(item: QueueItem): Promise<void> {
        const token = ++videoGeneration
        videoKey = item.key
        videoState.value = 'loading'
        let result: TrackVideoPlayback | undefined
        try {
            result = await getVideoPlayback(item.track.id ?? '', playerDependencies.videoFormats())
        } catch {
            result = undefined
        }

        if (token !== videoGeneration || !follower) {
            return
        }

        const source = videoSourceOf(result)
        if (!source) {
            onVideoState('failed')
            return
        }

        follower.load(source, result?.offsetMilliseconds ?? 0)
        follower.setActive(videoWanted.value)
    }

    /** Loads, shows or hides the video of the current entry; a new entry unloads the previous one. */
    function updateVideo(entryChanged: boolean): void {
        const item = current.value
        if (entryChanged) {
            videoGeneration++
            videoKey = null
            videoRetried = null
            follower?.unload()
            videoState.value = 'idle'
        }

        if (!follower) {
            return
        }

        if (!videoWanted.value || !item) {
            follower.setActive(false)
        } else if (videoKey === item.key) {
            follower.setActive(true)
        } else {
            void loadVideo(item)
        }
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

    // A new entry, view mode, stage or visibility: the video follows; the audio is not touched.
    watch([() => current.value?.key ?? null, videoWanted], ([key], [previousKey]) =>
        updateVideo(key !== previousKey),
    )

    // The audio starts, pauses or waits for data: the video does the same at once.
    watch([playback, loading], () => follower?.sync())

    if (typeof document !== 'undefined') {
        document.addEventListener('visibilitychange', () => {
            pageVisible.value = document.visibilityState !== 'hidden'
        })
    }

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
            isFavorite: track?.isFavorite ?? false,
            rating: track?.rating ?? 0,
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
        viewMode,
        stageView,
        videoState,
        hasVideo,
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
        selectViewMode,
        attachVideo,
        detachVideo,
        applyVideoOffset,
    }
})
