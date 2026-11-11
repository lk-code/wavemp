<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminNav from '@/components/admin/AdminNav.vue'
import JobList from '@/components/admin/JobList.vue'
import BaseAlert from '@/components/base/BaseAlert.vue'
import { usePolling } from '@/composables/usePolling'
import { useJobsStore } from '@/stores/jobs'

/** Admin page of the background jobs: last and next run of every job, and a manual start. */
const { t } = useI18n()
const jobs = useJobsStore()

const starting = ref<string | null>(null)
const message = ref<string | null>(null)

const polling = usePolling(async () => {
    await jobs.load()
    if (!jobs.anyActive) {
        polling.stop()
    }
}, 2000)

async function run(id: string): Promise<void> {
    starting.value = id
    message.value = null
    const result = await jobs.run(id)
    starting.value = null
    if (result === 'alreadyRunning') {
        message.value = t('adminJobs.alreadyRunning')
    } else if (result === 'error') {
        message.value = t('adminJobs.runError')
    }

    await jobs.load()
    if (jobs.anyActive) {
        polling.start()
    }
}

onMounted(async () => {
    await jobs.load()
    if (jobs.anyActive) {
        polling.start()
    }
})
</script>

<template>
    <div data-part="view" data-view="admin-jobs">
        <AdminNav />
        <BaseAlert
            v-if="jobs.status === 'error'"
            tone="error"
            :message="t('adminJobs.loadError')"
        />
        <BaseAlert v-if="message" tone="error" :message="message" />
        <JobList :jobs="jobs.jobs" :starting="starting" @run="run" />
    </div>
</template>
