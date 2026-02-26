import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { apiClient } from "../api/client";

interface User {
  id: string;
  username: string;
  email: string;
  role: string;
}

export const useAuthStore = defineStore("auth", () => {
  const user = ref<User | null>(null);
  const accessToken = ref<string | null>(localStorage.getItem("access_token"));
  const refreshToken = ref<string | null>(
    localStorage.getItem("refresh_token"),
  );
  const tokenExpiresAt = ref<number | null>(
    localStorage.getItem("token_expires_at")
      ? parseInt(localStorage.getItem("token_expires_at")!)
      : null,
  );

  const isAuthenticated = computed(() => !!accessToken.value && !!user.value);
  const isSystemAdmin = computed(() => user.value?.role === "systemAdmin");

  // Parse JWT token to get user info and expiration
  const parseJwt = (token: string) => {
    try {
      const base64Url = token.split(".")[1];
      if (!base64Url) {
        return null;
      }
      const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
      const jsonPayload = decodeURIComponent(
        atob(base64)
          .split("")
          .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
          .join(""),
      );
      return JSON.parse(jsonPayload);
    } catch {
      return null;
    }
  };

  // Set user from token
  const setUserFromToken = (token: string) => {
    const payload = parseJwt(token);
    if (payload) {
      let role = "user";
      if (payload.realm_access && payload.realm_access.roles) {
        const roles = payload.realm_access.roles;
        if (roles.includes("systemAdmin")) {
          role = "systemAdmin";
        } else if (roles.includes("orgAdmin")) {
          role = "orgAdmin";
        } else if (roles.includes("orgUser")) {
          role = "orgUser";
        }
      }

      user.value = {
        id: payload.sub || payload.userId,
        username: payload.preferred_username || payload.username,
        email: payload.email || "",
        role: role,
      };
      tokenExpiresAt.value = payload.exp * 1000;
      localStorage.setItem("token_expires_at", tokenExpiresAt.value.toString());
    }
  };

  const login = async (username: string, password: string) => {
    try {
      const response = await apiClient.adminLogin({
        Username: username,
        Password: password,
      });

      const { AccessToken: newAccessToken, RefreshToken: newRefreshToken } =
        response;

      if (!newAccessToken || !newRefreshToken) {
        return {
          success: false,
          error: "Invalid response from server",
        };
      }

      accessToken.value = newAccessToken;
      refreshToken.value = newRefreshToken;

      localStorage.setItem("access_token", newAccessToken);
      localStorage.setItem("refresh_token", newRefreshToken);

      setUserFromToken(newAccessToken);

      return { success: true };
    } catch (error: any) {
      return {
        success: false,
        error: error.message || "Login failed",
      };
    }
  };

  const logout = async () => {
    try {
      if (refreshToken.value) {
        await apiClient.logout({
          RefreshToken: refreshToken.value,
        });
      }
    } catch {
      // Logout completes via finally regardless of API error
    } finally {
      user.value = null;
      accessToken.value = null;
      refreshToken.value = null;
      tokenExpiresAt.value = null;

      localStorage.removeItem("access_token");
      localStorage.removeItem("refresh_token");
      localStorage.removeItem("token_expires_at");
    }
  };

  // Initialize auth state from stored tokens
  const initialize = () => {
    if (accessToken.value) {
      setUserFromToken(accessToken.value);
    }
  };

  return {
    user,
    accessToken,
    refreshToken,
    isAuthenticated,
    isSystemAdmin,
    login,
    logout,
    initialize,
  };
});
