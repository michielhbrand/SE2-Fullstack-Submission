<script setup lang="ts">
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { toast } from "vue-sonner";
import Button from "../components/ui/Button.vue";
import Card from "../components/ui/Card.vue";
import Input from "../components/ui/Input.vue";
import { paymentPlanService } from "../services/paymentPlans";
import type { PaymentPlanResponse } from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

const router = useRouter();
const plans = ref<PaymentPlanResponse[]>([]);
const isLoading = ref(false);
const editingId = ref<number | null>(null);
const editCost = ref<number>(0);
const isSaving = ref(false);

const fetchPlans = async () => {
  isLoading.value = true;
  try {
    plans.value = await paymentPlanService.getAll();
  } catch (error) {
    toast.error(getErrorMessage(error, "Failed to load payment plans"));
  } finally {
    isLoading.value = false;
  }
};

const startEdit = (plan: PaymentPlanResponse) => {
  editingId.value = plan.Id!;
  editCost.value = plan.MonthlyCostRand ?? 0;
};

const cancelEdit = () => {
  editingId.value = null;
};

const saveCost = async (plan: PaymentPlanResponse) => {
  if (editCost.value < 0) {
    toast.error("Monthly cost cannot be negative");
    return;
  }
  isSaving.value = true;
  try {
    await paymentPlanService.updateCost(plan.Id!, editCost.value);
    toast.success(`${plan.Name} plan cost updated to R${editCost.value}`);
    editingId.value = null;
    await fetchPlans();
  } catch (error) {
    toast.error(getErrorMessage(error, "Failed to update plan cost"));
  } finally {
    isSaving.value = false;
  }
};

const userLimitLabel = (maxUsers: number | undefined) =>
  maxUsers === -1 ? "Unlimited" : `${maxUsers} users`;

const planColor = (name: string | undefined) => {
  switch (name?.toLowerCase()) {
    case "basic":    return "bg-gray-100 text-gray-800 border-gray-300";
    case "advanced": return "bg-blue-100 text-blue-800 border-blue-300";
    case "ultimate": return "bg-yellow-100 text-yellow-800 border-yellow-400";
    default:         return "bg-gray-100 text-gray-800 border-gray-300";
  }
};

onMounted(fetchPlans);
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <header class="sticky top-0 z-50 bg-white border-b border-gray-200 shadow-sm">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between items-center h-16">
          <div class="flex items-center gap-4">
            <Button variant="outline" size="sm" @click="router.push('/dashboard')">
              <svg class="h-4 w-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
              </svg>
              Back
            </Button>
            <h1 class="text-2xl font-bold text-gray-900">Payment Plans</h1>
          </div>
        </div>
      </div>
    </header>

    <main class="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <p class="text-gray-600 mb-6">
        Manage the monthly costs for each subscription plan. User limits are system-defined and cannot be changed here.
      </p>

      <!-- Loading -->
      <div v-if="isLoading" class="flex justify-center py-12">
        <svg class="animate-spin h-8 w-8 text-gray-600" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
        </svg>
      </div>

      <!-- Plans table -->
      <Card v-else class="overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 border-b border-gray-200">
            <tr>
              <th class="text-left px-6 py-3 font-semibold text-gray-700">Plan</th>
              <th class="text-left px-6 py-3 font-semibold text-gray-700">User Limit</th>
              <th class="text-left px-6 py-3 font-semibold text-gray-700">Monthly Cost (R)</th>
              <th class="px-6 py-3"></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="plan in plans"
              :key="plan.Id"
              class="border-b border-gray-100 last:border-0"
            >
              <td class="px-6 py-4">
                <span :class="['inline-block px-3 py-1 rounded-full text-xs font-semibold border', planColor(plan.Name)]">
                  {{ plan.Name }}
                </span>
              </td>
              <td class="px-6 py-4 text-gray-700">{{ userLimitLabel(plan.MaxUsers) }}</td>
              <td class="px-6 py-4">
                <div v-if="editingId === plan.Id" class="flex items-center gap-2">
                  <Input
                    v-model.number="editCost"
                    type="number"
                    min="0"
                    step="50"
                    class="w-32"
                    @keyup.enter="saveCost(plan)"
                    @keyup.escape="cancelEdit"
                  />
                  <Button size="sm" @click="saveCost(plan)" :disabled="isSaving">
                    {{ isSaving ? "Saving..." : "Save" }}
                  </Button>
                  <Button size="sm" variant="outline" @click="cancelEdit" :disabled="isSaving">
                    Cancel
                  </Button>
                </div>
                <span v-else class="text-gray-900 font-medium">R {{ plan.MonthlyCostRand?.toLocaleString('en-ZA') }}</span>
              </td>
              <td class="px-6 py-4 text-right">
                <button
                  v-if="editingId !== plan.Id"
                  @click="startEdit(plan)"
                  class="text-blue-600 hover:text-blue-800 text-sm font-medium"
                >
                  Edit cost
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </Card>

      <p class="mt-4 text-xs text-gray-400">
        Plan names and user limits are system-defined. Only the monthly cost can be updated.
      </p>
    </main>
  </div>
</template>
