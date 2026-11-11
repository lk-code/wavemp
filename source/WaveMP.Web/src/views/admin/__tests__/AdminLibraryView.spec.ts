import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import AdminJobsView from '../AdminJobsView.vue'
import AdminLibraryView from '../AdminLibraryView.vue'
import type { Scan } from '@/api/generated/models'
import { createWaveMpI18n } from '@/i18n'
import { createRoutes } from '@/router/routes'
import { useJobsStore } from '@/stores/jobs'
import { useLibrarySettingsStore } from '@/stores/librarySettings'
import { useScansStore } from '@/stores/scans'

const finished: Scan = {
    id: 'done',
    status: 'Succeeded',
    trigger: 'Manual',
    queuedAt: new Date(Date.UTC(2026, 10, 11, 9, 30)),
    statistics: { filesFound: 12, tracksTotal: 4 },
    issues: [],
}
const running: Scan = { id: 'run', status: 'Running', progress: { phase: 'Discovering' } }

async function mountView(component: unknown, path: string) {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push(path)
    const settings = useLibrarySettingsStore()
    vi.spyOn(settings, 'load').mockImplementation(async () => {
        settings.settings = { scanIntervalMinutes: 360, fileWatcherEnabled: true }
        settings.status = 'ready'
    })
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const wrapper = mount(component as any, {
        global: { plugins: [router, createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return wrapper
}

describe('AdminLibraryView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.useFakeTimers()
    })

    afterEach(() => {
        vi.useRealTimers()
    })

    it('shows the scan, the last result, the history and the settings', async () => {
        const scans = useScansStore()
        vi.spyOn(scans, 'loadCurrent').mockResolvedValue(null)
        vi.spyOn(scans, 'loadHistory').mockImplementation(async () => {
            scans.history = [finished]
            scans.historyTotal = 1
            scans.status = 'ready'
        })

        const wrapper = await mountView(AdminLibraryView, '/admin/library')

        expect(
            wrapper.get('[data-view="admin-library"]').find('[data-part="admin-nav"]').exists(),
        ).toBe(true)
        expect(wrapper.findAll('[data-part="section-title"]').map((title) => title.text())).toEqual(
            ['Scan', 'History', 'Settings'],
        )
        expect(wrapper.get('[data-part="scan-status"]').attributes('data-state')).toBe('idle')
        expect(wrapper.find('[data-part="stat-list"]').exists()).toBe(true)
        expect(wrapper.findAll('[data-part="run-item"]')).toHaveLength(1)
        expect(wrapper.find('form[data-part="form"]').exists()).toBe(true)
        wrapper.unmount()
    })

    it('starts a scan, follows it and shows the result when it has finished', async () => {
        const scans = useScansStore()
        const loadCurrent = vi.spyOn(scans, 'loadCurrent').mockImplementationOnce(async () => {
            scans.current = null
            return null
        })
        vi.spyOn(scans, 'loadHistory').mockImplementation(async () => {
            scans.status = 'ready'
        })
        vi.spyOn(scans, 'start').mockImplementation(async () => {
            scans.current = running
            return 'started'
        })
        const wrapper = await mountView(AdminLibraryView, '/admin/library')

        await wrapper.get('[data-part="scan-status"] button').trigger('click')
        await flushPromises()
        expect(wrapper.get('[data-part="scan-status"]').attributes('data-state')).toBe('running')

        loadCurrent.mockImplementation(async () => {
            scans.current = null
            return null
        })
        await vi.advanceTimersByTimeAsync(1100)
        await flushPromises()

        expect(wrapper.get('[data-part="scan-status"]').attributes('data-state')).toBe('idle')
        expect(scans.loadHistory).toHaveBeenCalledTimes(3)
        wrapper.unmount()
    })

    it('reports a scan that is already running', async () => {
        const scans = useScansStore()
        vi.spyOn(scans, 'loadCurrent').mockResolvedValue(null)
        vi.spyOn(scans, 'loadHistory').mockResolvedValue()
        vi.spyOn(scans, 'start').mockResolvedValue('alreadyRunning')
        const wrapper = await mountView(AdminLibraryView, '/admin/library')

        await wrapper.get('[data-part="scan-status"] button').trigger('click')
        await flushPromises()

        expect(wrapper.text()).toContain('A scan is already running.')
        wrapper.unmount()
    })
})

describe('AdminJobsView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
    })

    it('lists the jobs and runs one', async () => {
        const jobs = useJobsStore()
        vi.spyOn(jobs, 'load').mockImplementation(async () => {
            jobs.jobs = [{ id: 'library-scan', state: 'Idle', intervalMinutes: 360 }]
            jobs.status = 'ready'
        })
        const run = vi.spyOn(jobs, 'run').mockResolvedValue('alreadyRunning')

        const wrapper = await mountView(AdminJobsView, '/admin/jobs')
        await wrapper.get('[data-part="job-actions"] button').trigger('click')
        await flushPromises()

        expect(wrapper.find('[data-part="admin-nav"]').exists()).toBe(true)
        expect(wrapper.get('[data-part="job-name"]').text()).toBe('Library scan')
        expect(run).toHaveBeenCalledWith('library-scan')
        expect(wrapper.text()).toContain('This job is already running.')
        wrapper.unmount()
    })
})
