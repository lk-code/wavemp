<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import TrackCollection from '@/components/library/TrackCollection.vue'
import type { PagedListStatus } from '@/composables/usePagedList'
import { parseSort } from '@/library/sorting'
import { usePageTitleStore } from '@/stores/pageTitle'

/** The songs of a genre, grouped by album and sortable. */
const props = defineProps<{
    genre: string
}>()

const { t } = useI18n()
const route = useRoute()
const pageTitle = usePageTitleStore()
const status = ref<PagedListStatus>('idle')

const filter = computed(() => ({ genre: props.genre }))
const sortState = computed(() => parseSort('tracks', route.query, false))

watch(
    () => props.genre,
    (genre) => pageTitle.set(genre),
    { immediate: true },
)
onBeforeUnmount(() => pageTitle.clear())
</script>

<template>
    <div
        data-part="view"
        data-view="library-genre"
        :aria-busy="status === 'loading' ? 'true' : undefined"
    >
        <DetailHeader :title="genre">
            <template #media>
                <BaseCover :placeholder="false">
                    <BaseIcon name="genre" />
                </BaseCover>
            </template>
        </DetailHeader>
        <TrackCollection
            :label="t('library.songsTitle')"
            :filter="filter"
            :sort="sortState.sort"
            :direction="sortState.direction"
            @status="status = $event"
        />
    </div>
</template>
