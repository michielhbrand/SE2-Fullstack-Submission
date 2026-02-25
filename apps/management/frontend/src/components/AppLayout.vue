<script setup lang="ts">
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import { toast } from "vue-sonner";
import {
  LayoutDashboard,
  Building2,
  CreditCard,
  LogOut,
  User,
  Zap,
} from "lucide-vue-next";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const navItems = [
  { label: "Dashboard", path: "/dashboard", icon: LayoutDashboard },
  { label: "Organizations", path: "/organizations", icon: Building2 },
  { label: "Payment Plans", path: "/payment-plans", icon: CreditCard },
];

const isActive = (path: string) => {
  if (path === "/dashboard") return route.path === "/dashboard";
  return route.path.startsWith(path);
};

const handleLogout = async () => {
  await authStore.logout();
  toast.success("Logged out successfully");
  router.push("/login");
};
</script>

<template>
  <div class="flex h-screen overflow-hidden bg-gray-50">
    <!-- Sidebar -->
    <aside
      class="w-64 bg-slate-900 flex flex-col flex-shrink-0 border-r border-slate-800"
    >
      <!-- Brand -->
      <div class="flex items-center gap-3 px-5 py-5 border-b border-slate-800">
        <div
          class="h-9 w-9 rounded-lg bg-blue-600 flex items-center justify-center flex-shrink-0"
        >
          <Zap class="h-5 w-5 text-white" />
        </div>
        <div>
          <p class="text-sm font-bold text-white leading-tight">Management</p>
          <p class="text-xs text-slate-400 mt-0.5">Admin Portal</p>
        </div>
      </div>

      <!-- Navigation -->
      <nav class="flex-1 px-3 py-5 space-y-0.5">
        <p class="text-xs font-semibold text-slate-500 uppercase tracking-wider px-3 mb-3">
          Main Menu
        </p>
        <router-link
          v-for="item in navItems"
          :key="item.path"
          :to="item.path"
          class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all duration-150"
          :class="
            isActive(item.path)
              ? 'bg-blue-600 text-white shadow-sm'
              : 'text-slate-400 hover:bg-slate-800 hover:text-slate-100'
          "
        >
          <component :is="item.icon" class="h-4 w-4 flex-shrink-0" />
          {{ item.label }}
        </router-link>
      </nav>

      <!-- User / Logout -->
      <div class="px-3 py-4 border-t border-slate-800">
        <div class="flex items-center gap-3 px-3 py-2 mb-1">
          <div
            class="h-8 w-8 rounded-full bg-slate-700 flex items-center justify-center flex-shrink-0"
          >
            <User class="h-4 w-4 text-slate-400" />
          </div>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-slate-100 truncate">
              {{ authStore.user?.username }}
            </p>
            <p class="text-xs text-slate-500">System Admin</p>
          </div>
        </div>
        <button
          @click="handleLogout"
          class="flex items-center gap-3 w-full px-3 py-2 rounded-lg text-sm font-medium text-slate-400 hover:bg-slate-800 hover:text-slate-100 transition-colors"
        >
          <LogOut class="h-4 w-4 flex-shrink-0" />
          Sign out
        </button>
      </div>
    </aside>

    <!-- Main content area -->
    <div class="flex-1 min-w-0 overflow-auto">
      <router-view />
    </div>
  </div>
</template>
