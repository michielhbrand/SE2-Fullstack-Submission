<script setup lang="ts">
import { ref, onMounted } from "vue";
import { toast } from "vue-sonner";
import Button from "../components/ui/Button.vue";
import Card from "../components/ui/Card.vue";
import Input from "../components/ui/Input.vue";
import { paymentPlanService } from "../services/paymentPlans";
import type { PaymentPlanResponse } from "../api/generated/api-client";
import { getErrorMessage } from "../lib/error-utils";

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
    toast.success(`${plan.Name} plan updated to R${editCost.value}`);
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

const planBadgeClass = (name: string | undefined) => {
  switch (name?.toLowerCase()) {
    case "basic":
      return "bg-gray-100 text-gray-700 border border-gray-200";
    case "advanced":
      return "bg-blue-100 text-blue-700 border border-blue-200";
    case "ultimate":
      return "bg-amber-100 text-amber-700 border border-amber-200";
    default:
      return "bg-gray-100 text-gray-700 border border-gray-200";
  }
};

onMounted(fetchPlans);
</script>

<template>
  <div class="min-h-full bg-gray-50">
    <!-- Page header -->
    <div class="bg-white border-b border-gray-200 px-8 py-6">
      <h1 class="text-2xl font-bold text-gray-900">Payment Plans</h1>
      <p class="text-sm text-gray-500 mt-0.5">
        Manage subscription tier pricing
      </p>
    </div>

    <div class="px-8 py-6 max-w-3xl">
      <!-- Loading -->
      <div v-if="isLoading" class="flex items-center justify-center py-16">
        <div
          class="h-7 w-7 animate-spin rounded-full border-4 border-blue-600 border-r-transparent"
        />
      </div>

      <Card v-else class="overflow-hidden">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 border-b border-gray-200">
            <tr>
              <th
                class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide"
              >
                Plan
              </th>
              <th
                class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide"
              >
                User Limit
              </th>
              <th
                class="text-left px-6 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wide"
              >
                Monthly Cost (R)
              </th>
              <th class="px-6 py-3" />
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="plan in plans"
              :key="plan.Id"
              class="border-b border-gray-100 last:border-0"
            >
              <td class="px-6 py-4">
                <span
                  :class="[
                    'inline-block px-2.5 py-0.5 rounded-full text-xs font-semibold',
                    planBadgeClass(plan.Name),
                  ]"
                >
                  {{ plan.Name }}
                </span>
              </td>
              <td class="px-6 py-4 text-gray-600">
                {{ userLimitLabel(plan.MaxUsers) }}
              </td>
              <td class="px-6 py-4">
                <div
                  v-if="editingId === plan.Id"
                  class="flex items-center gap-2"
                >
                  <Input
                    v-model.number="editCost"
                    type="number"
                    min="0"
                    step="50"
                    class="w-28"
                    @keyup.enter="saveCost(plan)"
                    @keyup.escape="cancelEdit"
                  />
                  <Button size="sm" :disabled="isSaving" @click="saveCost(plan)">
                    {{ isSaving ? "Saving..." : "Save" }}
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    :disabled="isSaving"
                    @click="cancelEdit"
                  >
                    Cancel
                  </Button>
                </div>
                <span v-else class="font-medium text-gray-900">
                  R {{ plan.MonthlyCostRand?.toLocaleString("en-ZA") }}
                </span>
              </td>
              <td class="px-6 py-4 text-right">
                <button
                  v-if="editingId !== plan.Id"
                  class="text-sm font-medium text-blue-600 hover:text-blue-700 transition-colors"
                  @click="startEdit(plan)"
                >
                  Edit cost
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </Card>

      <p class="mt-3 text-xs text-gray-400">
        Plan names and user limits are system-defined. Only the monthly cost can
        be updated.
      </p>
    </div>
  </div>
</template>
