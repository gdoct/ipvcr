import { config } from '../config';
import { HomeRecordingsViewModel, ScheduledRecording, TaskDefinitionModel } from '../types/recordings';
import { AuthService } from './AuthService';
import { settingsApi } from './SettingsApiService';

// Base API URL from config
const RECORDINGAPI_BASE_URL = config.apiBaseUrl + "/recordings";

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
export const recordingsApi = {
  // Get all recordings
  getAllRecordings: async (): Promise<ScheduledRecording[]> => {
    const response = await fetch(`${RECORDINGAPI_BASE_URL}`, getCommonOptions());
    if (!response.ok) throw new Error('Failed to fetch recordings');
    return await response.json();
  },

  // Get a single recording
  getRecording: async (id: string): Promise<ScheduledRecording> => {
    const response = await fetch(`${RECORDINGAPI_BASE_URL}/${id}`, getCommonOptions());
    if (!response.ok) throw new Error('Failed to fetch recording');
    return await response.json();
  },

  // Create a new recording
  createRecording: async (recording: ScheduledRecording): Promise<ScheduledRecording> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'POST',
      body: JSON.stringify(recording)
    });

    const response = await fetch(`${RECORDINGAPI_BASE_URL}`, options);
    if (!response.ok) throw new Error('Failed to create recording');

    return await response.json();
  },

  // Update an existing recording
  updateRecording: async (id: string, recording: ScheduledRecording): Promise<void> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'PUT',
      body: JSON.stringify(recording)
    });

    const response = await fetch(`${RECORDINGAPI_BASE_URL}/${id}`, options);
    if (!response.ok) throw new Error('Failed to update recording');
  },

  // Delete a recording
  deleteRecording: async (id: string): Promise<void> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'DELETE'
    });

    const response = await fetch(`${RECORDINGAPI_BASE_URL}/${id}`, options);
    if (!response.ok) throw new Error('Failed to delete recording');
  },

  // Get task definition
  getTaskDefinition: async (id: string): Promise<TaskDefinitionModel> => {
    const response = await fetch(`${RECORDINGAPI_BASE_URL}/task/${id}`, getCommonOptions());
    if (!response.ok) throw new Error('Failed to fetch task definition');
    return await response.json();
  },

  // Update task definition
  updateTaskDefinition: async (id: string, taskFile: string): Promise<void> => {
    const options = withCsrf({
      ...getCommonOptions(),
      method: 'PUT',
      body: JSON.stringify({ id, taskfile: taskFile })
    });

    const response = await fetch(`${RECORDINGAPI_BASE_URL}/task/${id}`, options);
    if (!response.ok) throw new Error('Failed to update task definition');
  },

  // Get channel count
  getChannelCount: async (): Promise<number> => {
    try {
      const response = await fetch(`${RECORDINGAPI_BASE_URL}/channelcount`, {
        ...getCommonOptions(),
        headers: {
          ...getCommonOptions().headers,
          'Accept': 'application/json'
        }
      });

      if (!response.ok) return 0;
      // the backend returns a number
      const count = parseInt(await response.text());
      return count;
    } catch (error) {
      console.error('Error fetching channel count:', error);
      return 0;
    }
  },

  // Get home recordings view model (combines channels and recordings)
  getHomeRecordingsViewModel: async (): Promise<HomeRecordingsViewModel> => {
    // In a real implementation, this might be a single API call
    // Here we combine multiple calls for demonstration
    const [recordings, channelsCount, schedulerSettings] = await Promise.all([
      recordingsApi.getAllRecordings(),
      recordingsApi.getChannelCount(),
      settingsApi.getSchedulerSettings()
    ]);

    return {
      recordings,
      channelsCount,
      recordingPath: schedulerSettings.mediaPath || '/recordings'
    };
  }
};