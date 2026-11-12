import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import type { TranscodeJob, TranscodingSettings, TranscodingStatus } from '@/api/generated/models'

const api = vi.hoisted(() => ({
    status: vi.fn(),
    jobs: vi.fn(),
    cancel: vi.fn(),
    settings: vi.fn(),
    patch: vi.fn(),
}))

vi.mock('@/api/client', () => ({
    apiClient: {
        api: {
            v1: {
                transcoding: {
                    status: { get: () => api.status() },
                    jobs: {
                        get: (config: unknown) => api.jobs(config),
                        byJobId: (id: string) => ({ delete: () => api.cancel(id) }),
                    },
                },
                settings: {
                    transcoding: {
                        get: () => api.settings(),
                        patch: (body: unknown) => api.patch(body),
                    },
                },
            },
        },
    },
}))

const { default: AdminTranscodingView } = await import('../AdminTranscodingView.vue')
const { createWaveMpI18n } = await import('@/i18n')
const { createRoutes } = await import('@/router/routes')

const status: TranscodingStatus = {
    available: true,
    ffmpegAvailable: true,
    cacheAvailable: true,
    runningJobs: 1,
    maxParallelJobs: 2,
    hardware: { state: 'Active', accelerator: 'Qsv', device: '/dev/dri/renderD128' },
}

const job: TranscodeJob = {
    id: 'job-1',
    trackId: 'track-1',
    track: { id: 'track-1', title: 'Song', artist: { id: 'band', name: 'Band' } },
    media: 'Video',
    accelerator: 'Qsv',
    startSegment: 0,
    completedSegments: 5,
    segmentCount: 20,
    startedAt: new Date(Date.UTC(2026, 10, 12, 9, 30)),
}

const settings: TranscodingSettings = {
    hardwareAcceleration: 'Auto',
    maxParallelJobs: 2,
    maxVideoHeight: 1080,
    audioBitrateKbps: 192,
}

async function mountView() {
    const router = createRouter({ history: createMemoryHistory(), routes: createRoutes() })
    await router.push('/admin/transcoding')
    const wrapper = mount(AdminTranscodingView, {
        global: { plugins: [router, createWaveMpI18n('en')] },
        attachTo: document.body,
    })
    await flushPromises()
    return wrapper
}

describe('AdminTranscodingView', () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        for (const mock of Object.values(api)) {
            mock.mockReset()
        }
        api.status.mockResolvedValue(status)
        api.jobs.mockResolvedValue({ items: [job], total: 1, offset: 0, limit: 50 })
        api.settings.mockResolvedValue(settings)
    })

    afterEach(() => {
        document.body.innerHTML = ''
    })

    it('shows the status, the running transcodes and the settings', async () => {
        const wrapper = await mountView()

        expect(wrapper.get('[data-part="view"]').attributes('data-view')).toBe('admin-transcoding')
        expect(wrapper.get('[data-part="admin-nav-item"][aria-current="page"]').text()).toBe(
            'Transcoding',
        )
        const sections = wrapper.findAll('section[data-part="section"]')
        expect(sections.map((section) => section.attributes('data-item'))).toEqual([
            'status',
            'jobs',
            'settings',
        ])
        const hardware = wrapper.get('[data-part="stat-item"][data-item="hardware"]')
        expect(hardware.get('[data-part="badge"]').attributes('data-state')).toBe('active')
        expect(hardware.text()).toContain('Active')
        expect(wrapper.get('[data-item="encoder"]').text()).toContain('Quick Sync')
        expect(wrapper.get('[data-item="device"]').text()).toContain('/dev/dri/renderD128')
        expect(wrapper.get('[data-item="running-jobs"] [data-part="stat-value"]').text()).toBe(
            '1 of 2',
        )

        const item = wrapper.get('[data-part="transcode-item"]')
        expect(item.attributes('data-state')).toBe('video')
        expect(item.get('[data-part="transcode-title"]').text()).toBe('Song – Band')
        expect(item.get('[data-part="progress"]').attributes('aria-valuenow')).toBe('25')
        expect(api.jobs).toHaveBeenCalledWith({ queryParameters: { limit: 50 } })

        const selects = wrapper.findAll('select')
        expect(selects.map((select) => (select.element as HTMLSelectElement).value)).toEqual([
            'Auto',
            '2',
            '1080',
            '192',
        ])
        wrapper.unmount()
    })

    it('stops a running transcode', async () => {
        const wrapper = await mountView()
        api.cancel.mockResolvedValue(undefined)
        api.jobs.mockResolvedValue({ items: [], total: 0, offset: 0, limit: 50 })

        await wrapper.get('[data-part="transcode-actions"] [data-item="cancel"]').trigger('click')
        await flushPromises()

        expect(api.cancel).toHaveBeenCalledWith('job-1')
        expect(wrapper.find('[data-part="transcode-item"]').exists()).toBe(false)
        expect(wrapper.get('[data-item="jobs"] [data-part="empty-state"]').text()).toBe(
            'No transcodes are running.',
        )
        wrapper.unmount()
    })

    it('saves the settings', async () => {
        const wrapper = await mountView()
        api.patch.mockResolvedValue({
            ...settings,
            hardwareAcceleration: 'Off',
            maxParallelJobs: 4,
        })

        const selects = wrapper.findAll('select')
        await selects[0]!.setValue('Off')
        await selects[1]!.setValue('4')
        await wrapper.get('form').trigger('submit')
        await flushPromises()

        expect(api.patch).toHaveBeenCalledWith({
            hardwareAcceleration: 'Off',
            maxParallelJobs: 4,
            maxVideoHeight: 1080,
            audioBitrateKbps: 192,
        })
        expect(wrapper.get('[data-item="settings"] [data-part="alert"]').text()).toBe(
            'The settings are saved.',
        )
        wrapper.unmount()
    })

    it('says why nothing can be transcoded and why the hardware is not used', async () => {
        api.status.mockResolvedValue({
            ...status,
            available: false,
            ffmpegAvailable: false,
            hardware: { state: 'Unavailable', accelerator: 'None', reason: 'ffmpeg_missing' },
        })
        api.jobs.mockResolvedValue({ items: [], total: 0, offset: 0, limit: 50 })

        const wrapper = await mountView()

        const panel = wrapper.get('[data-part="transcoding-status"]')
        expect(panel.get('[data-part="alert"]').text()).toContain('Nothing can be transcoded')
        expect(panel.text()).toContain('ffmpeg cannot be started.')
        expect(
            panel.get('[data-item="hardware"] [data-part="badge"]').attributes('data-state'),
        ).toBe('unavailable')
        wrapper.unmount()
    })

    it('shows an error when the status cannot be loaded', async () => {
        api.status.mockRejectedValue(new Error('offline'))

        const wrapper = await mountView()

        expect(wrapper.get('[data-item="status"] [data-part="alert"]').text()).toBe(
            'The transcoding status could not be loaded.',
        )
        wrapper.unmount()
    })
})
