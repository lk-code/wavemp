<script setup lang="ts">
import { onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminNav from '@/components/admin/AdminNav.vue'
import LibrarySettingsForm from '@/components/admin/LibrarySettingsForm.vue'
import ScanHistory from '@/components/admin/ScanHistory.vue'
import ScanPanel from '@/components/admin/ScanPanel.vue'
import ScanSummary from '@/components/admin/ScanSummary.vue'
import BaseAlert from '@/components/base/BaseAlert.vue'
import { usePolling } from '@/composables/usePolling'
import { useScansStore } from '@/stores/scans'

/** Admin page of the library: start a scan, follow it, see its result, the history and settings. */
const { t } = useI18n()
const scans = useScansStore()

const starting = ref(false)
const message = ref<string | null>(null)
const scanTitleId = useId()
const historyTitleId = useId()
const settingsTitleId = useId()

const polling = usePolling(refresh, 1000)

/** Follows the active scan; once it has finished, its result appears in the history. */
async function refresh(): Promise<void> {
    const current = await scans.loadCurrent()
    if (!current) {
        polling.stop()
        await scans.loadHistory()
    }
}

async function start(): Promise<void> {
    starting.value = true
    message.value = null
    const result = await scans.start()
    starting.value = false
    if (result === 'alreadyRunning') {
        message.value = t('adminLibrary.alreadyRunning')
    } else if (result === 'error') {
        message.value = t('adminLibrary.startError')
    }

    if (scans.current) {
        polling.start()
    }

    await scans.loadHistory()
}

onMounted(async () => {
    await Promise.all([scans.loadCurrent().catch(() => null), scans.loadHistory()])
    if (scans.current) {
        polling.start()
    }
})
</script>

<template>
    <div data-part="view" data-view="admin-library">
        <AdminNav />
        <section data-part="section" :aria-labelledby="scanTitleId">
            <h2 :id="scanTitleId" data-part="section-title">{{ t('adminLibrary.scanTitle') }}</h2>
            <ScanPanel
                :scan="scans.current"
                :activity="scans.activity"
                :starting="starting"
                @start="start"
            />
            <BaseAlert v-if="message" tone="error" :message="message" />
            <ScanSummary v-if="scans.lastFinished" :scan="scans.lastFinished" />
            <BaseAlert
                v-else-if="scans.status === 'ready' && scans.activity === 'idle'"
                tone="info"
                :message="t('adminLibrary.noScanYet')"
            />
        </section>
        <section data-part="section" :aria-labelledby="historyTitleId">
            <h2 :id="historyTitleId" data-part="section-title">
                {{ t('adminLibrary.historyTitle') }}
            </h2>
            <BaseAlert
                v-if="scans.status === 'error'"
                tone="error"
                :message="t('adminLibrary.loadError')"
            />
            <ScanHistory
                v-if="scans.history.length > 0"
                :scans="scans.history"
                :has-more="scans.hasMoreHistory"
                :loading="scans.status === 'loading'"
                @more="scans.loadHistory({ reset: false })"
            />
            <BaseAlert
                v-else-if="scans.status === 'ready'"
                tone="info"
                :message="t('adminLibrary.noHistory')"
            />
        </section>
        <section data-part="section" :aria-labelledby="settingsTitleId">
            <h2 :id="settingsTitleId" data-part="section-title">
                {{ t('adminLibrary.settingsTitle') }}
            </h2>
            <LibrarySettingsForm />
        </section>
    </div>
</template>
