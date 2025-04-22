import React, { useRef } from 'react';
import { Button, Card, Col, Form, InputGroup, Row } from 'react-bootstrap';
import { settingsApi } from '../../services/SettingsApiService';
import { PlaylistSettings } from '../../types/recordings';

interface PlaylistSettingsProps {
  settings: PlaylistSettings;
  handleInputChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  setSuccess: (message: string | null) => void;
  setError: (message: string | null) => void;
  refreshSettings: () => void;
}

const PlaylistSettingsComponent: React.FC<PlaylistSettingsProps> = ({ 
  settings, 
  handleInputChange, 
  setSuccess, 
  setError,
  refreshSettings 
}) => {
  const [uploading, setUploading] = React.useState<boolean>(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Handle M3U file upload
  const handleUploadM3U = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!fileInputRef.current || !fileInputRef.current.files || fileInputRef.current.files.length === 0) {
      setError('Please select a file to upload');
      return;
    }
    
    const file = fileInputRef.current.files[0];
    setUploading(true);
    setError(null);
    
    try {
      const result = await settingsApi.uploadM3uPlaylist(file);
      setSuccess(result.message || 'M3U file uploaded successfully');
      
      // Refresh settings to get updated M3U path
      refreshSettings();
      
      // Reset file input
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    } catch (err) {
      console.error('Error uploading file:', err);
      setError('Failed to upload M3U file');
      setSuccess(null);
    } finally {
      setUploading(false);
    }
  };

  return (
    <Card className="mb-4">
      <Card.Header className="bg-light">
        <i className="bi bi-music-note-list me-2"></i>Playlist Management
      </Card.Header>
      <Card.Body>
        <Row>
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>M3U Playlist Path</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-music-note-list"></i>
                </InputGroup.Text>
                <Form.Control
                  type="text"
                  name="m3uPlaylistPath"
                  value={settings.m3uPlaylistPath}
                  onChange={handleInputChange}
                />
              </InputGroup>
              <Form.Text className="text-muted">
                Path to your M3U playlist file
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Playlist Auto-Update Interval (hours)</Form.Label>
              <Form.Control
                type="number"
                name="playlistAutoUpdateInterval"
                value={settings.playlistAutoUpdateInterval}
                onChange={handleInputChange}
                min={0}
                max={168}
              />
              <Form.Text className="text-muted">
                Set to 0 to disable auto-updates
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Check
                type="checkbox"
                id="autoReloadPlaylist"
                name="autoReloadPlaylist"
                label="Auto-reload playlist after changes"
                checked={settings.autoReloadPlaylist}
                onChange={handleInputChange}
              />
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Check
                type="checkbox"
                id="filterEmptyGroups"
                name="filterEmptyGroups"
                label="Filter empty groups"
                checked={settings.filterEmptyGroups}
                onChange={handleInputChange}
              />
            </Form.Group>
          </Col>

          <Col md={12} lg={6}>
            <Card className="h-100">
              <Card.Header className="bg-light">
                <i className="bi bi-cloud-upload me-2"></i>Upload M3U Playlist
              </Card.Header>
              <Card.Body>
                <Form onSubmit={handleUploadM3U}>
                  <Form.Group className="mb-3">
                    <Form.Label>Select M3U Playlist File</Form.Label>
                    <Form.Control
                      type="file"
                      ref={fileInputRef}
                      accept=".m3u,.m3u8"
                    />
                  </Form.Group>
                  <div className="d-grid">
                    <Button 
                      type="submit" 
                      variant="primary" 
                      disabled={uploading}
                    >
                      {uploading ? (
                        <>
                          <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                          Uploading...
                        </>
                      ) : (
                        <>
                          <i className="bi bi-cloud-upload me-2"></i>Upload Playlist
                        </>
                      )}
                    </Button>
                  </div>
                </Form>
              </Card.Body>
            </Card>
          </Col>
        </Row>
      </Card.Body>
    </Card>
  );
};

export default PlaylistSettingsComponent;