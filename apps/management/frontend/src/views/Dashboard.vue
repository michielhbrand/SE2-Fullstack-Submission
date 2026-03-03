<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import { useRouter } from "vue-router";
import { toast } from "vue-sonner";
import VueApexCharts from "vue3-apexcharts";
import {
  TrendingUp,
  Building2,
  Users,
  Banknote,
  Plus,
  ArrowRight,
  CheckCircle,
  XCircle,
} from "lucide-vue-next";
import Card from "../components/ui/Card.vue";
import Button from "../components/ui/Button.vue";
import CreateOrganizationDialog from "../components/CreateOrganizationDialog.vue";
import { organizationService } from "../services/organizations";
import type { OrganizationResponse } from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

const router = useRouter();
const organizations = ref<OrganizationResponse[]>([]);
const isLoading = ref(true);
const isCreateDialogOpen = ref(false);

const fetchOrganizations = async () => {
  isLoading.value = true;
  try {
    organizations.value = await organizationService.getAll();
  } catch (error) {
    toast.error(getErrorMessage(error, "Failed to fetch organizations"));
  } finally {
    isLoading.value = false;
  }
};

const handleOrganizationCreated = () => {
  fetchOrganizations();
};

// ─── KPI Computeds ───────────────────────────────────────────────────────────

const activeOrgs = computed(() => organizations.value.filter((o) => o.Active));
const inactiveOrgs = computed(() =>
  organizations.value.filter((o) => !o.Active)
);

const totalMRR = computed(() =>
  activeOrgs.value.reduce(
    (sum, o) => sum + (o.PaymentPlan?.MonthlyCostRand ?? 0),
    0
  )
);

const totalARR = computed(() => totalMRR.value * 12);

const totalMembers = computed(() =>
  organizations.value.reduce((sum, o) => sum + (o.MemberCount ?? 0), 0)
);

const avgRevenuePerOrg = computed(() =>
  activeOrgs.value.length > 0
    ? Math.round(totalMRR.value / activeOrgs.value.length)
    : 0
);

// ─── Revenue by Plan ─────────────────────────────────────────────────────────

interface PlanSummary {
  name: string;
  count: number;
  revenue: number;
  color: string;
}

const PLAN_COLORS: Record<string, string> = {
  Basic: "#6B7280",
  Advanced: "#3B82F6",
  Ultimate: "#F59E0B",
};

const planSummaries = computed((): PlanSummary[] => {
  const planMap = new Map<string, { count: number; revenue: number }>();
  activeOrgs.value.forEach((org) => {
    const name = org.PaymentPlan?.Name ?? "Unknown";
    const existing = planMap.get(name) ?? { count: 0, revenue: 0 };
    planMap.set(name, {
      count: existing.count + 1,
      revenue: existing.revenue + (org.PaymentPlan?.MonthlyCostRand ?? 0),
    });
  });
  return [...planMap.entries()].map(([name, data]) => ({
    name,
    count: data.count,
    revenue: data.revenue,
    color: PLAN_COLORS[name] ?? "#9CA3AF",
  }));
});

// ─── Org Growth (last 6 months) ───────────────────────────────────────────────

const orgGrowthData = computed(() => {
  const months: { label: string; key: string }[] = [];
  const now = new Date();
  for (let i = 5; i >= 0; i--) {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
    const label = d.toLocaleDateString("en-US", {
      month: "short",
      year: "2-digit",
    });
    months.push({ key, label });
  }
  return {
    categories: months.map((m) => m.label),
    data: months.map(
      (m) =>
        organizations.value.filter((org) => {
          if (!org.CreatedAt) return false;
          const d = new Date(org.CreatedAt);
          const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
          return key === m.key;
        }).length
    ),
  };
});

// ─── Recent orgs ─────────────────────────────────────────────────────────────

const recentOrgs = computed(() =>
  [...organizations.value]
    .sort(
      (a, b) =>
        new Date(b.CreatedAt ?? 0).getTime() -
        new Date(a.CreatedAt ?? 0).getTime()
    )
    .slice(0, 6)
);

// ─── Chart configs ────────────────────────────────────────────────────────────

