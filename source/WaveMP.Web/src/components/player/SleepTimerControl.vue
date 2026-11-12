<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseContextMenu, { type MenuItem } from '@/components/base/BaseContextMenu.vue'
import {
    sleepTimerStateTokens,
    type SleepTimerChoice,
    type TransportSleepTimer,
} from '@/components/shell/transport'
import { useFormat } from '@/composables/useFormat'
import { sleepTimerDurations } from '@/player/sleepTimer'

/**
 * Button of the sleep timer with its menu: off, a duration or the end of the track or the queue.
 * Shows the remaining time while a timer runs. Controlled through props and events only.
 */
const props = defineProps<{
    timer?: TransportSleepTimer
}>()

const emit = defineEmits<{
    choose: [choice: SleepTimerChoice]
}>()

const { t } = useI18n()
const format = useFormat()

const menu = ref<InstanceType<typeof BaseContextMenu> | null>(null)
const open = computed(() => menu.value?.isOpen ?? false)
/** Minutes of the last chosen duration, to mark it in the menu. */
const chosenMinutes = ref<number | null>(null)

const mode = computed(() => props.timer?.mode ?? 'off')
const active = computed(() => mode.value !== 'off')
const tokens = computed(() => sleepTimerStateTokens(props.timer))

const items = computed<MenuItem[]>(() => [
    { id: 'sleep-off', label: t('player.sleepTimerOff'), checked: mode.value === 'off' },
    ...sleepTimerDurations.map((minutes) => ({
        id: `sleep-${minutes}`,
        label: t('player.sleepTimerMinutes', { minutes: format.number(minutes) }),
        checked: mode.value === 'duration' && chosenMinutes.value === minutes,
    })),
    { id: 'sleep-separator', separator: true },
    {
        id: 'sleep-end-of-track',
        label: t('player.sleepTimerEndOfTrack'),
        checked: mode.value === 'endOfTrack',
    },
    {
        id: 'sleep-end-of-queue',
        label: t('player.sleepTimerEndOfQueue'),
        checked: mode.value === 'endOfQueue',
    },
])

const remainingText = computed(() => {
    switch (mode.value) {
        case 'duration':
            return format.duration(props.timer?.remaining ?? 0)
        case 'endOfTrack':
            return t('player.sleepTimerEndOfTrackShort')
        case 'endOfQueue':
            return t('player.sleepTimerEndOfQueueShort')
        default:
            return ''
    }
})

const label = computed(() =>
    mode.value === 'duration'
        ? `${t('player.sleepTimer')}: ${t('player.sleepTimerRemaining', { time: remainingText.value })}`
        : active.value
          ? `${t('player.sleepTimer')}: ${remainingText.value}`
          : t('player.sleepTimer'),
)

function toggle(event: MouseEvent): void {
    if (open.value) {
        menu.value?.close()
        return
    }

    const rect = (event.currentTarget as HTMLElement | null)?.getBoundingClientRect()
    void menu.value?.openAt(rect?.left ?? 0, rect?.top ?? 0)
}

function choose(id: string): void {
    if (id === 'sleep-off') {
        chosenMinutes.value = null
        emit('choose', { mode: 'off' })
    } else if (id === 'sleep-end-of-track') {
        emit('choose', { mode: 'endOfTrack' })
    } else if (id === 'sleep-end-of-queue') {
        emit('choose', { mode: 'endOfQueue' })
    } else {
        const minutes = Number(id.slice('sleep-'.length))
        if (sleepTimerDurations.includes(minutes)) {
            chosenMinutes.value = minutes
            emit('choose', { mode: 'duration', minutes })
        }
    }
}
</script>

<template>
    <div data-part="sleep-timer" :data-state="tokens">
        <BaseButton
            part="btn-sleep-timer"
            variant="icon"
            icon="sleep-timer"
            :label="label"
            :pressed="active"
            :state="active ? 'on' : 'off'"
            aria-haspopup="menu"
            :aria-expanded="open ? 'true' : 'false'"
            @click="toggle"
        />
        <span v-if="active" data-part="sleep-timer-remaining" aria-hidden="true">{{
            remainingText
        }}</span>
        <BaseContextMenu
            ref="menu"
            :items="items"
            :label="t('player.sleepTimerMenu')"
            @select="choose"
        />
    </div>
</template>
