import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import axios from 'axios'
import router from '../router'

const API_URL = 'http://localhost:5000'

interface LoginCredentials {
  username: string
  password: string
}

interface TokenResponse {
  access_token: string
  refresh_token: string
  expires_in: number
  token_type: string
  roles?: string[]
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
  // State
  const accessToken = ref<string | null>(null)
  const refreshToken = ref<string | null>(null)
  const tokenExpirationTimer = ref<number | null>(null)
  const isAdmin = ref(false)
  const username = ref<string>('')
  const userId = ref<string | null>(null)
  
  // Constants
  const TOKEN_EXPIRED_FLAG = 'token_expired_redirect'
  const IS_ADMIN_FLAG = 'is_admin_user'

  // Computed
  const isAuthenticated = computed(() => {
    if (!accessToken.value) {
      // Try to load from localStorage
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
      console.error('Failed to decode JWT:', error)
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
      console.log('Token expired, logging out...')
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
      const endpoint = isAdminLogin ? '/api/auth/admin/login' : '/api/auth/login'
      const response = await axios.post<TokenResponse>(
        `${API_URL}${endpoint}`,
        {
          username: credentials.username,
          password: credentials.password
        },
        {
          headers: {
            'Content-Type': 'application/json',
          },
        }
      )

      accessToken.value = response.data.access_token
      refreshToken.value = response.data.refresh_token
      
      // Calculate expiration time
      const expirationTime = Date.now() + (response.data.expires_in * 1000)
      
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
      setupTokenExpirationCheck(response.data.expires_in)

      return true
    } catch (error) {
      console.error('Login failed:', error)
      return false
    }
  }

  async function logout(dueToExpiration: boolean = false) {
    // Get refresh token before clearing
    const currentRefreshToken = refreshToken.value || localStorage.getItem('refresh_token')
    
    // Call AuthAPI logout endpoint if we have a refresh token
    if (currentRefreshToken) {
      try {
        await axios.post(
          `${API_URL}/api/auth/logout`,
          { refreshToken: currentRefreshToken },
          {
            headers: {
              'Content-Type': 'application/json',
            },
          }
        )
      } catch (error) {
        console.error('Logout request failed:', error)
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
    } catch (error) {
      console.error('Failed to get user info:', error)
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
    } catch (error) {
      console.error('Failed to get current user ID:', error)
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

      const response = await axios.get<UserInfo[]>(
        `${API_URL}/api/auth/admin/users`,
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        }
      )

      return response.data
    } catch (error) {
      console.error('Failed to get users:', error)
      return []
    }
  }

  async function updateUserRole(userId: string, isAdminRole: boolean): Promise<boolean> {
    try {
      const token = getAccessToken()
      if (!token) return false

      await axios.put(
        `${API_URL}/api/auth/admin/users/${userId}/role`,
        { isAdmin: isAdminRole },
        {
          headers: {
            'Authorization': `Bearer ${token}`,
            'Content-Type': 'application/json',
          },
        }
      )

      return true
    } catch (error) {
      console.error('Failed to update user role:', error)
      return false
    }
  }

  return {
    // State
    accessToken,
    refreshToken,
    isAdmin,
    username,
    userId,
    
    // Computed
    isAuthenticated,
    
    // Actions
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
    loadTokenFromStorage,
  }
})
