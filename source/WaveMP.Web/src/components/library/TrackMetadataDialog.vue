<script setup lang="ts">
import { computed, reactive, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
    ProblemCodeObject,
    TrackMetadataStatusObject,
    type TrackMetadata,
    type TrackMetadataStatus,
} from '@/api/generated/models'
import { problemCodeOf, statusOf, validationErrorsOf } from '@/api/problems'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseDialog from '@/components/base/BaseDialog.vue'
import BaseProgress from '@/components/base/BaseProgress.vue'
import BaseTextField from '@/components/base/BaseTextField.vue'
import { validationMessagesByField } from '@/i18n/validation'
import { getTrackMetadata, updateTrackMetadata } from '@/library/api'
import {
    emptyMetadataForm,
    metadataChanges,
    metadataDataFields,
    metadataLimits,
    toMetadataForm,
    validateMetadataForm,
    type MetadataField,
    type MetadataForm,
} from '@/library/metadata'

/**
 * Metadata editor of a track for admins: title, artist, album, album artist, year, track and
 * disc number and genre. The values are written into the tags of the audio file of the track;
 * without an audio file or if it cannot be written, the fields are disabled with a hint. Only
 * changed fields are sent; empty fields remove the value from the tags.
 */
const props = defineProps<{
    trackId: string
}>()

const emit = defineEmits<{
    saved: [metadata: TrackMetadata]
}>()

const open = defineModel<boolean>('open', { default: false })

const { t } = useI18n()
const formId = useId()

const state = ref<'loading' | 'error' | 'ready'>('loading')
const loadError = ref<string | null>(null)
const status = ref<TrackMetadataStatus | null>(null)
const audioFilePath = ref<string | null>(null)
const original = ref<MetadataForm>(emptyMetadataForm())
const form = reactive<MetadataForm>(emptyMetadataForm())
const fieldErrors = ref<Partial<Record<MetadataField, string>>>({})
const error = ref<string | null>(null)
const saving = ref(false)
let generation = 0

const editable = computed(
    () => state.value === 'ready' && status.value === TrackMetadataStatusObject.Editable,
)
const fieldsDisabled = computed(() => !editable.value || saving.value)
const hint = computed(() => {
    if (status.value === TrackMetadataStatusObject.NoAudio) {
        return t('trackMetadata.hints.noAudio')
    }
    if (status.value === TrackMetadataStatusObject.NotWritable) {
        return t('trackMetadata.hints.notWritable')
    }
    return null
})

const labels = computed<Record<MetadataField, string>>(() => ({
    title: t('trackMetadata.fields.title'),
    artist: t('trackMetadata.fields.artist'),
    album: t('trackMetadata.fields.album'),
    albumArtist: t('trackMetadata.fields.albumArtist'),
    year: t('trackMetadata.fields.year'),
    trackNumber: t('trackMetadata.fields.trackNumber'),
    discNumber: t('trackMetadata.fields.discNumber'),
    genre: t('trackMetadata.fields.genre'),
}))

const textFields: readonly MetadataField[] = ['title', 'artist', 'album', 'albumArtist']
const numberFields: readonly MetadataField[] = ['year', 'trackNumber', 'discNumber']

function apply(metadata: TrackMetadata): void {
    status.value = metadata.status ?? null
    audioFilePath.value = metadata.audioFilePath ?? null
    original.value = toMetadataForm(metadata)
    Object.assign(form, original.value)
}

async function load(): Promise<void> {
    const current = ++generation
    state.value = 'loading'
    loadError.value = null
    error.value = null
    fieldErrors.value = {}
    try {
        const metadata = await getTrackMetadata(props.trackId)
        if (current !== generation) {
            return
        }
        if (!metadata) {
            throw new Error('No metadata')
        }
        apply(metadata)
        state.value = 'ready'
    } catch (failure) {
        if (current === generation) {
            loadError.value =
                problemCodeOf(failure) === ProblemCodeObject.TrackNot_found
                    ? t('trackMetadata.errors.notFound')
                    : t('trackMetadata.errors.loadFailed')
            state.value = 'error'
        }
    }
}

watch(
    open,
    (value) => {
        if (value) {
            void load()
        } else {
            generation++
        }
    },
    { immediate: true },
)