const revenueDonutOptions = computed(() => {
  const names = planSummaries.value.map((p) => p.name);
  const colors = planSummaries.value.map((p) => p.color);
  const mrrFormatted = `R ${totalMRR.value.toLocaleString("en-ZA")}`;
  return {
    chart: {
      type: "donut" as const,
      background: "transparent",
      fontFamily: "inherit",
    },
    labels: names,
    colors,
    legend: {
      position: "bottom" as const,
      fontFamily: "inherit",
      fontSize: "13px",
      markers: { size: 10 },
    },
    stroke: { width: 0 },
    plotOptions: {
      pie: {
        donut: {
          size: "70%",
          labels: {
            show: true,
            name: {
              show: true,
              fontSize: "13px",
              color: "#6B7280",
              offsetY: -4,
            },
            value: {
              show: true,
              fontSize: "22px",
              fontWeight: "700",
              color: "#111827",
              formatter: (val: string) =>
                `R ${Number(val).toLocaleString("en-ZA")}`,
            },
            total: {
              show: true,
              label: "Monthly",
              fontWeight: "600",
              color: "#6B7280",
              fontSize: "13px",
              formatter: () => mrrFormatted,
            },
          },
        },
      },
    },
    dataLabels: { enabled: false },
    tooltip: {
      y: {
        formatter: (val: number) => `R ${val.toLocaleString("en-ZA")}/mo`,
      },
    },
  };
});

const revenueDonutSeries = computed(() =>
  planSummaries.value.map((p) => p.revenue)
);

const growthBarOptions = computed(() => ({
  chart: {
    type: "bar" as const,
    toolbar: { show: false },
    background: "transparent",
    fontFamily: "inherit",
  },
  colors: ["#3B82F6"],
  xaxis: {
    categories: orgGrowthData.value.categories,
    axisBorder: { show: false },
    axisTicks: { show: false },
    labels: { style: { colors: "#9CA3AF", fontSize: "12px" } },
  },
  yaxis: {
    labels: { style: { colors: "#9CA3AF", fontSize: "12px" } },
    tickAmount: 4,
    min: 0,
    forceNiceScale: true,
  },
  plotOptions: {
    bar: { borderRadius: 5, columnWidth: "50%" },
  },
  dataLabels: { enabled: false },
  grid: {
    borderColor: "#E5E7EB",
    strokeDashArray: 4,
    yaxis: { lines: { show: true } },
    xaxis: { lines: { show: false } },
  },
  tooltip: {
    y: {
      formatter: (val: number) => `${val} org${val !== 1 ? "s" : ""}`,
    },
  },
}));

const growthBarSeries = computed(() => [
  { name: "New Organizations", data: orgGrowthData.value.data },
]);

// ─── Helpers ──────────────────────────────────────────────────────────────────

const formatCurrency = (val: number) =>
  `R ${val.toLocaleString("en-ZA", { minimumFractionDigits: 0 })}`;

const planBadgeClass = (name?: string) => {
  switch (name?.toLowerCase()) {
    case "basic":
      return "bg-gray-100 text-gray-700 border border-gray-200";
    case "advanced":
      return "bg-blue-100 text-blue-700 border border-blue-200";
    case "ultimate":
      return "bg-amber-100 text-amber-700 border border-amber-200";
    default:
      return "bg-gray-100 text-gray-600 border border-gray-200";
  }
};

const today = new Date().toLocaleDateString("en-ZA", {
  weekday: "long",
  year: "numeric",
  month: "long",
  day: "numeric",
});

onMounted(fetchOrganizations);
</script>

