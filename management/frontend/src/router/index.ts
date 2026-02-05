import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import Login from '../views/Login.vue'
import Dashboard from '../views/Dashboard.vue'
import Organizations from '../views/Organizations.vue'
import OrganizationDetails from '../views/OrganizationDetails.vue'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      redirect: '/dashboard',
    },
    {
      path: '/login',
      name: 'Login',
      component: Login,
      meta: { requiresAuth: false },
    },
    {
      path: '/dashboard',
      name: 'Dashboard',
      component: Dashboard,
      meta: { requiresAuth: true, requiresSystemAdmin: true },
    },
    {
      path: '/organizations',
      name: 'Organizations',
      component: Organizations,
      meta: { requiresAuth: true, requiresSystemAdmin: true },
    },
    {
      path: '/organizations/:id',
      name: 'OrganizationDetails',
      component: OrganizationDetails,
      meta: { requiresAuth: true, requiresSystemAdmin: true },
    },
  ],
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  const isAuthenticated = authStore.isAuthenticated
  const isSystemAdmin = authStore.isSystemAdmin

  if (to.meta.requiresAuth && !isAuthenticated) {
    next('/login')
  } else if (to.meta.requiresSystemAdmin && !isSystemAdmin) {
    next('/login')
  } else if (to.path === '/login' && isAuthenticated && isSystemAdmin) {
    next('/dashboard')
  } else {
    next()
  }
})

export default router
