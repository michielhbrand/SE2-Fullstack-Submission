# Frontend Applications — Design Decisions

## Vue 3 (Composition API)

- **Vue 3** chosen as the frontend framework for both applications
- Composition API (`<script setup>`) used throughout — benefits:
  - Better TypeScript integration — type inference works naturally with `ref()`, `computed()`, `defineProps()`
  - Logic reuse via composables — no mixins or options API limitations
  - Cleaner code organization — related logic grouped together instead of split across `data`, `methods`, `computed` options
- Vue's reactivity system provides fine-grained DOM updates without virtual DOM diffing overhead

## TypeScript

- **Full TypeScript** across both frontends — no `.js` files in source
- Catches type errors at compile time rather than runtime
- Auto-generated API client types ensure frontend ↔ backend type safety
- `vue-tsc` used for type checking Vue SFC files during build

## Vite

- **Vite** as the build tool and dev server
- Chosen over Webpack for:
  - Near-instant dev server startup via native ES modules — no bundling during development
  - Hot Module Replacement (HMR) that updates in milliseconds
  - Optimized production builds using Rollup under the hood
  - First-class Vue support via `@vitejs/plugin-vue`

## Tailwind CSS

- **Tailwind CSS v4** for utility-first styling
- Benefits:
  - No context switching between template and stylesheet files
  - Consistent design system via configuration
  - Automatic purging of unused styles — minimal production CSS bundle
  - Responsive design via utility prefixes (`md:`, `lg:`)
- `tailwindcss-animate` plugin for transition and animation utilities

## Component Libraries

- **Radix Vue / Reka UI** — headless, accessible UI primitives
  - Provides behavior and accessibility (ARIA, keyboard navigation) without opinionated styling
  - Components like Dialog, Select, Dropdown, Tooltip follow WAI-ARIA patterns out of the box
  - Styled with Tailwind — full visual control
- **Vuetify** — used selectively for complex data components
  - Material Design component library with rich data table, form, and layout components
  - `vite-plugin-vuetify` for tree-shaking — only imported components are bundled
- **Lucide Vue Next** — icon library
  - Tree-shakeable SVG icons — only used icons are included in the bundle
  - Consistent icon style across the application
- **Class Variance Authority (CVA)** — variant-based component styling
  - Type-safe component variants (size, color, state) defined declaratively
  - Works with Tailwind classes — no runtime CSS generation
- **`tailwind-merge`** and **`clsx`** — utility for merging Tailwind classes without conflicts

## State Management (Pinia)

- **Pinia** as the state management solution
- Chosen over Vuex for:
  - First-class TypeScript support — full type inference without boilerplate
  - Composition API style — stores defined with `defineStore()` using `ref()` and `computed()`
  - No mutations — simpler mental model with direct state modification
  - Devtools integration for debugging
- Stores organized by domain:
  - `auth` — authentication state, token management, user info
  - `organization` — active organization context, organization switching
  - `ui` — sidebar state, UI preferences

## Routing (Vue Router)

- **Vue Router** with HTML5 History mode — clean URLs without hash fragments
- Route-level authentication guards via `beforeEach` navigation guard:
  - `requiresAuth` meta — redirects unauthenticated users to login
  - `requiresAdmin` meta — restricts admin routes to admin users
  - Authenticated users redirected away from login page to their appropriate dashboard
- Nested routes for admin section (`/admin/users`, `/admin/payment-details`, `/admin/edit-organization`)

## Auto-Generated API Clients (NSwag)

```
┌─────────────────────────────────────────────────────────┐
│  Vue Component                                          │
│  (e.g. Invoices.vue)                                    │
└────────────────────────┬────────────────────────────────┘
                         │ calls
                         ▼
┌─────────────────────────────────────────────────────────┐
│  Generated API Client  (NSwag)                          │
│  api-client.ts — typed methods, DTOs, error handling    │
└────────────────────────┬────────────────────────────────┘
                         │ uses
                         ▼
┌─────────────────────────────────────────────────────────┐
│  Axios Instance  (http-client.ts)                       │
│                                                         │
│  Request Interceptor ──► inject Bearer token            │
│  Response Interceptor ──► 401? refresh token & retry    │
└────────────────────────┬────────────────────────────────┘
                         │ HTTP
                         ▼
                  ┌──────────────┐
                  │  Backend API │
                  └──────────────┘
```

- **NSwag** generates TypeScript API clients from the backend's OpenAPI specification
- Benefits:
  - Zero manual API call code — all endpoints, request/response types, and error handling generated
  - Type-safe API calls — TypeScript compiler catches mismatched parameters or response types
  - Single source of truth — regenerate when the backend API changes
  - `generate-client.sh` script automates the generation workflow
- Custom Axios instance (`http-client.ts`) injected into the generated client for:
  - Automatic Bearer token injection via request interceptor
  - 401 response handling with automatic token refresh via response interceptor

## Authentication Flow

- JWT tokens stored in `localStorage` for persistence across page reloads
- Token expiration tracked with a timer — automatic logout with user notification when token expires
- **Refresh token rotation** — 401 responses trigger automatic token refresh before retrying the failed request
- Request queuing during refresh — concurrent requests wait for the refresh to complete, then retry with the new token
- Dual login flows: regular user login and admin login with separate Keycloak endpoints
- JWT decoded client-side to extract user info (username, name, roles) without an additional API call

## Toast Notifications (Vue Sonner)

- **Vue Sonner** for non-intrusive toast notifications
- Used for:
  - Success feedback (entity created, updated, deleted)
  - Error messages extracted from RFC 9457 Problem Details responses
  - Session expiration warnings
- Centralized error extraction function handles multiple error response formats (NSwag exceptions, Axios errors, validation errors)

## UI Component Architecture

- Custom `ui/` component library built on Radix Vue primitives
- Components follow a consistent pattern: headless primitive + Tailwind styling + CVA variants
- Includes: Button, Card, Dialog, Input, Label, Select, Table, Badge, Avatar, Pagination, Skeleton, Tooltip, and more
- Modal components (`modals/`) encapsulate form logic for creating/editing entities
- `Layout.vue` provides the application shell with collapsible sidebar navigation

## Organization Context

- Organization switcher allows users belonging to multiple organizations to switch context
- Active organization stored in Pinia and persisted — all API calls scoped to the selected organization
- Organization context initialized on login and cleared on logout
