<script setup lang="ts">
import { onMounted, watch } from 'vue'
import { RouterView, useRoute, useRouter } from 'vue-router'
import AppShell from '@/components/shell/AppShell.vue'
import { useLocaleSync } from '@/composables/useLocaleSync'
import { useSessionStore } from '@/stores/session'

const route = useRoute()
const router = useRouter()
const session = useSessionStore()

useLocaleSync()

// The session can end while a page is open (rejected token): then the profile selection opens
// and leads back to the page afterwards.
watch(
    () => session.isAuthenticated,
    (authenticated) => {
        if (!authenticated && !route.meta.public) {
            void router.replace({ name: 'profiles', query: { redirect: route.fullPath } })
        }
    },
)

// The remembered profile may have changed on another device (name, role, language).
onMounted(() => {
    if (session.isAuthenticated) {
        void session.refresh()
    }
})
</script>

<template>
    <RouterView v-if="route.meta.bare" />
    <AppShell v-else>
        <RouterView />
    </AppShell>
</template>
