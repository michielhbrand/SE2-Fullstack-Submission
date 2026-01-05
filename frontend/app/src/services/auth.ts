import axios from 'axios'

const KEYCLOAK_URL = 'http://localhost:9090'
const REALM = 'microservices'
const CLIENT_ID = 'frontend-app'
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

  async login(credentials: LoginCredentials): Promise<boolean> {
    try {
      const params = new URLSearchParams()
      params.append('client_id', CLIENT_ID)
      params.append('grant_type', 'password')
      params.append('username', credentials.username)
      params.append('password', credentials.password)

      const response = await axios.post<TokenResponse>(
        `${KEYCLOAK_URL}/realms/${REALM}/protocol/openid-connect/token`,
        params,
        {
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
          },
        }
      )

      this.accessToken = response.data.access_token
      this.refreshToken = response.data.refresh_token
      
      localStorage.setItem('access_token', this.accessToken)
      localStorage.setItem('refresh_token', this.refreshToken)

      return true
    } catch (error) {
      console.error('Login failed:', error)
      return false
    }
  }

  logout() {
    this.accessToken = null
    this.refreshToken = null
    localStorage.removeItem('access_token')
    localStorage.removeItem('refresh_token')
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
    return !!this.getAccessToken()
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
}

export const authService = new AuthService()