<template>
  <div class="min-h-full bg-gray-50">
    <!-- Page header -->
    <div class="bg-white border-b border-gray-200 px-8 py-6">
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Business Overview</h1>
          <p class="text-sm text-gray-500 mt-0.5">{{ today }}</p>
        </div>
        <Button @click="isCreateDialogOpen = true">
          <Plus class="h-4 w-4 mr-2" />
          New Organization
        </Button>
      </div>
    </div>

    <div class="px-8 py-6 space-y-6">
      <!-- Loading skeleton -->
      <div v-if="isLoading" class="flex items-center justify-center py-24">
        <div
          class="h-8 w-8 animate-spin rounded-full border-4 border-blue-600 border-r-transparent"
        />
      </div>

      <template v-else>
        <!-- ─── KPI Cards ─────────────────────────────────────────────── -->
        <div class="grid grid-cols-2 xl:grid-cols-4 gap-4">
          <!-- MRR -->
          <Card class="p-5">
            <div class="flex items-start justify-between">
              <div>
                <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Monthly Revenue
                </p>
                <p class="text-2xl font-bold text-gray-900 mt-2">
                  {{ formatCurrency(totalMRR) }}
                </p>
                <p class="text-xs text-gray-400 mt-1">
                  ARR {{ formatCurrency(totalARR) }}
                </p>
              </div>
              <div
                class="h-10 w-10 rounded-xl bg-green-100 flex items-center justify-center flex-shrink-0"
              >
                <Banknote class="h-5 w-5 text-green-600" />
              </div>
            </div>
          </Card>

          <!-- Active Orgs -->
          <Card class="p-5">
            <div class="flex items-start justify-between">
              <div>
                <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Active Organizations
                </p>
                <p class="text-2xl font-bold text-gray-900 mt-2">
                  {{ activeOrgs.length }}
                </p>
                <p class="text-xs text-gray-400 mt-1">
                  {{ inactiveOrgs.length }} inactive &middot;
                  {{ organizations.length }} total
                </p>
              </div>
              <div
                class="h-10 w-10 rounded-xl bg-blue-100 flex items-center justify-center flex-shrink-0"
              >
                <Building2 class="h-5 w-5 text-blue-600" />
              </div>
            </div>
          </Card>

          <!-- Total Members -->
          <Card class="p-5">
            <div class="flex items-start justify-between">
              <div>
                <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Total Members
                </p>
                <p class="text-2xl font-bold text-gray-900 mt-2">
                  {{ totalMembers }}
                </p>
                <p class="text-xs text-gray-400 mt-1">Across all organizations</p>
              </div>
              <div
                class="h-10 w-10 rounded-xl bg-purple-100 flex items-center justify-center flex-shrink-0"
              >
                <Users class="h-5 w-5 text-purple-600" />
              </div>
            </div>
          </Card>

          <!-- Avg Revenue / Org -->
          <Card class="p-5">
            <div class="flex items-start justify-between">
              <div>
                <p class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                  Avg Revenue / Org
                </p>
                <p class="text-2xl font-bold text-gray-900 mt-2">
                  {{ formatCurrency(avgRevenuePerOrg) }}
                </p>
                <p class="text-xs text-gray-400 mt-1">Per active organization</p>
              </div>
              <div
                class="h-10 w-10 rounded-xl bg-amber-100 flex items-center justify-center flex-shrink-0"
              >
                <TrendingUp class="h-5 w-5 text-amber-600" />
              </div>
            </div>
          </Card>
        </div>

        <!-- ─── Charts row ─────────────────────────────────────────────── -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <!-- Revenue by Plan donut -->
          <Card class="p-5">
            <h2 class="text-sm font-semibold text-gray-700 mb-1">
              Revenue by Plan
            </h2>
            <p class="text-xs text-gray-400 mb-3">Active subscriptions</p>
            <div
              v-if="revenueDonutSeries.length === 0"
              class="flex items-center justify-center h-52 text-sm text-gray-400"
            >
              No active organizations
            </div>
            <VueApexCharts
              v-else
              type="donut"
              height="240"
              :options="revenueDonutOptions"
              :series="revenueDonutSeries"
            />
          </Card>

          <!-- Org growth bar chart (2/3 width) -->
          <Card class="p-5 lg:col-span-2">
            <h2 class="text-sm font-semibold text-gray-700 mb-1">
              Organization Growth
            </h2>
            <p class="text-xs text-gray-400 mb-3">New organizations per month (last 6 months)</p>
            <VueApexCharts
              type="bar"
              height="220"
              :options="growthBarOptions"
              :series="growthBarSeries"
            />
          </Card>
        </div>

        <!-- ─── Plan summary + Recent orgs ────────────────────────────── -->
        <div class="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <!-- Plan breakdown table -->
          <Card class="p-5">
            <h2 class="text-sm font-semibold text-gray-700 mb-4">
              Plan Breakdown
            </h2>
            <div
              v-if="planSummaries.length === 0"
              class="text-sm text-gray-400 py-6 text-center"
            >
              No active subscriptions
            </div>
            <div v-else class="space-y-3">
              <div
                v-for="plan in planSummaries"
                :key="plan.name"
                class="flex items-center gap-3"
              >
                <div
                  class="h-2.5 w-2.5 rounded-full flex-shrink-0"
                  :style="{ backgroundColor: plan.color }"
                />
                <div class="flex-1 min-w-0">
                  <div class="flex items-center justify-between mb-1">
                    <span class="text-sm font-medium text-gray-700 truncate">
                      {{ plan.name }}
                    </span>
                    <span class="text-xs text-gray-500 ml-2 flex-shrink-0">
                      {{ plan.count }} org{{ plan.count !== 1 ? "s" : "" }}
                    </span>
                  </div>
                  <div class="flex items-center justify-between">
                    <div class="flex-1 bg-gray-100 rounded-full h-1.5 mr-2">
                      <div
                        class="h-1.5 rounded-full"
                        :style="{
                          width:
                            totalMRR > 0
                              ? `${Math.round((plan.revenue / totalMRR) * 100)}%`
                              : '0%',
                          backgroundColor: plan.color,
                        }"
                      />
                    </div>
                    <span class="text-xs font-medium text-gray-600 flex-shrink-0">
                      {{ formatCurrency(plan.revenue) }}
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Total row -->
            <div
              v-if="planSummaries.length > 0"
              class="mt-4 pt-4 border-t border-gray-100 flex items-center justify-between"
            >
              <span class="text-xs font-semibold text-gray-500 uppercase tracking-wide">
                Total MRR
              </span>
              <span class="text-sm font-bold text-gray-900">
                {{ formatCurrency(totalMRR) }}
              </span>
            </div>
          </Card>

          <!-- Recent Organizations -->
          <Card class="p-5 lg:col-span-2">
            <div class="flex items-center justify-between mb-4">
              <div>
                <h2 class="text-sm font-semibold text-gray-700">
                  Recent Organizations
                </h2>
                <p class="text-xs text-gray-400 mt-0.5">Last added</p>
              </div>
              <button
                @click="router.push('/organizations')"
                class="flex items-center gap-1 text-xs font-medium text-blue-600 hover:text-blue-700 transition-colors"
              >
                View all
                <ArrowRight class="h-3.5 w-3.5" />
              </button>
            </div>

            <div
              v-if="recentOrgs.length === 0"
              class="text-sm text-gray-400 py-6 text-center"
            >
              No organizations yet
            </div>
            <div v-else class="overflow-x-auto -mx-1">
              <table class="w-full text-sm">
                <thead>
                  <tr class="border-b border-gray-100">
                    <th
                      class="text-left text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2 px-1"
                    >
                      Name
                    </th>
                    <th
                      class="text-left text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2 px-1"
                    >
                      Plan
                    </th>
                    <th
                      class="text-right text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2 px-1"
                    >
                      Members
                    </th>
                    <th
                      class="text-right text-xs font-semibold text-gray-400 uppercase tracking-wide pb-2 px-1"
                    >
                      Status
                    </th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="org in recentOrgs"
                    :key="org.Id"
                    class="border-b border-gray-50 last:border-0 hover:bg-gray-50 cursor-pointer transition-colors"
                    @click="router.push(`/organizations/${org.Id}`)"
                  >
                    <td class="py-2.5 px-1">
                      <p class="font-medium text-gray-800 truncate max-w-[140px]">
                        {{ org.Name }}
                      </p>
                      <p class="text-xs text-gray-400">
                        {{ new Date(org.CreatedAt!).toLocaleDateString("en-ZA") }}
                      </p>
                    </td>
                    <td class="py-2.5 px-1">
                      <span
                        :class="[
                          'px-2 py-0.5 text-xs font-semibold rounded-full',
                          planBadgeClass(org.PaymentPlan?.Name),
                        ]"
                      >
                        {{ org.PaymentPlan?.Name ?? "—" }}
                      </span>
                    </td>
                    <td class="py-2.5 px-1 text-right text-gray-600 font-medium">
                      {{ org.MemberCount ?? 0 }}
                    </td>
                    <td class="py-2.5 px-1 text-right">
                      <span
                        class="inline-flex items-center gap-1 text-xs font-medium"
                        :class="
                          org.Active ? 'text-green-600' : 'text-red-500'
                        "
                      >
                        <CheckCircle v-if="org.Active" class="h-3.5 w-3.5" />
                        <XCircle v-else class="h-3.5 w-3.5" />
                        {{ org.Active ? "Active" : "Inactive" }}
                      </span>
                    </td>
                  </tr>
                </tbody>
              </table>
            </div>
          </Card>
        </div>
      </template>
    </div>

    <CreateOrganizationDialog
      v-model:open="isCreateDialogOpen"
      @success="handleOrganizationCreated"
    />
  </div>
</template>
