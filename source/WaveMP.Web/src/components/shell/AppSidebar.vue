<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import BaseTree from '@/components/base/BaseTree.vue'
import type { TreeNode } from '@/components/base/BaseTreeItem.vue'
import { activeTreeId, type RouteInfo } from '@/navigation/breadcrumb'
import {
    buildLibraryTree,
    initiallyExpanded,
    type NavigationTreeNode,
} from '@/navigation/libraryTree'
import { usePlaylistsStore } from '@/stores/playlists'
import { useSessionStore } from '@/stores/session'

const props = withDefaults(
    defineProps<{
        /** Drawer state on mobile; on the desktop the sidebar is always shown. */
        open?: boolean
        /** Location that decides the active node; by default the current route. */
        location?: RouteInfo
    }>(),
    { open: false, location: undefined },
)

const emit = defineEmits<{
    close: []
}>()

const { t } = useI18n()
const route = useRoute()
const playlists = usePlaylistsStore()
const session = useSessionStore()

const expanded = ref<string[]>([...initiallyExpanded])

function toTreeNode(node: NavigationTreeNode): TreeNode {
    return {
        id: node.id,
        label: node.label ?? t(node.labelKey),
        icon: node.icon,
        to: node.to,
        disabled: node.disabled,
        children: node.children?.map(toTreeNode),
    }
}

const nodes = computed(() => buildLibraryTree(playlists.playlists).map(toTreeNode))
const active = computed(() => activeTreeId(props.location ?? route))

// The playlists of the profile below "Playlists", loaded again for every session.
watch(
    () => session.token,
    (token) => {
        if (token) {
            void playlists.ensureLoaded()
        }
    },
    // After the store forgot the playlists of the previous session.
    { immediate: true, flush: 'post' },
)
</script>

<template>
    <aside
        id="wavemp-sidebar"
        data-part="sidebar"
        :data-state="open ? 'open' : 'closed'"
        :aria-label="t('sidebar.label')"
    >
        <BaseTree
            v-model:expanded="expanded"
            :nodes="nodes"
            :label="t('sidebar.label')"
            :active="active"
        />
    </aside>
    <div
        data-part="sidebar-backdrop"
        :data-state="open ? 'open' : 'closed'"
        aria-hidden="true"
        @click="emit('close')"
    ></div>
</template>
