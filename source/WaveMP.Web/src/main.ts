import { createApp } from 'vue'
import { createPinia } from 'pinia'

import './styles/index.css'
import App from './App.vue'
import router from './router'
import { createWaveMpI18n } from './i18n'
import { browserLocales, resolveLocale } from './i18n/locales'
import { applyTheme, resolveThemeId } from './themes/themes'

// Until a profile is selected (step 4), the language of the browser applies.
const locale = resolveLocale(browserLocales())
document.documentElement.lang = locale

// index.html loads the default theme for the first paint; the selection per user follows later.
applyTheme(resolveThemeId(document.documentElement.dataset.theme))

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(createWaveMpI18n(locale))

// Mount after the first navigation, so that pages without the shell never show it.
void router.isReady().then(() => app.mount('#app'))
