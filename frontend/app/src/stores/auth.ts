import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import router from '../router'
import { authApi } from '../services/api'
import { apiClient } from '../api/http-client'
import { UserRole } from '../api/generated/api-client'
import { toast } from 'vue-sonner'

interface LoginCredentials {
  username: string
  password: string
}

export interface UserInfo {
  id: string
  username: string
  email: string
  firstName: string
  lastName: string
  enabled: boolean
  roles: string[]
}

export const useAuthStore = defineStore('auth', () => {
  const accessToken = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const tokenExpirationTimer = ref<number | null>(null)
  const isAdmin = ref(false)
  const username = ref<string>('')
  const userId = ref<string | null>(null)
  
  const TOKEN_EXPIRED_FLAG = 'token_expired_redirect'
  const IS_ADMIN_FLAG = 'is_admin_user'

  const isAuthenticated = computed(() => {
    if (!accessToken.value) {
      loadTokenFromStorage()
    }
    return !!accessToken.value && !isTokenExpired()
  })

  // Actions
  function loadTokenFromStorage() {
    accessToken.value = localStorage.getItem('access_token')
    refreshToken.value = localStorage.getItem('refresh_token')
    isAdmin.value = localStorage.getItem(IS_ADMIN_FLAG) === 'true'
    
    if (accessToken.value) {
      const decoded = decodeJWT(accessToken.value)
      if (decoded) {
        username.value = decoded.preferred_username || decoded.username || decoded.sub
        userId.value = decoded.sub || null
      }
    }
  }

  function isTokenExpired(): boolean {
    const expirationTime = localStorage.getItem('token_expiration')
    if (!expirationTime) return true
    
    const expiration = parseInt(expirationTime, 10)
    return Date.now() >= expiration
  }

  function decodeJWT(token: string): any {
    try {
      const base64Url = token.split('.')[1]
      if (!base64Url) return null
      
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      )
      return JSON.parse(jsonPayload)
    } catch (error) {
      // Silently fail JWT decoding - this is expected for invalid tokens
      return null
    }
  }

  function setupTokenExpirationCheck(expiresIn: number) {
    // Clear any existing timer
    if (tokenExpirationTimer.value) {
      clearTimeout(tokenExpirationTimer.value)
    }
    
    // Set timer to logout when token expires
    // Subtract 5 seconds to logout slightly before actual expiration
    const timeoutDuration = Math.max((expiresIn - 5) * 1000, 0)
    
    tokenExpirationTimer.value = window.setTimeout(() => {
      toast.info('Your session has expired. Please log in again.')
      logout(true)
    }, timeoutDuration)
  }

  function initializeExpirationCheck() {
    const expirationTime = localStorage.getItem('token_expiration')
    if (!expirationTime) return
    
    const expiration = parseInt(expirationTime, 10)
    const now = Date.now()
    
    if (now >= expiration) {
      // Token already expired
      logout(true)
    } else {
      // Set up timer for remaining time
      const remainingTime = Math.floor((expiration - now) / 1000)
      setupTokenExpirationCheck(remainingTime)
    }
  }

  async function login(credentials: LoginCredentials, isAdminLogin: boolean = false): Promise<boolean> {
    try {
      const loginFn = isAdminLogin ? authApi.adminLogin : authApi.login
      const response = await loginFn(credentials.username, credentials.password)

      if (!response.access_token || !response.refresh_token || !response.expires_in) {
        toast.error('Invalid response from server. Please try again.')
        return false
      }

      accessToken.value = response.access_token
      refreshToken.value = response.refresh_token
      
      // Calculate expiration time
      const expirationTime = Date.now() + (response.expires_in * 1000)
      
      localStorage.setItem('access_token', accessToken.value)
      localStorage.setItem('refresh_token', refreshToken.value)
      localStorage.setItem('token_expiration', expirationTime.toString())
      
      // Store admin flag if admin login
      if (isAdminLogin) {
        localStorage.setItem(IS_ADMIN_FLAG, 'true')
        isAdmin.value = true
      } else {
        localStorage.removeItem(IS_ADMIN_FLAG)
        isAdmin.value = false
      }
      
      // Decode token to get user info
      const decoded = decodeJWT(accessToken.value)
      if (decoded) {
        username.value = decoded.preferred_username || decoded.username || decoded.sub
        userId.value = decoded.sub || null
      }
      
      // Reset sidebar to expanded state on successful login
      localStorage.setItem('sidebarCollapsed', 'false')
      
      // Set up automatic logout when token expires
      setupTokenExpirationCheck(response.expires_in)

      return true
    } catch (error: any) {
      // Extract error message from problem details response
      const errorMessage = error?.response?.data?.detail
        || error?.response?.data?.title
        || error?.response?.data?.message
        || 'Login failed. Please check your credentials and try again.'
      
      toast.error(errorMessage)
      return false
    }
  }

  async function logout(dueToExpiration: boolean = false) {
    // Get refresh token before clearing
    const currentRefreshToken = refreshToken.value || localStorage.getItem('refresh_token')
    
    // Call AuthAPI logout endpoint if we have a refresh token
    if (currentRefreshToken) {
      try {
        await authApi.logout(currentRefreshToken)
      } catch (error: any) {
        // Extract error message from problem details response
        const errorMessage = error?.response?.data?.detail
          || error?.response?.data?.title
          || error?.response?.data?.message
        
        if (errorMessage) {
          toast.error(`Logout error: ${errorMessage}`)
        }
        // Continue with local cleanup even if server logout fails
      }
    }
    
    // Clear local state
    accessToken.value = null
    refreshToken.value = null
    username.value = ''
    userId.value = null
    isAdmin.value = false
    
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
    localStorage.removeItem('token_expiration')
    localStorage.removeItem(IS_ADMIN_FLAG)
    
    // Clear the expiration timer
    if (tokenExpirationTimer.value) {
      clearTimeout(tokenExpirationTimer.value)
      tokenExpirationTimer.value = null
    }
    
    // Set flag if logout is due to token expiration
    if (dueToExpiration) {
      sessionStorage.setItem(TOKEN_EXPIRED_FLAG, 'true')
    }
    
    // Redirect to login page
    if (router.currentRoute.value.path !== '/login') {
      router.push('/login')
    }
  }

  function getAccessToken(): string | null {
    if (!accessToken.value) {
      accessToken.value = localStorage.getItem('access_token')
    }
    return accessToken.value
  }

  function getUserInfo(): { username: string } | null {
    const token = getAccessToken()
    if (!token) return null

    try {
      const decodedToken = decodeJWT(token)
      if (!decodedToken) return null

      const user = decodedToken.preferred_username || decodedToken.username || decodedToken.sub
      return { username: user }
    } catch (error: any) {
      // Extract error message from problem details response
      const errorMessage = error?.response?.data?.detail
        || error?.response?.data?.title
        || error?.response?.data?.message
        || 'Failed to get user information'
      
      toast.error(errorMessage)
      return null
    }
  }

  function getCurrentUserId(): string | null {
    const token = getAccessToken()
    if (!token) return null

    try {
      const decodedToken = decodeJWT(token)
      if (!decodedToken) return null

      return decodedToken.sub || null
    } catch (error: any) {
      // Extract error message from problem details response
      const errorMessage = error?.response?.data?.detail
        || error?.response?.data?.title
        || error?.response?.data?.message
        || 'Failed to get current user ID'
      
      toast.error(errorMessage)
      return null
    }
  }

  function wasRedirectedDueToExpiration(): boolean {
    return sessionStorage.getItem(TOKEN_EXPIRED_FLAG) === 'true'
  }

  function clearExpirationFlag() {
    sessionStorage.removeItem(TOKEN_EXPIRED_FLAG)
  }

  async function getAllUsers(): Promise<UserInfo[]> {
    try {
      const token = getAccessToken()
      if (!token) return []

      const response = await apiClient.get<UserInfo[]>('/api/auth/admin/users')

      // Parse the response data if it's a string (due to transformResponse in apiClient)
      const data = typeof response.data === 'string' ? JSON.parse(response.data) : response.data
      
      return data
    } catch (error: any) {
      // Extract error message from problem details response
      const errorMessage = error?.response?.data?.detail
        || error?.response?.data?.title
        || error?.response?.data?.message
        || 'Failed to load users'
      
      toast.error(errorMessage)
      return []
    }
  }

  async function updateUserRole(userId: string, role: string): Promise<boolean> {
    try {
      const token = getAccessToken()
      if (!token) return false

      // Convert string to UserRole enum
      const userRole = role as UserRole
      await authApi.updateUserRole(userId, userRole)

      return true
    } catch (error: any) {
      // Extract error message from problem details response
      const errorMessage = error?.response?.data?.detail
        || error?.response?.data?.title
        || error?.response?.data?.message
        || 'Failed to update user role'
      
      toast.error(errorMessage)
      return false
    }
  }

  async function createUser(userData: {
    username: string
    email: string
    firstName: string
    lastName: string
    password: string
    role: string
  }): Promise<boolean> {
    try {
      const token = getAccessToken()
      if (!token) return false

      await authApi.createUser(userData)

      return true
    } catch (error: any) {
      // Extract error message from problem details response
      const errorMessage = error?.response?.data?.detail
        || error?.response?.data?.title
        || error?.response?.data?.message
        || 'Failed to create user'
      
      toast.error(errorMessage)
      throw error
    }
  }

  async function refreshAccessToken(): Promise<string | null> {
    try {
      const currentRefreshToken = refreshToken.value || localStorage.getItem('refresh_token')
      
      if (!currentRefreshToken) {
        return null
      }

      const response = await authApi.refreshToken(currentRefreshToken)

      if (!response.access_token || !response.refresh_token || !response.expires_in) {
        return null
      }

      accessToken.value = response.access_token
      refreshToken.value = response.refresh_token
      
      // Calculate expiration time
      const expirationTime = Date.now() + (response.expires_in * 1000)
      
      // Store tokens (we know they're not null at this point)
      localStorage.setItem('access_token', response.access_token)
      localStorage.setItem('refresh_token', response.refresh_token)
      localStorage.setItem('token_expiration', expirationTime.toString())
      
      // Decode token to update user info
      const decoded = decodeJWT(response.access_token)
      if (decoded) {
        username.value = decoded.preferred_username || decoded.username || decoded.sub
        userId.value = decoded.sub || null
      }
      
      // Set up automatic logout when token expires
      setupTokenExpirationCheck(response.expires_in)

      return response.access_token
    } catch (error: any) {
      // Don't show toast here - the http-client will handle it
      return null
    }
  }

  return {
    accessToken,
    refreshToken,
    isAdmin,
    username,
    userId,
    isAuthenticated,
    login,
    logout,
    getAccessToken,
    getUserInfo,
    getCurrentUserId,
    initializeExpirationCheck,
    wasRedirectedDueToExpiration,
    clearExpirationFlag,
    getAllUsers,
    updateUserRole,
    createUser,
    loadTokenFromStorage,
    refreshAccessToken,
  }
})
