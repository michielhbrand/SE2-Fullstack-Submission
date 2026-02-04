<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/auth";
import { toast } from "vue-sonner";
import Card from "../components/ui/Card.vue";
import Button from "../components/ui/Button.vue";
import Input from "../components/ui/Input.vue";
import Label from "../components/ui/Label.vue";

const router = useRouter();
const authStore = useAuthStore();

const username = ref("");
const password = ref("");
const loading = ref(false);

const handleLogin = async () => {
  if (!username.value || !password.value) {
    toast.error("Please enter username and password");
    return;
  }

  loading.value = true;
  try {
    const result = await authStore.login(username.value, password.value);

    if (result.success) {
      toast.success("Login successful");
      router.push("/dashboard");
    } else {
      toast.error(result.error || "Login failed");
    }
  } catch (error: any) {
    toast.error(error.message || "An error occurred during login");
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-gray-50 px-4">
    <Card class="w-full max-w-md p-8">
      <div class="mb-8 text-center">
        <h1 class="text-3xl font-bold text-gray-900 mb-2">Management Portal</h1>
        <p class="text-gray-600">Sign in with your System Admin account</p>
      </div>

      <form @submit.prevent="handleLogin" class="space-y-6">
        <div class="space-y-2">
          <Label for="username">Username</Label>
          <Input
            id="username"
            v-model="username"
            type="text"
            placeholder="Enter your username"
            :disabled="loading"
          />
        </div>

        <div class="space-y-2">
          <Label for="password">Password</Label>
          <Input
            id="password"
            v-model="password"
            type="password"
            placeholder="Enter your password"
            :disabled="loading"
          />
        </div>

        <Button type="submit" class="w-full" :disabled="loading">
          {{ loading ? "Signing in..." : "Sign In" }}
        </Button>
      </form>

      <div class="mt-6 text-center text-sm text-gray-600">
        <p>System Administrator Access Only</p>
      </div>
    </Card>
  </div>
</template>
