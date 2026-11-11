// Sizes of covers. The API scales covers to 96, 300 or 1000 pixels (the largest width and height);
// the web app asks for the size an element is shown with, so lists load small pictures and large
// headers sharp ones. Pure functions only.

/** Sizes the API delivers scaled covers in. */
export const coverSizes = [96, 300, 1000] as const

export type CoverSize = (typeof coverSizes)[number]

/** Size while the shown size cannot be measured, e.g. before the layout or in tests. */
export const defaultCoverSize: CoverSize = 300

/**
 * Highest pixel density covers are loaded for. Phones with a density of 3 would otherwise load the
 * largest pictures for small tiles, without a visible difference.
 */
export const maxCoverPixelRatio = 2

/**
 * The size of the API for a cover shown with the width in CSS pixels on a screen with the pixel
 * density: the smallest size that is at least as large as the needed pixels, otherwise the
 * largest. Unknown widths give the default size.
 */
export function coverSizeFor(cssPixels: number, pixelRatio = 1): CoverSize {
    if (!Number.isFinite(cssPixels) || cssPixels <= 0) {
        return defaultCoverSize
    }

    const ratio = Number.isFinite(pixelRatio)
        ? Math.min(Math.max(pixelRatio, 1), maxCoverPixelRatio)
        : 1
    const pixels = Math.ceil(cssPixels * ratio)
    return coverSizes.find((size) => size >= pixels) ?? coverSizes[coverSizes.length - 1]!
}
