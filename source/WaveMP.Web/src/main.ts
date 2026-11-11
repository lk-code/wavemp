import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import { createWaveMpI18n } from './i18n'
import { browserLocales, resolveLocale } from './i18n/locales'

// Until a profile is selected (step 4), the language of the browser applies.
const locale = resolveLocale(browserLocales())
document.documentElement.lang = locale

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.use(createWaveMpI18n(locale))

app.mount('#app')
