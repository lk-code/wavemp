<script setup lang="ts">
import { useId } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import { useFormat } from '@/composables/useFormat'
import { artistLabelKey, durationSeconds } from '@/library/presentation'
import type { Track } from '@/api/generated/models'
import type { QueueItem } from '@/player/queue'

/**
 * The queue: every entry plays when it is chosen, and its buttons move it up and down or remove
 * it. The current entry cannot be removed, and clearing keeps it.
 */
const props = defineProps<{
    items: readonly QueueItem[]
    currentIndex: number
}>()

const emit = defineEmits<{
    play: [key: string]
    move: [key: string, offset: -1 | 1]
    remove: [key: string]
    clear: []
}>()

const { t } = useI18n()
const format = useFormat()
const titleId = useId()

function artistName(track: Track): string {
    const key = artistLabelKey(track.artist)
    return key ? t(key) : (track.artist?.name ?? '')
}

function itemState(index: number): string | undefined {
    if (index === props.currentIndex) {
        return 'current'
    }

    return index < props.currentIndex ? 'played' : undefined
}

function duration(track: Track): string {
    const seconds = durationSeconds(track.durationMilliseconds)
    return seconds === null ? '' : format.duration(seconds)
}
</script>

<template>
    <section data-part="queue" :aria-labelledby="titleId">
        <h2 :id="titleId" data-part="section-title">{{ t('nowPlaying.queue') }}</h2>
        <BaseButton
            data-item="clear-queue"
            icon="clear-queue"
            :label="t('nowPlaying.clear')"
            :disabled="items.length <= 1"
            @click="emit('clear')"
        />
        <ol data-part="queue-list">
            <li
                v-for="(item, index) in items"
                :key="item.key"
                data-part="queue-item"
                :data-state="itemState(index)"
                :aria-current="index === currentIndex ? 'true' : undefined"
            >
                <button
                    type="button"
                    data-part="queue-item-info"
                    :aria-label="t('nowPlaying.playItem', { title: item.track.title ?? '' })"
                    @click="emit('play', item.key)"
                >
                    <span data-part="queue-item-title">{{ item.track.title }}</span>
                    <span data-part="queue-item-artist">{{ artistName(item.track) }}</span>
                </button>
                <span data-part="queue-item-duration">{{ duration(item.track) }}</span>
                <div data-part="queue-item-actions" role="group">
                    <BaseButton
                        variant="icon"
                        icon="move-up"
                        data-item="move-up"
                        :label="t('nowPlaying.moveUp')"
                        :disabled="index === 0"
                        @click="emit('move', item.key, -1)"
                    />
                    <BaseButton
                        variant="icon"
                        icon="move-down"
                        data-item="move-down"
                        :label="t('nowPlaying.moveDown')"
                        :disabled="index === items.length - 1"
                        @click="emit('move', item.key, 1)"
                    />
                    <BaseButton
                        variant="icon"
                        icon="remove-from-queue"
                        data-item="remove"
                        :label="t('nowPlaying.remove')"
                        :disabled="index === currentIndex"
                        @click="emit('remove', item.key)"
                    />
                </div>
            </li>
        </ol>
    </section>
</template>