watch(
    () => props.trackId,
    () => {
        if (open.value) {
            void load()
        }
    },
)

function report(failure: unknown): void {
    const code = problemCodeOf(failure)
    if (code === ProblemCodeObject.TrackNot_writable) {
        status.value = TrackMetadataStatusObject.NotWritable
        error.value = t('trackMetadata.errors.notWritable')
    } else if (code === ProblemCodeObject.TrackNo_audio) {
        status.value = TrackMetadataStatusObject.NoAudio
    } else if (code === ProblemCodeObject.TrackNot_found) {
        error.value = t('trackMetadata.errors.notFound')
    } else if (statusOf(failure) === 400) {
        // The fields of the form are named like the members of the request.
        const fields = Object.fromEntries(
            Object.keys(metadataDataFields).map((field) => [field, field as MetadataField]),
        )
        fieldErrors.value = validationMessagesByField(
            validationErrorsOf(failure),
            fields,
            (key, parameters) => t(key, parameters),
        )
        error.value = t('trackMetadata.errors.invalid')
    } else {
        error.value = t('trackMetadata.errors.saveFailed')
    }
}

async function save(): Promise<void> {
    if (saving.value || !editable.value) {
        return
    }

    error.value = null
    const invalid = validateMetadataForm(form)
    fieldErrors.value = Object.fromEntries(
        Object.keys(invalid).map((field) => [
            field,
            t('trackMetadata.errors.number', { max: metadataLimits.maxNumber }),
        ]),
    )
    if (Object.keys(invalid).length > 0) {
        return
    }

    const changes = metadataChanges(original.value, form)
    if (Object.keys(changes).length === 0) {
        open.value = false
        return
    }

    saving.value = true
    try {
        const saved = await updateTrackMetadata(props.trackId, changes)
        open.value = false
        if (saved) {
            emit('saved', saved)
        }
    } catch (failure) {
        report(failure)
    } finally {
        saving.value = false
    }
}
</script>

<template>
    <BaseDialog v-model:open="open" :title="t('trackMetadata.title')">
        <BaseProgress v-if="state === 'loading'" :label="t('trackMetadata.loading')" />
        <template v-else-if="state === 'error'">
            <BaseAlert tone="error" :message="loadError ?? ''" />
            <BaseButton :label="t('library.retry')" @click="load" />
        </template>
        <form v-else :id="formId" data-part="form" novalidate @submit.prevent="save">
            <BaseAlert v-if="error" tone="error" :message="error" />
            <BaseAlert v-if="hint" tone="info" :message="hint" />
            <BaseTextField
                v-for="field in textFields"
                :key="field"
                v-model="form[field]"
                :data-field="metadataDataFields[field]"
                :label="labels[field]"
                :error="fieldErrors[field]"
                :maxlength="metadataLimits.text"
                :disabled="fieldsDisabled"
                autocomplete="off"
            />
            <div data-part="field-row">
                <BaseTextField
                    v-for="field in numberFields"
                    :key="field"
                    v-model="form[field]"
                    :data-field="metadataDataFields[field]"
                    :label="labels[field]"
                    :error="fieldErrors[field]"
                    :maxlength="metadataLimits.numberDigits"
                    :disabled="fieldsDisabled"
                    inputmode="numeric"
                    autocomplete="off"
                />
            </div>
            <BaseTextField
                v-model="form.genre"
                :data-field="metadataDataFields.genre"
                :label="labels.genre"
                :error="fieldErrors.genre"
                :maxlength="metadataLimits.genre"
                :disabled="fieldsDisabled"
                autocomplete="off"
            />
            <p v-if="editable" data-part="form-note">{{ t('trackMetadata.emptyRemoves') }}</p>
            <p v-if="audioFilePath" data-part="form-note">
                {{ t('trackMetadata.file', { path: audioFilePath }) }}
            </p>
        </form>
        <template #footer>
            <BaseButton :label="t('actions.cancel')" @click="open = false" />
            <BaseButton
                type="submit"
                variant="primary"
                :form="formId"
                :label="t('trackMetadata.save')"
                :disabled="!editable || saving"
            />
        </template>
    </BaseDialog>
</template>
