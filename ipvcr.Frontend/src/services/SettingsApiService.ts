import { config } from '../config';
import { AdminPasswordSettings, AppSettings, FfmpegSettings, PlaylistSettings, SchedulerSettings, TlsSettings } from '../types/recordings';
import { AuthService } from './AuthService';

// Base API URL from config
const SETTINGS_API_BASE_URL = config.apiBaseUrl + "/settings";

// Helper for common fetch options
const getCommonOptions = () => ({
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + AuthService.getToken()
  }
});

// Get CSRF token from the page if available
const getCsrfToken = (): string | null => {
  const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
  return tokenElement ? (tokenElement as HTMLInputElement).value : null;
};

// Add CSRF token to request headers if available
const withCsrf = (options: RequestInit): RequestInit => {
  const token = getCsrfToken();
  if (token) {
    return {
      ...options,
      headers: {
        ...options.headers,
        'RequestVerificationToken': token
      }
    };
  }
  return options;
};

// API functions
export const settingsApi = {
  // Get all settings (handles individual endpoint failures gracefully)
  getAllSettings: async (): Promise<AppSettings> => {
    // Initialize with default values
    const result: AppSettings = {
      general: {
        mediaPath: '',
        dataPath: '',
        m3uPlaylistPath: '',
        removeTaskAfterExecution: true
      },
      playlist: {
        m3uPlaylistPath: '',
        playlistAutoUpdateInterval: 24,
        autoReloadPlaylist: false,
        filterEmptyGroups: true
      },
      userManagement: {
        adminUsername: '',
        allowUserRegistration: false,
        maxUsersAllowed: 10
      },
      tls: {
        useSsl: false,
        certificatePath: '',
        certificatePassword: ''
      },
      ffmpeg: {
        fileType: 'mp4',
        codec: 'libx264',
        audioCodec: 'aac',
        videoBitrate: '1000k',
        audioBitrate: '128k',
        resolution: '1280x720',
        frameRate: '30',
        aspectRatio: '16:9',
        outputFormat: 'mp4'
      }
    };

    // Try to fetch scheduler settings
    try {
      console.log('Fetching scheduler settings...');
      const schedulerSettings = await settingsApi.getSchedulerSettings();
      console.log('Scheduler settings received:', schedulerSettings);
      
      result.general = {
        mediaPath: schedulerSettings.mediaPath || '',
        dataPath: schedulerSettings.dataPath || '',
        m3uPlaylistPath: schedulerSettings.m3uPlaylistPath || '',
        removeTaskAfterExecution: schedulerSettings.removeTaskAfterExecution ?? true
      };
      
    } catch (error) {
      console.error('Error fetching scheduler settings:', error);
    }
    
    // Try to fetch playlist settings
    try {
      console.log('Fetching playlist settings...');
      const playlistSettings = await settingsApi.getPlaylistSettings();
      console.log('Playlist settings received:', playlistSettings);
      result.playlist = {
        m3uPlaylistPath: playlistSettings.m3uPlaylistPath || '',
        playlistAutoUpdateInterval: playlistSettings.playlistAutoUpdateInterval || 24,
        autoReloadPlaylist: playlistSettings.autoReloadPlaylist ?? false,
        filterEmptyGroups: playlistSettings.filterEmptyGroups ?? true
      };
    } catch (error) {
      console.error('Error fetching playlist settings:', error);
    }
    
    // Try to fetch admin password settings
    try {
      console.log('Fetching admin settings...');
      const adminSettings = await settingsApi.getAdminPasswordSettings();
      console.log('Admin settings received:', adminSettings);
      result.userManagement = {
        adminUsername: adminSettings.adminUsername ?? 'admin',
        allowUserRegistration: adminSettings.allowUserRegistration ?? false,
        maxUsersAllowed: adminSettings.maxUsersAllowed ?? 10
      };
    } catch (error) {
      console.error('Error fetching admin settings:', error);
    }
    
    // Try to fetch SSL settings
    try {
      console.log('Fetching SSL settings...');
      const sslSettings = await settingsApi.getSslSettings();
      console.log('SSL settings received:', sslSettings);
      
      // Since we've updated the TlsSettings interface to match the server response,
      // we can now directly assign the values without property name mapping
      result.tls = sslSettings;
      
      console.log('Mapped SSL settings:', result.tls);
    } catch (error) {
      console.error('Error fetching SSL settings:', error);
    }
    
    // Try to fetch FFmpeg settings
    try {
      console.log('Fetching FFmpeg settings...');
      const ffmpegSettings = await settingsApi.getFfmpegSettings();
      console.log('FFmpeg settings received:', ffmpegSettings);
      result.ffmpeg = ffmpegSettings;
    } catch (error) {
      console.error('Error fetching FFmpeg settings:', error);
    }
    
    console.log('All settings combined:', result);
    return result;
  },
  
  // Get scheduler settings
  getSchedulerSettings: async (): Promise<SchedulerSettings> => {
    try {
      const response = await fetch(`${SETTINGS_API_BASE_URL}/scheduler`, getCommonOptions());
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to access scheduler settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Server error ${response.status}: ${errorText || response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Error in getSchedulerSettings:', error);
      throw error;
    }
  },
  
  // Update scheduler settings
  updateSchedulerSettings: async (settings: SchedulerSettings): Promise<void> => {
    try {
      const options = withCsrf({
        ...getCommonOptions(),
        method: 'PUT',
        body: JSON.stringify(settings)
      });

      const response = await fetch(`${SETTINGS_API_BASE_URL}/scheduler`, options);
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to update scheduler settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update scheduler settings: ${errorText || response.statusText}`);
      }
    } catch (error) {
      console.error('Error in updateSchedulerSettings:', error);
      throw error;
    }
  },
  
  // Get playlist settings
  getPlaylistSettings: async (): Promise<PlaylistSettings> => {
    try {
      const response = await fetch(`${SETTINGS_API_BASE_URL}/playlist`, getCommonOptions());
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to access playlist settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Server error ${response.status}: ${errorText || response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Error in getPlaylistSettings:', error);
      throw error;
    }
  },
  
  // Update playlist settings
  updatePlaylistSettings: async (settings: PlaylistSettings): Promise<void> => {
    try {
      const options = withCsrf({
        ...getCommonOptions(),
        method: 'PUT',
        body: JSON.stringify(settings)
      });

      const response = await fetch(`${SETTINGS_API_BASE_URL}/playlist`, options);
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to update playlist settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update playlist settings: ${errorText || response.statusText}`);
      }
    } catch (error) {
      console.error('Error in updatePlaylistSettings:', error);
      throw error;
    }
  },
  
  // Get SSL settings
  getSslSettings: async (): Promise<TlsSettings> => {
    try {
      const response = await fetch(`${SETTINGS_API_BASE_URL}/ssl`, getCommonOptions());
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to access SSL settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Server error ${response.status}: ${errorText || response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Error in getSslSettings:', error);
      throw error;
    }
  },
  
  // Update SSL settings
  updateSslSettings: async (settings: TlsSettings): Promise<void> => {
    try {
      // No need to convert property names anymore since we're using the same ones as the server
      const options = withCsrf({
        ...getCommonOptions(),
        method: 'PUT',
        body: JSON.stringify(settings)
      });

      console.log('Sending SSL settings to server:', settings);
      const response = await fetch(`${SETTINGS_API_BASE_URL}/ssl`, options);
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to update SSL settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update SSL settings: ${errorText || response.statusText}`);
      }
    } catch (error) {
      console.error('Error in updateSslSettings:', error);
      throw error;
    }
  },
  
  // Upload SSL certificate
  uploadSslCertificate: async (file: File): Promise<{ message: string, path: string }> => {
    try {
      const formData = new FormData();
      formData.append('file', file);

      const options = withCsrf({
        ...getCommonOptions(),
        method: 'POST',
        body: formData,
        // Don't set Content-Type as it will be automatically set with the boundary for FormData
        headers: {} 
      });

      console.log('Uploading certificate file:', file.name);
      const response = await fetch(`${SETTINGS_API_BASE_URL}/ssl/certificate`, options);
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to upload certificates.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to upload certificate: ${errorText || response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Error uploading certificate:', error);
      throw error;
    }
  },

  // Regenerate a self-signed SSL certificate
  regenerateSelfSignedCertificate: async (): Promise<{ message: string, path: string }> => {
    try {
      const options = withCsrf({
        ...getCommonOptions(),
        method: 'POST'
      });

      console.log('Requesting self-signed certificate generation');
      const response = await fetch(`${SETTINGS_API_BASE_URL}/ssl/generate-certificate`, options);
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to generate certificates.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to generate certificate: ${errorText || response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Error generating self-signed certificate:', error);
      throw error;
    }
  },
  
  // Get admin password settings
  getAdminPasswordSettings: async (): Promise<AdminPasswordSettings> => {
    try {
      const response = await fetch(`${SETTINGS_API_BASE_URL}/adminsettings`, getCommonOptions());
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to access admin settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Server error ${response.status}: ${errorText || response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Error in getAdminPasswordSettings:', error);
      throw error;
    }
  },
  
  // Update admin password settings
  updateAdminPasswordSettings: async (settings: AdminPasswordSettings): Promise<void> => {
    try {
      const options = withCsrf({
        ...getCommonOptions(),
        method: 'PUT',
        body: JSON.stringify(settings)
      });

      console.log('Sending admin settings to server:', settings);
      const response = await fetch(`${SETTINGS_API_BASE_URL}/adminsettings`, options);
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to update admin settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update admin settings: ${errorText || response.statusText}`);
      }
    } catch (error) {
      console.error('Error in updateAdminPasswordSettings:', error);
      throw error;
    }
  },
  
  // Upload M3U playlist
  uploadM3uPlaylist: async (file: File): Promise<{ message: string }> => {
    try {
      const formData = new FormData();
      formData.append('file', file);

      const options = withCsrf({
        method: 'POST',
        body: formData,
        // Don't set Content-Type header when using FormData
        headers: {
          'RequestVerificationToken': getCsrfToken() || '',
          'Authorization': `Bearer ${AuthService.getToken()}`, 
        }
      });

      console.log('Uploading M3U playlist file:', file.name);
      const response = await fetch(`${SETTINGS_API_BASE_URL}/upload-m3u`, options);
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to upload M3U files.');
      } else if (response.status === 400) {
        const errorText = await response.text();
        throw new Error(`Invalid file or configuration: ${errorText || 'Please check your file and settings'}`);
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Server error ${response.status}: ${errorText || response.statusText}`);
      }

      const result = await response.json();
      console.log('M3U upload successful:', result);
      return result;
    } catch (error) {
      console.error('Error in uploadM3uPlaylist:', error);
      throw error;
    }
  },

  // Get FFmpeg settings
  getFfmpegSettings: async (): Promise<FfmpegSettings> => {
    try {
      const response = await fetch(`${SETTINGS_API_BASE_URL}/ffmpeg`, getCommonOptions());
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to access FFmpeg settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Server error ${response.status}: ${errorText || response.statusText}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error('Error in getFfmpegSettings:', error);
      throw error;
    }
  },
  
  // Update FFmpeg settings
  updateFfmpegSettings: async (settings: FfmpegSettings): Promise<void> => {
    try {
      const options = withCsrf({
        ...getCommonOptions(),
        method: 'PUT',
        body: JSON.stringify(settings)
      });

      console.log('Sending FFmpeg settings to server:', settings);
      const response = await fetch(`${SETTINGS_API_BASE_URL}/ffmpeg`, options);
      
      if (response.status === 401) {
        throw new Error('Authentication required. Please log in again.');
      } else if (response.status === 403) {
        throw new Error('You do not have permission to update FFmpeg settings.');
      } else if (!response.ok) {
        const errorText = await response.text();
        throw new Error(`Failed to update FFmpeg settings: ${errorText || response.statusText}`);
      }
    } catch (error) {
      console.error('Error in updateFfmpegSettings:', error);
      throw error;
    }
  }
};