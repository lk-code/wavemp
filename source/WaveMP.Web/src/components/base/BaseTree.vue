<script setup lang="ts">
import { computed, nextTick, ref, useId, watch } from 'vue'
import BaseTreeItem, { type TreeNode } from './BaseTreeItem.vue'
import { nextFocus, visibleItems } from './tree'

const props = withDefaults(
    defineProps<{
        nodes: readonly TreeNode[]
        /** Accessible name of the tree. */
        label: string
        /** Id of the node of the current page (aria-current). */
        active?: string
    }>(),
    { active: undefined },
)

/** Ids of the expanded nodes. */
const expanded = defineModel<string[]>('expanded', { default: () => [] })

const treeId = useId()
const expandedSet = computed(() => new Set(expanded.value))
const focusedId = ref<string | undefined>(undefined)

// Roving tabindex: exactly one item is in the tab order, by default the active one.
const tabStop = computed(() => {
    const visible = visibleItems(props.nodes, expandedSet.value)
    const candidates = [focusedId.value, props.active]
    return candidates.find((id) => id && visible.some((item) => item.id === id)) ?? visible[0]?.id
})

watch(
    () => props.active,
    () => {
        focusedId.value = undefined
    },
)

function elementId(nodeId: string): string {
    return `${treeId}-${nodeId}`
}

function setExpanded(nodeId: string, value: boolean): void {
    const others = expanded.value.filter((id) => id !== nodeId)
    expanded.value = value ? [...others, nodeId] : others
}

function toggle(nodeId: string): void {
    setExpanded(nodeId, !expandedSet.value.has(nodeId))
}

async function focusItem(nodeId: string): Promise<void> {
    focusedId.value = nodeId
    await nextTick()
    document.getElementById(elementId(nodeId))?.focus()
}

function onKeydown(event: KeyboardEvent): void {
    const current = focusedId.value ?? tabStop.value
    if (!current) {
        return
    }

    const action = nextFocus(event.key, current, props.nodes, expandedSet.value)
    if (!action) {
        return
    }

    event.preventDefault()
    if (action.type === 'focus') {
        void focusItem(action.id)
    } else {
        setExpanded(action.id, action.type === 'expand')
    }
}

defineExpose({ focusItem, tabStop })
</script>

<template>
    <ul data-part="tree" role="tree" :aria-label="label" @keydown="onKeydown">
        <BaseTreeItem
            v-for="node in nodes"
            :key="node.id"
            :node="node"
            :level="1"
            :expanded="expandedSet"
            :active="active"
            :tab-stop="tabStop"
            :element-id="elementId"
            @toggle="toggle"
            @focus-item="(id: string) => (focusedId = id)"
        />
    </ul>
</template>
