<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { Scan } from '@/api/generated/models'
import {
    durationSeconds,
    errorLabelKey,
    statusPresentation,
    triggerLabelKey,
} from '@/admin/presentation'
import BaseButton from '@/components/base/BaseButton.vue'
import { useFormat } from '@/composables/useFormat'

/** Earlier scans, newest first, with a button to load more. */
withDefaults(
    defineProps<{
        scans: readonly Scan[]
        hasMore?: boolean
        loading?: boolean
    }>(),
    { hasMore: false, loading: false },
)

const emit = defineEmits<{
    more: []
}>()

const { t } = useI18n()
const format = useFormat()

function when(scan: Scan): string {
    const date = scan.startedAt ?? scan.queuedAt
    return date ? format.date(date, { dateStyle: 'medium', timeStyle: 'short' }) : ''
}

function changes(scan: Scan): string | null {
    const statistics = scan.statistics
    return statistics
        ? t('adminLibrary.changes', {
              added: format.number(statistics.filesAdded ?? 0),
              updated: format.number(statistics.filesUpdated ?? 0),
              removed: format.number(statistics.filesRemoved ?? 0),
          })
        : null
}
</script>

<template>
    <ul data-part="run-list" :aria-label="t('adminLibrary.history')">
        <li
            v-for="scan in scans"
            :key="scan.id ?? ''"
            data-part="run-item"
            :data-state="statusPresentation(scan.status).token"
        >
            <span data-part="badge" :data-state="statusPresentation(scan.status).token">
                <span data-part="label">{{ t(statusPresentation(scan.status).labelKey) }}</span>
            </span>
            <span data-part="label">{{ when(scan) }}</span>
            <span data-part="run-meta">
                <span data-part="label">{{ t(triggerLabelKey(scan.trigger)) }}</span>
                <span
                    v-if="durationSeconds(scan.durationMilliseconds) !== null"
                    data-part="label"
                    >{{ format.duration(durationSeconds(scan.durationMilliseconds) ?? 0) }}</span
                >
                <span v-if="changes(scan)" data-part="label">{{ changes(scan) }}</span>
                <span v-if="errorLabelKey(scan.errorCode)" data-part="label">{{
                    t(errorLabelKey(scan.errorCode) ?? '')
                }}</span>
            </span>
        </li>
    </ul>
    <BaseButton
        v-if="hasMore"
        :label="t('adminLibrary.showMore')"
        :disabled="loading"
        @click="emit('more')"
    />
</template>
