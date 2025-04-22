import { config } from '../config';
import { ChannelInfo } from '../types/recordings';
import { AuthService } from './AuthService';
// Use the base URL from the config
const RECORDINGAPI_BASE_URL = config.apiBaseUrl + "/recordings";

export const searchChannels = async (query: string): Promise<ChannelInfo[]> => {
  // Always ensure we have a valid query string
  if (!query || query.trim() === '') return [];
  
  try {
    console.log(`Searching channels with query: "${query}" at ${RECORDINGAPI_BASE_URL}/channels/search`);
    
    const response = await fetch(`${RECORDINGAPI_BASE_URL}/channels/search?query=${encodeURIComponent(query)}`, {
      method: 'GET',
      headers: {
        'Accept': 'application/json',
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + AuthService.getToken()
      },
    });
    
    if (!response.ok) {
      console.error(`Error searching channels: ${response.status} ${response.statusText}`);
      // Try to get more error details if available
      try {
        const errorText = await response.text();
        console.error(`Error response body: ${errorText}`);
      } catch (textError) {
        console.error('Could not read error response body');
      }
      throw new Error(`Error ${response.status}: ${response.statusText}`);
    }
    
    const data = await response.json();
    console.log(`Found ${data.length} channels matching query: "${query}"`);
    return data;
  } catch (error) {
    console.error('Error searching channels:', error);
    return [];
  }
};

export const getChannelLogo = async (channelUri: string, channelName: string): Promise<string> => {
  try {
    // Verify we have a valid channel name
    if (!channelName || channelName.trim() === '') {
      console.log('Channel name is empty, cannot search for logo');
      return '';
    }
    
    // Search with exact channel name to find the logo immediately
    console.log(`Searching for channel logo with exact name: "${channelName}"`);
    
    // Use the full channel name for the search
    const results = await searchChannels(channelName.trim());
    
    // First try exact match on URI
    let match = results.find(c => c.uri === channelUri);
    
    // If no exact URI match, try exact name match
    if (!match) {
      match = results.find(c => c.name === channelName);
    }
    
    // If still no match, use the first result if available
    if (!match && results.length > 0) {
      match = results[0];
      console.log(`No exact match found, using first result: ${match.name}`);
    }
    
    if (match && match.logo) {
      console.log(`Found logo for channel ${channelName}: ${match.logo}`);
      return match.logo;
    }
    
    console.log(`No logo found for channel: ${channelName}`);
    return ''; // No logo found
  } catch (error) {
    console.error('Error fetching channel logo:', error);
    return '';
  }
};