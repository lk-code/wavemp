<script setup lang="ts">
import { onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import AdminNav from '@/components/admin/AdminNav.vue'
import CacheClearDialog from '@/components/admin/CacheClearDialog.vue'
import CacheSettingsForm from '@/components/admin/CacheSettingsForm.vue'
import CacheUsage from '@/components/admin/CacheUsage.vue'
import BaseAlert, { type AlertTone } from '@/components/base/BaseAlert.vue'
import { useCacheStore } from '@/stores/cache'

/** Admin page of the cache: its used space, clearing it and its size limit. */
const { t } = useI18n()
const cache = useCacheStore()

const dialogOpen = ref(false)
const clearing = ref(false)
const message = ref<{ text: string; tone: AlertTone } | null>(null)
const usageTitleId = useId()
const settingsTitleId = useId()

async function clear(): Promise<void> {
    clearing.value = true
    message.value = null
    try {
        await cache.clear()
        message.value = { text: t('adminCache.cleared'), tone: 'success' }
    } catch {
        message.value = { text: t('adminCache.clearError'), tone: 'error' }
    } finally {
        clearing.value = false
        dialogOpen.value = false
    }
}

onMounted(() => cache.load())
</script>

<template>
    <div
        data-part="view"
        data-view="admin-cache"
        :aria-busy="cache.status === 'loading' || undefined"
    >
        <AdminNav />
        <section data-part="section" data-item="usage" :aria-labelledby="usageTitleId">
            <h2 :id="usageTitleId" data-part="section-title">{{ t('adminCache.usageTitle') }}</h2>
            <BaseAlert
                v-if="cache.status === 'error' && !cache.usage"
                tone="error"
                :message="t('adminCache.loadError')"
            />
            <BaseAlert
                v-if="cache.usage && !cache.usage.isAvailable"
                tone="error"
                :message="t('adminCache.unavailable')"
            />
            <CacheUsage
                v-if="cache.usage"
                :usage="cache.usage"
                :clearing="clearing"
                @clear="dialogOpen = true"
            />
            <BaseAlert v-if="message" :tone="message.tone" :message="message.text" />
        </section>
        <section data-part="section" data-item="settings" :aria-labelledby="settingsTitleId">
            <h2 :id="settingsTitleId" data-part="section-title">
                {{ t('adminCache.settingsTitle') }}
            </h2>
            <CacheSettingsForm @saved="cache.load()" />
        </section>
        <CacheClearDialog v-model:open="dialogOpen" :busy="clearing" @confirm="clear" />
    </div>
</template>
