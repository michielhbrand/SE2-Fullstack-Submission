import { createApp } from 'vue'
import './assets/index.css'
import App from './App.vue'
import router from './router'
import { authService } from './services/auth'

// Vuetify
// import 'vuetify/styles'
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

// Initialize token expiration check on app startup
authService.initializeExpirationCheck()

createApp(App).use(router).use(vuetify).mount('#app')
