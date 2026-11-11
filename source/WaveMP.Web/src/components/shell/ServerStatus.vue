<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useHealthStore } from '@/stores/health'

const { t } = useI18n()
const health = useHealthStore()

const state = computed(() => health.status.toLowerCase())

const label = computed(() => {
    switch (health.status) {
        case 'Checking':
            return t('server.checking')
        case 'Healthy':
            return t('server.healthy')
        case 'Degraded':
            return t('server.degraded')
        case 'Unhealthy':
            return t('server.unhealthy')
        default:
            return t('server.unreachable')
    }
})

// Tooltip: the version of the service, once it is known.
const title = computed(() =>
    health.version ? t('server.version', { version: health.version }) : t('server.status'),
)
</script>

<template>
    <div data-part="server-status" role="status" :data-state="state" :title="title">
        <span data-part="status-indicator" aria-hidden="true"></span>
        <span data-part="label">{{ label }}</span>
    </div>
</template>
