import { createApp } from "vue";
import { createPinia } from "pinia";
import "./assets/index.css";
import App from "./App.vue";
import router from "./router";
import { useAuthStore } from "./stores/auth";

import "vue-sonner/style.css";

import VueApexCharts from "vue3-apexcharts";

const pinia = createPinia();

const app = createApp(App);

// Plugins
app.use(pinia);
app.use(router);
app.use(VueApexCharts);

// Initialize auth store
const authStore = useAuthStore();
authStore.initialize();

app.mount("#app");
