<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import BaseCover from '@/components/base/BaseCover.vue'
import { useInView } from '@/composables/useInView'
import { coverSizeFor, type CoverSize } from '@/library/covers'
import { useCoversStore, type CoverKind } from '@/stores/covers'

/**
 * Cover of an album or track from the API. It is loaded once it comes near the viewport, in the
 * size the theme shows it with (its width times the pixel density), and freed when the component
 * goes; until then, and without a cover, the placeholder of the theme shows.
 */
const props = defineProps<{
    kind: CoverKind
    id: string | null | undefined
    /** Whether the API has a cover (hasCover); otherwise nothing is loaded. */
    available: boolean
}>()

const covers = useCoversStore()
const cover = ref<InstanceType<typeof BaseCover> | null>(null)
const visible = useInView(cover)
const src = ref<string | null>(null)
let held: { kind: CoverKind; id: string; size: CoverSize } | null = null

/** The size of the API for the shown width of the cover. */
function measure(): CoverSize {
    const element: unknown = cover.value?.$el
    const width = element instanceof Element ? element.getBoundingClientRect().width : 0
    const pixelRatio = typeof window === 'undefined' ? 1 : window.devicePixelRatio
    return coverSizeFor(width, pixelRatio)
}

function release(): void {
    if (held) {
        covers.release(held.kind, held.id, held.size)
        held = null
    }

    src.value = null
}

/** Loads the cover once it is visible; runs after rendering, so the shown width is known. */
async function update(): Promise<void> {
    const { kind, id, available } = props
    if (held && (held.kind !== kind || held.id !== id || !available)) {
        release()
    }

    if (!visible.value || !available || !id || held) {
        return
    }

    const current = { kind, id, size: measure() }
    held = current
    const url = await covers.acquire(kind, id, current.size)
    if (held === current) {
        src.value = url
    }
}

watch(() => [props.kind, props.id, props.available, visible.value] as const, update, {
    flush: 'post',
})

onMounted(update)
onBeforeUnmount(release)
</script>

<template>
    <BaseCover ref="cover" :src="src" />
</template>
