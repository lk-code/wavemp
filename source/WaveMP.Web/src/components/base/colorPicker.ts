// Keyboard logic of BaseColorPicker (radio group pattern with roving tabindex): the arrow keys
// move the selection and wrap around, Home and End select the first and last color.

/** Index of the color to select for a key, or null if the key does nothing. */
export function nextColorIndex(key: string, current: number, count: number): number | null {
    if (count <= 0) {
        return null
    }

    const from = current < 0 ? 0 : current
    switch (key) {
        case 'ArrowRight':
        case 'ArrowDown':
            return current < 0 ? 0 : (from + 1) % count
        case 'ArrowLeft':
        case 'ArrowUp':
            return current < 0 ? count - 1 : (from - 1 + count) % count
        case 'Home':
            return 0
        case 'End':
            return count - 1
        default:
            return null
    }
}

/** Index of the swatch in the tab order: the selected one, otherwise the first. */
export function colorTabStop(selected: number): number {
    return selected < 0 ? 0 : selected
}
