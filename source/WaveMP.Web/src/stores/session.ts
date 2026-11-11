import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { sessionTokens } from '@/api/auth'
import { apiClient } from '@/api/client'
import {
    ReplayGainModeObject,
    RoleObject,
    type Language,
    type ReplayGainMode,
    type Session,
    type UserSettings,
} from '@/api/generated/models'
import { isUnauthorized } from '@/api/problems'
import { isSupportedLocale, type SupportedLocale } from '@/i18n/locales'
import {
    clearStoredSession,
    loadStoredSession,
    saveStoredSession,
    toStoredProfile,
    type StoredProfile,
    type StoredSession,
} from '@/session/storage'

const replayGainModes = new Set<string>(Object.values(ReplayGainModeObject))

function toReplayGain(settings: UserSettings | null | undefined): ReplayGainMode | null {
    const mode = settings?.replayGain
    return mode && replayGainModes.has(mode) ? mode : null
}

function toLocale(language: Language | null | undefined): SupportedLocale | null {
    return language && isSupportedLocale(language) ? language : null
}

/**
 * The selected profile and its session. Selecting a profile creates a session on the server; its
 * token authenticates all requests and is remembered, so the next start goes straight on. A
 * rejected token ends the session locally (App.vue then shows the profile selection); network
 * errors keep it, so the installed app also starts offline.
 */
export const useSessionStore = defineStore('session', () => {
    const token = ref<string | null>(null)
    const sessionId = ref<string | null>(null)
    const expiresAt = ref<string | null>(null)
    const profile = ref<StoredProfile | null>(null)
    const language = ref<SupportedLocale | null>(null)
    /** ReplayGain mode of the profile; Off until the settings are loaded. */
    const replayGain = ref<ReplayGainMode>(ReplayGainModeObject.Off)

    const isAuthenticated = computed(
        () =>
            token.value !== null &&
            profile.value !== null &&
            expiresAt.value !== null &&
            Date.parse(expiresAt.value) > Date.now(),
    )
    const isAdmin = computed(
        () => isAuthenticated.value && profile.value?.role === RoleObject.Admin,
    )
    const isBuiltInAdmin = computed(() => isAuthenticated.value && !!profile.value?.isBuiltIn)

    function apply(session: StoredSession | null): void {
        token.value = session?.token ?? null
        sessionId.value = session?.sessionId ?? null
        expiresAt.value = session?.expiresAt ?? null
        profile.value = session?.profile ?? null
        language.value = session?.language ?? null
        replayGain.value = ReplayGainModeObject.Off
        sessionTokens.current = token.value
    }

    function persist(): void {
        if (token.value && sessionId.value && expiresAt.value && profile.value) {
            saveStoredSession({
                token: token.value,
                sessionId: sessionId.value,
                expiresAt: expiresAt.value,
                profile: profile.value,
                language: language.value,
            })
        }
    }

    /** Forgets the session locally, e.g. after the server rejected its token. */
    function clear(): void {
        apply(null)
        clearStoredSession()
    }

    /** Takes over the remembered session; called once on start, before the first navigation. */
    function restore(): void {
        apply(loadStoredSession())
        sessionTokens.onUnauthorized = clear
    }

    function applySession(session: Session | null | undefined): boolean {
        const snapshot = toStoredProfile(session?.profile)
        if (!snapshot || !session?.id || !session.expiresAt) {
            return false
        }

        sessionId.value = session.id
        expiresAt.value = session.expiresAt.toISOString()
        profile.value = snapshot
        return true
    }

    /**
     * Selects a profile. Another selected profile is signed out first; selecting the current
     * profile again changes nothing. Errors (e.g. profile.not_found) are passed on.
     */
    async function select(profileId: string): Promise<void> {
        if (isAuthenticated.value && profile.value?.id === profileId) {
            return
        }

        if (isAuthenticated.value) {
            await signOut()
        }

        const created = await apiClient.api.v1.sessions.post({ profileId })
        if (!created?.token) {
            throw new Error('The server returned no session token.')
        }

        token.value = created.token
        sessionTokens.current = created.token
        language.value = null
        replayGain.value = ReplayGainModeObject.Off
        if (!applySession(created.session)) {
            clear()
            throw new Error('The server returned an incomplete session.')
        }

        try {
            const settings = await apiClient.api.v1.me.settings.get()
            language.value = toLocale(settings?.language)
            replayGain.value = toReplayGain(settings) ?? ReplayGainModeObject.Off
        } catch {
            // The language follows with the next refresh; until then the browser language applies.
        }

        persist()
    }

    /** Updates profile and language from the server; a rejected token ends the session. */
    async function refresh(): Promise<void> {
        const requestToken = token.value
        if (!requestToken) {
            return
        }

        try {
            const [session, settings] = await Promise.all([
                apiClient.api.v1.sessions.current.get(),
                apiClient.api.v1.me.settings.get(),
            ])
            if (token.value !== requestToken) {
                return
            }

            applySession(session)
            language.value = toLocale(settings?.language) ?? language.value
            replayGain.value = toReplayGain(settings) ?? replayGain.value
            persist()
        } catch (error) {
            if (isUnauthorized(error) && token.value === requestToken) {
                clear()
            }
        }
    }

    /** Saves the language of the profile on the server. Errors are passed on. */
    async function updateLanguage(locale: SupportedLocale): Promise<void> {
        const settings = await apiClient.api.v1.me.settings.patch({ language: locale })
        language.value = toLocale(settings?.language) ?? locale
        persist()
    }

    /** Saves the ReplayGain mode of the profile on the server. Errors are passed on. */
    async function updateReplayGain(mode: ReplayGainMode): Promise<void> {
        const settings = await apiClient.api.v1.me.settings.patch({ replayGain: mode })
        replayGain.value = toReplayGain(settings) ?? mode
    }

    /** Ends the session on the server (if reachable) and forgets it. */
    async function signOut(): Promise<void> {
        if (token.value) {
            try {
                await apiClient.api.v1.sessions.current.delete()
            } catch {
                // Offline or already ended: forgetting the token is enough.
            }
        }

        clear()
    }

    return {
        token,
        sessionId,
        expiresAt,
        profile,
        language,
        replayGain,
        isAuthenticated,
        isAdmin,
        isBuiltInAdmin,
        restore,
        select,
        refresh,
        updateLanguage,
        updateReplayGain,
        signOut,
        clear,
    }
})
