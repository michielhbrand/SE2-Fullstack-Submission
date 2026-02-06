<script setup lang="ts">
import { computed, onMounted, onUnmounted } from "vue";
import { cn } from "@/lib/utils";

interface Props {
  open?: boolean;
  title?: string;
  description?: string;
  maxWidth?: "sm" | "md" | "lg" | "xl" | "2xl";
}

const props = withDefaults(defineProps<Props>(), {
  open: false,
  maxWidth: "md",
});

const emit = defineEmits<{
  "update:open": [value: boolean];
  close: [];
}>();

const maxWidthClasses = {
  sm: "max-w-sm",
  md: "max-w-md",
  lg: "max-w-lg",
  xl: "max-w-xl",
  "2xl": "max-w-2xl",
};

const dialogClass = computed(() => {
  return cn(
    "relative bg-white rounded-lg shadow-xl w-full",
    maxWidthClasses[props.maxWidth],
  );
});

const handleClose = () => {
  emit("update:open", false);
  emit("close");
};

const handleBackdropClick = (event: MouseEvent) => {
  if (event.target === event.currentTarget) {
    handleClose();
  }
};

const handleEscape = (event: KeyboardEvent) => {
  if (event.key === "Escape" && props.open) {
    handleClose();
  }
};

onMounted(() => {
  document.addEventListener("keydown", handleEscape);
});

onUnmounted(() => {
  document.removeEventListener("keydown", handleEscape);
});
</script>

<template>
  <Teleport to="body">
    <Transition
      enter-active-class="transition-opacity duration-200"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition-opacity duration-200"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="open"
        class="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"
        @click="handleBackdropClick"
      >
        <Transition
          enter-active-class="transition-all duration-200"
          enter-from-class="opacity-0 scale-95"
          enter-to-class="opacity-100 scale-100"
          leave-active-class="transition-all duration-200"
          leave-from-class="opacity-100 scale-100"
          leave-to-class="opacity-0 scale-95"
        >
          <div v-if="open" :class="dialogClass">
            <!-- Header -->
            <div
              v-if="title || description"
              class="px-6 pt-6 pb-4 border-b border-gray-200"
            >
              <div class="flex items-start justify-between">
                <div>
                  <h2
                    v-if="title"
                    class="text-xl font-semibold text-gray-900 mb-1"
                  >
                    {{ title }}
                  </h2>
                  <p v-if="description" class="text-sm text-gray-600">
                    {{ description }}
                  </p>
                </div>
                <button
                  type="button"
                  class="text-gray-400 hover:text-gray-600 transition-colors"
                  @click="handleClose"
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
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              </div>
            </div>

            <!-- Content -->
            <div class="px-6 py-4">
              <slot />
            </div>

            <!-- Footer -->
            <div
              v-if="$slots.footer"
              class="px-6 py-4 border-t border-gray-200 bg-gray-50 rounded-b-lg"
            >
              <slot name="footer" />
            </div>
          </div>
        </Transition>
      </div>
    </Transition>
  </Teleport>
</template>
