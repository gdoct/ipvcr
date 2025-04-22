import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Alert, Button, Card, Col, Container, Form, InputGroup, ListGroup, Row } from 'react-bootstrap';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import FolderBrowserModal from '../components/FolderBrowserModal';
import FfmpegSettingsComponent from '../components/settings/FfmpegSettingsComponent';
import { searchChannels } from '../services/api';
import { recordingsApi } from '../services/RecordingsApi';
import { settingsApi } from '../services/SettingsApiService';
import { ChannelInfo, FfmpegSettings, formatFileDate, formatNiceDate, ScheduledRecording } from '../types/recordings';

const RecordingFormPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const location = useLocation();
  const baseRecordingPath = location.state?.recordingPath || '';
  
  const isEdit = Boolean(id && id !== '00000000-0000-0000-0000-000000000000');

  // State management
  const [loading, setLoading] = useState<boolean>(isEdit);
  const [error, setError] = useState<string | null>(null);
  const [formData, setFormData] = useState<Partial<ScheduledRecording>>({
    id: id || '00000000-0000-0000-0000-000000000000',
    name: '',
    description: '',
    channelUri: '',
    channelName: '',
    startTime: getTomorrowDatetime(),
    endTime: getTomorrowDatetimePlusHour(),
    filename: '',
    ffmpegSettings: undefined
  });
  
  // Separate FFmpeg settings state to avoid unnecessary renders
  const [ffmpegSettings, setFfmpegSettings] = useState<FfmpegSettings | undefined>(undefined);
  
  // FFmpeg settings state
  const [loadingFfmpegSettings, setLoadingFfmpegSettings] = useState<boolean>(true);
  const [showAdvancedSettings, setShowAdvancedSettings] = useState<boolean>(false);
  
  // Folder browser state
  const [showFolderBrowser, setShowFolderBrowser] = useState(false);
  const [selectedFolder, setSelectedFolder] = useState('');
  
  const [channelLogo, setChannelLogo] = useState<string>('');
  const [channelQuery, setChannelQuery] = useState<string>('');
  const [searchResults, setSearchResults] = useState<ChannelInfo[]>([]);
  const [isSearching, setIsSearching] = useState<boolean>(false);
  const [showDropdown, setShowDropdown] = useState<boolean>(false);
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  
  // Store the last visibility state to prevent layout shifts
  const advancedSettingsRef = useRef<HTMLDivElement>(null);

  // Helper functions for timezone conversion
  const convertUtcToLocal = (utcDateStr: string): string => {
    const date = new Date(utcDateStr);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  const convertLocalToUtc = (localDateStr: string): string => {
    const local = new Date(localDateStr);
    return local.toISOString();
  };

  // Set ffmpeg settings in form data only when needed
  useEffect(() => {
    if (ffmpegSettings) {
      setFormData(prev => ({
        ...prev,
        ffmpegSettings
      }));
    }
  }, [ffmpegSettings]);

  // Load recording data if in edit mode
  useEffect(() => {
    if (isEdit && id) {
      const fetchRecording = async () => {
        try {
          setLoading(true);
          const recording = await recordingsApi.getRecording(id);
          if (recording) {
            // Convert UTC times from server to local timezone for display
            setFormData({
              ...recording,
              startTime: convertUtcToLocal(recording.startTime),
              endTime: convertUtcToLocal(recording.endTime),
            });
            
            // Set FFmpeg settings separately 
            if (recording.ffmpegSettings) {
              setFfmpegSettings(recording.ffmpegSettings);
            }
            
            setChannelQuery(recording.channelName);
            
            // Extract folder from filename
            const folderPath = extractFolderPath(recording.filename);
            setSelectedFolder(folderPath);
            
            // Try to fetch the channel logo
            fetchChannelInfoForName(recording.channelName, recording.channelUri);
          }
        } catch (err) {
          console.error('Error fetching recording:', err);
          setError('Failed to load recording details');
        } finally {
          setLoading(false);
        }
      };
      
      fetchRecording();
    } else {
      // For new recordings, initialize with the base path
      setSelectedFolder(baseRecordingPath);
    }
  }, [id, isEdit, baseRecordingPath]);

  // Helper to extract folder path from filename
  const extractFolderPath = (filename: string): string => {
    if (!filename) return baseRecordingPath;
    
    const lastSlashIndex = filename.lastIndexOf('/');
    if (lastSlashIndex === -1) return baseRecordingPath;
    
    return filename.substring(0, lastSlashIndex);
  };

  // Utility function to fetch channel info by name
  const fetchChannelInfoForName = async (channelName: string, channelUri: string) => {
    if (channelName) {
      setIsSearching(true);
      try {
        // Search with exact channel name
        const results = await searchChannels(channelName);
        
        // Find exact match by URI
        const matchedChannel = results.find(c => c.uri === channelUri);
        if (matchedChannel) {
          setChannelLogo(matchedChannel.logo);
        } else if (results.length > 0) {
          // If no exact match, try to use the first result
          setChannelLogo(results[0].logo);
        }
      } catch (error) {
        console.error('Error fetching channel info:', error);
      } finally {
        setIsSearching(false);
      }
    }
  };

  // Utility function to format date for the date picker
  const formatDateForDatePicker = (dateString: string) => {
    const date = new Date(dateString);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    return `${year}-${month}-${day}T${hours}:${minutes}`;
  };

  // Helper to get tomorrow's date
  function getTomorrowDatetime(): string {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    return tomorrow.toISOString().slice(0, 16);
  }

  // Helper to get tomorrow plus one hour
  function getTomorrowDatetimePlusHour(): string {
    const tomorrowPlusHour = new Date();
    tomorrowPlusHour.setDate(tomorrowPlusHour.getDate() + 1);
    tomorrowPlusHour.setHours(tomorrowPlusHour.getHours() + 1);
    return tomorrowPlusHour.toISOString().slice(0, 16);
  }

  // Helper to parse channel name
  function parseChannelName(fullChannelName: string): string {
    const parts = fullChannelName.split('|');
    if (parts.length > 1) {
      return parts[1].trim();
    }
    return fullChannelName.trim();
  }

  // Load default FFmpeg settings when component mounts - fixed dependency array
  useEffect(() => {
    // Only load settings once when we don't have them
    if (!ffmpegSettings && (!isEdit || !formData.ffmpegSettings)) {
      const loadDefaultFfmpegSettings = async () => {
        try {
          setLoadingFfmpegSettings(true);
          const settings = await settingsApi.getFfmpegSettings();
          setFfmpegSettings(settings);
        } catch (err) {
          console.error('Error fetching FFmpeg settings:', err);
          setError('Failed to load FFmpeg settings. Some default values will be used.');
        } finally {
          setLoadingFfmpegSettings(false);
        }
      };
      
      loadDefaultFfmpegSettings();
    } else if (formData.ffmpegSettings && !ffmpegSettings) {
      setFfmpegSettings(formData.ffmpegSettings);
      setLoadingFfmpegSettings(false);
    }
  }, [isEdit]); // Only depends on isEdit, not on formData.ffmpegSettings

  // Handle FFmpeg settings changes with useCallback to prevent unnecessary rerenders
  const handleFfmpegSettingsChange = useCallback((e: React.ChangeEvent<HTMLSelectElement>) => {
    const { name, value } = e.target;
    
    setFfmpegSettings(prev => {
      if (!prev) return prev;
      return {
        ...prev,
        [name]: value
      };
    });
  }, []);

  // Toggle advanced settings with minimal impact
  const toggleAdvancedSettings = useCallback(() => {
    setShowAdvancedSettings(prev => !prev);
  }, []);

  // Handle channel search with debounce
  const handleChannelSearch = (query: string) => {
    setChannelQuery(query);
    
    // Always show dropdown if we have a query
    if (query) {
      setShowDropdown(true);
    } else {
      setShowDropdown(false);
      setSearchResults([]);
      return;
    }
    
    // Clear previous timeout
    if (searchTimeoutRef.current) {
      clearTimeout(searchTimeoutRef.current);
    }
    
    // Set a new timeout for debouncing
    searchTimeoutRef.current = setTimeout(async () => {
      if (query) {
        setIsSearching(true);
        try {
          const results = await searchChannels(query);
          setSearchResults(results);
        } catch (error) {
          console.error('Error searching channels:', error);
          setSearchResults([]);
        } finally {
          setIsSearching(false);
        }
      } else {
        setSearchResults([]);
      }
    }, 300); // 300ms debounce
  };

  // Handle channel selection
  const handleChannelSelect = (channel: ChannelInfo) => {
    setFormData(prev => ({
      ...prev,
      channelUri: channel.uri,
      channelName: parseChannelName(channel.name)
    }));
    setChannelQuery(parseChannelName(channel.name));
    setChannelLogo(channel.logo || '');
    setShowDropdown(false);
    
    // Update filename and description when channel changes
    updateFilenameAndDescription(
      formData.name || '', 
      formData.startTime || '', 
      parseChannelName(channel.name)
    );
  };

  // Handler for input changes
  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    
    setFormData(prev => ({ ...prev, [name]: value }));
    
    // Update filename and description when necessary fields change
    if (['name', 'startTime'].includes(name)) {
      updateFilenameAndDescription(
        name === 'name' ? value : (formData.name || ''),
        name === 'startTime' ? value : (formData.startTime || ''),
        formData.channelName || ''
      );
    }
  };

  // Handle folder selection
  const handleFolderSelect = (folderPath: string) => {
    setSelectedFolder(folderPath);
    
    // Update filename with the new folder path
    updateFilenameAndDescription(
      formData.name || '',
      formData.startTime || '',
      formData.channelName || '',
      folderPath
    );
  };

  // Update filename and description based on form data
  const updateFilenameAndDescription = (
    name: string,
    startTimeStr: string, 
    channelName: string,
    folderPath?: string
  ) => {
    if (name && startTimeStr && channelName) {
      // Generate filename
      const startDate = new Date(startTimeStr);
      const startTimeFormatted = formatFileDate(startDate) + 
        startTimeStr.split('T')[1].replace(':', '').substring(0, 4);
      
      const sanitizedName = name.replace(/ /g, '_').toLowerCase();
      // Important: Always use the provided folderPath parameter first, fall back to state only if not provided
      const useFolderPath = folderPath !== undefined ? folderPath : selectedFolder || baseRecordingPath;
      const filename = `${useFolderPath}/${sanitizedName}_${startTimeFormatted}.mp4`;
      
      // Generate description
      const niceStartTime = formatNiceDate(startDate);
      const description = `${name} - recorded from ${channelName} at ${niceStartTime}`;
      
      setFormData(prev => ({
        ...prev,
        filename,
        description
      }));
    }
  };

  // Form submission handler
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    // Validate form
    if (!formData.name || !formData.startTime || !formData.endTime || !formData.channelUri) {
      setError('Please fill all required fields');
      return;
    }
    
    try {
      // Ensure FFmpeg settings are included in the submission
      const submissionData = {
        ...formData,
        ffmpegSettings: ffmpegSettings,
        // Convert local times to UTC before submission
        startTime: convertLocalToUtc(formData.startTime || ''),
        endTime: convertLocalToUtc(formData.endTime || '')
      };
      
      // Submit the form
      if (isEdit && id) {
        await recordingsApi.updateRecording(id, submissionData as ScheduledRecording);
      } else {
        await recordingsApi.createRecording(submissionData as ScheduledRecording);
      }
      // Redirect back to recordings page on success
      navigate('/');
    } catch (err) {
      console.error('Error saving recording:', err);
      setError('Failed to save recording');
    }
  };

  // Check if form is valid
  const isFormValid = 
    formData.name && 
    formData.startTime && 
    formData.endTime && 
    formData.channelUri &&
    formData.channelName;

  // Handle cancel button click
  const handleCancel = () => {
    navigate('/');
  };

  // Get the full folder path
  const getFullFolderPath = () => {
    let path = selectedFolder || baseRecordingPath;
    return path.endsWith('/') ? path : path + '/';
  };

  return (
    <Container className="py-3">
      <Card className="border-0 shadow-sm">
        <Card.Header className="bg-dark text-white py-2">
          <h5 className="mb-0">
            {isEdit ? (
              <><i className="bi bi-pencil-square me-2"></i>Edit Recording</>
            ) : (
              <><i className="bi bi-plus-circle me-2"></i>New Recording</>
            )}
          </h5>
        </Card.Header>
        <Card.Body className="p-3">
          {loading ? (
            <div className="text-center py-4">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Loading...</span>
              </div>
            </div>
          ) : (
            <>
              {error && (
                <Alert variant="danger" onClose={() => setError(null)} dismissible className="py-2 mb-3">
                  <i className="bi bi-exclamation-triangle-fill me-2"></i>
                  {error}
                </Alert>
              )}
              
              <Form onSubmit={handleSubmit}>
                <input type="hidden" name="id" value={formData.id || ''} />
                
                <Row className="mb-3">
                  {/* Channel logo display area */}
                  <Col md={2} className="mb-3 text-center">
                    {channelLogo ? (
                      <div className="mb-2">
                        <img 
                          src={channelLogo} 
                          alt="Channel logo"
                          className="img-fluid rounded border" 
                          style={{ maxHeight: '80px', maxWidth: '100%', objectFit: 'contain' }} 
                        />
                      </div>
                    ) : (
                      <div className="bg-light rounded d-flex align-items-center justify-content-center mb-2" 
                        style={{ height: '80px', width: '100%' }}>
                        <i className="bi bi-tv text-muted" style={{ fontSize: '2rem' }}></i>
                      </div>
                    )}
                  </Col>
                  
                  {/* Recording name and channel selection */}
                  <Col md={6}>
                    <Form.Group className="mb-3">
                      <Form.Label className="fw-bold">Recording Name</Form.Label>
                      <Form.Control
                        type="text"
                        name="name"
                        value={formData.name || ''}
                        onChange={handleInputChange}
                        required
                        data-testid="recording-name-input"
                        placeholder="Enter recording name"
                      />
                    </Form.Group>
                    
                    <Form.Group className="mb-3 position-relative">
                      <Form.Label className="fw-bold">Channel</Form.Label>
                      <InputGroup>
                        <Form.Control 
                          type="text"
                          placeholder="Search for a channel..."
                          value={channelQuery}
                          onChange={(e) => handleChannelSearch(e.target.value)}
                          onFocus={() => channelQuery && setShowDropdown(true)}
                          required
                          data-testid="channel-search-input"
                        />
                        {isSearching && (
                          <InputGroup.Text>
                            <div className="spinner-border spinner-border-sm" role="status">
                              <span className="visually-hidden">Loading...</span>
                            </div>
                          </InputGroup.Text>
                        )}
                      </InputGroup>
                      
                      {showDropdown && (
                        <ListGroup 
                          className="position-absolute w-100 mt-1 z-3 shadow channel-dropdown" 
                          style={{ maxHeight: '200px', overflowY: 'auto' }}
                          data-testid="channel-dropdown"
                        >
                          {searchResults.length === 0 ? (
                            <ListGroup.Item className="text-muted">
                              {isSearching ? 'Searching...' : 'No channels found'}
                            </ListGroup.Item>
                          ) : (
                            searchResults.map((channel) => (
                              <ListGroup.Item 
                                key={channel.uri}
                                action
                                onClick={() => handleChannelSelect(channel)}
                                className="d-flex align-items-center"
                                data-test-group="channel-option"
                                data-testid={`channel-option-${channel.name.replace(/\s+/g, '-').toLowerCase()}`}
                              >
                                {channel.logo && (
                                  <img 
                                    src={channel.logo} 
                                    alt="Logo" 
                                    className="me-2" 
                                    style={{ height: '24px', width: '24px', objectFit: 'contain' }}
                                  />
                                )}
                                <span>{channel.name}</span>
                              </ListGroup.Item>
                            ))
                          )}
                        </ListGroup>
                      )}
                    </Form.Group>
                  </Col>
                  
                  {/* Schedule times */}
                  <Col md={4}>
                    <Form.Group className="mb-3">
                      <Form.Label className="fw-bold">Start Time</Form.Label>
                      <Form.Control
                        type="datetime-local"
                        name="startTime"
                        value={formData.startTime || ''}
                        onChange={handleInputChange}
                        required
                      />
                    </Form.Group>
                    
                    <Form.Group className="mb-3">
                      <Form.Label className="fw-bold">End Time</Form.Label>
                      <Form.Control
                        type="datetime-local"
                        name="endTime"
                        value={formData.endTime || ''}
                        onChange={handleInputChange}
                        required
                      />
                    </Form.Group>
                  </Col>
                </Row>

                {/* Save location */}
                <Form.Group className="mb-3">
                  <Form.Label className="fw-bold d-flex align-items-center">
                    <i className="bi bi-folder me-2 text-primary"></i>Save Location
                  </Form.Label>
                  <InputGroup>
                    <InputGroup.Text className="bg-dark text-white">
                      <i className="bi bi-folder"></i>
                    </InputGroup.Text>
                    <Form.Control
                      readOnly
                      value={baseRecordingPath + (selectedFolder ? '/' + selectedFolder : '')}
                      className="bg-light"
                    />
                    <Button 
                      variant="outline-primary" 
                      onClick={() => setShowFolderBrowser(true)}
                      title="Browse folders"
                    >
                      <i className="bi bi-folder-symlink"></i>
                    </Button>
                  </InputGroup>
                </Form.Group>

                {/* Encoding settings card */}
                <Card className="mb-3 mt-3">
                  <Card.Header 
                    className="bg-light py-2 d-flex justify-content-between align-items-center"
                    onClick={toggleAdvancedSettings}
                    style={{ cursor: 'pointer' }}
                  >
                    <div>
                      <i className="bi bi-film me-2"></i>
                      <strong>Encoding Settings</strong>
                    </div>
                    <Button 
                      variant="outline-secondary" 
                      size="sm" 
                      onClick={(e) => {
                        e.stopPropagation();
                        toggleAdvancedSettings();
                      }}
                    >
                      {showAdvancedSettings ? (
                        <i className="bi bi-chevron-up"></i>
                      ) : (
                        <i className="bi bi-chevron-down"></i>
                      )}
                    </Button>
                  </Card.Header>
                  
                  <div 
                    ref={advancedSettingsRef}
                    style={{ 
                      display: showAdvancedSettings ? 'block' : 'none',
                      transition: 'height 0.3s ease-in-out',
                      overflow: 'hidden'
                    }}
                  >
                    <Card.Body className="p-2 border-top">
                      {loadingFfmpegSettings ? (
                        <div className="text-center py-2">
                          <div className="spinner-border spinner-border-sm text-primary" role="status">
                            <span className="visually-hidden">Loading...</span>
                          </div>
                        </div>
                      ) : ffmpegSettings ? (
                        <FfmpegSettingsComponent
                          settings={ffmpegSettings}
                          handleInputChange={handleFfmpegSettingsChange}
                        />
                      ) : (
                        <Alert variant="warning" className="py-2 mb-0">
                          <i className="bi bi-exclamation-triangle me-2"></i>
                          Failed to load encoding settings.
                        </Alert>
                      )}
                    </Card.Body>
                  </div>
                </Card>
                
                {/* Action buttons */}
                <div className="d-flex justify-content-end mt-3">
                  <Button variant="outline-secondary" className="me-2" onClick={handleCancel}>
                    Cancel
                  </Button>
                  <Button 
                    variant="primary" 
                    type="submit" 
                    disabled={!isFormValid}
                    data-testid="save-recording-btn"
                  >
                    <i className="bi bi-save me-1"></i>
                    Save Recording
                  </Button>
                </div>
              </Form>
            </>
          )}
        </Card.Body>
      </Card>

      {/* Folder browser modal */}
      <FolderBrowserModal
        show={showFolderBrowser}
        onHide={() => setShowFolderBrowser(false)}
        onSelect={handleFolderSelect}
        basePath={baseRecordingPath}
        initialPath={selectedFolder || baseRecordingPath}
      />
    </Container>
  );
};

export default RecordingFormPage;