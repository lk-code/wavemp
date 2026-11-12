<script setup lang="ts">
import { useId } from 'vue'
import { useI18n } from 'vue-i18n'
import type { RouteLocationRaw } from 'vue-router'
import BaseLink from '@/components/base/BaseLink.vue'
import TileList, { type Tile } from '@/components/library/TileList.vue'

/**
 * A row of the home page: a title, actions (slot actions, e.g. play), a link to the whole list and
 * the first tiles of the list, which the base shows side by side in one scrollable row.
 */
defineProps<{
    /** Id of the row (data-item), e.g. "recently-played". */
    item: string
    title: string
    tiles: readonly Tile[]
    /** The page with the whole list. */
    to?: RouteLocationRaw
}>()

const { t } = useI18n()
const titleId = useId()
</script>

<template>
    <section data-part="shelf" :data-item="item" :aria-labelledby="titleId">
        <div data-part="shelf-header">
            <h2 :id="titleId" data-part="shelf-title">{{ title }}</h2>
            <div v-if="$slots.actions || to" data-part="shelf-actions">
                <slot name="actions" />
                <BaseLink v-if="to" :to="to">{{ t('home.showAll') }}</BaseLink>
            </div>
        </div>
        <TileList :tiles="tiles" :label="title" />
    </section>
</template>
