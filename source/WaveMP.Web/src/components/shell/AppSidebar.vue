<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRoute } from 'vue-router'
import BaseTree from '@/components/base/BaseTree.vue'
import type { TreeNode } from '@/components/base/BaseTreeItem.vue'
import { activeTreeId } from '@/navigation/breadcrumb'
import { initiallyExpanded, libraryTree, type NavigationTreeNode } from '@/navigation/libraryTree'

withDefaults(
    defineProps<{
        /** Drawer state on mobile; on the desktop the sidebar is always shown. */
        open?: boolean
    }>(),
    { open: false },
)

const emit = defineEmits<{
    close: []
}>()

const { t } = useI18n()
const route = useRoute()

const expanded = ref<string[]>([...initiallyExpanded])

function toTreeNode(node: NavigationTreeNode): TreeNode {
    return {
        id: node.id,
        label: t(node.labelKey),
        icon: node.icon,
        to: node.to,
        disabled: node.disabled,
        children: node.children?.map(toTreeNode),
    }
}

const nodes = computed(() => libraryTree.map(toTreeNode))
const active = computed(() => activeTreeId(route))
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
