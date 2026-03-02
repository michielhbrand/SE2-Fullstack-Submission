import { describe, it, expect, vi, beforeEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import { useAuthStore } from './auth'

// Mock the API client module so tests don't make real HTTP calls
vi.mock('../api/client', () => ({
  apiClient: {
    adminLogin: vi.fn(),
    logout: vi.fn(),
    refreshToken: vi.fn(),
  },
}))

// A minimal JWT with a systemAdmin role for testing
function makeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))
  const body = btoa(JSON.stringify(payload))
  return `${header}.${body}.signature`
}

const SYSTEM_ADMIN_PAYLOAD = {
  sub: 'user-123',
  preferred_username: 'admin',
  email: 'admin@example.com',
  realm_access: { roles: ['systemAdmin'] },
  exp: Math.floor(Date.now() / 1000) + 3600,
}

const ORG_USER_PAYLOAD = {
  sub: 'user-456',
  preferred_username: 'alice',
  email: 'alice@example.com',
  realm_access: { roles: ['orgUser'] },
  exp: Math.floor(Date.now() / 1000) + 3600,
}

describe('useAuthStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
    localStorage.clear()
    vi.clearAllMocks()
  })

  describe('initialize()', () => {
    it('parses a stored JWT and sets user and isSystemAdmin', () => {
      const token = makeJwt(SYSTEM_ADMIN_PAYLOAD)
      localStorage.setItem('access_token', token)

      const store = useAuthStore()
      store.initialize()

      expect(store.user).not.toBeNull()
      expect(store.user?.id).toBe('user-123')
      expect(store.user?.role).toBe('systemAdmin')
      expect(store.isSystemAdmin).toBe(true)
    })

    it('sets isSystemAdmin to false for a non-admin JWT', () => {
      const token = makeJwt(ORG_USER_PAYLOAD)
      localStorage.setItem('access_token', token)

      const store = useAuthStore()
      store.initialize()

      expect(store.user?.role).toBe('orgUser')
      expect(store.isSystemAdmin).toBe(false)
    })

    it('leaves user as null when no token is stored', () => {
      const store = useAuthStore()
      store.initialize()

      expect(store.user).toBeNull()
    })
  })

  describe('login()', () => {
    it('stores tokens and sets user on success', async () => {
      const { apiClient } = await import('../api/client')
      const token = makeJwt(SYSTEM_ADMIN_PAYLOAD)
      vi.mocked(apiClient.adminLogin).mockResolvedValueOnce({
        AccessToken: token,
        RefreshToken: 'refresh-xyz',
        ExpiresIn: 3600,
      } as never)

      const store = useAuthStore()
      const result = await store.login('admin', 'password')

      expect(result).toEqual({ success: true })
      expect(store.accessToken).toBe(token)
      expect(store.user?.role).toBe('systemAdmin')
      expect(localStorage.getItem('access_token')).toBe(token)
    })

    it('returns an error message on failure', async () => {
      const { apiClient } = await import('../api/client')
      vi.mocked(apiClient.adminLogin).mockRejectedValueOnce(
        new Error('Invalid credentials'),
      )

      const store = useAuthStore()
      const result = await store.login('admin', 'wrong')

      expect(result.success).toBe(false)
      expect(result.error).toBeTruthy()
      expect(store.accessToken).toBeNull()
    })
  })

  describe('logout()', () => {
    it('clears tokens and user', async () => {
      const { apiClient } = await import('../api/client')
      vi.mocked(apiClient.logout).mockResolvedValueOnce(undefined)

      const token = makeJwt(SYSTEM_ADMIN_PAYLOAD)
      localStorage.setItem('access_token', token)
      localStorage.setItem('refresh_token', 'refresh-abc')

      const store = useAuthStore()
      store.initialize()

      expect(store.user).not.toBeNull()

      await store.logout()

      expect(store.user).toBeNull()
      expect(store.accessToken).toBeNull()
      expect(store.refreshToken).toBeNull()
      expect(localStorage.getItem('access_token')).toBeNull()
    })
  })

  describe('isAuthenticated', () => {
    it('is false before initialize() is called with no stored token', () => {
      const store = useAuthStore()
      expect(store.isAuthenticated).toBe(false)
    })

    it('is true after a valid login', async () => {
      const { apiClient } = await import('../api/client')
      const token = makeJwt(SYSTEM_ADMIN_PAYLOAD)
      vi.mocked(apiClient.adminLogin).mockResolvedValueOnce({
        AccessToken: token,
        RefreshToken: 'refresh-xyz',
        ExpiresIn: 3600,
      } as never)

      const store = useAuthStore()
      await store.login('admin', 'password')

      expect(store.isAuthenticated).toBe(true)
    })
  })
})
