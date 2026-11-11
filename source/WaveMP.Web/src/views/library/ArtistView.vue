<script setup lang="ts">
import { computed, onBeforeUnmount, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Album, Artist } from '@/api/generated/models'
import { statusOf } from '@/api/problems'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseCover from '@/components/base/BaseCover.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import DetailHeader from '@/components/library/DetailHeader.vue'
import MetaItem from '@/components/library/MetaItem.vue'
import TileCollection from '@/components/library/TileCollection.vue'
import TrackCollection from '@/components/library/TrackCollection.vue'
import { useFormat } from '@/composables/useFormat'
import { useLibraryTiles } from '@/composables/useLibraryTiles'
import type { PagedListStatus } from '@/composables/usePagedList'
import { getArtist, listArtistAlbums } from '@/library/api'
import { usePageTitleStore } from '@/stores/pageTitle'

/** An artist with its albums (also those it only contributes to) and its songs. */
const props = defineProps<{
    id: string
}>()

const { t } = useI18n()
const format = useFormat()
const tiles = useLibraryTiles()
const pageTitle = usePageTitleStore()
const albumsTitleId = useId()
const songsTitleId = useId()

const artist = ref<Artist | null>(null)
const status = ref<'loading' | 'ready' | 'notFound' | 'error'>('loading')
const albumsStatus = ref<PagedListStatus>('idle')
const songsStatus = ref<PagedListStatus>('idle')
let generation = 0

const busy = computed(
    () =>
        status.value === 'loading' ||
        albumsStatus.value === 'loading' ||
        songsStatus.value === 'loading',
)
const filter = computed(() => ({ artistId: props.id }))

async function load(): Promise<void> {
    const current = ++generation
    status.value = 'loading'
    pageTitle.clear()
    try {
        const loaded = await getArtist(props.id)
        if (current !== generation) {
            return
        }

        artist.value = loaded ?? null
        status.value = loaded ? 'ready' : 'notFound'
        pageTitle.set(loaded?.name ?? null)
    } catch (error) {
        if (current === generation) {
            artist.value = null
            status.value = statusOf(error) === 404 ? 'notFound' : 'error'
        }
    }
}

function fetchAlbums(offset: number, limit: number) {
    return listArtistAlbums(props.id, { offset, limit, sort: 'Year' })
}

watch(() => props.id, load, { immediate: true })
onBeforeUnmount(() => pageTitle.clear())
</script>

<template>
    <div data-part="view" data-view="library-artist" :aria-busy="busy ? 'true' : undefined">
        <div v-if="status === 'notFound'" data-part="empty-state">
            <p data-part="empty-state-text">{{ t('library.artistNotFound') }}</p>
            <BaseLink :to="{ name: 'library-section', params: { section: 'artists' } }">
                {{ t('library.toLibrary') }}
            </BaseLink>
        </div>
        <div v-else-if="status === 'error'" data-part="empty-state">
            <p data-part="empty-state-title">{{ t('library.loadError') }}</p>
            <BaseButton :label="t('library.retry')" @click="load" />
        </div>
        <template v-else-if="artist">
            <DetailHeader :title="artist.name ?? ''">
                <template #media>
                    <BaseCover :placeholder="false">
                        <BaseIcon name="artist" />
                    </BaseCover>
                </template>
                <MetaItem field="albums" :label="t('list.albums')">
                    {{ format.number(artist.albumCount ?? 0) }}
                </MetaItem>
                <MetaItem field="tracks" :label="t('list.songs')">
                    {{ format.number(artist.trackCount ?? 0) }}
                </MetaItem>
            </DetailHeader>
            <section
                v-if="(artist.albumCount ?? 0) > 0"
                data-part="section"
                data-item="albums"
                :aria-labelledby="albumsTitleId"
            >
                <h2 :id="albumsTitleId" data-part="section-title">
                    {{ t('library.albumsTitle') }}
                </h2>
                <TileCollection
                    :label="t('library.albumsTitle')"
                    :fetch-page="fetchAlbums"
                    :to-tile="(item: Album) => tiles.album(item)"
                    :reload-key="id"
                    filtered
                    @status="albumsStatus = $event"
                />
            </section>
            <section data-part="section" data-item="tracks" :aria-labelledby="songsTitleId">
                <h2 :id="songsTitleId" data-part="section-title">{{ t('library.songsTitle') }}</h2>
                <TrackCollection
                    :label="t('library.songsTitle')"
                    :filter="filter"
                    @status="songsStatus = $event"
                />
            </section>
        </template>
    </div>
</template>
