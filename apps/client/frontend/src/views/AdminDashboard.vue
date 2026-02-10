<script setup lang="ts">
import { ref, computed } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useAuthStore } from "../stores/auth";
import { useOrganizationStore } from "../stores/organization";
import { Button } from "../components/ui/index";

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const organizationStore = useOrganizationStore();
const menuOpen = ref(false);

const currentOrg = computed(() => organizationStore.currentOrganization);

const handleLogout = async () => {
  await authStore.logout();
};

const toggleMenu = () => {
  menuOpen.value = !menuOpen.value;
};

const isActive = (path: string) => {
  return route.path === path;
};

const navigateTo = (path: string) => {
  router.push(path);
  menuOpen.value = false;
};

const menuItems = [
  {
    path: '/admin/users',
    label: 'User Management',
    icon: 'M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z'
  },
  {
    path: '/admin/payment-details',
    label: 'Payment Details',
    icon: 'M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z'
  },
  {
    path: '/admin/edit-organization',
    label: 'Edit Organization',
    icon: 'M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4'
  }
];
</script>

<template>
  <div class="min-h-screen bg-gray-50">
    <!-- Header -->
    <header class="bg-gray-900 text-white shadow-lg">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex items-center justify-between py-6">
          <div>
            <h1 class="text-3xl font-bold">Admin Portal</h1>
            <p class="mt-1 text-gray-300">Management Dashboard</p>
            <p v-if="currentOrg" class="mt-1 text-sm text-gray-400">
              Organization: <span class="font-semibold text-gray-200">{{ currentOrg.name }}</span>
            </p>
          </div>
          <Button
            @click="handleLogout"
            class="bg-gray-800 hover:bg-gray-700 text-white"
          >
            Logout
          </Button>
        </div>

        <!-- Desktop Navigation Tabs -->
        <nav class="hidden md:flex space-x-1 pb-4">
          <button
            v-for="item in menuItems"
            :key="item.path"
            @click="navigateTo(item.path)"
            :class="[
              'flex items-center gap-2 px-4 py-2.5 rounded-t-lg font-medium transition-all',
              isActive(item.path)
                ? 'bg-gray-50 text-gray-900'
                : 'text-gray-300 hover:text-white hover:bg-gray-800',
            ]"
          >
            <svg
              class="h-5 w-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                :d="item.icon"
              />
            </svg>
            <span>{{ item.label }}</span>
          </button>
        </nav>

        <!-- Mobile Menu Button -->
        <div class="md:hidden pb-4">
          <button
            @click="toggleMenu"
            class="flex items-center justify-between w-full px-4 py-2.5 bg-gray-800 rounded-lg text-left"
          >
            <span class="font-medium">
              {{ menuItems.find(item => isActive(item.path))?.label || 'Menu' }}
            </span>
            <svg
              class="h-5 w-5 transition-transform"
              :class="{ 'rotate-180': menuOpen }"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M19 9l-7 7-7-7"
              />
            </svg>
          </button>

          <!-- Mobile Dropdown Menu -->
          <div
            v-if="menuOpen"
            class="mt-2 bg-gray-800 rounded-lg overflow-hidden shadow-lg"
          >
            <button
              v-for="item in menuItems"
              :key="item.path"
              @click="navigateTo(item.path)"
              :class="[
                'flex items-center gap-3 w-full px-4 py-3 text-left transition-colors',
                isActive(item.path)
                  ? 'bg-gray-700 text-white'
                  : 'text-gray-300 hover:bg-gray-700 hover:text-white',
              ]"
            >
              <svg
                class="h-5 w-5"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2"
                  :d="item.icon"
                />
              </svg>
              <span>{{ item.label }}</span>
            </button>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="max-w-7xl mx-auto">
      <router-view />
    </main>
  </div>
</template>
