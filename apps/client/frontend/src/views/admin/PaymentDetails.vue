<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../../stores/auth";
import { Button, Card, Spinner, Input, Label } from "../../components/ui/index";
import { toast } from "vue-sonner";
import { generatedClient } from "../../services/api";
import type { BankAccountResponse } from "../../api/generated/api-client";

const router = useRouter();
const authStore = useAuthStore();
const bankAccounts = ref<BankAccountResponse[]>([]);
const loading = ref(true);
const organizationId = ref<number | null>(null);

// Add modal state
const showAddModal = ref(false);
const saving = ref(false);
const newAccount = ref({
  bankName: "",
  branchCode: "",
  accountNumber: "",
  accountType: "Cheque",
});
const formErrors = ref<Record<string, string>>({});

// Per-card action states
const settingActive = ref<number | null>(null);
const deleting = ref<number | null>(null);

onMounted(async () => {
  if (!authStore.isAdmin) {
    router.push("/login");
    return;
  }
  await loadBankAccounts();
});

const loadBankAccounts = async () => {
  loading.value = true;
  try {
    const organizations = await generatedClient.organization_GetOrganizations();
    if (organizations && organizations.length > 0) {
      const org = organizations[0];
      if (org) {
        organizationId.value = org.id ?? null;
        bankAccounts.value = org.bankAccounts || [];
      }
    }
  } catch (err) {
    toast.error("Failed to load bank accounts");
    console.error("Error loading bank accounts:", err);
  } finally {
    loading.value = false;
  }
};

const validateForm = (): boolean => {
  formErrors.value = {};
  if (!newAccount.value.bankName.trim())
    formErrors.value.bankName = "Bank name is required";
  if (!newAccount.value.branchCode.trim())
    formErrors.value.branchCode = "Branch code is required";
  if (!newAccount.value.accountNumber.trim())
    formErrors.value.accountNumber = "Account number is required";
  if (!newAccount.value.accountType.trim())
    formErrors.value.accountType = "Account type is required";
  return Object.keys(formErrors.value).length === 0;
};

const handleAddAccount = async () => {
  if (!validateForm() || !organizationId.value) return;

  saving.value = true;
  try {
    await generatedClient.organization_AddBankAccount(organizationId.value, {
      bankName: newAccount.value.bankName.trim(),
      branchCode: newAccount.value.branchCode.trim(),
      accountNumber: newAccount.value.accountNumber.trim(),
      accountType: newAccount.value.accountType.trim(),
    });
    toast.success("Bank account added successfully");
    showAddModal.value = false;
    newAccount.value = { bankName: "", branchCode: "", accountNumber: "", accountType: "Cheque" };
    await loadBankAccounts();
  } catch (err: any) {
    const msg = err?.response?.data?.detail || err?.response?.data?.message;
    toast.error(msg || "Failed to add bank account");
  } finally {
    saving.value = false;
  }
};

const handleSetActive = async (accountId: number) => {
  if (!organizationId.value) return;
  settingActive.value = accountId;
  try {
    await generatedClient.organization_SetActiveBankAccount(organizationId.value, accountId);
    toast.success("Active bank account updated");
    await loadBankAccounts();
  } catch (err: any) {
    const msg = err?.response?.data?.detail || err?.response?.data?.message;
    toast.error(msg || "Failed to update active account");
  } finally {
    settingActive.value = null;
  }
};

const handleDelete = async (accountId: number) => {
  if (!organizationId.value) return;
  deleting.value = accountId;
  try {
    await generatedClient.organization_DeleteBankAccount(organizationId.value, accountId);
    toast.success("Bank account removed");
    await loadBankAccounts();
  } catch (err: any) {
    const msg = err?.response?.data?.detail || err?.response?.data?.message;
    toast.error(msg || "Failed to delete bank account");
  } finally {
    deleting.value = null;
  }
};

const maskAccountNumber = (accountNumber?: string): string => {
  if (!accountNumber) return "****";
  if (accountNumber.length <= 4) return accountNumber;
  return "****" + accountNumber.slice(-4);
};

const closeModal = () => {
  showAddModal.value = false;
  newAccount.value = { bankName: "", branchCode: "", accountNumber: "", accountType: "Cheque" };
  formErrors.value = {};
};
</script>

