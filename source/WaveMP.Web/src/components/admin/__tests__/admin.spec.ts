import { describe, expect, it } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createMemoryHistory, createRouter } from 'vue-router'
import AdminNav from '../AdminNav.vue'
import JobList from '../JobList.vue'
import ScanHistory from '../ScanHistory.vue'
import ScanPanel from '../ScanPanel.vue'
import ScanSummary from '../ScanSummary.vue'
import type { Job, Scan } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'

const time = new Date(Date.UTC(2026, 10, 11, 9, 30))

async function mountWithRouter<T>(
    component: T,
    props: Record<string, unknown>,
    path = '/admin/library',
) {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push(path)
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return mount(component as any, { props, global: { plugins: [router, createWaveMpI18n('en')] } })
}

describe('AdminNav', () => {
    it('links the pages of the administration and marks the current one', async () => {
        const wrapper = await mountWithRouter(AdminNav, {}, '/admin/jobs')

        const nav = wrapper.get('nav[data-part="admin-nav"]')
        expect(nav.attributes('aria-label')).toBe('Administration')
        const items = wrapper.findAll('[data-part="admin-nav-item"]')
        expect(items.map((item) => item.text())).toEqual([
            'Library',
            'Jobs',
            'Cache',
            'Transcoding',
            'Profiles',
        ])
        expect(items.map((item) => item.attributes('href'))).toEqual([
            '/admin/library',
            '/admin/jobs',
            '/admin/cache',
            '/admin/transcoding',
            '/admin/users',
        ])
        expect(items.map((item) => item.attributes('data-item'))).toContain('admin-cache')
        expect(items[1]!.attributes('aria-current')).toBe('page')
        expect(items[1]!.attributes('data-state')).toBe('active')
        expect(items[0]!.attributes('aria-current')).toBeUndefined()
    })
})

describe('ScanPanel', () => {
    it('offers a scan while idle', async () => {
        const wrapper = await mountWithRouter(ScanPanel, { scan: null, activity: 'idle' })

        const status = wrapper.get('[data-part="scan-status"]')
        expect(status.attributes('data-state')).toBe('idle')
        expect(status.get('[data-part="badge"]').text()).toBe('Idle')
        expect(wrapper.find('[data-part="progress"]').exists()).toBe(false)
        const button = wrapper.get('button')
        expect(button.text()).toBe('Scan now')
        expect(button.attributes('disabled')).toBeUndefined()

        await button.trigger('click')

        expect(wrapper.emitted('start')).toHaveLength(1)
    })

    it('shows the progress of a running scan and blocks a second one', async () => {
        const scan: Scan = {
            id: 'run',
            status: 'Running',
            progress: {
                phase: 'Processing',
                filesToRead: 40,
                filesRead: 10,
                directoriesToProcess: 2,
            },
        }

        const wrapper = await mountWithRouter(ScanPanel, { scan, activity: 'running' })

        expect(wrapper.get('[data-part="scan-status"]').attributes('data-state')).toBe('running')
        const progress = wrapper.get('[data-part="progress"]')
        expect(progress.attributes('aria-valuenow')).toBe('25')
        expect(progress.attributes('aria-valuetext')).toBe('10 of 40 files')
        expect(wrapper.text()).toContain('Reading files')
        expect(wrapper.get('button').attributes('disabled')).toBeDefined()
    })

    it('shows an indeterminate progress while looking for files', async () => {
        const scan: Scan = {
            status: 'Running',
            progress: { phase: 'Discovering', filesFound: 1200, directoriesFound: 30 },
        }

        const wrapper = await mountWithRouter(ScanPanel, { scan, activity: 'running' })

        const progress = wrapper.get('[data-part="progress"]')
        expect(progress.attributes('data-state')).toBe('indeterminate')
        expect(progress.attributes('aria-valuetext')).toBe('1,200 files in 30 folders')
    })
})

