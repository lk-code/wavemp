// Placeholder content for the library view until the library API exists (step 6); not real data.
// It only shows the layout of the list. Names come from the messages (placeholders.*), so the
// placeholders are recognizable as such in every language.

export interface PlaceholderTrack {
    /** Number of the track on the album. */
    readonly number: number
    /** Number in the placeholder title ("Placeholder Track 7"), unique across all albums. */
    readonly titleNumber: number
    readonly durationSeconds: number
    readonly hasVideo: boolean
}

export interface PlaceholderAlbum {
    readonly number: number
    readonly artistNumber: number
    readonly genreNumber: number
    readonly year: number
    readonly tracks: readonly PlaceholderTrack[]
}

export const placeholderAlbums: readonly PlaceholderAlbum[] = [
    {
        number: 1,
        artistNumber: 1,
        genreNumber: 1,
        year: 2006,
        tracks: [
            { number: 1, titleNumber: 1, durationSeconds: 225, hasVideo: true },
            { number: 2, titleNumber: 2, durationSeconds: 198, hasVideo: false },
            { number: 3, titleNumber: 3, durationSeconds: 254, hasVideo: false },
            { number: 4, titleNumber: 4, durationSeconds: 187, hasVideo: true },
            { number: 5, titleNumber: 5, durationSeconds: 301, hasVideo: false },
        ],
    },
    {
        number: 2,
        artistNumber: 2,
        genreNumber: 2,
        year: 2007,
        tracks: [
            { number: 1, titleNumber: 6, durationSeconds: 242, hasVideo: false },
            { number: 2, titleNumber: 7, durationSeconds: 216, hasVideo: true },
            { number: 3, titleNumber: 8, durationSeconds: 279, hasVideo: false },
            { number: 4, titleNumber: 9, durationSeconds: 3725, hasVideo: false },
        ],
    },
    {
        number: 3,
        artistNumber: 1,
        genreNumber: 1,
        year: 2009,
        tracks: [
            { number: 1, titleNumber: 10, durationSeconds: 205, hasVideo: false },
            { number: 2, titleNumber: 11, durationSeconds: 233, hasVideo: false },
            { number: 3, titleNumber: 12, durationSeconds: 189, hasVideo: true },
            { number: 4, titleNumber: 13, durationSeconds: 262, hasVideo: false },
            { number: 5, titleNumber: 14, durationSeconds: 214, hasVideo: false },
            { number: 6, titleNumber: 15, durationSeconds: 318, hasVideo: false },
        ],
    },
]
