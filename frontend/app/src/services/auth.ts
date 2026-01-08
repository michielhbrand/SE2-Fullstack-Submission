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
}

class AuthService {
  private accessToken: string | null = null
  private refreshToken: string | null = null
  private tokenExpirationTimer: number | null = null
  private readonly TOKEN_EXPIRED_FLAG = 'token_expired_redirect'

  async login(credentials: LoginCredentials): Promise<boolean> {
    try {
      const response = await axios.post<TokenResponse>(
        `${API_URL}/api/Auth/login`,
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

      this.accessToken = response.data.access_token
      this.refreshToken = response.data.refresh_token
      
      // Calculate expiration time
      const expirationTime = Date.now() + (response.data.expires_in * 1000)
      
      localStorage.setItem('access_token', this.accessToken)
      localStorage.setItem('refresh_token', this.refreshToken)
      localStorage.setItem('token_expiration', expirationTime.toString())
      
      // Reset sidebar to expanded state on successful login
      localStorage.setItem('sidebarCollapsed', 'false')
      
      // Set up automatic logout when token expires
      this.setupTokenExpirationCheck(response.data.expires_in)

      return true
    } catch (error) {
      console.error('Login failed:', error)
      return false
    }
  }

  async logout(dueToExpiration: boolean = false) {
    // Get refresh token before clearing
    const refreshToken = this.refreshToken || localStorage.getItem('refresh_token')
    
    // Call AuthAPI logout endpoint if we have a refresh token
    if (refreshToken) {
      try {
        await axios.post(
          `${API_URL}/api/Auth/logout`,
          { refreshToken },
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
    this.accessToken = null
    this.refreshToken = null
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
    localStorage.removeItem('token_expiration')
    
    // Clear the expiration timer
    if (this.tokenExpirationTimer) {
      clearTimeout(this.tokenExpirationTimer)
      this.tokenExpirationTimer = null
    }
    
    // Set flag if logout is due to token expiration
    if (dueToExpiration) {
      sessionStorage.setItem(this.TOKEN_EXPIRED_FLAG, 'true')
    }
    
    // Redirect to login page
    if (router.currentRoute.value.path !== '/login') {
      router.push('/login')
    }
  }

  getAccessToken(): string | null {
    if (!this.accessToken) {
      this.accessToken = localStorage.getItem('access_token')
    }
    return this.accessToken
  }

  getToken(): string | null {
    return this.getAccessToken()
  }

  isAuthenticated(): boolean {
    const token = this.getAccessToken()
    if (!token) return false
    
    // Check if token is expired
    return !this.isTokenExpired()
  }
  
  private isTokenExpired(): boolean {
    const expirationTime = localStorage.getItem('token_expiration')
    if (!expirationTime) return true
    
    const expiration = parseInt(expirationTime, 10)
    return Date.now() >= expiration
  }
  
  private setupTokenExpirationCheck(expiresIn: number) {
    // Clear any existing timer
    if (this.tokenExpirationTimer) {
      clearTimeout(this.tokenExpirationTimer)
    }
    
    // Set timer to logout when token expires
    // Subtract 5 seconds to logout slightly before actual expiration
    const timeoutDuration = Math.max((expiresIn - 5) * 1000, 0)
    
    this.tokenExpirationTimer = window.setTimeout(() => {
      console.log('Token expired, logging out...')
      this.logout(true)
    }, timeoutDuration)
  }
  
  // Initialize expiration check on app load
  initializeExpirationCheck() {
    const expirationTime = localStorage.getItem('token_expiration')
    if (!expirationTime) return
    
    const expiration = parseInt(expirationTime, 10)
    const now = Date.now()
    
    if (now >= expiration) {
      // Token already expired
      this.logout(true)
    } else {
      // Set up timer for remaining time
      const remainingTime = Math.floor((expiration - now) / 1000)
      this.setupTokenExpirationCheck(remainingTime)
    }
  }

  private decodeJWT(token: string): any {
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

  async getUserInfo(): Promise<{ username: string } | null> {
    const token = this.getAccessToken()
    if (!token) return null

    try {
      // Decode the JWT token to extract user information from Keycloak
      const decodedToken = this.decodeJWT(token)
      if (!decodedToken) return null

      // Keycloak stores username in 'preferred_username' claim
      const username = decodedToken.preferred_username || decodedToken.username || decodedToken.sub
      return { username }
    } catch (error) {
      console.error('Failed to get user info:', error)
      return null
    }
  }

  // Check if the redirect was due to token expiration
  wasRedirectedDueToExpiration(): boolean {
    return sessionStorage.getItem(this.TOKEN_EXPIRED_FLAG) === 'true'
  }

  // Clear the token expiration flag
  clearExpirationFlag() {
    sessionStorage.removeItem(this.TOKEN_EXPIRED_FLAG)
  }
}

export const authService = new AuthService()
