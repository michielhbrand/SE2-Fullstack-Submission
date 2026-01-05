<script setup lang="ts">
import { onMounted } from 'vue'

onMounted(() => {
  // Force light mode by ensuring no dark class is present
  document.documentElement.classList.remove('dark')
  // Also set a light class to be explicit
  document.documentElement.classList.add('light')
})
</script>

<template>
  <router-view />
</template>
