<script lang="ts">
export interface ListColumn {
    readonly id: string
    readonly label: string
}

export interface ListRowBase {
    readonly id: string
    /** data-state tokens of the row besides "selected", e.g. "has-video". */
    readonly state?: readonly string[]
}

export interface ListGroup<Row extends ListRowBase> {
    readonly id: string
    readonly title: string
    readonly rows: readonly Row[]
}
</script>

<script setup lang="ts" generic="Row extends ListRowBase">
import { computed, nextTick, useId } from 'vue'
import { useLongPress } from '@/composables/useLongPress'

const props = withDefaults(
    defineProps<{
        columns: readonly ListColumn[]
        groups: readonly ListGroup<Row>[]
        /** Accessible name of the list. */
        label: string
        /** data-state tokens of the list, e.g. "placeholder". */
        state?: string
        /**
         * Rows have a context menu: right click, the context menu key, Shift+F10 and a long press
         * on touch screens emit row-menu instead of opening the menu of the browser.
         */
        rowMenu?: boolean
    }>(),
    { state: undefined, rowMenu: false },
)

const emit = defineEmits<{
    'row-menu': [rowId: string, x: number, y: number]
}>()

defineSlots<{
    cell?(props: { row: Row; column: ListColumn }): unknown
    'group-info'?(props: { group: ListGroup<Row> }): unknown
}>()

/** Id of the selected row (single selection). */
const selected = defineModel<string | undefined>('selected', { default: undefined })

const listId = useId()
const allRows = computed(() => props.groups.flatMap((group) => group.rows))
const tabStop = computed(() => selected.value ?? allRows.value[0]?.id)

function rowElementId(rowId: string): string {
    return `${listId}-row-${rowId}`
}

function titleId(groupId: string): string {
    return `${listId}-group-${groupId}`
}

function rowState(row: Row): string | undefined {
    const tokens = [...(row.state ?? []), ...(row.id === selected.value ? ['selected'] : [])]
    return tokens.length > 0 ? tokens.join(' ') : undefined
}

async function select(rowId: string, focus = false): Promise<void> {
    selected.value = rowId
    if (focus) {
        await nextTick()
        document.getElementById(rowElementId(rowId))?.focus()
    }
}

const longPress = useLongPress<string>((rowId, x, y) => {
    void select(rowId)
    emit('row-menu', rowId, x, y)
})

function onContextMenu(event: MouseEvent, rowId: string): void {
    if (!props.rowMenu) {
        return
    }

    event.preventDefault()
    longPress.cancel()
    void select(rowId)
    emit('row-menu', rowId, event.clientX, event.clientY)
}

function onPointerdown(event: PointerEvent, rowId: string): void {
    if (props.rowMenu) {
        longPress.onPointerdown(event, rowId)
    }
}

function openMenuAtRow(event: KeyboardEvent, rowId: string): void {
    event.preventDefault()
    void select(rowId)
    const rect = (event.currentTarget as HTMLElement).getBoundingClientRect()
    emit('row-menu', rowId, rect.left, rect.bottom)
}

function onKeydown(event: KeyboardEvent, rowId: string): void {
    if (props.rowMenu && (event.key === 'ContextMenu' || (event.key === 'F10' && event.shiftKey))) {
        openMenuAtRow(event, rowId)
        return
    }

    const rows = allRows.value
    const index = rows.findIndex((row) => row.id === rowId)
    const target = {
        ArrowDown: rows[index + 1],
        ArrowUp: rows[index - 1],
        Home: rows[0],
        End: rows[rows.length - 1],
    }[event.key]

    if (event.key === ' ' || event.key === 'Enter') {
        event.preventDefault()
        void select(rowId)
        return
    }

    if (target) {
        event.preventDefault()
        void select(target.id, true)
    }
}
</script>

<template>
    <div data-part="list" role="grid" aria-readonly="true" :aria-label="label" :data-state="state">
        <div data-part="list-header" role="row">
            <span
                v-for="column in columns"
                :key="column.id"
                data-part="list-column"
                role="columnheader"
                :data-column="column.id"
            >
                <span data-part="label">{{ column.label }}</span>
            </span>
        </div>
        <div
            v-for="group in groups"
            :key="group.id"
            data-part="list-group"
            role="rowgroup"
            :aria-labelledby="titleId(group.id)"
        >
            <div data-part="list-group-header" role="row">
                <span
                    :id="titleId(group.id)"
                    data-part="list-group-title"
                    role="rowheader"
                    :aria-colspan="columns.length"
                >
                    {{ group.title }}
                </span>
            </div>
            <div v-if="$slots['group-info']" data-part="list-group-info" role="row">
                <slot name="group-info" :group="group" />
            </div>
            <div data-part="list-group-rows" role="presentation">
                <div
                    v-for="row in group.rows"
                    :id="rowElementId(row.id)"
                    :key="row.id"
                    data-part="list-row"
                    role="row"
                    :aria-selected="row.id === selected ? 'true' : 'false'"
                    :data-state="rowState(row)"
                    :tabindex="row.id === tabStop ? 0 : -1"
                    @click="select(row.id)"
                    @keydown="onKeydown($event, row.id)"
                    @contextmenu="onContextMenu($event, row.id)"
                    @pointerdown="onPointerdown($event, row.id)"
                    @pointermove="longPress.onPointermove"
                    @pointerup="longPress.cancel"
                    @pointercancel="longPress.cancel"
                    @pointerleave="longPress.cancel"
                >
                    <span
                        v-for="column in columns"
                        :key="column.id"
                        data-part="list-cell"
                        role="gridcell"
                        :data-column="column.id"
                    >
                        <slot name="cell" :row="row" :column="column" />
                    </span>
                </div>
            </div>
        </div>
    </div>
</template>
