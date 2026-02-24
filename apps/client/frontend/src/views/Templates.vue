<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { Button, Spinner, Skeleton, Badge, Table, TableHeader, TableBody, TableHead, TableRow, TableCell } from '../components/ui/index'
import { templateApi } from '../services/api'
import Layout from '../components/Layout.vue'
import { toast } from 'vue-sonner'

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
const previewUrl = ref<string | null>(null)
const previewLoading = ref(false)
const previewTemplate = ref<string | null>(null)

onMounted(async () => {
  await fetchTemplates()
})

const fetchTemplates = async () => {
  loading.value = true
  
  try {
    const response = await templateApi.getTemplates()
    const typeNames: Record<number, string> = { 0: 'Invoice', 1: 'Quote' }
    templates.value = (response.data || []).map((template) => ({
      id: template.id ?? 0,
      name: template.name ?? '',
      version: template.version ?? 0,
      createdBy: template.createdBy ?? '',
      created: template.created ? new Date(template.created).toLocaleDateString() : '',
      storageKey: template.storageKey ?? '',
      type: typeNames[(template as any).type] ?? 'Unknown',
      organizationId: (template as any).organizationId ?? 0
    }))
  } catch (err: any) {
    console.error('Failed to fetch templates:', err)
    toast.error(err.response?.data?.message || 'Failed to load templates')
  } finally {
    loading.value = false
  }
}

const previewTemplateHandler = async (template: any) => {
  previewLoading.value = true
  previewTemplate.value = template.name
  previewUrl.value = null
  
  try {
    // Get preview URL from InvoiceTrackerApi
    const response = await templateApi.getPreviewUrl(template.id)
    previewUrl.value = response.url ?? null
  } catch (err: any) {
    console.error('Failed to generate preview:', err)
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
        <div class="mb-8">
          <h2 class="text-3xl font-bold text-gray-900">Templates</h2>
          <p class="mt-2 text-gray-600">Manage and preview your invoice templates</p>
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
      
      <div v-else-if="templates.length > 0" class="overflow-x-auto">
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
                <Button
                  @click="previewTemplateHandler(template)"
                  variant="outline"
                  size="sm"
                  class="inline-flex items-center"
                >
                  <svg class="w-4 h-4 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                  </svg>
                  Preview
                </Button>
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </div>
      
      <!-- Empty State -->
      <div v-else class="px-6 py-12 text-center text-gray-500">
        <svg class="w-16 h-16 mx-auto text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
        </svg>
        <h3 class="text-lg font-medium text-gray-900 mb-2">No templates found</h3>
        <p class="text-gray-500">Upload your first template to get started</p>
      </div>
    </div>

    <!-- Preview Modal -->
    <div
      v-if="previewUrl"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50 p-4"
      @click.self="closePreview"
    >
      <div class="bg-white rounded-lg shadow-xl w-full max-w-6xl h-[90vh] flex flex-col">
        <!-- Modal Header -->
        <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200">
          <div>
            <h2 class="text-xl font-semibold text-gray-900">Template Preview</h2>
            <p class="text-sm text-gray-500 mt-1">{{ previewTemplate }}</p>
          </div>
          <Button
            @click="closePreview"
            variant="ghost"
            size="sm"
            class="text-gray-400 hover:text-gray-600"
          >
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </Button>
        </div>

        <!-- Modal Content -->
        <div class="flex-1 overflow-hidden relative">
          <div v-if="previewLoading" class="absolute inset-0 flex items-center justify-center bg-gray-50">
            <Spinner class="w-8 h-8" />
          </div>
          <iframe
            v-else
            :src="previewUrl"
            class="w-full h-full border-0"
            title="Template Preview"
          />
        </div>

        <!-- Modal Footer -->
        <div class="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50">
          <Button
            @click="closePreview"
            variant="outline"
          >
            Close
          </Button>
          <a
            :href="previewUrl"
            download
            target="_blank"
          >
            <Button variant="default">
              <svg class="w-4 h-4 mr-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/>
              </svg>
              Download
            </Button>
          </a>
        </div>
      </div>
    </div>
      </div>
    </div>
  </Layout>
</template>
