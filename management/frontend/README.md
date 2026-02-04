# Management Portal

A Vue 3 + Vite + TypeScript management portal for system administrators to manage organizations.

## Tech Stack

- **Vue 3** - Progressive JavaScript framework
- **Vite** - Next generation frontend tooling
- **TypeScript** - Typed superset of JavaScript
- **Tailwind CSS** - Utility-first CSS framework
- **Vue Router** - Official router for Vue.js
- **Pinia** - State management for Vue
- **Axios** - HTTP client
- **Vuetify** - Material Design component framework
- **Shadcn-vue components** - Re-usable components built with Radix Vue and Tailwind CSS

## Features

- System Admin authentication
- Organization management dashboard
- User management
- System settings
- Reports and analytics

## Getting Started

### Prerequisites

- Node.js (v18 or higher)
- npm or yarn

### Installation

1. Install dependencies:
```bash
npm install
```

2. Set up environment variables:
Create a `.env` file in the root directory with:
```
VITE_API_URL=http://localhost:5000
```

3. Start the development server:
```bash
npm run dev
```

The application will be available at `http://localhost:5174`

### Build for Production

```bash
npm run build
```

### Preview Production Build

```bash
npm run preview
```

## Project Structure

```
management/frontend/
├── src/
│   ├── assets/          # Static assets and styles
│   ├── components/      # Reusable Vue components
│   │   └── ui/         # UI components (Button, Input, Card, etc.)
│   ├── lib/            # Utility functions
│   ├── router/         # Vue Router configuration
│   ├── services/       # API services
│   ├── stores/         # Pinia stores
│   ├── views/          # Page components
│   ├── App.vue         # Root component
│   └── main.ts         # Application entry point
├── public/             # Public static assets
├── index.html          # HTML entry point
├── vite.config.ts      # Vite configuration
├── tailwind.config.js  # Tailwind CSS configuration
└── tsconfig.json       # TypeScript configuration
```

## Authentication

The portal requires System Admin credentials to access. Only users with the `systemAdmin` role can log in.

## Development

- The portal runs on port 5174 by default (different from the main client app on 5173)
- Hot module replacement (HMR) is enabled for fast development
- TypeScript strict mode is enabled for better type safety

## License

Private - Internal use only
