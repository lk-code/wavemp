// The play queue: the tracks in the order they play, the current one and the shuffle state. Pure
// functions that return a new queue, so the store can keep it as plain state and tests can check
// every step. The same track may be in the queue more than once; each entry has its own key.

import type { Track } from '@/api/generated/models'
import type { RepeatMode } from '@/components/shell/transport'

export interface QueueItem {
    /** Unique within the queue; stays the same while the entry moves. */
    readonly key: string
    readonly track: Track
}

export interface Queue {
    /** The entries in the order they play. */
    readonly items: readonly QueueItem[]
    /** Index of the current entry; -1 without one. */
    readonly index: number
    readonly shuffle: boolean
    /** Keys in the order before shuffling, to restore it; null while not shuffled. */
    readonly original: readonly string[] | null
}

export type Random = () => number

export const emptyQueue: Queue = { items: [], index: -1, shuffle: false, original: null }

let sequence = 0

/** New entries for tracks; tracks without id or audio are left out. */
export function toQueueItems(tracks: readonly Track[]): QueueItem[] {
    return tracks
        .filter((track) => !!track.id && track.hasAudio !== false)
        .map((track) => ({ key: `q${++sequence}`, track }))
}

export function currentItem(queue: Queue): QueueItem | undefined {
    return queue.index >= 0 ? queue.items[queue.index] : undefined
}

/** Fisher-Yates shuffle of a copy. */
function shuffled<T>(values: readonly T[], random: Random): T[] {
    const result = [...values]
    for (let index = result.length - 1; index > 0; index--) {
        const other = Math.floor(random() * (index + 1))
        const value = result[index]!
        result[index] = result[other]!
        result[other] = value
    }

    return result
}

/** The current entry first, all others shuffled after it. */
function shuffleAround(queue: Queue, random: Random): Queue {
    const current = currentItem(queue)
    const others = queue.items.filter((item) => item !== current)
    const items = current ? [current, ...shuffled(others, random)] : shuffled(others, random)
    return {
        items,
        index: current ? 0 : queue.index,
        shuffle: true,
        original: queue.original ?? queue.items.map((item) => item.key),
    }
}

/**
 * Replaces the queue with tracks and makes the entry of the track at start the current one. With
 * shuffle, it plays first and the others follow in random order; without a valid start, the first
 * entry starts (with shuffle a random one).
 */
export function playTracks(
    tracks: readonly Track[],
    start: number,
    shuffle: boolean,
    random: Random = Math.random,
): Queue {
    const all = toQueueItems(tracks)
    const startTrack = tracks[start]
    let index = startTrack ? all.findIndex((item) => item.track === startTrack) : -1
    if (index < 0 && all.length > 0) {
        index = shuffle ? Math.floor(random() * all.length) : 0
    }

    const queue: Queue = { items: all, index, shuffle: false, original: null }
    return shuffle ? shuffleAround(queue, random) : queue
}

/** Puts tracks right after the current entry (at the start without one). */
export function insertNext(queue: Queue, tracks: readonly Track[]): Queue {
    const added = toQueueItems(tracks)
    if (added.length === 0) {
        return queue
    }

    const at = queue.index + 1
    const items = [...queue.items.slice(0, at), ...added, ...queue.items.slice(at)]
    let original = queue.original
    if (original) {
        const currentKey = currentItem(queue)?.key
        const position = currentKey ? original.indexOf(currentKey) + 1 : 0
        original = [
            ...original.slice(0, position),
            ...added.map((item) => item.key),
            ...original.slice(position),
        ]
    }

    return { ...queue, items, original }
}

/** Adds tracks at the end of the queue. */
export function append(queue: Queue, tracks: readonly Track[]): Queue {
    const added = toQueueItems(tracks)
    if (added.length === 0) {
        return queue
    }

    return {
        ...queue,
        items: [...queue.items, ...added],
        original: queue.original ? [...queue.original, ...added.map((item) => item.key)] : null,
    }
}

/** Removes an entry; the current entry stays (stopping ends it). */
export function remove(queue: Queue, key: string): Queue {
    const at = queue.items.findIndex((item) => item.key === key)
    if (at < 0 || at === queue.index) {
        return queue
    }

    return {
        ...queue,
        items: queue.items.filter((item) => item.key !== key),
        index: at < queue.index ? queue.index - 1 : queue.index,
        original: queue.original?.filter((itemKey) => itemKey !== key) ?? null,
    }
}

/** Moves an entry one place up (-1) or down (+1). */
export function move(queue: Queue, key: string, offset: -1 | 1): Queue {
    const from = queue.items.findIndex((item) => item.key === key)
    const to = from + offset
    if (from < 0 || to < 0 || to >= queue.items.length) {
        return queue
    }

    const items = [...queue.items]
    items[from] = queue.items[to]!
    items[to] = queue.items[from]!
    let index = queue.index
    if (index === from) {
        index = to
    } else if (index === to) {
        index = from
    }

    return { ...queue, items, index }
}

/** Removes all entries but the current one. */
export function clearUpcoming(queue: Queue): Queue {
    const current = currentItem(queue)
    const original = queue.shuffle ? (current ? [current.key] : []) : null
    return current
        ? { items: [current], index: 0, shuffle: queue.shuffle, original }
        : { ...emptyQueue, shuffle: queue.shuffle, original }
}

/** Makes an entry the current one. */
export function jumpTo(queue: Queue, key: string): Queue {
    const index = queue.items.findIndex((item) => item.key === key)
    return index < 0 ? queue : { ...queue, index }
}

/** Makes the entry at the index the current one. */
export function withIndex(queue: Queue, index: number): Queue {
    return index >= 0 && index < queue.items.length ? { ...queue, index } : queue
}

/** Shuffles the entries around the current one, or restores the order from before. */
export function setShuffle(queue: Queue, shuffle: boolean, random: Random = Math.random): Queue {
    if (shuffle === queue.shuffle) {
        return queue
    }

    if (shuffle) {
        return shuffleAround(queue, random)
    }

    const position = new Map((queue.original ?? []).map((key, at) => [key, at]))
    const current = currentItem(queue)
    const items = [...queue.items].sort(
        (a, b) =>
            (position.get(a.key) ?? Number.MAX_SAFE_INTEGER) -
            (position.get(b.key) ?? Number.MAX_SAFE_INTEGER),
    )
    return {
        items,
        index: current ? items.indexOf(current) : queue.index,
        shuffle: false,
        original: null,
    }
}

/**
 * Index of the entry after the current one, or null at the end. When a track ends by itself
 * (automatic), repeat one plays it again; skipping always moves on. Repeat all and one start again
 * at the beginning after the last entry.
 */
export function nextIndex(queue: Queue, repeat: RepeatMode, automatic: boolean): number | null {
    if (queue.items.length === 0) {
        return null
    }

    if (automatic && repeat === 'one' && queue.index >= 0) {
        return queue.index
    }

    if (queue.index + 1 < queue.items.length) {
        return queue.index + 1
    }

    return repeat === 'off' ? null : 0
}

/** Index of the entry before the current one, or null at the start (without repeat). */
export function previousIndex(queue: Queue, repeat: RepeatMode): number | null {
    if (queue.items.length === 0) {
        return null
    }

    if (queue.index > 0) {
        return queue.index - 1
    }

    return repeat === 'off' ? null : queue.items.length - 1
}
