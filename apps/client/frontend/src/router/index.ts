import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import Login from '../views/Login.vue'
import Dashboard from '../views/Dashboard.vue'
import Welcome from '../views/Welcome.vue'
import Clients from '../views/Clients.vue'
import Invoices from '../views/Invoices.vue'
import Quotes from '../views/Quotes.vue'
import Templates from '../views/Templates.vue'
import Workflows from '../views/Workflows.vue'
import WorkflowDetail from '../views/WorkflowDetail.vue'
import AdminDashboard from '../views/AdminDashboard.vue'
import Users from '../views/admin/Users.vue'
import PaymentDetails from '../views/admin/PaymentDetails.vue'
import EditOrganization from '../views/admin/EditOrganization.vue'

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
      component: AdminDashboard,
      meta: { requiresAuth: true, requiresAdmin: true },
      redirect: '/admin/users',
      children: [
        {
          path: 'users',
          name: 'AdminUsers',
          component: Users,
          meta: { requiresAuth: true, requiresAdmin: true },
        },
        {
          path: 'payment-details',
          name: 'PaymentDetails',
          component: PaymentDetails,
          meta: { requiresAuth: true, requiresAdmin: true },
        },
        {
          path: 'edit-organization',
          name: 'EditOrganization',
          component: EditOrganization,
          meta: { requiresAuth: true, requiresAdmin: true },
        },
      ],
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
    {
      path: '/workflows',
      name: 'Workflows',
      component: Workflows,
      meta: { requiresAuth: true },
    },
    {
      path: '/workflows/:id',
      name: 'WorkflowDetail',
      component: WorkflowDetail,
      meta: { requiresAuth: true },
    },
  ],
})

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  const isAuthenticated = authStore.isAuthenticated
  const isAdmin = authStore.isAdmin

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
