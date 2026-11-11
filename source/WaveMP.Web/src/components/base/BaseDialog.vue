<script setup lang="ts">
import { onMounted, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseButton from './BaseButton.vue'

/** Modal dialog on the native <dialog> element: focus handling and Escape come from the browser. */
defineProps<{
    title: string
}>()

const open = defineModel<boolean>('open', { default: false })

const { t } = useI18n()
const titleId = useId()
const dialog = ref<HTMLDialogElement | null>(null)

function sync(): void {
    const element = dialog.value
    if (!element) {
        return
    }

    if (open.value && !element.open) {
        element.showModal?.()
    } else if (!open.value && element.open) {
        element.close?.()
    }
}

watch(open, sync)
onMounted(sync)

function close(): void {
    open.value = false
}
</script>

<template>
    <dialog ref="dialog" data-part="dialog" :aria-labelledby="titleId" @close="close">
        <div data-part="dialog-header">
            <h2 :id="titleId" data-part="dialog-title">{{ title }}</h2>
            <BaseButton
                part="btn-close"
                variant="icon"
                icon="close"
                :label="t('dialog.close')"
                @click="close"
            />
        </div>
        <div data-part="dialog-body">
            <slot />
        </div>
        <div v-if="$slots.footer" data-part="dialog-footer">
            <slot name="footer" />
        </div>
    </dialog>
</template>
