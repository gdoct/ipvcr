import React, { useEffect, useState } from 'react';
import { Alert, Button, Card, Col, Container, Modal, Nav, Row, Tab } from 'react-bootstrap';
import FfmpegSettingsComponent from '../components/settings/FfmpegSettingsComponent';
import GeneralSettingsComponent from '../components/settings/GeneralSettingsComponent';
import PlaylistSettingsComponent from '../components/settings/PlaylistSettingsComponent';
import SystemSettingsComponent from '../components/settings/SystemSettingsComponent';
import TlsSettingsComponent from '../components/settings/TlsSettingsComponent';
import UserManagementSettingsComponent from '../components/settings/UserManagementSettingsComponent';
import { AuthService } from '../services/AuthService';
import { settingsApi } from '../services/SettingsApiService';
import { AppSettings } from '../types/recordings';

// Define a type for tab keys
type TabKey = 'general' | 'playlist' | 'users' | 'ssl' | 'system' | 'ffmpeg';

const SettingsPage: React.FC = () => {
  // State for settings with five categories
  const [settings, setSettings] = useState<AppSettings>({
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
  });
  
  // Add state for original settings (to detect changes)
  const [originalSettings, setOriginalSettings] = useState<AppSettings | null>(null);

  // Add state for tracking which tabs have unsaved changes
  const [hasUnsavedChanges, setHasUnsavedChanges] = useState<Record<TabKey, boolean>>({
    general: false,
    playlist: false,
    users: false,
    ssl: false,
    system: false,
    ffmpeg: false
  });

  // State for confirm modal when there are unsaved changes
  const [showConfirmModal, setShowConfirmModal] = useState(false);
  const [pendingTabChange, setPendingTabChange] = useState<TabKey | null>(null);

  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabKey>('general');
  
  // Validate token on page load
  useEffect(() => {
    const validateTokenOnLoad = async () => {
      try {
        // This will automatically redirect to login if token is invalid
        await AuthService.validateToken();
      } catch (error) {
        console.error('Token validation error:', error);
        // The validateToken function will handle redirection if token is invalid
      }
    };
    
    validateTokenOnLoad();
  }, []);

  // Load settings on component mount
  useEffect(() => {
    fetchSettings();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Fetch settings from API
  const fetchSettings = async () => {
    setLoading(true);
    try {
      const data = await settingsApi.getAllSettings();
      setSettings(data);
      setOriginalSettings(data);
      setError(null);
    } catch (err) {
      console.error('Error fetching settings:', err);
      setError('Failed to load settings');
    } finally {
      setLoading(false);
    }
  };

  // Handle input changes for different setting categories
  const handleGeneralInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setSettings(prev => ({
      ...prev,
      general: {
        ...prev.general,
        [name]: type === 'checkbox' ? checked : value
      }
    }));
    setHasUnsavedChanges(prev => ({
      ...prev,
      general: true
    }));
  };

  const handlePlaylistInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setSettings(prev => ({
      ...prev,
      playlist: {
        ...prev.playlist,
        [name]: type === 'checkbox' ? checked : type === 'number' ? Number(value) : value
      }
    }));
    setHasUnsavedChanges(prev => ({
      ...prev,
      playlist: true
    }));
  };

  const handleUserManagementInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setSettings(prev => ({
      ...prev,
      userManagement: {
        ...prev.userManagement,
        [name]: type === 'checkbox' ? checked : type === 'number' ? Number(value) : value
      }
    }));
    setHasUnsavedChanges(prev => ({
      ...prev,
      users: true
    }));
  };

  const handleTlsInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setSettings(prev => ({
      ...prev,
      tls: {
        ...prev.tls,
        [name]: type === 'checkbox' ? checked : value
      }
    }));
    setHasUnsavedChanges(prev => ({
      ...prev,
      ssl: true
    }));
  };

  const handleFfmpegInputChange = (e: React.ChangeEvent<HTMLSelectElement | HTMLInputElement>) => {
    const { name, value } = e.target;
    setSettings(prev => ({
      ...prev,
      ffmpeg: {
        ...prev.ffmpeg,
        [name]: value // Empty string ('') will be used for 'default' option
      }
    }));
    setHasUnsavedChanges(prev => ({
      ...prev,
      ffmpeg: true
    }));
  };

  // Handle form submission
  const handleSubmit = async () => {
    try {
      setLoading(true);
      
      // Save only settings for the active tab
      switch (activeTab) {
        case 'general':
          await settingsApi.updateSchedulerSettings({
            mediaPath: settings.general.mediaPath,
            dataPath: settings.general.dataPath,
            m3uPlaylistPath: settings.general.m3uPlaylistPath,
            removeTaskAfterExecution: settings.general.removeTaskAfterExecution
          });
          setHasUnsavedChanges(prev => ({ ...prev, general: false }));
          setSuccess('General settings saved successfully');
          break;
          
        case 'playlist':
          await settingsApi.updatePlaylistSettings(settings.playlist);
          setHasUnsavedChanges(prev => ({ ...prev, playlist: false }));
          setSuccess('Playlist settings saved successfully');
          break;
          
        case 'users':
          await settingsApi.updateAdminPasswordSettings(settings.userManagement);
          setHasUnsavedChanges(prev => ({ ...prev, users: false }));
          setSuccess('User management settings saved successfully');
          break;
          
        case 'ssl':
          await settingsApi.updateSslSettings(settings.tls);
          setHasUnsavedChanges(prev => ({ ...prev, ssl: false }));
          setSuccess('SSL settings saved successfully');
          break;
          
        case 'ffmpeg':
          await settingsApi.updateFfmpegSettings(settings.ffmpeg);
          setHasUnsavedChanges(prev => ({ ...prev, ffmpeg: false }));
          setSuccess('FFmpeg settings saved successfully');
          break;
      }
      
      setError(null);
      
      // Update the original settings for the current tab to reflect saved state
      if (originalSettings) {
        setOriginalSettings(prev => {
          if (!prev) return settings;
          
          switch (activeTab) {
            case 'general':
              return { ...prev, general: { ...settings.general } };
            case 'playlist':
              return { ...prev, playlist: { ...settings.playlist } };
            case 'users':
              return { ...prev, userManagement: { ...settings.userManagement } };
            case 'ssl':
              return { ...prev, tls: { ...settings.tls } };
            case 'ffmpeg':
              return { ...prev, ffmpeg: { ...settings.ffmpeg } };
            default:
              return prev;
          }
        });
      }
      
      // Reset success message after 3 seconds
      setTimeout(() => {
        setSuccess(null);
      }, 3000);
    } catch (err) {
      console.error('Error saving settings:', err);
      setError('Failed to save settings');
      setSuccess(null);
    } finally {
      setLoading(false);
    }
  };

  const handleTabChange = (key: string | null) => {
    if (!key) return;
    
    // Cast to TabKey since we know our tab keys are limited to our defined options
    const newTabKey = key as TabKey;
    
    if (hasUnsavedChanges[activeTab]) {
      setPendingTabChange(newTabKey);
      setShowConfirmModal(true);
    } else {
      setActiveTab(newTabKey);
    }
  };

  // Make sure all useEffect hooks have proper dependencies
  useEffect(() => {
    fetchSettings();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // This is intended to run only once on mount

  const handleConfirmTabChange = () => {
    setActiveTab(pendingTabChange || 'general');
    setShowConfirmModal(false);
    setPendingTabChange(null);
  };

  const handleCancelTabChange = () => {
    setShowConfirmModal(false);
    setPendingTabChange(null);
  };

  return (
    <Container fluid className="px-4">
      <div className="d-flex align-items-center mb-4">
        <img 
          src="/ipvcr.png" 
          className="img-fluid me-3" 
          alt="IPVCR Logo" 
          style={{ height: '60px' }}
        />
        <h2 className="mb-0">System Settings</h2>
      </div>
      
      {error && (
        <Alert variant="danger" onClose={() => setError(null)} dismissible>
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          {error}
        </Alert>
      )}
      
      {success && (
        <Alert variant="success" onClose={() => setSuccess(null)} dismissible>
          <i className="bi bi-check-circle-fill me-2"></i>
          {success}
        </Alert>
      )}
      
      {loading ? (
        <div className="text-center py-5">
          <div className="spinner-border text-primary" role="status">
            <span className="visually-hidden">Loading...</span>
          </div>
          <p className="mt-2">Loading settings...</p>
        </div>
      ) : (
        <Tab.Container id="settings-tabs" activeKey={activeTab} onSelect={handleTabChange}>
          <Row>
            <Col md={3} lg={2}>
                <Card className="mb-4 mb-md-0">
                <Card.Header className="bg-primary text-white">
                  <i className="bi bi-gear-fill me-2"></i>
                  Settings
                </Card.Header>
                <Card.Body className="p-0">
                  <Nav variant="pills" className="flex-column">
                  <Nav.Item>
                    <Nav.Link eventKey="general" className="rounded-0 border-bottom text-start">
                    <i className="bi bi-sliders me-2"></i>
                    General
                    </Nav.Link>
                  </Nav.Item>
                  <Nav.Item>
                    <Nav.Link eventKey="playlist" className="rounded-0 border-bottom text-start">
                    <i className="bi bi-music-note-list me-2"></i>
                    Playlist
                    </Nav.Link>
                  </Nav.Item>
                  <Nav.Item>
                    <Nav.Link eventKey="ffmpeg" className="rounded-0 border-bottom text-start">
                    <i className="bi bi-film me-2"></i>
                    FFmpeg
                    </Nav.Link>
                  </Nav.Item>
                  <Nav.Item>
                    <Nav.Link eventKey="users" className="rounded-0 border-bottom text-start">
                    <i className="bi bi-people-fill me-2"></i>
                    Security
                    </Nav.Link>
                  </Nav.Item>
                  <Nav.Item>
                    <Nav.Link eventKey="ssl" className="rounded-0 border-bottom text-start">
                    <i className="bi bi-shield-lock-fill me-2"></i>
                    TLS Settings
                    </Nav.Link>
                  </Nav.Item>
                  <Nav.Item>
                    <Nav.Link eventKey="system" className="rounded-0 text-start">
                    <i className="bi bi-pc-display me-2"></i>
                    System
                    </Nav.Link>
                  </Nav.Item>
                  </Nav>
                </Card.Body>
                </Card>
            </Col>
            
            <Col md={9} lg={10}>
              <Tab.Content>
                <Tab.Pane eventKey="general">
                  <GeneralSettingsComponent 
                    settings={settings.general} 
                    handleInputChange={handleGeneralInputChange} 
                  />
                  <div className="d-flex justify-content-end">
                    <Button variant="success" onClick={handleSubmit}>
                      <i className="bi bi-save me-2"></i>Save Settings
                    </Button>
                  </div>
                </Tab.Pane>
                
                <Tab.Pane eventKey="playlist">
                  <PlaylistSettingsComponent 
                    settings={settings.playlist}
                    handleInputChange={handlePlaylistInputChange}
                    setSuccess={setSuccess}
                    setError={setError}
                    refreshSettings={fetchSettings}
                  />
                  <div className="d-flex justify-content-end">
                    <Button variant="success" onClick={handleSubmit}>
                      <i className="bi bi-save me-2"></i>Save Settings
                    </Button>
                  </div>
                </Tab.Pane>
                
                <Tab.Pane eventKey="users">
                  <UserManagementSettingsComponent 
                    settings={settings.userManagement}
                    handleInputChange={handleUserManagementInputChange}
                    setSuccess={setSuccess}
                    setError={setError}
                  />
                  <div className="d-flex justify-content-end">
                    <Button variant="success" onClick={handleSubmit}>
                      <i className="bi bi-save me-2"></i>Save Settings
                    </Button>
                  </div>
                </Tab.Pane>
                
                <Tab.Pane eventKey="ssl">
                  <TlsSettingsComponent 
                    settings={settings.tls}
                    handleInputChange={handleTlsInputChange}
                    setSuccess={setSuccess}
                    setError={setError}
                    refreshSettings={fetchSettings}
                  />
                  <div className="d-flex justify-content-end">
                    <Button variant="success" onClick={handleSubmit}>
                      <i className="bi bi-save me-2"></i>Save Settings
                    </Button>
                  </div>
                </Tab.Pane>

                <Tab.Pane eventKey="ffmpeg">
                  <FfmpegSettingsComponent 
                    settings={settings.ffmpeg}
                    handleInputChange={handleFfmpegInputChange}
                  />
                  <div className="d-flex justify-content-end">
                    <Button variant="success" onClick={handleSubmit}>
                      <i className="bi bi-save me-2"></i>Save Settings
                    </Button>
                  </div>
                </Tab.Pane>

                <Tab.Pane eventKey="system">
                  <SystemSettingsComponent 
                    setSuccess={setSuccess}
                    setError={setError}
                  />
                </Tab.Pane>
              </Tab.Content>
            </Col>
          </Row>
        </Tab.Container>
      )}

      <Modal show={showConfirmModal} onHide={handleCancelTabChange}>
        <Modal.Header closeButton>
          <Modal.Title>Unsaved Changes</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          You have unsaved changes. Are you sure you want to switch tabs without saving?
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={handleCancelTabChange}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleConfirmTabChange}>
            Confirm
          </Button>
        </Modal.Footer>
      </Modal>
    </Container>
  );
};

export default SettingsPage;