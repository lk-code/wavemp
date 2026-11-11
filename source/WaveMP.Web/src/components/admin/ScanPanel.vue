<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Scan } from '@/api/generated/models'
import {
    phaseLabelKey,
    progressRatio,
    scanStateLabelKey,
    type Activity,
} from '@/admin/presentation'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseProgress from '@/components/base/BaseProgress.vue'
import { useFormat } from '@/composables/useFormat'

/** State of the library scan with its progress and the button that starts a scan. */
const props = withDefaults(
    defineProps<{
        /** The queued or running scan; null while idle. */
        scan: Scan | null
        activity: Activity
        /** A start was requested and has not been answered yet. */
        starting?: boolean
    }>(),
    { starting: false },
)

const emit = defineEmits<{
    start: []
}>()

const { t } = useI18n()
const format = useFormat()

const progress = computed(() => props.scan?.progress ?? null)
const ratio = computed(() => progressRatio(progress.value))
const progressText = computed(() => {
    const value = progress.value
    if (!value) {
        return undefined
    }

    if (value.phase === 'Discovering') {
        return t('adminLibrary.found', {
            files: format.number(value.filesFound ?? 0),
            folders: format.number(value.directoriesFound ?? 0),
        })
    }

    return (value.filesToRead ?? 0) > 0
        ? t('adminLibrary.progress', {
              done: format.number(value.filesRead ?? 0),
              total: format.number(value.filesToRead ?? 0),
          })
        : t('adminLibrary.progressFolders', {
              done: format.number(value.directoriesProcessed ?? 0),
              total: format.number(value.directoriesToProcess ?? 0),
          })
})
</script>

<template>
    <div data-part="scan-status" :data-state="activity">
        <span data-part="badge" :data-state="activity">
            <span data-part="label">{{ t(scanStateLabelKey(activity)) }}</span>
        </span>
        <template v-if="activity === 'running'">
            <span data-part="label">{{ t(phaseLabelKey(progress?.phase)) }}</span>
            <BaseProgress
                :label="t('adminLibrary.progressLabel')"
                :value="ratio"
                :value-text="progressText"
            />
            <span v-if="progressText" data-part="label">{{ progressText }}</span>
        </template>
        <BaseProgress
            v-else-if="activity === 'queued'"
            :label="t('adminLibrary.progressLabel')"
            :value-text="t('adminLibrary.states.queued')"
        />
        <BaseButton
            variant="primary"
            icon="scan"
            :label="t('adminLibrary.scanNow')"
            :disabled="activity !== 'idle' || starting"
            @click="emit('start')"
        />
    </div>
</template>
