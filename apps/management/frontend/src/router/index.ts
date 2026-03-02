import { createRouter, createWebHistory } from "vue-router";
import { useAuthStore } from "../stores/auth";
import Login from "../views/Login.vue";
import AppLayout from "../components/AppLayout.vue";

const Dashboard = () => import("../views/Dashboard.vue");
const Organizations = () => import("../views/Organizations.vue");
const OrganizationDetails = () => import("../views/OrganizationDetails.vue");
const PaymentPlans = () => import("../views/PaymentPlans.vue");

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/login",
      name: "Login",
      component: Login,
      meta: { requiresAuth: false },
    },
    {
      path: "/",
      component: AppLayout,
      redirect: "/dashboard",
      meta: { requiresAuth: true, requiresSystemAdmin: true },
      children: [
        {
          path: "dashboard",
          name: "Dashboard",
          component: Dashboard,
        },
        {
          path: "organizations",
          name: "Organizations",
          component: Organizations,
        },
        {
          path: "organizations/:id",
          name: "OrganizationDetails",
          component: OrganizationDetails,
        },
        {
          path: "payment-plans",
          name: "PaymentPlans",
          component: PaymentPlans,
        },
      ],
    },
  ],
});

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore();
  const isAuthenticated = authStore.isAuthenticated;
  const isSystemAdmin = authStore.isSystemAdmin;

  if (to.meta.requiresAuth && !isAuthenticated) {
    next("/login");
  } else if (to.meta.requiresSystemAdmin && !isSystemAdmin) {
    next("/login");
  } else if (to.path === "/login" && isAuthenticated && isSystemAdmin) {
    next("/dashboard");
  } else {
    next();
  }
});

export default router;
