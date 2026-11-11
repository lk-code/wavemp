<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { ScanIssue } from '@/api/generated/models'
import { issueLabelKey } from '@/admin/presentation'
import { useFormat } from '@/composables/useFormat'

/** Files with problems of a scan, collapsed by default. */
withDefaults(
    defineProps<{
        issues: readonly ScanIssue[]
        /** All files with problems, also those that are not listed. */
        failed: number
        /** Shows the list at once. */
        open?: boolean
    }>(),
    { open: false },
)

const { t } = useI18n()
const format = useFormat()
</script>

<template>
    <details data-part="disclosure" :open="open">
        <summary data-part="disclosure-summary">
            <span data-part="label">{{
                t('adminLibrary.issuesSummary', { count: format.number(failed) })
            }}</span>
            <span v-if="failed > issues.length" data-part="field-hint">{{
                t('adminLibrary.issuesListed', { count: format.number(issues.length) })
            }}</span>
        </summary>
        <ul data-part="issue-list">
            <li v-for="issue in issues" :key="issue.path ?? ''" data-part="issue-item">
                <span data-part="issue-path">{{ issue.path }}</span>
                <span data-part="label">{{ t(issueLabelKey(issue.code)) }}</span>
            </li>
        </ul>
    </details>
</template>