<template>
  <div class="p-8">
    <!-- Payment Details Card -->
    <Card class="p-6">
      <div class="flex items-center justify-between mb-6">
        <div>
          <h2 class="text-2xl font-semibold text-gray-900">Payment Details</h2>
          <p class="mt-1 text-sm text-gray-500">Manage your organization's bank account information</p>
        </div>
        <div class="flex items-center gap-3">
          <Button
            class="bg-gray-900 hover:bg-gray-800 text-white"
            @click="showAddModal = true"
          >
            <svg class="h-4 w-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
            </svg>
            Add Bank Account
          </Button>
          <button
            @click="loadBankAccounts"
            :disabled="loading"
            title="Refresh bank accounts"
            class="text-gray-600 hover:text-gray-900 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <svg
              class="h-5 w-5"
              :class="{ 'animate-spin': loading }"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
              />
            </svg>
          </button>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="flex justify-center items-center py-12">
        <Spinner class="h-8 w-8 text-gray-900" />
      </div>

      <!-- Bank Accounts Grid -->
      <div v-else-if="bankAccounts.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        <div
          v-for="account in bankAccounts"
          :key="account.id"
          class="relative bg-gradient-to-br from-gray-800 to-gray-900 rounded-xl p-6 text-white shadow-lg"
        >
          <!-- Active Badge -->
          <div class="absolute top-4 right-4">
            <span
              :class="[
                'px-2 py-1 text-xs font-semibold rounded-full',
                account.active ? 'bg-green-500 text-white' : 'bg-gray-600 text-gray-300',
              ]"
            >
              {{ account.active ? "In Use" : "Inactive" }}
            </span>
          </div>

          <!-- Bank Icon -->
          <div class="mb-4">
            <svg class="h-10 w-10 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z"
              />
            </svg>
          </div>

          <!-- Bank Details -->
          <div class="space-y-3">
            <div>
              <p class="text-xs text-gray-400 uppercase tracking-wide">Bank Name</p>
              <p class="text-lg font-semibold">{{ account.bankName || "N/A" }}</p>
            </div>

            <div class="grid grid-cols-2 gap-3">
              <div>
                <p class="text-xs text-gray-400 uppercase tracking-wide">Branch Code</p>
                <p class="text-sm font-medium">{{ account.branchCode || "N/A" }}</p>
              </div>
              <div>
                <p class="text-xs text-gray-400 uppercase tracking-wide">Account Type</p>
                <p class="text-sm font-medium">{{ account.accountType || "N/A" }}</p>
              </div>
            </div>

            <div>
              <p class="text-xs text-gray-400 uppercase tracking-wide">Account Number</p>
              <p class="text-xl font-mono tracking-wider">{{ maskAccountNumber(account.accountNumber) }}</p>
            </div>
          </div>

          <!-- Actions -->
          <div class="mt-6 flex gap-2">
            <!-- Set Active (only shown for inactive accounts) -->
            <button
              v-if="!account.active"
              @click="handleSetActive(account.id!)"
              :disabled="settingActive === account.id"
              class="flex-1 px-4 py-2 bg-green-500/20 hover:bg-green-500/40 rounded-lg text-sm font-medium transition-colors disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
            >
              <Spinner v-if="settingActive === account.id" class="h-3 w-3" />
              <span>{{ settingActive === account.id ? "Setting..." : "Set Active" }}</span>
            </button>

            <!-- Spacer when active (no set-active button) -->
            <div v-else class="flex-1" />

            <!-- Delete (disabled for active account) -->
            <button
              @click="handleDelete(account.id!)"
              :disabled="account.active || deleting === account.id"
              :title="account.active ? 'Cannot delete the active account — set another account as active first' : 'Delete account'"
              class="px-4 py-2 bg-white/10 hover:bg-red-500/40 rounded-lg text-sm font-medium transition-colors disabled:opacity-30 disabled:cursor-not-allowed flex items-center justify-center"
            >
              <Spinner v-if="deleting === account.id" class="h-4 w-4" />
              <svg v-else class="h-4 w-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- Empty State -->
      <div v-else class="text-center py-12">
        <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
        </svg>
        <h3 class="mt-2 text-sm font-medium text-gray-900">No bank accounts found</h3>
        <p class="mt-1 text-sm text-gray-500">Add a bank account to manage payment details.</p>
        <Button class="mt-4 bg-gray-900 hover:bg-gray-800 text-white" @click="showAddModal = true">
          Add Bank Account
        </Button>
      </div>
    </Card>

    <!-- Info Card -->
    <Card class="mt-6 p-6 bg-blue-50 border-blue-200">
      <div class="flex">
        <div class="flex-shrink-0">
          <svg class="h-5 w-5 text-blue-700" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
          </svg>
        </div>
        <div class="ml-3">
          <h3 class="text-sm font-medium text-blue-900">Payment Details Information</h3>
          <div class="mt-2 text-sm text-blue-700">
            <ul class="list-disc list-inside space-y-1">
              <li>The "In Use" account is included on generated invoices</li>
              <li>Only one account can be active at a time — use "Set Active" to switch</li>
              <li>The active account cannot be deleted; set another as active first</li>
              <li>Account numbers are masked for security purposes</li>
            </ul>
          </div>
        </div>
      </div>
    </Card>

    <!-- Add Bank Account Modal -->
    <div
      v-if="showAddModal"
      class="fixed inset-0 z-50 flex items-center justify-center"
    >
      <!-- Backdrop -->
      <div class="absolute inset-0 bg-black/50" @click="closeModal" />

      <!-- Modal -->
      <div class="relative bg-white rounded-xl shadow-2xl w-full max-w-md mx-4 p-6">
        <div class="flex items-center justify-between mb-6">
          <h3 class="text-lg font-semibold text-gray-900">Add Bank Account</h3>
          <button @click="closeModal" class="text-gray-400 hover:text-gray-600 transition-colors">
            <svg class="h-5 w-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        <div class="space-y-4">
          <!-- Bank Name -->
          <div>
            <Label for="bank-name">Bank Name *</Label>
            <Input
              id="bank-name"
              v-model="newAccount.bankName"
              placeholder="e.g. First National Bank"
              class="mt-1"
              :class="formErrors.bankName ? 'border-red-500' : ''"
            />
            <p v-if="formErrors.bankName" class="mt-1 text-xs text-red-600">{{ formErrors.bankName }}</p>
          </div>

          <!-- Branch Code -->
          <div>
            <Label for="branch-code">Branch Code *</Label>
            <Input
              id="branch-code"
              v-model="newAccount.branchCode"
              placeholder="e.g. 250655"
              class="mt-1"
              :class="formErrors.branchCode ? 'border-red-500' : ''"
            />
            <p v-if="formErrors.branchCode" class="mt-1 text-xs text-red-600">{{ formErrors.branchCode }}</p>
          </div>

          <!-- Account Number -->
          <div>
            <Label for="account-number">Account Number *</Label>
            <Input
              id="account-number"
              v-model="newAccount.accountNumber"
              placeholder="e.g. 62812345678"
              class="mt-1"
              :class="formErrors.accountNumber ? 'border-red-500' : ''"
            />
            <p v-if="formErrors.accountNumber" class="mt-1 text-xs text-red-600">{{ formErrors.accountNumber }}</p>
          </div>

          <!-- Account Type -->
          <div>
            <Label for="account-type">Account Type *</Label>
            <select
              id="account-type"
              v-model="newAccount.accountType"
              class="mt-1 w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gray-900"
              :class="formErrors.accountType ? 'border-red-500' : ''"
            >
              <option value="Cheque">Cheque</option>
              <option value="Savings">Savings</option>
              <option value="Transmission">Transmission</option>
              <option value="Business">Business</option>
            </select>
            <p v-if="formErrors.accountType" class="mt-1 text-xs text-red-600">{{ formErrors.accountType }}</p>
          </div>
        </div>

        <div class="flex items-center justify-end gap-3 mt-6">
          <Button variant="outline" @click="closeModal" :disabled="saving">Cancel</Button>
          <Button
            class="bg-gray-900 hover:bg-gray-800 text-white"
            @click="handleAddAccount"
            :disabled="saving"
          >
            <Spinner v-if="saving" class="h-4 w-4 mr-2" />
            {{ saving ? "Adding..." : "Add Account" }}
          </Button>
        </div>
      </div>
    </div>
  </div>
</template>
