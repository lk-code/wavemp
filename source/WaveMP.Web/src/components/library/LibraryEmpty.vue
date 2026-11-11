<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import BaseLink from '@/components/base/BaseLink.vue'
import { useSessionStore } from '@/stores/session'

/**
 * An empty list of the library: nothing matches the search text, nothing matches the filter of the
 * page, or the library is empty (admins get a link to the scan).
 */
const props = withDefaults(
    defineProps<{
        /** Search text of the list. */
        search?: string
        /** The list is filtered, e.g. by genre. */
        filtered?: boolean
    }>(),
    { search: '', filtered: false },
)

const { t } = useI18n()
const session = useSessionStore()
</script>

<template>
    <div data-part="empty-state">
        <template v-if="props.search">
            <p data-part="empty-state-title">{{ t('library.noMatchesTitle') }}</p>
            <p data-part="empty-state-text">
                {{ t('library.noMatchesText', { query: props.search }) }}
            </p>
        </template>
        <template v-else-if="props.filtered">
            <p data-part="empty-state-title">{{ t('library.noMatchesTitle') }}</p>
            <p data-part="empty-state-text">{{ t('library.filterEmptyText') }}</p>
        </template>
        <template v-else>
            <p data-part="empty-state-title">{{ t('library.emptyTitle') }}</p>
            <p data-part="empty-state-text">{{ t('library.emptyText') }}</p>
            <BaseLink v-if="session.isAdmin" :to="{ name: 'admin-library' }">
                {{ t('library.emptyAdminAction') }}
            </BaseLink>
        </template>
    </div>
</template>
