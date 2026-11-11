<script setup lang="ts" generic="T">
import { computed, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import LibraryEmpty from './LibraryEmpty.vue'
import LoadMore from './LoadMore.vue'
import TileList, { type Tile } from './TileList.vue'
import type { PageLike } from '@/api/paging'
import { usePagedList, type PagedListStatus } from '@/composables/usePagedList'

/**
 * Albums, artists, genres or years as tiles, loaded page by page. The list loads again whenever
 * the reload key changes (e.g. the search text or the sort order).
 */
const props = withDefaults(
    defineProps<{
        /** Accessible name of the list. */
        label: string
        fetchPage: (offset: number, limit: number) => Promise<PageLike<T> | undefined>
        toTile: (item: T) => Tile
        /** Any value that changes when the list must load again. */
        reloadKey?: string
        search?: string
        filtered?: boolean
    }>(),
    { reloadKey: '', search: '', filtered: false },
)

const emit = defineEmits<{
    status: [status: PagedListStatus]
}>()

const { t } = useI18n()
const list = usePagedList<T>((offset, limit) => props.fetchPage(offset, limit))
const tiles = computed(() => list.items.value.map((item) => props.toTile(item)))

watch(
    () => props.reloadKey,
    () => void list.reload(),
    { immediate: true },
)

watch(list.status, (status) => emit('status', status), { immediate: true })
</script>

<template>
    <div v-if="list.status.value === 'error'" data-part="empty-state">
        <p data-part="empty-state-title">{{ t('library.loadError') }}</p>
        <BaseButton :label="t('library.retry')" @click="list.reload()" />
    </div>
    <LibraryEmpty
        v-else-if="list.status.value === 'ready' && list.items.value.length === 0"
        :search="search"
        :filtered="filtered"
    />
    <template v-else-if="tiles.length > 0">
        <TileList :tiles="tiles" :label="label" />
        <LoadMore
            v-if="list.hasMore.value"
            :loading="list.loadingMore.value"
            @load="list.loadMore()"
        />
    </template>
</template>
