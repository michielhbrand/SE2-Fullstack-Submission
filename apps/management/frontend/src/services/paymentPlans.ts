import { apiClient } from "../api/client";
import type { PaymentPlanResponse, UpdatePaymentPlanRequest } from "../api/generated/api-client";

export const paymentPlanService = {
  async getAll(): Promise<PaymentPlanResponse[]> {
    return await apiClient.getPaymentPlans();
  },

  async updateCost(id: number, monthlyCostRand: number): Promise<PaymentPlanResponse> {
    const request: UpdatePaymentPlanRequest = { MonthlyCostRand: monthlyCostRand };
    return await apiClient.updatePaymentPlan(id, request);
  },
};
