<script setup lang="ts">
import Layout from '../components/Layout.vue'
</script>

<template>
  <Layout>
    <div class="p-6 lg:p-8">
      <div class="max-w-7xl mx-auto">
        <div class="mb-8">
          <h2 class="text-3xl font-bold text-gray-900">Dashboard</h2>
          <p class="mt-2 text-gray-600">Welcome to your dashboard overview</p>
        </div>

        <!-- Empty Dashboard - Ready for cards -->
        <div class="grid gap-6">
          <div class="flex items-center justify-center h-96 border-2 border-dashed border-gray-300 rounded-lg">
            <div class="text-center">
              <svg class="mx-auto h-12 w-12 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"/>
              </svg>
              <h3 class="mt-2 text-sm font-medium text-gray-900">Dashboard Content</h3>
              <p class="mt-1 text-sm text-gray-500">Cards and widgets will be added here</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Layout>
</template>
