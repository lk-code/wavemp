// Structure and keyboard logic of BaseTree (tree view pattern with roving tabindex).

export interface TreeNodeBase {
    readonly id: string
    readonly disabled?: boolean
    readonly children?: readonly TreeNodeBase[]
}

export interface VisibleTreeItem {
    readonly id: string
    readonly level: number
    readonly parentId: string | null
    readonly hasChildren: boolean
}

/** Items in display order; children of collapsed nodes are left out. */
export function visibleItems(
    nodes: readonly TreeNodeBase[],
    expanded: ReadonlySet<string>,
    level = 1,
    parentId: string | null = null,
): VisibleTreeItem[] {
    return nodes.flatMap((node) => {
        const hasChildren = (node.children?.length ?? 0) > 0
        const item: VisibleTreeItem = { id: node.id, level, parentId, hasChildren }
        if (!hasChildren || !expanded.has(node.id)) {
            return [item]
        }

        return [item, ...visibleItems(node.children ?? [], expanded, level + 1, node.id)]
    })
}

export type TreeAction =
    | { readonly type: 'focus'; readonly id: string }
    | { readonly type: 'expand'; readonly id: string }
    | { readonly type: 'collapse'; readonly id: string }

/** What a key does in the tree: move the focus, expand or collapse a node, or nothing (null). */
export function nextFocus(
    key: string,
    currentId: string,
    nodes: readonly TreeNodeBase[],
    expanded: ReadonlySet<string>,
): TreeAction | null {
    const items = visibleItems(nodes, expanded)
    const index = items.findIndex((item) => item.id === currentId)
    const current = items[index]
    if (!current) {
        return items[0] ? { type: 'focus', id: items[0].id } : null
    }

    const focus = (item: VisibleTreeItem | undefined): TreeAction | null =>
        item && item.id !== currentId ? { type: 'focus', id: item.id } : null

    switch (key) {
        case 'ArrowDown':
            return focus(items[index + 1])
        case 'ArrowUp':
            return focus(items[index - 1])
        case 'Home':
            return focus(items[0])
        case 'End':
            return focus(items[items.length - 1])
        case 'ArrowRight':
            if (!current.hasChildren) {
                return null
            }
            return expanded.has(current.id)
                ? focus(items[index + 1])
                : { type: 'expand', id: current.id }
        case 'ArrowLeft':
            if (current.hasChildren && expanded.has(current.id)) {
                return { type: 'collapse', id: current.id }
            }
            return current.parentId ? { type: 'focus', id: current.parentId } : null
        default:
            return null
    }
}
