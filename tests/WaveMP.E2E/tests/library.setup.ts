import { expect, test as setup } from '@playwright/test'
import { listTrackTitles, scanLibrary } from './helpers/api'

// Scans the synthetic test media once before the tests, so every test finds the library filled,
// independent of the order of the tests. The media comes from scripts/generate-media.sh.

const expectedTracks = process.env.E2E_EXPECTED_TRACKS
    ? Number(process.env.E2E_EXPECTED_TRACKS)
    : undefined

setup('scan the test media into the library', async ({ request }) => {
    setup.setTimeout(180_000)

    await scanLibrary(request)
    const titles = await listTrackTitles(request)

    expect(
        titles,
        'The library needs the test media of scripts/generate-media.sh in the media folder.',
    ).toEqual(expect.arrayContaining(['First Song', 'Second Song', 'Video Only', 'Loose Song']))
    if (expectedTracks !== undefined) {
        expect(titles).toHaveLength(expectedTracks)
    }
})
