<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { MediaFile } from '@/api/generated/models'
import { issueLabelKey } from '@/admin/presentation'
import { useFormat } from '@/composables/useFormat'
import { durationSeconds, fileKindKey, fileKindToken } from '@/library/presentation'

/**
 * The files of a track for admins: path, kind, whether it plays (primary), and its technical
 * details. Problems found by the scan are shown with the texts of the scan.
 */
defineProps<{
    files: readonly MediaFile[]
    /** Accessible name of the list. */
    label: string
}>()

const { t } = useI18n()
const format = useFormat()

function itemState(file: MediaFile): string {
    return [fileKindToken(file.kind), ...(file.isPrimary ? ['primary'] : [])].join(' ')
}

function details(file: MediaFile): string[] {
    const values: string[] = []
    const codec = file.codec ?? file.format
    if (codec) {
        values.push(codec.toUpperCase())
    }

    if (file.bitrate) {
        values.push(t('files.bitrate', { value: format.number(Math.round(file.bitrate / 1000)) }))
    }

    if (file.sampleRate) {
        values.push(
            t('files.sampleRate', {
                value: format.number(file.sampleRate / 1000, { maximumFractionDigits: 1 }),
            }),
        )
    }

    if (file.bitDepth) {
        values.push(t('files.bitDepth', { value: format.number(file.bitDepth) }))
    }

    if (file.channels) {
        values.push(t('files.channels', { count: format.number(file.channels) }))
    }

    if (file.width && file.height) {
        values.push(
            t('files.resolution', {
                width: format.number(file.width, { useGrouping: false }),
                height: format.number(file.height, { useGrouping: false }),
            }),
        )
    }

    const seconds = durationSeconds(file.durationMilliseconds)
    if (seconds !== null) {
        values.push(format.duration(seconds))
    }

    values.push(format.fileSize(file.size ?? 0))
    if (file.modifiedAt) {
        values.push(t('files.modified', { date: format.date(file.modifiedAt) }))
    }

    return values
}
</script>

<template>
    <ul data-part="file-list" :aria-label="label">
        <li
            v-for="file in files"
            :key="file.id ?? ''"
            data-part="file-item"
            :data-state="itemState(file)"
        >
            <span data-part="file-path">{{ file.relativePath }}</span>
            <span data-part="file-meta">
                <span data-part="badge" :data-state="fileKindToken(file.kind)">
                    <span data-part="label">{{ t(fileKindKey(file.kind)) }}</span>
                </span>
                <span v-if="file.isPrimary" data-part="badge" data-state="primary">
                    <span data-part="label">{{ t('files.primary') }}</span>
                </span>
                <span v-if="file.isLossless" data-part="badge" data-state="lossless">
                    <span data-part="label">{{ t('files.lossless') }}</span>
                </span>
                <span v-if="file.isWritable" data-part="badge" data-state="writable">
                    <span data-part="label">{{ t('files.writable') }}</span>
                </span>
                <span v-if="file.issue" data-part="badge" data-state="issue">
                    <span data-part="label">{{ t(issueLabelKey(file.issue)) }}</span>
                </span>
                <span v-for="(detail, index) in details(file)" :key="index" data-part="label">
                    {{ detail }}
                </span>
            </span>
        </li>
    </ul>
</template>
