import { createApp } from 'vue'
import { createPinia } from 'pinia'

import './styles/index.css'
import App from './App.vue'
import router from './router'
import { createWaveMpI18n } from './i18n'
import { browserLocales, resolveLocale } from './i18n/locales'
import { useSessionStore } from './stores/session'
import { applyTheme, resolveThemeId } from './themes/themes'

// The remembered profile is restored before the first navigation, so the route guard knows it.
const pinia = createPinia()
const session = useSessionStore(pinia)
session.restore()

// The language of the profile applies once one is selected; before that the browser language
// (useLocaleSync keeps it in line afterwards).
const locale =
    session.isAuthenticated && session.language ? session.language : resolveLocale(browserLocales())
document.documentElement.lang = locale

// index.html loads the default theme for the first paint; the selection per user follows later.
applyTheme(resolveThemeId(document.documentElement.dataset.theme))

const app = createApp(App)

app.use(pinia)
app.use(router)
app.use(createWaveMpI18n(locale))

// Mount after the first navigation, so that pages without the shell never show it.
void router.isReady().then(() => app.mount('#app'))
