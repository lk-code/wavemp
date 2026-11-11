import { describe, expect, it } from 'vitest'
import type { Track } from '@/api/generated/models'
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
} from '../queue'

function track(id: string, hasAudio = true): Track {
    return { id, title: id.toUpperCase(), hasAudio }
}

function ids(queue: Queue): string[] {
    return queue.items.map((item) => item.track.id ?? '')
}

/** Random numbers that always pick the first remaining element: reverses the order. */
const first = () => 0

const abcd = [track('a'), track('b'), track('c'), track('d')]

describe('playTracks', () => {
    it('starts at the given track', () => {
        const queue = playTracks(abcd, 2, false)

        expect(ids(queue)).toEqual(['a', 'b', 'c', 'd'])
        expect(currentItem(queue)?.track.id).toBe('c')
        expect(queue.shuffle).toBe(false)
    })

    it('leaves out tracks without audio and keeps the start track', () => {
        const queue = playTracks([track('a'), track('video', false), track('c')], 2, false)

        expect(ids(queue)).toEqual(['a', 'c'])
        expect(currentItem(queue)?.track.id).toBe('c')
    })

    it('starts at the first track without a valid start', () => {
        expect(currentItem(playTracks(abcd, 9, false))?.track.id).toBe('a')
        expect(playTracks([], 0, false).index).toBe(-1)
    })

    it('plays the start track first when shuffled and keeps the order to restore', () => {
        const queue = playTracks(abcd, 1, true, first)

        expect(ids(queue)[0]).toBe('b')
        expect(ids(queue)).toHaveLength(4)
        expect(new Set(ids(queue))).toEqual(new Set(['a', 'b', 'c', 'd']))
        expect(queue.index).toBe(0)
        expect(queue.shuffle).toBe(true)
        expect(queue.original).toHaveLength(4)
    })

    it('gives every entry its own key, also for the same track', () => {
        const queue = playTracks([abcd[0]!, abcd[0]!], 0, false)

        expect(new Set(queue.items.map((item) => item.key)).size).toBe(2)
    })
})

describe('editing', () => {
    it('inserts tracks right after the current entry', () => {
        const queue = insertNext(playTracks(abcd, 1, false), [track('x'), track('y')])

        expect(ids(queue)).toEqual(['a', 'b', 'x', 'y', 'c', 'd'])
        expect(currentItem(queue)?.track.id).toBe('b')
    })

    it('inserts at the start of an empty queue', () => {
        const queue = insertNext(emptyQueue, [track('x')])

        expect(ids(queue)).toEqual(['x'])
        expect(queue.index).toBe(-1)
    })

    it('appends tracks at the end', () => {
        expect(ids(append(playTracks(abcd, 0, false), [track('x')]))).toEqual([
            'a',
            'b',
            'c',
            'd',
            'x',
        ])
    })

    it('removes entries and keeps the current one', () => {
        const queue = playTracks(abcd, 2, false)
        const [a, , c] = queue.items

        const removed = remove(queue, a!.key)
        expect(ids(removed)).toEqual(['b', 'c', 'd'])
        expect(currentItem(removed)?.track.id).toBe('c')
        expect(remove(queue, c!.key)).toBe(queue)
        expect(remove(queue, 'unknown')).toBe(queue)
    })

    it('moves entries and follows the current one', () => {
        const queue = playTracks(abcd, 1, false)
        const [a, b, , d] = queue.items

        const down = move(queue, b!.key, 1)
        expect(ids(down)).toEqual(['a', 'c', 'b', 'd'])
        expect(currentItem(down)?.track.id).toBe('b')

        const up = move(queue, queue.items[2]!.key, -1)
        expect(ids(up)).toEqual(['a', 'c', 'b', 'd'])
        expect(currentItem(up)?.track.id).toBe('b')

        expect(move(queue, a!.key, -1)).toBe(queue)
        expect(move(queue, d!.key, 1)).toBe(queue)
    })

    it('clears all but the current entry', () => {
        const queue = clearUpcoming(playTracks(abcd, 2, false))

        expect(ids(queue)).toEqual(['c'])
        expect(queue.index).toBe(0)
        expect(clearUpcoming(emptyQueue).items).toEqual([])
    })

    it('jumps to an entry', () => {
        const queue = playTracks(abcd, 0, false)

        expect(currentItem(jumpTo(queue, queue.items[3]!.key))?.track.id).toBe('d')
        expect(jumpTo(queue, 'unknown')).toBe(queue)
        expect(withIndex(queue, 2).index).toBe(2)
        expect(withIndex(queue, 7)).toBe(queue)
    })
})

describe('shuffle', () => {
    it('shuffles around the current entry and restores the order', () => {
        const queue = playTracks(abcd, 2, false)

        const shuffled = setShuffle(queue, true, first)
        expect(ids(shuffled)[0]).toBe('c')
        expect(shuffled.index).toBe(0)

        const restored = setShuffle(shuffled, false)
        expect(ids(restored)).toEqual(['a', 'b', 'c', 'd'])
        expect(currentItem(restored)?.track.id).toBe('c')
        expect(restored.original).toBeNull()
    })

    it('keeps tracks added while shuffled in the restored order', () => {
        let queue = setShuffle(playTracks(abcd, 0, false), true, first)
        queue = insertNext(queue, [track('x')])
        queue = append(queue, [track('y')])

        expect(ids(setShuffle(queue, false))).toEqual(['a', 'x', 'b', 'c', 'd', 'y'])
    })

    it('changes nothing when the state stays', () => {
        const queue = playTracks(abcd, 0, false)

        expect(setShuffle(queue, false)).toBe(queue)
    })
})

describe('navigation', () => {
    const last = withIndex(playTracks(abcd, 0, false), 3)
    const middle = withIndex(last, 1)

    it('moves on to the next entry', () => {
        expect(nextIndex(middle, 'off', false)).toBe(2)
        expect(nextIndex(middle, 'off', true)).toBe(2)
    })

    it('ends after the last entry without repeat', () => {
        expect(nextIndex(last, 'off', true)).toBeNull()
        expect(nextIndex(emptyQueue, 'all', true)).toBeNull()
    })

    it('starts again with repeat all', () => {
        expect(nextIndex(last, 'all', true)).toBe(0)
    })

    it('plays the entry again with repeat one, but skipping moves on', () => {
        expect(nextIndex(middle, 'one', true)).toBe(1)
        expect(nextIndex(middle, 'one', false)).toBe(2)
        expect(nextIndex(last, 'one', false)).toBe(0)
    })

    it('goes back to the previous entry', () => {
        expect(previousIndex(middle, 'off')).toBe(0)
        expect(previousIndex(withIndex(middle, 0), 'off')).toBeNull()
        expect(previousIndex(withIndex(middle, 0), 'all')).toBe(3)
        expect(previousIndex(emptyQueue, 'all')).toBeNull()
    })
})
