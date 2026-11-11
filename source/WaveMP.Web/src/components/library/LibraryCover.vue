<script setup lang="ts">
import { onBeforeUnmount, ref, watch } from 'vue'
import BaseCover from '@/components/base/BaseCover.vue'
import { useInView } from '@/composables/useInView'
import { useCoversStore, type CoverKind } from '@/stores/covers'

/**
 * Cover of an album or track from the API. It is loaded once it comes near the viewport and freed
 * when the component goes; until then, and without a cover, the placeholder of the theme shows.
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
let held: { kind: CoverKind; id: string } | null = null

function release(): void {
    if (held) {
        covers.release(held.kind, held.id)
        held = null
    }

    src.value = null
}

watch(
    () => [props.kind, props.id, props.available, visible.value] as const,
    async ([kind, id, available, isVisible]) => {
        if (held && (held.kind !== kind || held.id !== id || !available)) {
            release()
        }

        if (!isVisible || !available || !id || held) {
            return
        }

        const current = { kind, id }
        held = current
        const url = await covers.acquire(kind, id)
        if (held === current) {
            src.value = url
        }
    },
    { immediate: true },
)

onBeforeUnmount(release)
</script>

<template>
    <BaseCover ref="cover" :src="src" />
</template>
