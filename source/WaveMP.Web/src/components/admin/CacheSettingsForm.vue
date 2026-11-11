<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { CacheSettings } from '@/api/generated/models'
import { problemCodeOf, validationErrorsOf } from '@/api/problems'
import { bytesPerMegabyte, cacheLimitOptions } from '@/admin/presentation'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseSelect, { type SelectOption } from '@/components/base/BaseSelect.vue'
import { useFormat } from '@/composables/useFormat'
import { validationMessagesByField } from '@/i18n/validation'
import { useCacheSettingsStore } from '@/stores/cacheSettings'

/** Settings of the cache: its largest size. */
const emit = defineEmits<{
    saved: []
}>()

const { t } = useI18n()
const format = useFormat()
const store = useCacheSettingsStore()

const maxSize = ref('')
const saving = ref(false)
const saved = ref(false)
const failed = ref(false)
const invalid = ref<string | undefined>(undefined)

function show(settings: CacheSettings | null): void {
    if (settings?.maxSizeMegabytes) {
        maxSize.value = String(settings.maxSizeMegabytes)
    }
}

function sizeLabel(megabytes: number): string {
    return format.fileSize(megabytes * bytesPerMegabyte)
}

const options = computed<SelectOption[]>(() => {
    const offered = cacheLimitOptions.map((megabytes) => ({
        value: String(megabytes),
        label: sizeLabel(megabytes),
    }))
    if (maxSize.value && !offered.some((option) => option.value === maxSize.value)) {
        offered.push({ value: maxSize.value, label: sizeLabel(Number(maxSize.value)) })
    }

    return offered
})

async function save(): Promise<void> {
    if (saving.value) {
        return
    }

    saving.value = true
    saved.value = false
    failed.value = false
    invalid.value = undefined
    try {
        show(await store.update({ maxSizeMegabytes: Number(maxSize.value) }))
        saved.value = true
        emit('saved')
    } catch (error) {
        const messages = validationMessagesByField(
            validationErrorsOf(error),
            { maxSizeMegabytes: 'maxSize' },
            (key, parameters) => t(key, parameters),
            () => ({ keys: { out_of_range: 'adminCache.errors.maxSize' }, formatLimit: sizeLabel }),
        )
        if (problemCodeOf(error) === 'request.invalid' && messages.maxSize) {
            invalid.value = messages.maxSize
        } else {
            failed.value = true
        }
    } finally {
        saving.value = false
    }
}

watch(() => store.settings, show)

onMounted(async () => {
    if (!store.settings) {
        await store.load()
    }

    show(store.settings)
})
</script>

<template>
    <BaseAlert
        v-if="store.status === 'error' && !store.settings"
        tone="error"
        :message="t('adminCache.settingsLoadError')"
    />
    <form v-else data-part="form" @submit.prevent="save">
        <BaseSelect
            v-model="maxSize"
            :label="t('adminCache.maxSize')"
            :options="options"
            :hint="t('adminCache.maxSizeHint')"
            :error="invalid"
            :disabled="saving"
        />
        <BaseAlert v-if="saved" tone="success" :message="t('adminCache.saved')" />
        <BaseAlert v-if="failed" tone="error" :message="t('adminCache.saveError')" />
        <div data-part="view-actions">
            <BaseButton
                type="submit"
                variant="primary"
                :label="t('adminCache.save')"
                :disabled="saving || !store.settings"
            />
        </div>
    </form>
</template>
