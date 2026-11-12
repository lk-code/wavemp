<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseAlert from '@/components/base/BaseAlert.vue'
import EqualizerPanel from '@/components/player/EqualizerPanel.vue'
import { useEqualizerStore } from '@/stores/equalizer'
import { usePlayerStore } from '@/stores/player'

/**
 * The equalizer of the profile below Now Playing. Changes apply at once and are saved shortly
 * after; leaving the page saves what is still open.
 */
const { t } = useI18n()
const equalizer = useEqualizerStore()
const player = usePlayerStore()

onMounted(() => {
    void equalizer.ensureDefinition()
})

onUnmounted(() => {
    void equalizer.flush()
})
</script>

<template>
    <div data-part="view" data-view="equalizer">
        <BaseAlert
            v-if="equalizer.loadState === 'failed'"
            tone="info"
            :message="t('equalizer.loadError')"
        />
        <BaseAlert v-if="equalizer.saveError" tone="error" :message="t('equalizer.saveError')" />
        <EqualizerPanel
            :enabled="equalizer.settings.enabled"
            :preset="equalizer.settings.preset"
            :preamp-db="equalizer.output.preampDb"
            :gains-db="equalizer.output.gainsDb"
            :bands="equalizer.layout"
            :min-gain-db="equalizer.minGainDb"
            :max-gain-db="equalizer.maxGainDb"
            :available="player.equalizerAvailable"
            @enable="equalizer.setEnabled"
            @preset="equalizer.selectPreset"
            @band="equalizer.setBand"
            @preamp="equalizer.setPreamp"
            @reset="equalizer.reset"
        />
    </div>
</template>
