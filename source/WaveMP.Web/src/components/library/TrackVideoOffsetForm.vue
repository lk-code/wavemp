<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { problemCodeOf, statusOf, validationErrorsOf } from '@/api/problems'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseButton from '@/components/base/BaseButton.vue'
import BaseTextField from '@/components/base/BaseTextField.vue'
import { useFormat } from '@/composables/useFormat'
import { validationMessage, validationMessagesByField } from '@/i18n/validation'
import { getTrackVideoOffset, setTrackVideoOffset } from '@/library/api'
import { maxVideoOffset, minVideoOffset, parseVideoOffset } from '@/library/videoOffset'

/**
 * The offset of the video of a song against its audio, for admins: positive if the music in the
 * video starts later than in the song, negative if it starts earlier. The player applies a saved
 * offset to a playing video at once.
 */
const props = defineProps<{
    trackId: string
}>()

const emit = defineEmits<{
    saved: [offsetMilliseconds: number]
}>()

const { t } = useI18n()
const format = useFormat()

const offset = ref('')
const status = ref<'loading' | 'ready' | 'error'>('loading')
const saving = ref(false)
const saved = ref(false)
const failed = ref(false)
const invalid = ref<string | undefined>(undefined)
let generation = 0

function formatLimit(value: number): string {
    return format.number(value)
}

async function load(): Promise<void> {
    const current = ++generation
    status.value = 'loading'
    saved.value = false
    failed.value = false
    invalid.value = undefined
    try {
        const loaded = await getTrackVideoOffset(props.trackId)
        if (current === generation) {
            offset.value = String(loaded?.offsetMilliseconds ?? 0)
            status.value = 'ready'
        }
    } catch {
        if (current === generation) {
            status.value = 'error'
        }
    }
}

async function save(): Promise<void> {
    if (saving.value) {
        return
    }

    saved.value = false
    failed.value = false
    invalid.value = undefined
    const value = parseVideoOffset(String(offset.value))
    if (value === null) {
        invalid.value = validationMessage(
            {
                field: 'offsetMilliseconds',
                code: 'out_of_range',
                min: minVideoOffset,
                max: maxVideoOffset,
            },
            (key, parameters) => t(key, parameters),
            { keys: { out_of_range: 'trackVideo.offsetRange' }, formatLimit },
        )
        return
    }

    saving.value = true
    try {
        const result = await setTrackVideoOffset(props.trackId, value)
        const stored = result?.offsetMilliseconds ?? value
        offset.value = String(stored)
        saved.value = true
        emit('saved', stored)
    } catch (error) {
        const messages = validationMessagesByField(
            validationErrorsOf(error),
            { offsetMilliseconds: 'offset' },
            (key, parameters) => t(key, parameters),
            () => ({ keys: { out_of_range: 'trackVideo.offsetRange' }, formatLimit }),
        )
        if (problemCodeOf(error) === 'request.invalid' && messages.offset) {
            invalid.value = messages.offset
        } else if (statusOf(error) === 404) {
            status.value = 'error'
        } else {
            failed.value = true
        }
    } finally {
        saving.value = false
    }
}

watch(() => props.trackId, load)

onMounted(load)
</script>

<template>
    <BaseAlert v-if="status === 'error'" tone="error" :message="t('trackVideo.loadError')" />
    <form v-else data-part="form" @submit.prevent="save">
        <BaseTextField
            v-model="offset"
            data-field="video-offset"
            type="number"
            :label="t('trackVideo.offset')"
            :hint="t('trackVideo.offsetHint')"
            :error="invalid"
            :disabled="status !== 'ready' || saving"
            autocomplete="off"
        />
        <BaseAlert v-if="saved" tone="success" :message="t('trackVideo.saved')" />
        <BaseAlert v-if="failed" tone="error" :message="t('trackVideo.saveError')" />
        <div data-part="view-actions">
            <BaseButton
                type="submit"
                variant="primary"
                data-item="save-video-offset"
                :label="t('trackVideo.save')"
                :disabled="status !== 'ready' || saving"
            />
        </div>
    </form>
</template>
