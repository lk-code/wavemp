<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useHealthStore } from '@/stores/health'

const { t } = useI18n()
const health = useHealthStore()

const statusLabel = computed(() => {
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

onMounted(() => health.check())
</script>

<template>
    <main data-part="home">
        <h1 data-part="title">{{ t('app.name') }}</h1>
        <p data-part="health" :data-state="health.status.toLowerCase()" role="status">
            <span data-part="label">{{ t('server.status') }}</span>
            <span data-part="value" data-testid="health-status">{{ statusLabel }}</span>
        </p>
    </main>
</template>
