<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import BaseAlert from '@/components/base/BaseAlert.vue'
import BaseSelect, { type SelectOption } from '@/components/base/BaseSelect.vue'
import ProfileSummary from '@/components/profiles/ProfileSummary.vue'
import { isSupportedLocale, type SupportedLocale } from '@/i18n/locales'
import { useSessionStore } from '@/stores/session'

/** Settings of the current profile; changes are saved at once. */
const { t, locale } = useI18n({ useScope: 'global' })
const session = useSessionStore()

// Every language in its own language, so it can be found without understanding the current one.
const languageOptions = computed<SelectOption[]>(() => [
    { value: 'en', label: t('settings.languages.en') },
    { value: 'de', label: t('settings.languages.de') },
])

const language = ref<string>(session.language ?? locale.value)
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
                :label="t('settings.language')"
                :options="languageOptions"
                :hint="t('settings.languageHint')"
                :disabled="saving"
                @update:model-value="changeLanguage"
            />
        </form>
    </div>
</template>
