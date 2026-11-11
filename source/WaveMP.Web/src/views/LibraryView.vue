<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import BaseIcon from '@/components/base/BaseIcon.vue'
import AlbumGroupList from '@/components/library/AlbumGroupList.vue'

defineProps<{
    /** Section of the library (route parameter), e.g. "songs". */
    section: string
}>()

const { t } = useI18n()
</script>

<template>
    <div data-part="view" data-view="library" :data-section="section">
        <template v-if="section === 'songs'">
            <p data-part="placeholder-notice" data-state="placeholder" role="note">
                <BaseIcon name="info" />
                <span data-part="label">{{ t('library.placeholderNotice') }}</span>
            </p>
            <AlbumGroupList />
        </template>
        <div v-else data-part="empty-state">
            <p data-part="empty-state-title">{{ t('library.emptyTitle') }}</p>
            <p data-part="empty-state-text">{{ t('library.emptyText') }}</p>
        </div>
    </div>
</template>
