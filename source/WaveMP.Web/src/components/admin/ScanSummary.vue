<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Scan } from '@/api/generated/models'
import {
    durationSeconds,
    errorLabelKey,
    statusPresentation,
    triggerLabelKey,
} from '@/admin/presentation'
import ScanIssues from '@/components/admin/ScanIssues.vue'
import BaseAlert from '@/components/base/BaseAlert.vue'
import { useFormat } from '@/composables/useFormat'

/** Result of a finished scan: state, time, error, statistics and the files with problems. */
const props = defineProps<{
    scan: Scan
}>()

const { t } = useI18n()
const format = useFormat()

const status = computed(() => statusPresentation(props.scan.status))
const errorKey = computed(() => errorLabelKey(props.scan.errorCode))
const seconds = computed(() => durationSeconds(props.scan.durationMilliseconds))

const statistics = computed(() => {
    const value = props.scan.statistics
    if (!value) {
        return []
    }

    return [
        { id: 'filesFound', labelKey: 'adminLibrary.stats.filesFound', value: value.filesFound },
        { id: 'filesAdded', labelKey: 'adminLibrary.stats.filesAdded', value: value.filesAdded },
        {
            id: 'filesUpdated',
            labelKey: 'adminLibrary.stats.filesUpdated',
            value: value.filesUpdated,
        },
        {
            id: 'filesRemoved',
            labelKey: 'adminLibrary.stats.filesRemoved',
            value: value.filesRemoved,
        },
        {
            id: 'filesUnchanged',
            labelKey: 'adminLibrary.stats.filesUnchanged',
            value: value.filesUnchanged,
        },
        { id: 'filesFailed', labelKey: 'adminLibrary.stats.filesFailed', value: value.filesFailed },
        { id: 'tracksTotal', labelKey: 'adminLibrary.stats.tracksTotal', value: value.tracksTotal },
        { id: 'albumsTotal', labelKey: 'adminLibrary.stats.albumsTotal', value: value.albumsTotal },
        {
            id: 'artistsTotal',
            labelKey: 'adminLibrary.stats.artistsTotal',
            value: value.artistsTotal,
        },
    ].map((item) => ({ ...item, text: format.number(item.value ?? 0) }))
})
</script>

<template>
    <div data-part="run-meta">
        <span data-part="badge" :data-state="status.token">
            <span data-part="label">{{ t(status.labelKey) }}</span>
        </span>
        <span v-if="scan.queuedAt" data-part="label">{{
            t('adminLibrary.started', {
                date: format.date(scan.startedAt ?? scan.queuedAt, {
                    dateStyle: 'medium',
                    timeStyle: 'short',
                }),
            })
        }}</span>
        <span data-part="label">{{ t(triggerLabelKey(scan.trigger)) }}</span>
        <span v-if="scan.isPartial" data-part="label">{{ t('adminLibrary.partial') }}</span>
    </div>
    <BaseAlert v-if="errorKey" tone="error" :message="t(errorKey)" />
    <dl
        v-if="statistics.length > 0"
        data-part="stat-list"
        :aria-label="t('adminLibrary.stats.label')"
    >
        <div v-for="item in statistics" :key="item.id" data-part="stat-item" :data-item="item.id">
            <dt data-part="label">{{ t(item.labelKey) }}</dt>
            <dd data-part="stat-value">{{ item.text }}</dd>
        </div>
        <div v-if="seconds !== null" data-part="stat-item" data-item="duration">
            <dt data-part="label">{{ t('adminLibrary.stats.duration') }}</dt>
            <dd data-part="stat-value">{{ format.duration(seconds) }}</dd>
        </div>
    </dl>
    <ScanIssues
        v-if="(scan.issues?.length ?? 0) > 0"
        :issues="scan.issues ?? []"
        :failed="scan.statistics?.filesFailed ?? scan.issues?.length ?? 0"
    />
</template>
