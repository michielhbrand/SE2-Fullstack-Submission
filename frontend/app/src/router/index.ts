import { createRouter, createWebHistory } from 'vue-router'
import { authService } from '../services/auth'
import Login from '../views/Login.vue'
import Dashboard from '../views/Dashboard.vue'
import Welcome from '../views/Welcome.vue'
import Clients from '../views/Clients.vue'
import Invoices from '../views/Invoices.vue'

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
      meta: { requiresAuth: true },
    },
    {
      path: '/welcome',
      name: 'Welcome',
      component: Welcome,
      meta: { requiresAuth: true },
    },
    {
      path: '/clients',
      name: 'Clients',
      component: Clients,
      meta: { requiresAuth: true },
    },
    {
      path: '/invoices',
      name: 'Invoices',
      component: Invoices,
      meta: { requiresAuth: true },
    },
  ],
})

router.beforeEach((to, from, next) => {
  const isAuthenticated = authService.isAuthenticated()

  if (to.meta.requiresAuth && !isAuthenticated) {
    next('/login')
  } else if (to.path === '/login' && isAuthenticated) {
    next('/dashboard')
  } else {
    next()
  }
})

export default router
