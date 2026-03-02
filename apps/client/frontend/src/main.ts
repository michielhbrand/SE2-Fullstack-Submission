import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './assets/index.css'
import App from './App.vue'
import router from './router'
import { useAuthStore } from './stores/auth'

// Sonner toast notifications
import 'vue-sonner/style.css'

// ApexCharts
import VueApexCharts from 'vue3-apexcharts'

// Vuetify
import { createVuetify } from 'vuetify'
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg'

const vuetify = createVuetify({
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: { mdi },
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
app.use(VueApexCharts)

// Initialize auth store and token expiration check after pinia is registered
const authStore = useAuthStore()
authStore.initialize()

// Mount app
app.mount('#app')
