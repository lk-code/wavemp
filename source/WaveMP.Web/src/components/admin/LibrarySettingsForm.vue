<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import type { LibrarySettings } from '@/api/generated/models'
import { fieldErrorsOf, problemCodeOf } from '@/api/problems'
import { customIntervalKey, formatList, intervalOptions, parseList } from '@/admin/presentation'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseCheckbox from '@/components/base/BaseCheckbox.vue'
import BaseSelect, { type SelectOption } from '@/components/base/BaseSelect.vue'
import BaseTextField from '@/components/base/BaseTextField.vue'
import { useLibrarySettingsStore } from '@/stores/librarySettings'

type ErrorGroup = 'interval' | 'extensions' | 'coverNames'

/** Settings of the library scan: interval, file watcher, extension lists and album cover names. */
const { t } = useI18n()
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
const errors = ref<Partial<Record<ErrorGroup, boolean>>>({})

const fieldGroups: Record<string, ErrorGroup> = {
    scanIntervalMinutes: 'interval',
    audioExtensions: 'extensions',
    videoExtensions: 'extensions',
    imageExtensions: 'extensions',
    albumCoverNames: 'coverNames',
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
        const groups = fieldErrorsOf(error)
            .map((field) => fieldGroups[field])
            .filter((group) => group !== undefined)
        if (problemCodeOf(error) === 'request.invalid' && groups.length > 0) {
            errors.value = Object.fromEntries(groups.map((group) => [group, true]))
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
            :error="errors.interval ? t('adminLibrary.errors.interval') : undefined"
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
            :error="errors.extensions ? t('adminLibrary.errors.extensions') : undefined"
            :disabled="saving"
        />
        <BaseTextField
            v-model="video"
            :label="t('adminLibrary.videoExtensions')"
            :hint="t('adminLibrary.listHint')"
            :error="errors.extensions ? t('adminLibrary.errors.extensions') : undefined"
            :disabled="saving"
        />
        <BaseTextField
            v-model="images"
            :label="t('adminLibrary.imageExtensions')"
            :hint="t('adminLibrary.listHint')"
            :error="errors.extensions ? t('adminLibrary.errors.extensions') : undefined"
            :disabled="saving"
        />
        <BaseTextField
            v-model="coverNames"
            :label="t('adminLibrary.albumCoverNames')"
            :hint="t('adminLibrary.coverNamesHint')"
            :error="errors.coverNames ? t('adminLibrary.errors.coverNames') : undefined"
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
