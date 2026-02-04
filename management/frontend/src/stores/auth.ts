import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import apiClient from '../services/api'

interface User {
  id: string
  username: string
  email: string
  role: string
}

export const useAuthStore = defineStore('auth', () => {
  const user = ref<User | null>(null)
  const accessToken = ref<string | null>(localStorage.getItem('access_token'))
  const refreshToken = ref<string | null>(localStorage.getItem('refresh_token'))
  const tokenExpiresAt = ref<number | null>(
    localStorage.getItem('token_expires_at')
      ? parseInt(localStorage.getItem('token_expires_at')!)
      : null
  )

  const isAuthenticated = computed(() => !!accessToken.value && !!user.value)
  const isSystemAdmin = computed(() => user.value?.role === 'systemAdmin')

  // Parse JWT token to get user info and expiration
  const parseJwt = (token: string) => {
    try {
      const base64Url = token.split('.')[1]
      if (!base64Url) {
        console.error('Invalid JWT token format')
        return null
      }
      const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/')
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split('')
          .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
          .join('')
      )
      return JSON.parse(jsonPayload)
    } catch (error) {
      console.error('Error parsing JWT:', error)
      return null
    }
  }

  // Set user from token
  const setUserFromToken = (token: string) => {
    const payload = parseJwt(token)
    if (payload) {
      // Extract roles from Keycloak's realm_access
      let role = 'user'
      if (payload.realm_access && payload.realm_access.roles) {
        const roles = payload.realm_access.roles
        if (roles.includes('systemAdmin')) {
          role = 'systemAdmin'
        } else if (roles.includes('orgAdmin')) {
          role = 'orgAdmin'
        } else if (roles.includes('orgUser')) {
          role = 'orgUser'
        }
      }
      
      user.value = {
        id: payload.sub || payload.userId,
        username: payload.preferred_username || payload.username,
        email: payload.email || '',
        role: role,
      }
      tokenExpiresAt.value = payload.exp * 1000 // Convert to milliseconds
      localStorage.setItem('token_expires_at', tokenExpiresAt.value.toString())
    }
  }

  // Login function for system admins
  const login = async (username: string, password: string) => {
    try {
      const response = await apiClient.post('/api/auth/admin-login', {
        username,
        password,
      })

      const { accessToken: newAccessToken, refreshToken: newRefreshToken } = response.data

      accessToken.value = newAccessToken
      refreshToken.value = newRefreshToken

      localStorage.setItem('access_token', newAccessToken)
      localStorage.setItem('refresh_token', newRefreshToken)

      setUserFromToken(newAccessToken)

      return { success: true }
    } catch (error: any) {
      console.error('Login error:', error)
      return {
        success: false,
        error: error.response?.data?.message || 'Login failed',
      }
    }
  }

  // Logout function
  const logout = async () => {
    try {
      if (refreshToken.value) {
        await apiClient.post('/api/auth/logout', {
          refreshToken: refreshToken.value,
        })
      }
    } catch (error) {
      console.error('Logout error:', error)
    } finally {
      user.value = null
      accessToken.value = null
      refreshToken.value = null
      tokenExpiresAt.value = null

      localStorage.removeItem('access_token')
      localStorage.removeItem('refresh_token')
      localStorage.removeItem('token_expires_at')
    }
  }

  // Check if token is expired
  const isTokenExpired = () => {
    if (!tokenExpiresAt.value) return true
    return Date.now() >= tokenExpiresAt.value
  }

  // Initialize expiration check
  const initializeExpirationCheck = () => {
    // Check token on initialization
    if (accessToken.value) {
      if (isTokenExpired()) {
        logout()
      } else {
        setUserFromToken(accessToken.value)
      }
    }

    // Set up interval to check token expiration
    setInterval(() => {
      if (isTokenExpired() && accessToken.value) {
        logout()
        window.location.href = '/login'
      }
    }, 60000) // Check every minute
  }

  return {
    user,
    accessToken,
    refreshToken,
    isAuthenticated,
    isSystemAdmin,
    login,
    logout,
    initializeExpirationCheck,
  }
})
