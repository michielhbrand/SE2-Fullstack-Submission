<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Skeleton } from '../components/ui/index'
import Layout from '../components/Layout.vue'
import { dashboardApi } from '../services/api'
import { getStatusLabel, getEventLabel } from '../utils/workflow'
import { useOrganizationStore } from '../stores/organization'
import { useOrganizationContext } from '../composables/useOrganizationContext'
import { toast } from 'vue-sonner'

const organizationStore = useOrganizationStore()
const { ensureOrganizationContext } = useOrganizationContext()

const loading = ref(true)
const data = ref<any>(null)

onMounted(async () => {
  await ensureOrganizationContext()
  await fetchDashboard()
})

const fetchDashboard = async () => {
  loading.value = true
  try {
    const orgId = organizationStore.currentOrganizationId
    if (!orgId) return
    data.value = await dashboardApi.getDashboard(orgId)
  } catch {
    toast.error('Failed to load dashboard')
  } finally {
    loading.value = false
  }
}

// ── Formatting ────────────────────────────────────────────────────────────────
const fmtShort = (amount: number) => {
  if (amount >= 1_000_000) return 'R\u00a0' + (amount / 1_000_000).toFixed(1) + 'M'
  if (amount >= 1_000)     return 'R\u00a0' + (amount / 1_000).toFixed(1) + 'k'
  return 'R\u00a0' + amount.toLocaleString('en-ZA', { minimumFractionDigits: 0, maximumFractionDigits: 0 })
}

