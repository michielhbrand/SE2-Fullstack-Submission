import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import Login from '../views/Login.vue'

const Dashboard = () => import('../views/Dashboard.vue')
const Welcome = () => import('../views/Welcome.vue')
const Clients = () => import('../views/Clients.vue')
const Invoices = () => import('../views/Invoices.vue')
const Quotes = () => import('../views/Quotes.vue')
const Templates = () => import('../views/Templates.vue')
const Workflows = () => import('../views/Workflows.vue')
const WorkflowDetail = () => import('../views/WorkflowDetail.vue')
const AdminDashboard = () => import('../views/AdminDashboard.vue')
const Users = () => import('../views/admin/Users.vue')
const PaymentDetails = () => import('../views/admin/PaymentDetails.vue')
const EditOrganization = () => import('../views/admin/EditOrganization.vue')

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

router.beforeEach((to, _from, next) => {
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
