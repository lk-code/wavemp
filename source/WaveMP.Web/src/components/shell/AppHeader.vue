<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseRoundButton from '@/components/base/BaseRoundButton.vue'
import BaseTabs from '@/components/base/BaseTabs.vue'
import ServerStatus from './ServerStatus.vue'
import { historyAvailability } from '@/navigation/history'
import { mainNavigation, type MainTab } from '@/navigation/libraryTree'

withDefaults(
    defineProps<{
        /** State of the sidebar drawer on mobile (btn-menu). */
        sidebarOpen?: boolean
        /** Active tab; by default the tab of the current route. */
        activeTab?: MainTab
    }>(),
    { sidebarOpen: false, activeTab: undefined },
)

const emit = defineEmits<{
    toggleSidebar: []
}>()

const { t } = useI18n()
const route = useRoute()
const router = useRouter()

const tabs = computed(() =>
    mainNavigation.map((item) => ({
        id: item.id,
        label: t(item.labelKey),
        icon: item.icon,
        to: item.to,
    })),
)

const history = ref(historyAvailability(window.history.state))

function updateHistory(): void {
    history.value = historyAvailability(window.history.state)
}

let removeAfterEach: (() => void) | undefined
onMounted(() => {
    removeAfterEach = router.afterEach(updateHistory)
    window.addEventListener('popstate', updateHistory)
    updateHistory()
})
onUnmounted(() => {
    removeAfterEach?.()
    window.removeEventListener('popstate', updateHistory)
})
</script>

<template>
    <header data-part="header">
        <BaseButton
            part="btn-menu"
            variant="icon"
            icon="menu"
            :label="t('nav.menu')"
            :aria-expanded="sidebarOpen ? 'true' : 'false'"
            aria-controls="wavemp-sidebar"
            @click="emit('toggleSidebar')"
        />
        <span data-part="logo">
            <span data-part="logo-image" aria-hidden="true"></span>
            <span data-part="label">{{ t('app.name') }}</span>
        </span>
        <div data-part="nav-history" role="group" :aria-label="t('nav.history')">
            <BaseRoundButton
                part="btn-back"
                icon="back"
                :label="t('nav.back')"
                :disabled="!history.back"
                @click="router.back()"
            />
            <BaseRoundButton
                part="btn-forward"
                icon="forward"
                :label="t('nav.forward')"
                :disabled="!history.forward"
                @click="router.forward()"
            />
        </div>
        <BaseTabs :items="tabs" :label="t('nav.main')" :active="activeTab ?? route.meta.tab" />
        <ServerStatus />
    </header>
</template>
