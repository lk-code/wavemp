// HLS playback for audio and video elements. The server delivers transcoded media as HLS (master
// playlist, media playlist, MPEG-TS segments); hls.js (Apache-2.0) plays it through Media Source
// Extensions, browsers without them but with native HLS (Safari on older iPhones) play the playlist
// directly. hls.js is loaded on first use, so it is not part of the start of the app.

/** Content type of HLS playlists. */
export const hlsContentType = 'application/vnd.apple.mpegurl'

/** An HLS stream attached to a media element. */
export interface HlsAttachment {
    /** Detaches the stream and frees its buffers. */
    destroy(): void
}

/** What is needed to attach a stream; tests replace it. */
export interface HlsSupport {
    /** Attaches the stream; fails with an error if the browser cannot play HLS at all. */
    attach(
        media: HTMLMediaElement,
        url: string,
        onFatalError: (error: unknown) => void,
    ): Promise<HlsAttachment>
}

/**
 * Retries of a segment request. The server answers 503 with Retry-After while all transcode slots
 * are in use or the segment is not ready yet; hls.js then simply asks again.
 */
const segmentLoadPolicy = {
    default: {
        maxTimeToFirstByteMs: 60_000,
        maxLoadTimeMs: 120_000,
        timeoutRetry: { maxNumRetry: 4, retryDelayMs: 1000, maxRetryDelayMs: 8000 },
        errorRetry: { maxNumRetry: 8, retryDelayMs: 1000, maxRetryDelayMs: 8000 },
    },
}

export const browserHlsSupport: HlsSupport = {
    async attach(media, url, onFatalError) {
        const { default: Hls } = await import('hls.js')
        if (Hls.isSupported()) {
            const hls = new Hls({
                fragLoadPolicy: segmentLoadPolicy,
                // The playlists are VOD: everything is known, nothing has to be refreshed.
                startFragPrefetch: true,
            })
            hls.on(Hls.Events.ERROR, (_event, data) => {
                if (data.fatal) {
                    onFatalError(data.error ?? new Error(`HLS playback failed: ${data.details}`))
                }
            })
            hls.loadSource(url)
            hls.attachMedia(media)
            return { destroy: () => hls.destroy() }
        }

        if (media.canPlayType(hlsContentType) !== '') {
            media.src = url
            return {
                destroy: () => {
                    media.removeAttribute('src')
                },
            }
        }

        throw new Error('This browser cannot play HLS.')
    },
}

/**
 * The source of one media element: a file set as its src, or an HLS stream attached through the
 * HLS support. A new source detaches the previous stream; an attachment that completes after a
 * newer source was set is destroyed at once, and its errors are no longer reported.
 */
export class MediaSourceSlot {
    private attachment: HlsAttachment | null = null
    private loads = 0

    constructor(
        private readonly media: HTMLMediaElement,
        private readonly support: HlsSupport = browserHlsSupport,
    ) {}

    /** Sets a file or an HLS stream; onError gets the failures of the stream while it is set. */
    set(url: string, hls: boolean, onError: (error: unknown) => void): void {
        this.detach()
        const load = this.loads
        if (!hls) {
            this.media.src = url
            this.media.load()
            return
        }

        const failed = (error: unknown): void => {
            if (this.loads === load) {
                onError(error)
            }
        }
        this.support
            .attach(this.media, url, failed)
            .then((attachment) => {
                if (this.loads === load) {
                    this.attachment = attachment
                } else {
                    attachment.destroy()
                }
            })
            .catch(failed)
    }

    /** Detaches the stream and removes the source, so the element stops loading. */
    clear(): void {
        this.detach()
        if (this.media.getAttribute('src') !== null) {
            this.media.pause()
            this.media.removeAttribute('src')
            this.media.load()
        }
    }

    private detach(): void {
        this.loads++
        const attachment = this.attachment
        this.attachment = null
        attachment?.destroy()
    }
}
