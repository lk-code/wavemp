// The offset of the video of a song against its audio, in whole milliseconds. The limits are those
// of the API (UpdateTrackVideoOffset in the OpenAPI document), so the form rejects other values
// before it sends them.

/** Smallest offset: the music in the video starts ten minutes earlier than in the song. */
export const minVideoOffset = -600_000

/** Largest offset: the music in the video starts ten minutes later than in the song. */
export const maxVideoOffset = 600_000

/** The offset entered as text, or null if it is no whole number within the limits. */
export function parseVideoOffset(text: string): number | null {
    const trimmed = text.trim()
    if (!/^[-+]?\d{1,7}$/.test(trimmed)) {
        return null
    }

    const value = Number(trimmed)
    return value >= minVideoOffset && value <= maxVideoOffset ? value : null
}
