// Counts how long the current track is listened to and reports the play to the history once it
// counts. Reports that fail (offline, server unavailable) are kept while the page is open and sent
// again with the time the play started, until the server accepts or rejects them.

import { statusOf } from '@/api/problems'
import { historyThreshold, listenedBetween } from './scheduling'

export interface PendingPlay {
    readonly trackId: string
    readonly playedAt: Date
}

export type ReportPlay = (trackId: string, playedAt: Date) => Promise<unknown>

/** Highest number of failed reports kept; the oldest go first. */
export const maxPendingPlays = 200

export class PlayHistoryTracker {
    private trackId: string | null = null
    private startedAt: Date | null = null
    private listened = 0
    private lastPosition = 0
    private reported = false
    private readonly pending: PendingPlay[] = []
    private flushing: Promise<void> | null = null

    constructor(
        private readonly report: ReportPlay,
        private readonly now: () => Date = () => new Date(),
    ) {}

    /** A new play of a track starts at the position (usually 0). */
    start(trackId: string, position = 0): void {
        this.trackId = trackId
        this.startedAt = this.now()
        this.listened = 0
        this.lastPosition = position
        this.reported = false
    }

    /** No track plays any more. */
    stop(): void {
        this.trackId = null
        this.startedAt = null
    }

    /** The position after a jump; the jump does not count. */
    seek(position: number): void {
        this.lastPosition = position
    }

    /** Progress of the playback; reports the play once enough was listened to. */
    progress(position: number, duration: number): void {
        this.listened += listenedBetween(this.lastPosition, position)
        this.lastPosition = position
        if (
            this.trackId &&
            this.startedAt &&
            !this.reported &&
            this.listened >= historyThreshold(duration)
        ) {
            this.reported = true
            this.enqueue({ trackId: this.trackId, playedAt: this.startedAt })
        }
    }

    /** Number of reports that are still to be sent. */
    get pendingCount(): number {
        return this.pending.length
    }

    /** Sends the kept reports again, e.g. when the browser is back online. */
    flush(): Promise<void> {
        this.flushing ??= this.send().finally(() => {
            this.flushing = null
        })
        return this.flushing
    }

    private enqueue(play: PendingPlay): void {
        this.pending.push(play)
        if (this.pending.length > maxPendingPlays) {
            this.pending.splice(0, this.pending.length - maxPendingPlays)
        }

        void this.flush()
    }

    private async send(): Promise<void> {
        while (this.pending.length > 0) {
            const play = this.pending[0]!
            try {
                await this.report(play.trackId, play.playedAt)
            } catch (error) {
                const status = statusOf(error)
                // Network errors, server errors and an ended session are tried again later; the
                // server rejected the others for good (e.g. the track is gone, the play too old).
                if (status === undefined || status >= 500 || status === 401) {
                    return
                }
            }

            this.pending.shift()
        }
    }
}
