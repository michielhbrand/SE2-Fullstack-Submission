import { createRouter, createWebHistory } from 'vue-router'
import { authService } from '../services/auth'
import Login from '../views/Login.vue'
import Dashboard from '../views/Dashboard.vue'
import Welcome from '../views/Welcome.vue'
import Clients from '../views/Clients.vue'
import Invoices from '../views/Invoices.vue'
import Quotes from '../views/Quotes.vue'
import Templates from '../views/Templates.vue'
import AdminDashboard from '../views/AdminDashboard.vue'

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
      path: '/admin',
      name: 'AdminDashboard',
      component: AdminDashboard,
      meta: { requiresAuth: true, requiresAdmin: true },
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
    {
      path: '/quotes',
      name: 'Quotes',
      component: Quotes,
      meta: { requiresAuth: true },
    },
    {
      path: '/templates',
      name: 'Templates',
      component: Templates,
      meta: { requiresAuth: true },
    },
  ],
})

router.beforeEach((to, from, next) => {
  const isAuthenticated = authService.isAuthenticated()
  const isAdmin = authService.isAdmin()

  if (to.meta.requiresAuth && !isAuthenticated) {
    next('/login')
  } else if (to.meta.requiresAdmin && !isAdmin) {
    next('/login')
  } else if (to.path === '/login' && isAuthenticated) {
    if (isAdmin) {
      next('/admin')
    } else {
      next('/dashboard')
    }
  } else {
    next()
  }
})

export default router
