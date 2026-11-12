<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseCheckbox from '@/components/base/BaseCheckbox.vue'
import BaseLink from '@/components/base/BaseLink.vue'
import BaseSelect, { type SelectOption } from '@/components/base/BaseSelect.vue'
import ProfileSummary from '@/components/profiles/ProfileSummary.vue'
import { ReplayGainModeObject, type ReplayGainMode } from '@/api/generated/models'
import { useFormat } from '@/composables/useFormat'
import { isSupportedLocale, type SupportedLocale } from '@/i18n/locales'
import { maxCrossfadeSeconds } from '@/player/crossfade'
import { useEqualizerStore } from '@/stores/equalizer'
import { useSessionStore } from '@/stores/session'

/** Settings of the current profile; changes are saved at once. */
const { t, locale } = useI18n({ useScope: 'global' })
const session = useSessionStore()
const equalizer = useEqualizerStore()
const format = useFormat()

// Every language in its own language, so it can be found without understanding the current one.
const languageOptions = computed<SelectOption[]>(() => [
    { value: 'en', label: t('settings.languages.en') },
    { value: 'de', label: t('settings.languages.de') },
])

const replayGainOptions = computed<SelectOption[]>(() => [
    { value: ReplayGainModeObject.Off, label: t('settings.replayGainModes.off') },
    { value: ReplayGainModeObject.Track, label: t('settings.replayGainModes.track') },
    { value: ReplayGainModeObject.Album, label: t('settings.replayGainModes.album') },
])
const replayGainModes = new Set<string>(Object.values(ReplayGainModeObject))

const language = ref<string>(session.language ?? locale.value)
const replayGain = ref<string>(session.replayGain)
const saving = ref(false)
const error = ref<string | null>(null)

watch(
    () => session.language,
    (value) => {
        if (value && !saving.value) {
            language.value = value
        }
    },
)

watch(
    () => session.replayGain,
    (value) => {
        if (!saving.value) {
            replayGain.value = value
        }
    },
)

async function changeReplayGain(value: string): Promise<void> {
    if (!replayGainModes.has(value) || value === session.replayGain) {
        return
    }

    const previous = session.replayGain
    saving.value = true
    error.value = null
    try {
        await session.updateReplayGain(value as ReplayGainMode)
    } catch {
        error.value = t('settings.saveError')
        replayGain.value = previous
    } finally {
        saving.value = false
    }
}

async function changeLanguage(value: string): Promise<void> {
    if (!isSupportedLocale(value) || value === session.language) {
        return
    }

    const previous: SupportedLocale | null = session.language
    saving.value = true
    error.value = null
    try {
        await session.updateLanguage(value)
    } catch {
        error.value = t('settings.saveError')
        language.value = previous ?? locale.value
    } finally {
        saving.value = false
    }
}
const crossfadeOptions = computed<SelectOption[]>(() =>
    Array.from({ length: maxCrossfadeSeconds + 1 }, (_, seconds) => ({
        value: String(seconds),
        label:
            seconds === 0
                ? t('settings.crossfadeOff')
                : t('settings.crossfadeSeconds', { seconds: format.number(seconds) }),
    })),
)

/** Seconds of the fade-out the menu offers; another value of the profile is added. */
const sleepTimerFadeChoices = [5, 10, 15, 30, 60]
const sleepTimerFadeOptions = computed<SelectOption[]>(() =>
    [...new Set([...sleepTimerFadeChoices, session.sleepTimer.fadeSeconds])]
        .sort((a, b) => a - b)
        .map((seconds) => ({
            value: String(seconds),
            label: t('settings.sleepTimerFadeSeconds', { seconds: format.number(seconds) }),
        })),
)

const crossfade = ref(String(session.crossfade.seconds))
const crossfadeAlbums = ref(session.crossfade.albumsGapless)
const sleepTimerFade = ref(String(session.sleepTimer.fadeSeconds))

watch(
    () => session.crossfade,
    (value) => {
        if (!saving.value) {
            crossfade.value = String(value.seconds)
            crossfadeAlbums.value = value.albumsGapless
        }
    },
)

watch(
    () => session.sleepTimer,
    (value) => {
        if (!saving.value) {
            sleepTimerFade.value = String(value.fadeSeconds)
        }
    },
)

