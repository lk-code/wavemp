<script setup lang="ts">
import { RouterLink, type RouteLocationRaw } from 'vue-router'
import type { PartName } from '@/themes/parts'

/**
 * Link within the app: a plain anchor with its part, without the classes of RouterLink. Further
 * attributes (e.g. data-kind) go to the anchor.
 */
defineOptions({ inheritAttrs: false })

withDefaults(
    defineProps<{
        to: RouteLocationRaw
        part?: PartName
    }>(),
    { part: 'link' },
)
</script>

<template>
    <RouterLink v-slot="{ href, navigate }" :to="to" custom>
        <a v-bind="$attrs" :data-part="part" :href="href" @click="navigate">
            <slot />
        </a>
    </RouterLink>
</template>
