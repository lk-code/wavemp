<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from '@/components/base/BaseButton.vue'
import { useInView } from '@/composables/useInView'

/** Loads the next page of a list: by click, and by itself when it scrolls into view. */
const props = withDefaults(
    defineProps<{
        loading?: boolean
    }>(),
    { loading: false },
)

const emit = defineEmits<{
    load: []
}>()

const { t } = useI18n()
const button = ref<InstanceType<typeof BaseButton> | null>(null)
const visible = useInView(button)

// Only when it comes into view; after a page the button moves out of view again.
watch(visible, (isVisible) => {
    if (isVisible && !props.loading) {
        emit('load')
    }
})

function load(): void {
    if (!props.loading) {
        emit('load')
    }
}
</script>

<template>
    <BaseButton
        ref="button"
        part="load-more"
        :label="t('library.loadMore')"
        :state="loading ? 'loading' : undefined"
        :aria-busy="loading ? 'true' : undefined"
        @click="load"
    />
</template>
