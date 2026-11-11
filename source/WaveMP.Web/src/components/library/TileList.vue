<script lang="ts">
import type { RouteLocationRaw } from 'vue-router'

export type TileKind = 'album' | 'artist' | 'genre' | 'year'

export interface Tile {
    readonly id: string
    readonly kind: TileKind
    readonly title: string
    readonly subtitle?: string
    readonly to: RouteLocationRaw
    /** Cover of an album; tiles without cover show the icon of their kind. */
    readonly cover?: { readonly id: string; readonly available: boolean }
}
</script>

<script setup lang="ts">
import BaseCover from '@/components/base/BaseCover.vue'
import BaseIcon from '@/components/base/BaseIcon.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import LibraryCover from './LibraryCover.vue'

/** Tiles of albums, artists, genres or years that lead to their pages. */
defineProps<{
    tiles: readonly Tile[]
    /** Accessible name of the list. */
    label: string
}>()
</script>

<template>
    <ul data-part="tile-list" :aria-label="label">
        <li v-for="tile in tiles" :key="tile.id" data-part="tile-item">
            <BaseLink part="tile" :to="tile.to" :data-kind="tile.kind">
                <LibraryCover
                    v-if="tile.cover"
                    kind="album"
                    :id="tile.cover.id"
                    :available="tile.cover.available"
                />
                <BaseCover v-else :placeholder="false">
                    <BaseIcon :name="tile.kind" />
                </BaseCover>
                <span data-part="tile-title">{{ tile.title }}</span>
                <span v-if="tile.subtitle" data-part="tile-subtitle">{{ tile.subtitle }}</span>
            </BaseLink>
        </li>
    </ul>
</template>
