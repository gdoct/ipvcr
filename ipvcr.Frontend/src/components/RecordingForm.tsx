import React, { useEffect, useRef, useState } from 'react';
import { Button, Col, Form, InputGroup, ListGroup, Modal, Row } from 'react-bootstrap';
import { searchChannels } from '../services/api';
import { ChannelInfo, formatFileDate, formatNiceDate, ScheduledRecording } from '../types/recordings';

interface RecordingFormProps {
  show: boolean;
  onHide: () => void;
  onSave: (recording: ScheduledRecording) => void;
  recording?: ScheduledRecording | null;
  channels?: ChannelInfo[]; // Making channels optional
  recordingPath: string;
}

const RecordingForm: React.FC<RecordingFormProps> = ({
  show,
  onHide,
  onSave,
  recording,
  channels,
  recordingPath
}) => {
  const isEdit = Boolean(recording && recording.id);

  // Initialize form state
  const [formData, setFormData] = useState<Partial<ScheduledRecording>>({
    id: '00000000-0000-0000-0000-000000000000',
    name: '',
    description: '',
    channelUri: '',
    channelName: '',
    startTime: getTomorrowDatetime(),
    endTime: getTomorrowDatetimePlusHour(),
    filename: ''
  });
  
  const [channelLogo, setChannelLogo] = useState<string>('');
  const [channelQuery, setChannelQuery] = useState<string>('');
  const [searchResults, setSearchResults] = useState<ChannelInfo[]>([]);
  const [isSearching, setIsSearching] = useState<boolean>(false);
  const [showDropdown, setShowDropdown] = useState<boolean>(false);
  const searchTimeoutRef = useRef<NodeJS.Timeout | null>(null);

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

  // Update form when recording changes or modal opens
  useEffect(() => {
    if (recording) {
      setFormData({
        ...recording,
        startTime: formatDateForDatePicker(recording.startTime),
        endTime: formatDateForDatePicker(recording.endTime),
      });
      setChannelQuery(recording.channelName);
      
      // For editing, try to find the channel logo if available or fetch it
      if (channels) {
        const selectedChannel = channels.find(c => c.uri === recording.channelUri);
        if (selectedChannel) {
          setChannelLogo(selectedChannel.logo);
        }
      } else {
        // If channels not provided, fetch the single channel data using search
        const fetchChannelInfo = async () => {
          if (recording.channelName) {
            setIsSearching(true);
            try {
              // Search with exact channel name
              const results = await searchChannels(recording.channelName);
              
              // Find exact match by URI
              const matchedChannel = results.find(c => c.uri === recording.channelUri);
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
        
        fetchChannelInfo();
      }
    } else {
      // Reset form to default values
      setFormData({
        id: '00000000-0000-0000-0000-000000000000',
        name: '',
        description: '',
        channelUri: '',
        channelName: '',
        startTime: getTomorrowDatetime(),
        endTime: getTomorrowDatetimePlusHour(),
        filename: ''
      });
      setChannelQuery('');
      setChannelLogo('');
    }
  }, [recording, show, channels]);

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
          // Use the full query for search regardless of length
          console.log(`Searching channels with query: "${query}"`);
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
    setChannelLogo(channel.logo);
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

  // Update filename and description based on form data
  const updateFilenameAndDescription = (
    name: string,
    startTimeStr: string, 
    channelName: string
  ) => {
    if (name && startTimeStr && channelName) {
      // Generate filename
      const startDate = new Date(startTimeStr);
      const startTimeFormatted = formatFileDate(startDate) + 
        startTimeStr.split('T')[1].replace(':', '').substring(0, 4);
      
      const sanitizedName = name.replace(/ /g, '_').toLowerCase();
      const filename = `${recordingPath}/${sanitizedName}_${startTimeFormatted}.mp4`;
      
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
  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Validate form
    if (!formData.name || !formData.startTime || !formData.endTime || !formData.channelUri) {
      return; // Form validation failed
    }
    
    // Submit the form
    onSave(formData as ScheduledRecording);
  };

  // Check if form is valid
  const isFormValid = 
    formData.name && 
    formData.startTime && 
    formData.endTime && 
    formData.channelUri &&
    formData.channelName;

  return (
    <Modal show={show} onHide={onHide} size="lg">
      <Modal.Header className="bg-primary text-white">
        <Modal.Title>
          {isEdit ? (
            <><i className="bi bi-pencil-square me-2"></i>Edit Recording</>
          ) : (
            <><i className="bi bi-plus-circle me-2"></i>Add New Recording</>
          )}
        </Modal.Title>
        <Button variant="close" onClick={onHide} className="btn-close-white" aria-label="Close" />
      </Modal.Header>
      <Modal.Body>
        <Form onSubmit={handleSubmit}>
          <input type="hidden" name="id" value={formData.id || ''} />
          
          <Row className="mb-3">
            <Col md={3} className="text-center">
              {channelLogo !== '' && (
              <img 
                src={channelLogo} 
                alt=""
                className="img-fluid rounded mb-2" 
                style={{ maxHeight: '100px', maxWidth: '100%', objectFit: 'contain' }} 
              />
              )}
              {channelLogo === '' && (
                <div className="bg-light rounded d-flex align-items-center justify-content-center mb-2" style={{ height: '100px', width: '100px' }}>
                  <i className="bi bi-tv text-muted" style={{ fontSize: '2.5rem' }}></i>
                </div>
              )}
            </Col>
            <Col md={9}>
              <Form.Group className="mb-3">
                <Form.Label className="fw-bold">Recording name</Form.Label>
                <Form.Control
                  type="text"
                  name="name"
                  value={formData.name || ''}
                  onChange={handleInputChange}
                  required
                />
              </Form.Group>
              <Form.Group className="mb-3">
                <Form.Label className="fw-bold">Description</Form.Label>
                <Form.Control
                  as="textarea"
                  rows={2}
                  name="description"
                  value={formData.description || ''}
                  onChange={handleInputChange}
                  readOnly
                  className="bg-light text-muted"
                />
              </Form.Group>
            </Col>
          </Row>
          
          <Row className="mb-3">
            <Col md={6}>
              <Form.Group className="mb-3 position-relative">
                <Form.Label className="fw-bold">Channel</Form.Label>
                <InputGroup>
                  {channelLogo && (
                    <InputGroup.Text className="px-2">
                      <img 
                        src={channelLogo} 
                        alt="" 
                        style={{ height: '24px', width: '24px', objectFit: 'contain' }}
                      />
                    </InputGroup.Text>
                  )}
                  <Form.Control 
                    type="text"
                    placeholder="Search for a channel..."
                    value={channelQuery}
                    onChange={(e) => handleChannelSearch(e.target.value)}
                    onFocus={() => channelQuery && setShowDropdown(true)}
                    required
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
            <Col md={6}>
              <Row>
                <Col md={6}>
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
                </Col>
                <Col md={6}>
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
            </Col>
          </Row>

          {formData.filename && (
            <Form.Group className="mb-3">
              <Form.Label className="fw-bold">Recording will be saved to</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-file-earmark-play"></i>
                </InputGroup.Text>
                <Form.Control
                  className="bg-light text-muted text-truncate"
                  value={formData.filename}
                  disabled
                />
              </InputGroup>
            </Form.Group>
          )}
        </Form>
      </Modal.Body>
      <div className="card-footer bg-light p-3 d-flex justify-content-end">
        <Button variant="secondary" className="me-2" onClick={onHide}>
          <i className="bi bi-x-circle me-1"></i>Cancel
        </Button>
        <Button 
          variant="primary" 
          onClick={handleSubmit} 
          disabled={!isFormValid}
        >
          <i className="bi bi-save me-1"></i>Save
        </Button>
      </div>
    </Modal>
  );
};

export default RecordingForm;