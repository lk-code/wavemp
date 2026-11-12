<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { FavoriteType } from '@/api/generated/models'
import BaseButton from '@/components/base/BaseButton.vue'
import { useCollectionStore } from '@/stores/collection'

/**
 * Heart that marks a track, album or artist as favorite of the profile (toggle button). It shows
 * the state of the API (favorite) unless the profile changed it since. In lists it is no tab stop
 * (focusable false), since the rows are; the menu of a song offers the same.
 */
const props = withDefaults(
    defineProps<{
        type: FavoriteType
        id: string
        /** State of the API, e.g. isFavorite of a track. */
        favorite?: boolean | null
        focusable?: boolean
    }>(),
    { favorite: false, focusable: true },
)

const { t } = useI18n()
const collection = useCollectionStore()
const on = computed(() => collection.isFavorite(props.type, props.id, props.favorite))

function toggle(): void {
    void collection.toggleFavorite(props.type, props.id, props.favorite)
}
</script>

<template>
    <BaseButton
        part="btn-favorite"
        variant="icon"
        :icon="on ? 'favorite' : 'favorite-off'"
        :label="t('collection.favorite')"
        :pressed="on"
        :state="on ? 'on' : 'off'"
        :tabindex="focusable ? undefined : -1"
        @click.stop="toggle"
        @dblclick.stop
        @pointerdown.stop
    />
</template>
