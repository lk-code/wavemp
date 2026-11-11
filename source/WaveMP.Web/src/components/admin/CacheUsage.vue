<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { CacheUsage } from '@/api/generated/models'
import { usageRatio } from '@/admin/presentation'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseMeter from '@/components/base/BaseMeter.vue'
import { useFormat } from '@/composables/useFormat'

/** Used space of the cache against its limit, what it holds and the button that clears it. */
const props = withDefaults(
    defineProps<{
        usage: CacheUsage
        /** Whether the cache is being cleared. */
        clearing?: boolean
    }>(),
    { clearing: false },
)

const emit = defineEmits<{
    clear: []
}>()

const { t } = useI18n()
const format = useFormat()

const ratio = computed(() => usageRatio(props.usage.sizeBytes, props.usage.maxSizeBytes))
const text = computed(() =>
    t('adminCache.usage', {
        size: format.fileSize(props.usage.sizeBytes ?? 0),
        limit: format.fileSize(props.usage.maxSizeBytes ?? 0),
    }),
)
const entries = computed(() => props.usage.entryCount ?? 0)
const statistics = computed(() => [
    {
        id: 'covers',
        labelKey: 'adminCache.stats.covers',
        text: format.fileSize(props.usage.covers?.sizeBytes ?? 0),
    },
    {
        id: 'transcodes',
        labelKey: 'adminCache.stats.transcodes',
        text: format.fileSize(props.usage.transcodes?.sizeBytes ?? 0),
    },
    { id: 'entries', labelKey: 'adminCache.stats.entries', text: format.number(entries.value) },
])
</script>

<template>
    <div data-part="cache-usage">
        <BaseMeter :label="t('adminCache.usageLabel')" :value="ratio" :value-text="text" />
        <span data-part="label">{{ text }}</span>
        <dl data-part="stat-list" :aria-label="t('adminCache.stats.label')">
            <div
                v-for="item in statistics"
                :key="item.id"
                data-part="stat-item"
                :data-item="item.id"
            >
                <dt data-part="label">{{ t(item.labelKey) }}</dt>
                <dd data-part="stat-value">{{ item.text }}</dd>
            </div>
        </dl>
        <span data-part="label">{{ t('adminCache.hint') }}</span>
        <BaseButton
            icon="delete"
            :label="t('adminCache.clear')"
            :disabled="clearing || entries === 0"
            @click="emit('clear')"
        />
    </div>
</template>
