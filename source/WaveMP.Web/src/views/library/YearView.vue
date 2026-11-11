<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import TrackCollection from '@/components/library/TrackCollection.vue'
import { useFormat } from '@/composables/useFormat'
import type { PagedListStatus } from '@/composables/usePagedList'
import { parseSort } from '@/library/sorting'
import { usePageTitleStore } from '@/stores/pageTitle'

/** The songs of a year, grouped by album and sortable. */
const props = defineProps<{
    year: string
}>()

const { t } = useI18n()
const route = useRoute()
const format = useFormat()
const pageTitle = usePageTitleStore()
const status = ref<PagedListStatus>('idle')

const value = computed(() => Number(props.year))
const title = computed(() => format.year(value.value))
const filter = computed(() => ({ year: value.value }))
const sortState = computed(() => parseSort('tracks', route.query, false))

watch(title, (text) => pageTitle.set(text), { immediate: true })
onBeforeUnmount(() => pageTitle.clear())
</script>

<template>
    <div
        data-part="view"
        data-view="library-year"
        :aria-busy="status === 'loading' ? 'true' : undefined"
    >
        <DetailHeader :title="title">
            <template #media>
                <BaseCover :placeholder="false">
                    <BaseIcon name="year" />
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