const timeAgo = (dateStr: string) => {
  const diff = (Date.now() - new Date(dateStr).getTime()) / 1000
  if (diff < 60)    return 'just now'
  if (diff < 3600)  return `${Math.floor(diff / 60)}m ago`
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`
  return `${Math.floor(diff / 86400)}d ago`
}

// ── Revenue bar chart ─────────────────────────────────────────────────────────
const revenueSeries = computed(() => [{
  name: 'Revenue',
  data: data.value?.revenueByMonth?.map((m: any) => m.amount) ?? [],
}])

const revenueOptions = computed(() => ({
  chart: { toolbar: { show: false }, background: 'transparent' },
  colors: ['rgba(124,58,237,0.85)'],
  plotOptions: { bar: { borderRadius: 6, columnWidth: '55%' } },
  dataLabels: { enabled: false },
  legend: { show: false },
  xaxis: {
    categories: data.value?.revenueByMonth?.map((m: any) => m.month) ?? [],
    axisBorder: { show: false },
    axisTicks: { show: false },
  },
  yaxis: {
    labels: { formatter: (v: number) => fmtShort(v) },
  },
  grid: { borderColor: 'rgba(0,0,0,0.05)', xaxis: { lines: { show: false } } },
  tooltip: { y: { formatter: (v: number) => fmtShort(v) } },
}))

// ── Invoice status donut ──────────────────────────────────────────────────────
const statusSeries = computed(() => {
  const b = data.value?.invoiceStatusBreakdown
  return b ? [b.paid, b.overdue, b.notPaid] : [0, 0, 0]
})

const statusOptions = {
  labels: ['Paid', 'Overdue', 'Not Paid'],
  colors: ['#059669', '#dc2626', '#d1d5db'],
  chart: { background: 'transparent' },
  plotOptions: { pie: { donut: { size: '68%' } } },
  dataLabels: { enabled: false },
  legend: {
    position: 'bottom' as const,
    fontSize: '12px',
    itemMargin: { horizontal: 8 },
  },
  stroke: { width: 0 },
}

// ── Workflow pipeline ─────────────────────────────────────────────────────────
const statusOrder = [
  'Draft', 'PendingApproval', 'Approved', 'Rejected',
  'InvoiceCreated', 'SentForPayment', 'Paid', 'Cancelled', 'Terminated'
]

const workflowPipeline = computed(() => {
  const breakdown: any[] = data.value?.workflowStatusBreakdown ?? []
  const max = Math.max(...breakdown.map((s: any) => s.count), 1)
  return statusOrder
    .map(status => {
      const entry = breakdown.find((s: any) => s.status === status)
      return entry ? { ...entry, pct: Math.round((entry.count / max) * 100) } : null
    })
    .filter(Boolean)
})

const workflowStatusColor = (status: string): string => {
  const map: Record<string, string> = {
    Draft: 'bg-gray-400',
    PendingApproval: 'bg-amber-400',
    Approved: 'bg-green-400',
    Rejected: 'bg-red-400',
    InvoiceCreated: 'bg-blue-400',
    SentForPayment: 'bg-violet-400',
    Paid: 'bg-emerald-500',
    Cancelled: 'bg-orange-400',
    Terminated: 'bg-red-600',
  }
  return map[status] ?? 'bg-gray-400'
}

// ── Top clients ───────────────────────────────────────────────────────────────
const maxClientRevenue = computed(() => {
  const clients: any[] = data.value?.topClients ?? []
  return Math.max(...clients.map((c: any) => c.revenue), 1)
})

// ── Activity dots ─────────────────────────────────────────────────────────────
const activityDotColor = (eventType: string): string => {
  const map: Record<string, string> = {
    MarkedAsPaid: 'bg-emerald-500',
    SentForPayment: 'bg-violet-500',
    Approved: 'bg-green-500',
    Rejected: 'bg-red-500',
    SentForApproval: 'bg-amber-500',
    OverdueReminderSent: 'bg-orange-500',
    Terminated: 'bg-red-700',
    Cancelled: 'bg-orange-400',
  }
  return map[eventType] ?? 'bg-gray-400'
}
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">

        <!-- Header -->
        <div class="mb-8 flex items-center justify-between">
          <div>
            <h2 class="text-3xl font-bold text-gray-900">Dashboard</h2>
            <p class="mt-1 text-sm text-gray-500">Business overview for your organisation</p>
          </div>
          <button
            @click="fetchDashboard"
            :disabled="loading"
            title="Refresh"
            class="text-gray-500 hover:text-gray-800 disabled:opacity-40 transition-colors"
          >
            <svg class="h-5 w-5" :class="{ 'animate-spin': loading }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
          </button>
        </div>

        <!-- Loading skeletons -->
        <template v-if="loading">
          <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <Skeleton v-for="n in 4" :key="n" class="h-28 rounded-xl" />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-6">
            <Skeleton class="lg:col-span-2 h-64 rounded-xl" />
            <Skeleton class="h-64 rounded-xl" />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-6">
            <Skeleton class="h-52 rounded-xl" />
            <Skeleton class="h-52 rounded-xl" />
          </div>
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">
            <Skeleton class="h-64 rounded-xl" />
            <Skeleton class="h-64 rounded-xl" />
          </div>
        </template>

        <template v-else-if="data">

          <!-- ── Row 1: KPI cards ──────────────────────────────────────────── -->
          <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <div class="flex items-center justify-between mb-3">
                <span class="text-xs font-semibold uppercase tracking-wide text-gray-400">Total Revenue</span>
                <span class="w-8 h-8 rounded-lg bg-emerald-50 flex items-center justify-center">
                  <svg class="w-4 h-4 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
                </span>
              </div>
              <p class="text-2xl font-bold text-gray-900 truncate">{{ fmtShort(data.kpis.totalRevenue) }}</p>
              <p class="text-xs text-gray-400 mt-1">All-time paid invoices</p>
              <div class="mt-3 h-1 rounded-full bg-emerald-500"></div>
            </div>

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <div class="flex items-center justify-between mb-3">
                <span class="text-xs font-semibold uppercase tracking-wide text-gray-400">Outstanding</span>
                <span class="w-8 h-8 rounded-lg bg-amber-50 flex items-center justify-center">
                  <svg class="w-4 h-4 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
                </span>
              </div>
              <p class="text-2xl font-bold text-gray-900 truncate">{{ fmtShort(data.kpis.outstandingAmount) }}</p>
              <p class="text-xs text-gray-400 mt-1">Unpaid &amp; awaiting</p>
              <div class="mt-3 h-1 rounded-full bg-amber-400"></div>
            </div>

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <div class="flex items-center justify-between mb-3">
                <span class="text-xs font-semibold uppercase tracking-wide text-gray-400">Overdue</span>
                <span class="w-8 h-8 rounded-lg bg-red-50 flex items-center justify-center">
                  <svg class="w-4 h-4 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/></svg>
                </span>
              </div>
              <p class="text-2xl font-bold text-red-600 truncate">{{ fmtShort(data.kpis.overdueAmount) }}</p>
              <p class="text-xs text-gray-400 mt-1">Past due date, unpaid</p>
              <div class="mt-3 h-1 rounded-full bg-red-500"></div>
            </div>

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <div class="flex items-center justify-between mb-3">
                <span class="text-xs font-semibold uppercase tracking-wide text-gray-400">Active Workflows</span>
                <span class="w-8 h-8 rounded-lg bg-violet-50 flex items-center justify-center">
                  <svg class="w-4 h-4 text-violet-600" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/></svg>
                </span>
              </div>
              <p class="text-2xl font-bold text-gray-900">{{ data.kpis.activeWorkflows }}</p>
              <p class="text-xs text-gray-400 mt-1">
                {{ data.kpis.totalInvoices }} invoices &middot; {{ data.kpis.totalClients }} clients
              </p>
              <div class="mt-3 h-1 rounded-full bg-violet-500"></div>
            </div>

          </div>

          <!-- ── Row 2: Revenue chart + Invoice status ──────────────────────── -->
          <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 mb-6">

            <div class="lg:col-span-2 bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <h3 class="text-sm font-semibold text-gray-700 mb-4">Revenue — Last 6 Months</h3>
              <div style="height: 210px;">
                <apexchart type="bar" height="210" :series="revenueSeries" :options="revenueOptions" />
              </div>
            </div>

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5 flex flex-col">
              <h3 class="text-sm font-semibold text-gray-700 mb-2">Invoice Breakdown</h3>
              <div class="flex-1 flex items-center justify-center">
                <div style="height: 180px; width: 180px;">
                  <apexchart type="donut" height="180" :series="statusSeries" :options="statusOptions" />
                </div>
              </div>
              <div class="grid grid-cols-3 gap-1 pt-2 border-t border-gray-100">
                <div class="text-center py-2">
                  <p class="text-xl font-bold text-emerald-600">{{ data.invoiceStatusBreakdown.paid }}</p>
                  <p class="text-xs text-gray-400">Paid</p>
                </div>
                <div class="text-center py-2 border-x border-gray-100">
                  <p class="text-xl font-bold text-red-600">{{ data.invoiceStatusBreakdown.overdue }}</p>
                  <p class="text-xs text-gray-400">Overdue</p>
                </div>
                <div class="text-center py-2">
                  <p class="text-xl font-bold text-gray-500">{{ data.invoiceStatusBreakdown.notPaid }}</p>
                  <p class="text-xs text-gray-400">Not Paid</p>
                </div>
              </div>
            </div>

          </div>

          <!-- ── Row 3: Top clients + Workflow pipeline ────────────────────── -->
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-6">

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <h3 class="text-sm font-semibold text-gray-700 mb-4">Top Clients by Revenue</h3>
              <div v-if="!data.topClients.length" class="text-sm text-gray-400 py-6 text-center">
                No paid invoices yet
              </div>
              <div v-else class="space-y-4">
                <div v-for="(client, i) in data.topClients" :key="i">
                  <div class="flex items-center justify-between text-sm mb-1.5">
                    <span class="font-medium text-gray-700 truncate">{{ client.clientName }}</span>
                    <span class="text-xs text-gray-500 shrink-0 ml-2">
                      {{ fmtShort(client.revenue) }}
                      <span class="text-gray-400">&middot; {{ client.invoiceCount }} inv</span>
                    </span>
                  </div>
                  <div class="h-2 w-full bg-gray-100 rounded-full overflow-hidden">
                    <div
                      class="h-full rounded-full bg-violet-500"
                      :style="{ width: Math.round((client.revenue / maxClientRevenue) * 100) + '%' }"
                    ></div>
                  </div>
                </div>
              </div>
            </div>

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <h3 class="text-sm font-semibold text-gray-700 mb-4">Workflow Pipeline</h3>
              <div v-if="!workflowPipeline.length" class="text-sm text-gray-400 py-6 text-center">
                No workflows yet
              </div>
              <div v-else class="space-y-3">
                <div v-for="item in workflowPipeline" :key="item.status" class="flex items-center gap-3">
                  <span class="text-xs text-gray-500 w-32 shrink-0 truncate">{{ getStatusLabel(item.status) }}</span>
                  <div class="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
                    <div
                      class="h-full rounded-full"
                      :class="workflowStatusColor(item.status)"
                      :style="{ width: item.pct + '%' }"
                    ></div>
                  </div>
                  <span class="text-xs font-semibold text-gray-600 w-5 text-right shrink-0">{{ item.count }}</span>
                </div>
              </div>
            </div>

          </div>

          <!-- ── Row 4: Recent activity + Overdue invoices ──────────────────── -->
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-4">

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <h3 class="text-sm font-semibold text-gray-700 mb-4">Recent Activity</h3>
              <div v-if="!data.recentActivity.length" class="text-sm text-gray-400 py-6 text-center">
                No recent activity
              </div>
              <ol v-else class="relative border-l border-gray-100 ml-2">
                <li
                  v-for="(event, i) in data.recentActivity"
                  :key="i"
                  class="pl-5 pb-4 last:pb-0"
                >
                  <span
                    class="absolute -left-1.5 mt-0.5 w-3 h-3 rounded-full border-2 border-white"
                    :class="activityDotColor(event.eventType)"
                  ></span>
                  <div class="flex items-start justify-between gap-3">
                    <div class="min-w-0">
                      <p class="text-sm font-medium text-gray-700 leading-snug">{{ getEventLabel(event.eventType) }}</p>
                      <p class="text-xs text-gray-400 mt-0.5 truncate">{{ event.clientName }} &middot; WF-{{ event.workflowId }}</p>
                    </div>
                    <span class="text-xs text-gray-400 shrink-0">{{ timeAgo(event.occurredAt) }}</span>
                  </div>
                </li>
              </ol>
            </div>

            <div class="bg-white rounded-xl shadow-sm border border-gray-100 p-5">
              <h3 class="text-sm font-semibold text-gray-700 mb-4">Overdue Invoices</h3>
              <div v-if="!data.overdueInvoices.length" class="flex flex-col items-center justify-center py-8 gap-2 text-gray-400">
                <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
                <p class="text-sm">All invoices are on time</p>
              </div>
              <div v-else class="space-y-2">
                <div
                  v-for="inv in data.overdueInvoices"
                  :key="inv.invoiceId"
                  class="flex items-center justify-between p-3 rounded-lg bg-gray-50 hover:bg-red-50 transition-colors group"
                >
                  <div class="min-w-0">
                    <p class="text-sm font-medium text-gray-700 truncate">{{ inv.clientName }}</p>
                    <p class="text-xs text-gray-400 mt-0.5">
                      Inv #{{ inv.invoiceId }} &middot; due {{ new Date(inv.payByDate).toLocaleDateString() }}
                    </p>
                  </div>
                  <div class="text-right ml-3 shrink-0">
                    <p class="text-sm font-semibold text-gray-800">{{ fmtShort(inv.amount) }}</p>
                    <span class="text-xs font-medium px-1.5 py-0.5 rounded bg-red-100 text-red-700 group-hover:bg-red-200">
                      {{ inv.daysOverdue }}d overdue
                    </span>
                  </div>
                </div>
              </div>
            </div>

          </div>

        </template>

      </div>
    </div>
  </Layout>
</template>