describe('ScanSummary', () => {
    it('shows state, statistics, error and the files with problems', async () => {
        const scan: Scan = {
            id: 'done',
            status: 'Failed',
            trigger: 'Schedule',
            queuedAt: time,
            startedAt: time,
            durationMilliseconds: 65_000,
            errorCode: 'scan.media_folder_empty',
            statistics: { filesFound: 1500, filesAdded: 3, tracksTotal: 99, filesFailed: 60 },
            issues: [{ path: 'a/broken.mp3', code: 'file.probe_failed' }],
        }

        const wrapper = await mountWithRouter(ScanSummary, { scan })

        expect(
            wrapper.get('[data-part="run-meta"] [data-part="badge"]').attributes('data-state'),
        ).toBe('failed')
        expect(wrapper.get('[data-part="alert"]').text()).toContain('nothing was removed')
        const values = Object.fromEntries(
            wrapper
                .findAll('[data-part="stat-item"]')
                .map((item) => [
                    item.attributes('data-item'),
                    item.get('[data-part="stat-value"]').text(),
                ]),
        )
        expect(values.filesFound).toBe('1,500')
        expect(values.tracksTotal).toBe('99')
        expect(values.duration).toBe('1:05')
        const disclosure = wrapper.get('details[data-part="disclosure"]')
        expect(disclosure.get('[data-part="disclosure-summary"]').text()).toContain(
            '60 files with problems',
        )
        expect(disclosure.get('[data-part="issue-path"]').text()).toBe('a/broken.mp3')
        expect(disclosure.get('[data-part="issue-item"]').text()).toContain('No media information')
    })
})

describe('ScanHistory', () => {
    it('lists the scans with status, trigger and changes and loads more', async () => {
        const scans: Scan[] = [
            {
                id: 'one',
                status: 'Succeeded',
                trigger: 'FileWatcher',
                queuedAt: time,
                statistics: { filesAdded: 2, filesUpdated: 1, filesRemoved: 0 },
            },
            {
                id: 'two',
                status: 'Interrupted',
                trigger: 'Schedule',
                queuedAt: time,
                errorCode: 'job.interrupted',
            },
        ]

        const wrapper = await mountWithRouter(ScanHistory, { scans, hasMore: true })

        const items = wrapper.findAll('[data-part="run-item"]')
        expect(items.map((item) => item.attributes('data-state'))).toEqual([
            'succeeded',
            'interrupted',
        ])
        expect(items[0]!.get('[data-part="run-meta"]').text()).toContain('File watcher')
        expect(items[0]!.get('[data-part="run-meta"]').text()).toContain(
            '2 added, 1 updated, 0 removed',
        )
        expect(items[1]!.text()).toContain('because the server stopped')

        await wrapper.get('button').trigger('click')

        expect(wrapper.emitted('more')).toHaveLength(1)
    })
})

describe('JobList', () => {
    it('shows the jobs with their runs and runs an idle one', async () => {
        const jobs: Job[] = [
            {
                id: 'library-scan',
                state: 'Idle',
                intervalMinutes: 360,
                nextRunAt: time,
                lastRun: { status: 'Succeeded', startedAt: time, durationMilliseconds: 3_000 },
            },
        ]

        const wrapper = await mountWithRouter(JobList, { jobs })
        await flushPromises()

        const item = wrapper.get('[data-part="job-item"]')
        expect(item.attributes('data-state')).toBe('idle')
        expect(item.get('[data-part="job-name"]').text()).toBe('Library scan')
        const meta = item.get('[data-part="job-meta"]')
        expect(meta.get('[data-part="badge"]').attributes('data-state')).toBe('succeeded')
        expect(meta.text()).toContain('Every 360 minutes')
        expect(meta.text()).toContain('took 0:03')

        await item.get('button').trigger('click')

        expect(wrapper.emitted('run')).toEqual([['library-scan']])
    })

    it('says when a job without schedule runs', async () => {
        const jobs: Job[] = [
            { id: 'cover-cache', state: 'Idle', intervalMinutes: null, lastRun: null },
            { id: 'library-scan', state: 'Idle', intervalMinutes: null, lastRun: null },
        ]

        const wrapper = await mountWithRouter(JobList, { jobs })

        const items = wrapper.findAll('[data-part="job-item"]')
        expect(items[0]!.get('[data-part="job-name"]').text()).toBe('Prepare covers')
        expect(items[0]!.get('[data-part="job-meta"]').text()).toContain('Runs after scans')
        expect(items[1]!.get('[data-part="job-meta"]').text()).toContain('Runs only when started')
    })

    it('blocks a job that is running', async () => {
        const jobs: Job[] = [{ id: 'library-scan', state: 'Running', lastRun: null }]

        const wrapper = await mountWithRouter(JobList, { jobs })

        expect(wrapper.get('[data-part="job-item"]').attributes('data-state')).toBe('running')
        expect(wrapper.get('button').attributes('disabled')).toBeDefined()
        expect(wrapper.text()).toContain('Not run yet')
    })
})
