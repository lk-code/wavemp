// Value logic of BaseSlider (pointer and keyboard), independent of the DOM.

export interface SliderOptions {
    readonly min: number
    readonly max: number
    readonly step: number
    readonly largeStep: number
}

export function clamp(value: number, min: number, max: number): number {
    return Math.min(Math.max(value, min), max)
}

/** Rounds to the nearest step from min, within min and max, without floating point noise. */
export function roundToStep(value: number, options: SliderOptions): number {
    const { min, max, step } = options
    if (!Number.isFinite(value)) {
        return min
    }

    const steps = step > 0 ? Math.round((value - min) / step) : 0
    const rounded = step > 0 ? min + steps * step : value
    return clamp(Number(rounded.toFixed(10)), min, max)
}

/** Position of the value between min (0) and max (1). */
export function ratio(value: number, options: Pick<SliderOptions, 'min' | 'max'>): number {
    const { min, max } = options
    if (max <= min) {
        return 0
    }

    return (clamp(value, min, max) - min) / (max - min)
}

/** Value at a horizontal pointer position within the slider. */
export function valueFromPointer(
    clientX: number,
    rect: { readonly left: number; readonly width: number },
    options: SliderOptions,
): number {
    if (rect.width <= 0) {
        return options.min
    }

    const position = clamp((clientX - rect.left) / rect.width, 0, 1)
    return roundToStep(options.min + position * (options.max - options.min), options)
}

/** New value for a key of the slider pattern, or null if the key does not change the value. */
export function valueForKey(key: string, value: number, options: SliderOptions): number | null {
    switch (key) {
        case 'ArrowRight':
        case 'ArrowUp':
            return roundToStep(value + options.step, options)
        case 'ArrowLeft':
        case 'ArrowDown':
            return roundToStep(value - options.step, options)
        case 'PageUp':
            return roundToStep(value + options.largeStep, options)
        case 'PageDown':
            return roundToStep(value - options.largeStep, options)
        case 'Home':
            return options.min
        case 'End':
            return options.max
        default:
            return null
    }
}
