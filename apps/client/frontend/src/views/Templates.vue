<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { Skeleton, Badge, Button, Table, TableHeader, TableBody, TableHead, TableRow, TableCell, Pagination, PaginationContent, PaginationItem, PaginationPrevious, PaginationNext } from '../components/ui/index'
import { templateApi } from '../services/api'
import Layout from '../components/Layout.vue'
import { toast } from 'vue-sonner'
import TemplatePdfPreviewModal from '../components/modals/TemplatePdfPreviewModal.vue'
import { useOrganizationStore } from '../stores/organization'
import { useOrganizationContext } from '../composables/useOrganizationContext'

const organizationStore = useOrganizationStore()
const { ensureOrganizationContext } = useOrganizationContext()

interface Template {
  id: number
  name: string
  version: number
  createdBy: string
  created: string
  storageKey: string
  type: string
  organizationId: number
}

const templates = ref<Template[]>([])
const loading = ref(true)
const currentPage = ref(1)
const pageSize = ref(25)
const totalPages = ref(0)
const totalCount = ref(0)

const previewUrl = ref<string | null>(null)
const previewLoading = ref(false)
const previewTemplate = ref<string | null>(null)

onMounted(async () => {
  await ensureOrganizationContext()
  await fetchTemplates()
})

const fetchTemplates = async () => {
  loading.value = true
  try {
    const orgId = organizationStore.currentOrganizationId
    if (!orgId) {
      toast.error('No organization selected')
      return
    }
    const response = await templateApi.getTemplates(orgId, currentPage.value, pageSize.value)
    const typeNames: Record<number, string> = { 0: 'Invoice', 1: 'Quote' }
    templates.value = (response.data || []).map((template: any) => ({
      id: template.id ?? 0,
      name: template.name ?? '',
      version: template.version ?? 0,
      createdBy: template.createdBy ?? '',
      created: template.created ? new Date(template.created).toLocaleDateString() : '',
      storageKey: template.storageKey ?? '',
      type: typeNames[template.type] ?? 'Unknown',
      organizationId: template.organizationId ?? 0
    }))
    totalPages.value = response.pagination?.totalPages || 0
    totalCount.value = response.pagination?.totalCount || 0
  } catch (err: any) {
    toast.error(err.response?.data?.message || 'Failed to load templates')
  } finally {
    loading.value = false
  }
}

const goToPage = async (page: number) => {
  currentPage.value = page
  await fetchTemplates()
}

const onPageSizeChange = async () => {
  currentPage.value = 1
  await fetchTemplates()
}

const paginationPages = computed(() => {
  const pages = []
  const maxVisible = 5
  let start = Math.max(1, currentPage.value - Math.floor(maxVisible / 2))
  let end = Math.min(totalPages.value, start + maxVisible - 1)
  if (end - start < maxVisible - 1) start = Math.max(1, end - maxVisible + 1)
  for (let i = start; i <= end; i++) pages.push(i)
  return pages
})

const previewTemplateHandler = async (template: any) => {
  previewLoading.value = true
  previewTemplate.value = template.name
  previewUrl.value = null
  try {
    const response = await templateApi.getPreviewUrl(template.id)
    previewUrl.value = response.url ?? null
  } catch (err: any) {
    toast.error(err.response?.data?.message || 'Failed to generate preview')
  } finally {
    previewLoading.value = false
  }
}

const closePreview = () => {
  previewUrl.value = null
  previewTemplate.value = null
}
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">
        <!-- Header -->
        <div class="mb-8 flex items-center justify-between">
          <div>
            <h2 class="text-3xl font-bold text-gray-900">Templates</h2>
            <p class="mt-2 text-gray-600">Manage and preview your invoice templates</p>
          </div>
          <button
            @click="fetchTemplates"
            :disabled="loading"
            title="Refresh"
            class="text-gray-600 hover:text-gray-900 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
          >
            <svg class="h-5 w-5" :class="{ 'animate-spin': loading }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
            </svg>
          </button>
        </div>

        <!-- Templates Table -->
        <div class="bg-white rounded-lg shadow overflow-hidden">
          <!-- Loading Skeleton -->
          <div v-if="loading" class="p-6 space-y-3">
            <Skeleton class="h-12 w-full" />
            <Skeleton class="h-12 w-full" />
            <Skeleton class="h-12 w-full" />
            <Skeleton class="h-12 w-full" />
          </div>

          <div v-else class="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Template Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Version</TableHead>
                  <TableHead>Created By</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead class="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                <TableRow v-for="template in templates" :key="template.id">
                  <TableCell>
                    <div class="flex items-center">
                      <svg class="w-5 h-5 text-gray-400 mr-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                      </svg>
                      <span class="font-medium">{{ template.name }}</span>
                    </div>
                  </TableCell>
                  <TableCell>
                    <Badge :variant="template.type === 'Quote' ? 'secondary' : 'default'">{{ template.type }}</Badge>
                  </TableCell>
                  <TableCell>
                    <Badge variant="default">v{{ template.version }}</Badge>
                  </TableCell>
                  <TableCell>{{ template.createdBy }}</TableCell>
                  <TableCell>{{ template.created }}</TableCell>
                  <TableCell class="text-right">
                    <Button @click="previewTemplateHandler(template)" variant="outline" size="sm" class="inline-flex items-center">
                      <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                      </svg>
                      Preview
                    </Button>
                  </TableCell>
                </TableRow>
                <TableRow v-if="templates.length === 0">
                  <TableCell colspan="6" class="text-center py-12">
                    <svg class="w-16 h-16 mx-auto text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
                    </svg>
                    <h3 class="text-lg font-medium text-gray-900 mb-2">No templates found</h3>
                    <p class="text-gray-500">Upload your first template to get started</p>
                  </TableCell>
                </TableRow>
              </TableBody>
            </Table>
          </div>

          <!-- Footer -->
          <div class="px-6 py-3 border-t border-gray-200 bg-gray-50 flex items-center justify-between gap-4">
            <div class="flex items-center gap-4 text-sm text-gray-600">
              <span>{{ totalCount }} record{{ totalCount !== 1 ? 's' : '' }}</span>
              <div class="flex items-center gap-2">
                <span>Rows per page:</span>
                <select
                  v-model.number="pageSize"
                  @change="onPageSizeChange"
                  class="border border-gray-300 rounded px-2 py-1 text-sm bg-white focus:outline-none focus:ring-1 focus:ring-gray-400"
                >
                  <option :value="10">10</option>
                  <option :value="25">25</option>
                  <option :value="50">50</option>
                </select>
              </div>
            </div>
            <Pagination v-if="totalPages > 0" :total="totalCount" :sibling-count="1" :page="currentPage" :items-per-page="pageSize" @update:page="goToPage">
              <PaginationContent>
                <PaginationPrevious />
                <PaginationItem v-for="page in paginationPages" :key="page" :value="page" />
                <PaginationNext />
              </PaginationContent>
            </Pagination>
          </div>
        </div>

        <TemplatePdfPreviewModal
          :show="!!previewUrl"
          :preview-url="previewUrl"
          :template-name="previewTemplate"
          :loading="previewLoading"
          @close="closePreview"
        />
      </div>
    </div>
  </Layout>
</template>
