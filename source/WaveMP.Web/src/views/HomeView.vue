<script setup lang="ts">
import { onMounted } from 'vue'
import { useHealthStore } from '@/stores/health'

const health = useHealthStore()

onMounted(() => health.check())
</script>

<template>
    <main data-part="home">
        <h1 data-part="title">WaveMP</h1>
        <p data-part="health" :data-state="health.status.toLowerCase()" role="status">
            <span data-part="label">Server status:</span>
            <span data-part="value" data-testid="health-status">{{ health.status }}</span>
            <span v-if="health.version" data-part="version">(v{{ health.version }})</span>
        </p>
    </main>
</template>
