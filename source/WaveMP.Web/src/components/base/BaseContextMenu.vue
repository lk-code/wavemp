<script setup lang="ts">
import { nextTick, ref } from 'vue'
import BaseIcon from './BaseIcon.vue'
import { clampMenuPosition, firstEnabledIndex, lastEnabledIndex, nextEnabledIndex } from './menu'
import type { IconSlotName } from '@/themes/iconSlots'

export interface MenuItem {
    readonly id: string
    readonly label?: string
    readonly icon?: IconSlotName
    readonly disabled?: boolean
    readonly separator?: boolean
    /** Entry of a choice (role menuitemradio): whether it is the chosen one. */
    readonly checked?: boolean
}

/** Context menu on the Popover API: light dismiss and Escape come from the browser. */
const props = defineProps<{
    items: readonly MenuItem[]
    /** Accessible name of the menu. */
    label: string
}>()

const emit = defineEmits<{
    select: [id: string]
}>()

const menu = ref<HTMLElement | null>(null)
const isOpen = ref(false)
const x = ref(0)
const y = ref(0)
let returnFocus: HTMLElement | null = null

function itemElements(): HTMLElement[] {
    return [...(menu.value?.querySelectorAll<HTMLElement>('[data-part="menu-item"]') ?? [])]
}

function focusIndex(index: number): void {
    const entries = props.items.filter((item) => !item.separator)
    const id = entries[index]?.id
    itemElements()
        .find((element) => element.dataset.item === id)
        ?.focus()
}

/** Opens the menu at a point in viewport coordinates (e.g. of a contextmenu event). */
async function openAt(pointX: number, pointY: number): Promise<void> {
    const element = menu.value
    if (!element) {
        return
    }

    returnFocus = document.activeElement instanceof HTMLElement ? document.activeElement : null
    x.value = pointX
    y.value = pointY
    element.showPopover?.()
    isOpen.value = true
    await nextTick()

    const rect = element.getBoundingClientRect()
    const position = clampMenuPosition(
        { x: pointX, y: pointY },
        { width: rect.width, height: rect.height },
        { width: window.innerWidth, height: window.innerHeight },
    )
    x.value = position.x
    y.value = position.y
    focusIndex(firstEnabledIndex(props.items.filter((item) => !item.separator)))
}

function close(): void {
    menu.value?.hidePopover?.()
    isOpen.value = false
}

function onToggle(event: Event): void {
    const open = (event as ToggleEvent).newState === 'open'
    isOpen.value = open
    if (!open) {
        returnFocus?.focus()
        returnFocus = null
    }
}

function choose(item: MenuItem): void {
    if (item.disabled || item.separator) {
        return
    }

    close()
    emit('select', item.id)
}

function onKeydown(event: KeyboardEvent): void {
    const entries = props.items.filter((item) => !item.separator)
    const current = entries.findIndex(
        (item) => item.id === (event.target as HTMLElement).dataset.item,
    )
    let next: number | null = null
    switch (event.key) {
        case 'ArrowDown':
            next = nextEnabledIndex(current, 1, entries)
            break
        case 'ArrowUp':
            next = nextEnabledIndex(current < 0 ? entries.length : current, -1, entries)
            break
        case 'Home':
            next = firstEnabledIndex(entries)
            break
        case 'End':
            next = lastEnabledIndex(entries)
            break
        case 'Escape':
        case 'Tab':
            event.preventDefault()
            close()
            return
    }

    if (next !== null) {
        event.preventDefault()
        focusIndex(next)
    }
}

defineExpose({ openAt, close, isOpen })
</script>

<template>
    <div
        ref="menu"
        data-part="context-menu"
        role="menu"
        popover="auto"
        :aria-label="label"
        :data-state="isOpen ? 'open' : 'closed'"
        :style="{ '--wmp-menu-x': `${x}px`, '--wmp-menu-y': `${y}px` }"
        @toggle="onToggle"
        @keydown="onKeydown"
    >
        <template v-for="item in items" :key="item.id">
            <hr v-if="item.separator" data-part="menu-separator" />
            <button
                v-else
                type="button"
                data-part="menu-item"
                :role="item.checked === undefined ? 'menuitem' : 'menuitemradio'"
                :aria-checked="
                    item.checked === undefined ? undefined : item.checked ? 'true' : 'false'
                "
                tabindex="-1"
                :data-item="item.id"
                :disabled="item.disabled"
                @click="choose(item)"
            >
                <BaseIcon v-if="item.icon" :name="item.icon" />
                <span data-part="label">{{ item.label }}</span>
            </button>
        </template>
    </div>
</template>
