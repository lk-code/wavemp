<script lang="ts">
import type { RouteLocationRaw } from 'vue-router'
import type { IconSlotName } from '@/themes/iconSlots'

export interface TreeNode {
    readonly id: string
    readonly label: string
    readonly icon?: IconSlotName
    /** Target of the node; nodes without a target or disabled nodes cannot be activated. */
    readonly to?: RouteLocationRaw
    readonly disabled?: boolean
    readonly children?: readonly TreeNode[]
}
</script>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import BaseIcon from './BaseIcon.vue'

const props = defineProps<{
    node: TreeNode
    level: number
    expanded: ReadonlySet<string>
    active?: string
    tabStop?: string
    elementId: (nodeId: string) => string
}>()

const emit = defineEmits<{
    toggle: [nodeId: string]
    focusItem: [nodeId: string]
}>()

const router = useRouter()

const hasChildren = computed(() => (props.node.children?.length ?? 0) > 0)
const isExpanded = computed(() => hasChildren.value && props.expanded.has(props.node.id))
const isActive = computed(() => props.node.id === props.active)
const href = computed(() =>
    props.node.to && !props.node.disabled ? router.resolve(props.node.to).href : undefined,
)
const groupId = computed(() => `${props.elementId(props.node.id)}-group`)

const state = computed(
    () =>
        [
            isActive.value ? 'active' : undefined,
            hasChildren.value ? (isExpanded.value ? 'expanded' : 'collapsed') : undefined,
        ]
            .filter(Boolean)
            .join(' ') || undefined,
)

function activate(event: MouseEvent): void {
    if (!props.node.to || props.node.disabled) {
        event.preventDefault()
        return
    }

    // Let the browser handle new tabs and windows.
    if (event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
        return
    }

    event.preventDefault()
    void router.push(props.node.to)
}

function onToggleClick(event: MouseEvent): void {
    event.preventDefault()
    event.stopPropagation()
    emit('toggle', props.node.id)
}
</script>

<template>
    <li data-part="tree-node" role="none">
        <component
            :is="href ? 'a' : 'span'"
            :id="elementId(node.id)"
            data-part="tree-item"
            role="treeitem"
            :href="href"
            :tabindex="node.id === tabStop ? 0 : -1"
            :aria-level="level"
            :aria-expanded="hasChildren ? (isExpanded ? 'true' : 'false') : undefined"
            :aria-owns="hasChildren ? groupId : undefined"
            :aria-current="isActive ? 'page' : undefined"
            :aria-disabled="node.disabled ? 'true' : undefined"
            :data-state="state"
            :style="{ '--wmp-tree-level': level }"
            @click="activate"
            @focus="emit('focusItem', node.id)"
        >
            <span
                v-if="hasChildren"
                data-part="tree-toggle"
                aria-hidden="true"
                @click="onToggleClick"
            >
                <BaseIcon :name="isExpanded ? 'tree-collapse' : 'tree-expand'" />
            </span>
            <BaseIcon v-if="node.icon" :name="node.icon" />
            <span data-part="label">{{ node.label }}</span>
        </component>
        <ul
            v-if="hasChildren"
            :id="groupId"
            data-part="tree-group"
            role="group"
            :hidden="!isExpanded"
        >
            <BaseTreeItem
                v-for="child in node.children"
                :key="child.id"
                :node="child"
                :level="level + 1"
                :expanded="expanded"
                :active="active"
                :tab-stop="tabStop"
                :element-id="elementId"
                @toggle="(id: string) => emit('toggle', id)"
                @focus-item="(id: string) => emit('focusItem', id)"
            />
        </ul>
    </li>
</template>
