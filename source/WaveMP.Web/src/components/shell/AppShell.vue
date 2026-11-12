<script setup lang="ts">
import { computed, nextTick, onMounted, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute, useRouter } from 'vue-router'
import AppBottomNav from './AppBottomNav.vue'
import AppHeader from './AppHeader.vue'
import AppSidebar from './AppSidebar.vue'
import AppToolbar from './AppToolbar.vue'
import AppTransport from './AppTransport.vue'
import BaseAlert from '@/components/base/BaseAlert.vue'
import { pageTitleKey } from '@/navigation/breadcrumb'
import { useCollectionStore } from '@/stores/collection'
import { useHealthStore } from '@/stores/health'
import { usePageTitleStore } from '@/stores/pageTitle'
import { usePlayerStore } from '@/stores/player'

/**
 * App shell: the fixed areas of the theme API around the content of the current page. The DOM is
 * the same on every device; the mobile base and the themes arrange it with CSS only.
 */
const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const health = useHealthStore()
const pageTitle = usePageTitleStore()
const collection = useCollectionStore()

const player = usePlayerStore()

const sidebarOpen = ref(false)
const playerExpanded = ref(false)
// Detail pages show their name (album, artist, ...), the other pages the name of the page.
const title = computed(() => pageTitle.title ?? t(pageTitleKey(route)))

/** Moves the focus to an element of the shell after the next render. */
async function focus(selector: string): Promise<void> {
    await nextTick()
    document.querySelector<HTMLElement>(`[data-part="app"] ${selector}`)?.focus()
}

function toggleSidebar(): void {
    sidebarOpen.value = !sidebarOpen.value
    if (sidebarOpen.value) {
        void focus('[data-part="sidebar"] [data-part="tree-item"][tabindex="0"]')
    }
}

function closeSidebar(): void {
    if (sidebarOpen.value) {
        sidebarOpen.value = false
        void focus('[data-part="btn-menu"]')
    }
}

function expandPlayer(): void {
    playerExpanded.value = true
    void focus('[data-part="btn-collapse"]')
}

function collapsePlayer(): void {
    if (playerExpanded.value) {
        playerExpanded.value = false
        void focus('[data-part="now-playing-mini"]')
    }
}

function onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape' && !event.defaultPrevented) {
        closeSidebar()
        collapsePlayer()
    }
}

// Navigating closes the drawer and the full-screen player and dismisses the error of a favorite or a
// rating.
watch(
    () => route.fullPath,
    () => {
        sidebarOpen.value = false
        playerExpanded.value = false
        collection.clearError()
    },
)

function checkHealth(): void {
    void health.check()
}

onMounted(() => {
    checkHealth()
    document.addEventListener('keydown', onKeydown)
    window.addEventListener('online', checkHealth)
    window.addEventListener('offline', checkHealth)
})

onUnmounted(() => {
    document.removeEventListener('keydown', onKeydown)
    window.removeEventListener('online', checkHealth)
    window.removeEventListener('offline', checkHealth)
})
</script>

<template>
    <div data-part="app">
        <AppHeader :sidebar-open="sidebarOpen" @toggle-sidebar="toggleSidebar" />
        <AppToolbar />
        <AppSidebar :open="sidebarOpen" @close="closeSidebar" />
        <main id="wavemp-content" data-part="content">
            <h1 data-part="content-title">{{ title }}</h1>
            <BaseAlert
                v-if="collection.error"
                tone="error"
                :message="
                    t(
                        collection.error === 'rating'
                            ? 'collection.errors.rating'
                            : 'collection.errors.favorite',
                    )
                "
            />
            <slot />
        </main>
        <AppTransport
            :state="player.state"
            :expanded="playerExpanded"
            @play="player.play"
            @pause="player.pause"
            @stop="player.stop"
            @previous="player.previous"
            @next="player.next"
            @seek="player.seek"
            @toggle-shuffle="player.toggleShuffle"
            @cycle-repeat="player.cycleRepeat"
            @toggle-mute="player.toggleMute"
            @change-volume="player.setVolume"
            @show-now-playing="router.push({ name: 'now-playing' })"
            @expand="expandPlayer"
            @collapse="collapsePlayer"
        />
        <AppBottomNav />
    </div>
</template>
