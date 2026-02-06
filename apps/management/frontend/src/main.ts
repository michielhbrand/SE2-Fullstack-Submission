import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './assets/index.css'
import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'

// Sonner toast notifications
import 'vue-sonner/style.css'

// Vuetify
import { createVuetify } from 'vuetify'
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
import '@mdi/font/css/materialdesignicons.css'

const vuetify = createVuetify({
  components,
  directives,
  icons: {
    defaultSet: 'mdi',
  },
})

// Create Pinia instance
const pinia = createPinia()

// Create app
const app = createApp(App)

// Use plugins
app.use(pinia)
app.use(router)
app.use(vuetify)

// Initialize auth store and token expiration check after pinia is registered
const authStore = useAuthStore()
authStore.initializeExpirationCheck()

// Mount app
app.mount('#app')
