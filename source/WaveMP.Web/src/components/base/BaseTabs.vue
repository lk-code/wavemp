<script setup lang="ts">
import { RouterLink, type RouteLocationRaw } from 'vue-router'
import BaseIcon from './BaseIcon.vue'
import type { IconSlotName } from '@/themes/iconSlots'
import type { PartName } from '@/themes/parts'

export interface TabItem {
    readonly id: string
    readonly label: string
    readonly icon: IconSlotName
    readonly to: RouteLocationRaw
}

withDefaults(
    defineProps<{
        items: readonly TabItem[]
        /** Accessible name of the navigation. */
        label: string
        /** Id of the active item. */
        active?: string
        part?: Extract<PartName, 'tabs' | 'bottom-nav'>
    }>(),
    { active: undefined, part: 'tabs' },
)
</script>

<template>
    <nav :data-part="part" :aria-label="label">
        <RouterLink
            v-for="item in items"
            :key="item.id"
            v-slot="{ href, navigate }"
            :to="item.to"
            custom
        >
            <a
                :data-part="part === 'tabs' ? 'tab' : 'bottom-nav-item'"
                :href="href"
                :aria-current="item.id === active ? 'page' : undefined"
                :data-state="item.id === active ? 'active' : undefined"
                @click="navigate"
            >
                <BaseIcon :name="item.icon" />
                <span data-part="label">{{ item.label }}</span>
                <span v-if="part === 'tabs'" data-part="indicator">
                    <BaseIcon name="tab-indicator" />
                </span>
            </a>
        </RouterLink>
    </nav>
</template>
