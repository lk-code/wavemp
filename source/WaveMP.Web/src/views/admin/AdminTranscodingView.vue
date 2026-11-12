<script setup lang="ts">
import { onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminNav from '@/components/admin/AdminNav.vue'
import TranscodeJobList from '@/components/admin/TranscodeJobList.vue'
import TranscodingSettingsForm from '@/components/admin/TranscodingSettingsForm.vue'
import TranscodingStatus from '@/components/admin/TranscodingStatus.vue'
import BaseAlert from '@/components/base/BaseAlert.vue'
import { usePolling } from '@/composables/usePolling'
import { useTranscodingStore } from '@/stores/transcoding'

/**
 * Admin page of the transcoding: whether it works, the hardware acceleration, the running
 * transcodes (refreshed while the page is open) and the settings.
 */
const { t } = useI18n()
const transcoding = useTranscodingStore()

const cancelling = ref<string | null>(null)
const message = ref<string | null>(null)
const statusTitleId = useId()
const jobsTitleId = useId()
const settingsTitleId = useId()

const polling = usePolling(() => transcoding.load(), 3000)

async function cancel(id: string): Promise<void> {
    cancelling.value = id
    message.value = null
    try {
        await transcoding.cancel(id)
    } catch {
        message.value = t('adminTranscoding.cancelError')
        await transcoding.load()
    } finally {
        cancelling.value = null
    }
}

onMounted(async () => {
    await transcoding.load()
    polling.start()
})
</script>

<template>
    <div
        data-part="view"
        data-view="admin-transcoding"
        :aria-busy="transcoding.loadStatus === 'loading' || undefined"
    >
        <AdminNav />
        <section data-part="section" data-item="status" :aria-labelledby="statusTitleId">
            <h2 :id="statusTitleId" data-part="section-title">
                {{ t('adminTranscoding.statusTitle') }}
            </h2>
            <BaseAlert
                v-if="transcoding.loadStatus === 'error' && !transcoding.status"
                tone="error"
                :message="t('adminTranscoding.loadError')"
            />
            <TranscodingStatus v-if="transcoding.status" :status="transcoding.status" />
        </section>
        <section data-part="section" data-item="jobs" :aria-labelledby="jobsTitleId">
            <h2 :id="jobsTitleId" data-part="section-title">{{ t('adminTranscoding.jobs') }}</h2>
            <BaseAlert v-if="message" tone="error" :message="message" />
            <TranscodeJobList
                v-if="transcoding.status"
                :jobs="transcoding.jobs"
                :cancelling="cancelling"
                @cancel="cancel"
            />
        </section>
        <section data-part="section" data-item="settings" :aria-labelledby="settingsTitleId">
            <h2 :id="settingsTitleId" data-part="section-title">
                {{ t('adminTranscoding.settingsTitle') }}
            </h2>
            <TranscodingSettingsForm @saved="transcoding.load()" />
        </section>
    </div>
</template>
