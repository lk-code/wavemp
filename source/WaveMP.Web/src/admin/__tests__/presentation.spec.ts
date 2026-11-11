import { describe, expect, it } from 'vitest'
import {
    activityOf,
    durationSeconds,
    errorLabelKey,
    formatList,
    intervalOptions,
    isActive,
    issueLabelKey,
    jobNameKey,
    parseList,
    phaseLabelKey,
    progressRatio,
    statusPresentation,
    triggerLabelKey,
} from '../presentation'

describe('presentation of the administration', () => {
    it('offers intervals the server accepts, in ascending order', () => {
        const minutes = intervalOptions.map((option) => option.minutes)

        expect(minutes).toEqual([...minutes].sort((a, b) => a - b))
        expect(minutes.every((value) => value >= 15 && value <= 1440)).toBe(true)
        expect(minutes).toContain(360)
    })

    it('parses and formats the lists of the settings', () => {
        expect(parseList(' mp3, .FLAC ;ogg\n\n ,')).toEqual(['mp3', '.FLAC', 'ogg'])
        expect(parseList('')).toEqual([])
        expect(formatList(['mp3', 'flac'])).toBe('mp3, flac')
        expect(formatList(null)).toBe('')
    })

    it('computes the progress of a scan', () => {
        expect(progressRatio(null)).toBeNull()
        expect(progressRatio({ phase: 'Discovering', filesFound: 10 })).toBeNull()
        expect(progressRatio({ phase: 'Processing', filesToRead: 40, filesRead: 10 })).toBe(0.25)
        expect(
            progressRatio({
                phase: 'Processing',
                filesToRead: 0,
                directoriesToProcess: 4,
                directoriesProcessed: 3,
            }),
        ).toBe(0.75)
        expect(progressRatio({ phase: 'Processing', filesToRead: 2, filesRead: 5 })).toBe(1)
        expect(progressRatio({ phase: 'CleaningUp' })).toBe(1)
    })

    it('maps the status of runs to activities, labels and badge tokens', () => {
        expect(activityOf('Queued')).toBe('queued')
        expect(activityOf('Running')).toBe('running')
        expect(activityOf('Succeeded')).toBe('idle')
        expect(activityOf(null)).toBe('idle')
        expect(isActive('Running')).toBe(true)
        expect(isActive('Failed')).toBe(false)
        expect(statusPresentation('Interrupted')).toEqual({
            labelKey: 'jobRuns.statuses.interrupted',
            token: 'interrupted',
        })
    })

    it('translates the codes of the API', () => {
        expect(triggerLabelKey('FileWatcher')).toBe('jobRuns.triggers.fileWatcher')
        expect(errorLabelKey('scan.media_folder_empty')).toBe('jobRuns.errors.mediaFolderEmpty')
        expect(errorLabelKey('something.new')).toBe('jobRuns.errors.jobFailed')
        expect(errorLabelKey(null)).toBeNull()
        expect(issueLabelKey('file.path_too_long')).toBe('adminLibrary.issues.pathTooLong')
        expect(phaseLabelKey('CleaningUp')).toBe('adminLibrary.phases.cleaningUp')
        expect(jobNameKey('library-scan')).toBe('adminJobs.names.libraryScan')
        expect(jobNameKey('unknown')).toBeNull()
    })

    it('converts durations to seconds', () => {
        expect(durationSeconds(83_000)).toBe(83)
        expect(durationSeconds(null)).toBeNull()
        expect(durationSeconds(undefined)).toBeNull()
    })
})
