<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { LibrarySettings } from '@/api/generated/models'
import { problemCodeOf, validationErrorsOf } from '@/api/problems'
import { customIntervalKey, formatList, intervalOptions, parseList } from '@/admin/presentation'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseCheckbox from '@/components/base/BaseCheckbox.vue'
import BaseSelect, { type SelectOption } from '@/components/base/BaseSelect.vue'
import BaseTextField from '@/components/base/BaseTextField.vue'
import { useFormat } from '@/composables/useFormat'
import { validationMessagesByField, type ValidationMessageOptions } from '@/i18n/validation'
import { useLibrarySettingsStore } from '@/stores/librarySettings'

type Field = 'interval' | 'audio' | 'video' | 'images' | 'coverNames'

/** Settings of the library scan: interval, file watcher, extension lists and album cover names. */
const { t } = useI18n()
const format = useFormat()
const store = useLibrarySettingsStore()

const interval = ref('')
const fileWatcher = ref(true)
const audio = ref('')
const video = ref('')
const images = ref('')
const coverNames = ref('')
const saving = ref(false)
const saved = ref(false)
const failed = ref(false)
const errors = ref<Partial<Record<Field, string>>>({})

const fields: Record<string, Field> = {
    scanIntervalMinutes: 'interval',
    audioExtensions: 'audio',
    videoExtensions: 'video',
    imageExtensions: 'images',
    albumCoverNames: 'coverNames',
}

const extensionKeys = {
    invalid_format: 'adminLibrary.errors.extensionFormat',
    duplicate: 'adminLibrary.errors.extensionDuplicate',
    required: 'adminLibrary.errors.extensionsRequired',
} as const

/** Texts of the form for single codes; all other codes get the general validation messages. */
const messageKeys: Record<Field, ValidationMessageOptions['keys']> = {
    interval: { out_of_range: 'adminLibrary.errors.interval' },
    audio: extensionKeys,
    video: extensionKeys,
    images: extensionKeys,
    coverNames: { invalid_format: 'adminLibrary.errors.coverNames' },
}

function show(settings: LibrarySettings | null): void {
    if (!settings) {
        return
    }

    interval.value = String(settings.scanIntervalMinutes ?? 360)
    fileWatcher.value = settings.fileWatcherEnabled ?? true
    audio.value = formatList(settings.audioExtensions)
    video.value = formatList(settings.videoExtensions)
    images.value = formatList(settings.imageExtensions)
    coverNames.value = formatList(settings.albumCoverNames)
}

const intervalSelectOptions = computed<SelectOption[]>(() => {
    const options = intervalOptions.map((option) => ({
        value: String(option.minutes),
        label: t(option.labelKey),
    }))
    if (interval.value && !options.some((option) => option.value === interval.value)) {
        options.push({
            value: interval.value,
            label: t(customIntervalKey, { minutes: interval.value }),
        })
    }

    return options
})

async function save(): Promise<void> {
    if (saving.value) {
        return
    }

    saving.value = true
    saved.value = false
    failed.value = false
    errors.value = {}
    try {
        const result = await store.update({
            scanIntervalMinutes: Number(interval.value),
            fileWatcherEnabled: fileWatcher.value,
            audioExtensions: parseList(audio.value),
            videoExtensions: parseList(video.value),
            imageExtensions: parseList(images.value),
            albumCoverNames: parseList(coverNames.value),
        })
        show(result)
        saved.value = true
    } catch (error) {
        const messages = validationMessagesByField(
            validationErrorsOf(error),
            fields,
            (key, parameters) => t(key, parameters),
            (field) => ({ keys: messageKeys[field], formatLimit: format.number }),
        )
        if (problemCodeOf(error) === 'request.invalid' && Object.keys(messages).length > 0) {
            errors.value = messages
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
        :message="t('adminLibrary.settingsLoadError')"
    />
    <form v-else data-part="form" @submit.prevent="save">
        <BaseSelect
            v-model="interval"
            :label="t('adminLibrary.interval')"
            :options="intervalSelectOptions"
            :hint="t('adminLibrary.intervalHint')"
            :error="errors.interval"
            :disabled="saving"
        />
        <BaseCheckbox
            v-model="fileWatcher"
            :label="t('adminLibrary.fileWatcher')"
            :hint="t('adminLibrary.fileWatcherHint')"
            :disabled="saving"
        />
        <BaseTextField
            v-model="audio"
            :label="t('adminLibrary.audioExtensions')"
            :hint="t('adminLibrary.listHint')"
            :error="errors.audio"
            :disabled="saving"
        />
        <BaseTextField
            v-model="video"
            :label="t('adminLibrary.videoExtensions')"
            :hint="t('adminLibrary.listHint')"
            :error="errors.video"
            :disabled="saving"
        />
        <BaseTextField
            v-model="images"
            :label="t('adminLibrary.imageExtensions')"
            :hint="t('adminLibrary.listHint')"
            :error="errors.images"
            :disabled="saving"
        />
        <BaseTextField
            v-model="coverNames"
            :label="t('adminLibrary.albumCoverNames')"
            :hint="t('adminLibrary.coverNamesHint')"
            :error="errors.coverNames"
            :disabled="saving"
        />
        <BaseAlert v-if="saved" tone="success" :message="t('adminLibrary.saved')" />
        <BaseAlert v-if="failed" tone="error" :message="t('adminLibrary.saveError')" />
        <div data-part="view-actions">
            <BaseButton
                type="submit"
                variant="primary"
                :label="t('adminLibrary.save')"
                :disabled="saving || !store.settings"
            />
        </div>
    </form>
</template>
