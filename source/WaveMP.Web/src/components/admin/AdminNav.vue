<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { RouterLink, useRoute } from 'vue-router'
import BaseIcon from '@/components/base/BaseIcon.vue'
import type { IconSlotName } from '@/themes/iconSlots'

interface AdminNavItem {
    readonly id: string
    readonly labelKey: string
    readonly icon: IconSlotName
}

/** Navigation between the pages of the administration. */
const { t } = useI18n()
const route = useRoute()

const items: readonly AdminNavItem[] = [
    { id: 'admin-library', labelKey: 'admin.nav.library', icon: 'admin-library' },
    { id: 'admin-jobs', labelKey: 'admin.nav.jobs', icon: 'admin-jobs' },
    { id: 'admin-cache', labelKey: 'admin.nav.cache', icon: 'admin-cache' },
    { id: 'admin-users', labelKey: 'admin.nav.profiles', icon: 'manage-profiles' },
]

const active = computed(() => route.name)
</script>

<template>
    <nav data-part="admin-nav" :aria-label="t('admin.nav.label')">
        <RouterLink
            v-for="item in items"
            :key="item.id"
            v-slot="{ href, navigate }"
            :to="{ name: item.id }"
            custom
        >
            <a
                data-part="admin-nav-item"
                :data-item="item.id"
                :href="href"
                :aria-current="item.id === active ? 'page' : undefined"
                :data-state="item.id === active ? 'active' : undefined"
                @click="navigate"
            >
                <BaseIcon :name="item.icon" />
                <span data-part="label">{{ t(item.labelKey) }}</span>
            </a>
        </RouterLink>
    </nav>
</template>
