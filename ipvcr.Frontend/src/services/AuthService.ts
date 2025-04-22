import { config } from '../config';
const AUTH_TOKEN_KEY = 'auth_token';

interface LoginResponse {
  token: string;
}

interface LoginRequest {
  username: string;
  password: string;
}

interface RestartResponse {
  message: string;
}

const AUTH_BASE_URI = config.apiBaseUrl;

export const AuthService = {
  login: async (username: string, password: string): Promise<boolean> => {
    try {
      const response = await fetch(`${AUTH_BASE_URI}/login`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password } as LoginRequest),
      });

      if (!response.ok) {
        return false;
      }

      const data: LoginResponse = await response.json();
      // Store token synchronously to ensure it's available immediately
      localStorage.setItem(AUTH_TOKEN_KEY, data.token);
      return true;
    } catch (error) {
      console.error('Login error:', error);
      return false;
    }
  },

  logout: (): void => {
    // Remove token from localStorage
    localStorage.removeItem(AUTH_TOKEN_KEY);
    
    // Force reload of the application to ensure clean state
    window.location.href = '/login';
  },

  isAuthenticated: (): boolean => {
    return !!localStorage.getItem(AUTH_TOKEN_KEY);
  },

  getToken: (): string | null => {
    return localStorage.getItem(AUTH_TOKEN_KEY);
  },

  setToken: (token: string): void => {
    localStorage.setItem(AUTH_TOKEN_KEY, token);
  },
  
  validateToken: async (): Promise<boolean> => {
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    
    // If no token exists, token is invalid
    if (!token) {
      return false;
    }
    
    try {
      // JWT tokens are structured as header.payload.signature
      const parts = token.split('.');
      if (parts.length !== 3) {
        // Not a valid JWT token format
        AuthService.logout();
        return false;
      }
      
      // Parse the payload to check expiration
      const payload = JSON.parse(atob(parts[1]));
      
      // Check if token has an expiration claim
      if (payload.exp) {
        // exp is in seconds since epoch, current time is in milliseconds
        const currentTime = Math.floor(Date.now() / 1000);
        
        if (payload.exp < currentTime) {
          console.log('Token expired, redirecting to login');
          AuthService.logout();
          return false;
        }
      }
      
      // Skip server-side validation which may cause freezing issues
      // Just check the token format and expiration locally
      return true;
    } catch (error) {
      console.error('Token validation error:', error);
      AuthService.logout();
      return false;
    }
  },

  restartServer: async (): Promise<boolean> => {
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    if (!token) {
      return false;
    }

    try {
      const response = await fetch(`${AUTH_BASE_URI}/login/restart`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        console.error('Failed to restart server:', response.statusText);
        return false;
      }

      const data: RestartResponse = await response.json();
      console.log(data.message);
      return true;
    } catch (error) {
      console.error('Restart server error:', error);
      return false;
    }
  },
  
  resetToDefaults: async (): Promise<boolean> => {
    const token = localStorage.getItem(AUTH_TOKEN_KEY);
    if (!token) {
      return false;
    }

    try {
      const response = await fetch(`${AUTH_BASE_URI}/login/resetdefault`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        console.error('Failed to reset to defaults:', response.statusText);
        return false;
      }

      const data: RestartResponse = await response.json();
      console.log(data.message);
      return true;
    } catch (error) {
      console.error('Reset to defaults error:', error);
      return false;
    }
  }
};
