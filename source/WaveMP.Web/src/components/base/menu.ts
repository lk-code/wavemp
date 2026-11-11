// Keyboard and position logic of BaseContextMenu.

export interface MenuEntry {
    readonly disabled?: boolean
    readonly separator?: boolean
}

export interface Point {
    readonly x: number
    readonly y: number
}

export interface Size {
    readonly width: number
    readonly height: number
}

function isFocusable(entry: MenuEntry | undefined): boolean {
    return !!entry && !entry.disabled && !entry.separator
}

/**
 * Index of the next focusable entry in the given direction, wrapping around at the ends.
 * Disabled entries and separators are skipped. Returns -1 if no entry is focusable.
 */
export function nextEnabledIndex(
    current: number,
    delta: 1 | -1,
    entries: readonly MenuEntry[],
): number {
    const count = entries.length
    for (let offset = 1; offset <= count; offset++) {
        const index = (((current + delta * offset) % count) + count) % count
        if (isFocusable(entries[index])) {
            return index
        }
    }

    return -1
}

export function firstEnabledIndex(entries: readonly MenuEntry[]): number {
    return nextEnabledIndex(-1, 1, entries)
}

export function lastEnabledIndex(entries: readonly MenuEntry[]): number {
    return nextEnabledIndex(entries.length, -1, entries)
}

/** Keeps a menu of the given size at the point within the viewport, with a small margin. */
export function clampMenuPosition(point: Point, size: Size, viewport: Size, margin = 4): Point {
    const maxX = Math.max(margin, viewport.width - size.width - margin)
    const maxY = Math.max(margin, viewport.height - size.height - margin)
    return {
        x: Math.min(Math.max(point.x, margin), maxX),
        y: Math.min(Math.max(point.y, margin), maxY),
    }
}
