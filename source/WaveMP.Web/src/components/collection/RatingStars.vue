<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseIcon from '@/components/base/BaseIcon.vue'
import { nextRating, ratingStars, starState } from '@/collection/presentation'
import { useCollectionStore } from '@/stores/collection'

/**
 * Rating of a track with five stars. A click on a star rates the track with it, a click on the
 * star of the current rating removes the rating. It shows the state of the API (rating) unless
 * the profile changed it since. In lists the stars are no tab stops (focusable false), since the
 * rows are.
 */
const props = withDefaults(
    defineProps<{
        trackId: string
        /** State of the API: 0 (no rating) to 5. */
        rating?: number | null
        focusable?: boolean
    }>(),
    { rating: 0, focusable: true },
)

const { t } = useI18n()
const collection = useCollectionStore()
const value = computed(() => collection.ratingOf(props.trackId, props.rating))
const description = computed(() =>
    value.value > 0 ? t('collection.rated', { count: value.value }) : t('collection.unrated'),
)

function rate(star: number): void {
    void collection.rate(props.trackId, nextRating(value.value, star))
}
</script>

<template>
    <span
        data-part="rating"
        role="group"
        :aria-label="t('collection.rating')"
        :aria-description="description"
        :data-state="value > 0 ? 'rated' : 'unrated'"
        :data-value="value"
        @click.stop
        @dblclick.stop
        @pointerdown.stop
    >
        <button
            v-for="star in ratingStars"
            :key="star"
            type="button"
            data-part="rating-star"
            :data-state="starState(value, star)"
            :aria-pressed="star === value ? 'true' : 'false'"
            :title="t('collection.stars', { count: star })"
            :tabindex="focusable ? undefined : -1"
            @click="rate(star)"
        >
            <BaseIcon :name="starState(value, star) === 'on' ? 'rating-star' : 'rating-star-off'" />
            <span data-part="label">{{ t('collection.stars', { count: star }) }}</span>
        </button>
    </span>
</template>
