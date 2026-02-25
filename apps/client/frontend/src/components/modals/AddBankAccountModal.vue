<script setup lang="ts">
import { ref, reactive } from 'vue'
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, Button, Input, Label } from '../ui/index'
import { Spinner } from '../ui/index'

interface Props {
  show: boolean
}

interface BankAccountData {
  bankName: string
  branchCode: string
  accountNumber: string
  accountType: string
}

interface Emits {
  (e: 'close'): void
  (e: 'save', data: BankAccountData): void
}

const props = defineProps<Props>()
const emit = defineEmits<Emits>()

const saving = ref(false)
const formErrors = ref<Record<string, string>>({})

const form = reactive<BankAccountData>({
  bankName: '',
  branchCode: '',
  accountNumber: '',
  accountType: 'Cheque',
})

const resetForm = () => {
  form.bankName = ''
  form.branchCode = ''
  form.accountNumber = ''
  form.accountType = 'Cheque'
  formErrors.value = {}
}

const validate = (): boolean => {
  formErrors.value = {}
  if (!form.bankName.trim()) formErrors.value.bankName = 'Bank name is required'
  if (!form.branchCode.trim()) formErrors.value.branchCode = 'Branch code is required'
  if (!form.accountNumber.trim()) formErrors.value.accountNumber = 'Account number is required'
  if (!form.accountType.trim()) formErrors.value.accountType = 'Account type is required'
  return Object.keys(formErrors.value).length === 0
}

const handleSave = async () => {
  if (!validate()) return
  saving.value = true
  try {
    emit('save', {
      bankName: form.bankName.trim(),
      branchCode: form.branchCode.trim(),
      accountNumber: form.accountNumber.trim(),
      accountType: form.accountType.trim(),
    })
  } finally {
    saving.value = false
  }
}

const handleClose = () => {
  resetForm()
  emit('close')
}
</script>

<template>
  <Dialog :open="show" @update:open="(open) => !open && handleClose()">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>Add Bank Account</DialogTitle>
      </DialogHeader>

      <div class="space-y-4 py-2">
        <div>
          <Label for="ba-bank-name">Bank Name *</Label>
          <Input
            id="ba-bank-name"
            v-model="form.bankName"
            placeholder="e.g. First National Bank"
            class="mt-1"
            :class="formErrors.bankName ? 'border-red-500' : ''"
            :disabled="saving"
          />
          <p v-if="formErrors.bankName" class="mt-1 text-xs text-red-600">{{ formErrors.bankName }}</p>
        </div>

        <div>
          <Label for="ba-branch-code">Branch Code *</Label>
          <Input
            id="ba-branch-code"
            v-model="form.branchCode"
            placeholder="e.g. 250655"
            class="mt-1"
            :class="formErrors.branchCode ? 'border-red-500' : ''"
            :disabled="saving"
          />
          <p v-if="formErrors.branchCode" class="mt-1 text-xs text-red-600">{{ formErrors.branchCode }}</p>
        </div>

        <div>
          <Label for="ba-account-number">Account Number *</Label>
          <Input
            id="ba-account-number"
            v-model="form.accountNumber"
            placeholder="e.g. 62812345678"
            class="mt-1"
            :class="formErrors.accountNumber ? 'border-red-500' : ''"
            :disabled="saving"
          />
          <p v-if="formErrors.accountNumber" class="mt-1 text-xs text-red-600">{{ formErrors.accountNumber }}</p>
        </div>

        <div>
          <Label for="ba-account-type">Account Type *</Label>
          <select
            id="ba-account-type"
            v-model="form.accountType"
            class="mt-1 w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gray-900"
            :class="formErrors.accountType ? 'border-red-500' : ''"
            :disabled="saving"
          >
            <option value="Cheque">Cheque</option>
            <option value="Savings">Savings</option>
            <option value="Transmission">Transmission</option>
            <option value="Business">Business</option>
          </select>
          <p v-if="formErrors.accountType" class="mt-1 text-xs text-red-600">{{ formErrors.accountType }}</p>
        </div>
      </div>

      <DialogFooter>
        <Button variant="outline" @click="handleClose" :disabled="saving">Cancel</Button>
        <Button class="bg-gray-900 hover:bg-gray-800 text-white" @click="handleSave" :disabled="saving">
          <Spinner v-if="saving" class="h-4 w-4 mr-2" />
          {{ saving ? 'Adding...' : 'Add Account' }}
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
