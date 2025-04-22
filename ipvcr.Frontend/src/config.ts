/**
 * Application configuration with environment-specific settings
 */

// Determine the base API URL based on the environment
let apiBaseUrl = '';

// if in node development mode, and the local port is 3000, use localhost:5000/api instead of localhost:3000/api
if (process.env.NODE_ENV === 'development' && window.location.port === '3000') {
  apiBaseUrl = 'http://localhost:5000/api';
} else if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
  apiBaseUrl = '/api';
} else {
  // In production, use relative URLs which will use the same domain
  apiBaseUrl = '/api';
}

export const config = {
  apiBaseUrl,
};