const equalizerEnabled = computed(() => equalizer.settings.enabled)

/** Saves a change of the profile and shows an error if it fails; reset restores the field. */
async function save(change: () => Promise<void>, reset: () => void): Promise<void> {
    saving.value = true
    error.value = null
    try {
        await change()
    } catch {
        error.value = t('settings.saveError')
        reset()
    } finally {
        saving.value = false
    }
}

async function changeEqualizer(enabled: boolean): Promise<void> {
    error.value = null
    equalizer.setEnabled(enabled)
    await equalizer.flush()
    if (equalizer.saveError) {
        equalizer.dismissError()
        error.value = t('settings.saveError')
    }
}

async function changeCrossfade(value: string): Promise<void> {
    const seconds = Number(value)
    if (!Number.isInteger(seconds) || seconds === session.crossfade.seconds) {
        return
    }

    const previous = session.crossfade.seconds
    await save(
        () => session.updateCrossfade({ seconds }),
        () => {
            crossfade.value = String(previous)
        },
    )
}

async function changeCrossfadeAlbums(value: boolean): Promise<void> {
    if (value === session.crossfade.albumsGapless) {
        return
    }

    const previous = session.crossfade.albumsGapless
    await save(
        () => session.updateCrossfade({ albumsGapless: value }),
        () => {
            crossfadeAlbums.value = previous
        },
    )
}

async function changeSleepTimerFade(value: string): Promise<void> {
    const fadeSeconds = Number(value)
    if (!Number.isInteger(fadeSeconds) || fadeSeconds === session.sleepTimer.fadeSeconds) {
        return
    }

    const previous = session.sleepTimer.fadeSeconds
    await save(
        () => session.updateSleepTimer({ fadeSeconds }),
        () => {
            sleepTimerFade.value = String(previous)
        },
    )
}
</script>

<template>
    <div data-part="view" data-view="settings">
        <ProfileSummary
            v-if="session.profile"
            :name="session.profile.name"
            :initials="session.profile.initials"
            :color="session.profile.color"
        />
        <BaseAlert v-if="error" tone="error" :message="error" />
        <form data-part="form" @submit.prevent>
            <BaseSelect
                v-model="language"
                data-field="language"
                :label="t('settings.language')"
                :options="languageOptions"
                :hint="t('settings.languageHint')"
                :disabled="saving"
                @update:model-value="changeLanguage"
            />
            <BaseSelect
                v-model="replayGain"
                data-field="replay-gain"
                :label="t('settings.replayGain')"
                :options="replayGainOptions"
                :hint="t('settings.replayGainHint')"
                :disabled="saving"
                @update:model-value="changeReplayGain"
            />
            <h2 data-part="section-title">{{ t('settings.playback') }}</h2>
            <BaseCheckbox
                data-field="equalizer"
                :model-value="equalizerEnabled"
                :label="t('settings.equalizerOn')"
                :hint="t('settings.equalizerHint')"
                @update:model-value="changeEqualizer"
            />
            <p data-part="form-note">
                <BaseLink :to="{ name: 'equalizer' }" data-item="open-equalizer">{{
                    t('settings.openEqualizer')
                }}</BaseLink>
            </p>
            <BaseSelect
                v-model="crossfade"
                data-field="crossfade"
                :label="t('settings.crossfade')"
                :options="crossfadeOptions"
                :hint="t('settings.crossfadeHint')"
                :disabled="saving"
                @update:model-value="changeCrossfade"
            />
            <BaseCheckbox
                v-model="crossfadeAlbums"
                data-field="crossfade-albums"
                :label="t('settings.crossfadeAlbums')"
                :disabled="saving || crossfade === '0'"
                @update:model-value="changeCrossfadeAlbums"
            />
            <BaseSelect
                v-model="sleepTimerFade"
                data-field="sleep-timer-fade"
                :label="t('settings.sleepTimerFade')"
                :options="sleepTimerFadeOptions"
                :hint="t('settings.sleepTimerFadeHint')"
                :disabled="saving"
                @update:model-value="changeSleepTimerFade"
            />
        </form>
    </div>
</template>
