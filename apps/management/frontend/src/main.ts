import { createApp } from "vue";
import { createPinia } from "pinia";
import "./assets/index.css";
import App from "./App.vue";
import router from "./router";
import { useAuthStore } from "./stores/auth";

import "vue-sonner/style.css";

import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import "@mdi/font/css/materialdesignicons.css";

const vuetify = createVuetify({
  components,
  directives,
  icons: {
    defaultSet: "mdi",
  },
});

const pinia = createPinia();

const app = createApp(App);

// Plugins
app.use(pinia);
app.use(router);
app.use(vuetify);

// Initialize auth store
const authStore = useAuthStore();
authStore.initialize();

app.mount("#app");
