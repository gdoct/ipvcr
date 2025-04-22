import React, { useRef, useState } from 'react';
import { Button, Card, Col, Form, InputGroup, Row } from 'react-bootstrap';
import { settingsApi } from '../../services/SettingsApiService';
import { TlsSettings } from '../../types/recordings';

interface TlsSettingsProps {
  settings: TlsSettings;
  handleInputChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  uploadCertificate?: (file: File) => Promise<void>;
  setSuccess?: (message: string | null) => void;
  setError?: (message: string | null) => void;
  refreshSettings?: () => void;
}

const TlsSettingsComponent: React.FC<TlsSettingsProps> = ({ 
  settings, 
  handleInputChange, 
  uploadCertificate, 
  setSuccess,
  setError,
  refreshSettings 
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isRegenerating, setIsRegenerating] = useState<boolean>(false);
  
  const handleFileSelect = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files.length > 0 && uploadCertificate) {
      try {
        await uploadCertificate(e.target.files[0]);
      } catch (error) {
        console.error('Failed to upload certificate:', error);
      }
    }
  };

  const triggerFileInput = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleRegenerateCertificate = async () => {
    if (!settings.useSsl) {
      return;
    }

    setIsRegenerating(true);
    try {
      const result = await settingsApi.regenerateSelfSignedCertificate();
      
      if (setSuccess) {
        setSuccess(result.message || 'Self-signed certificate generated successfully');
      }
      
      // Refresh settings to get updated certificate path
      if (refreshSettings) {
        refreshSettings();
      }
    } catch (error) {
      console.error('Error regenerating certificate:', error);
      if (setError) {
        setError('Failed to generate self-signed certificate');
      }
    } finally {
      setIsRegenerating(false);
    }
  };

  return (
    <Card className="mb-4">
      <Card.Header className="bg-light d-flex justify-content-between align-items-center">
        <div>
          <i className="bi bi-shield-lock-fill me-2"></i>SSL Settings
        </div>
        
      </Card.Header>
      <Card.Body>
        <div className="alert alert-warning mb-3">
          <i className="bi bi-exclamation-triangle-fill me-2"></i>
          {settings.useSsl ? 
            "SSL is currently enabled. Changing SSL settings will require a server restart to take effect." :
            "SSL is currently disabled. Enable SSL to secure connections to your server."}
        </div>
        <Row>
            <Col md={12} lg={6}>
            <div className="d-flex align-items-center mb-3">
              <Form.Label className="me-3 mb-0">Enable SSL</Form.Label>
              <Form.Check
              type="switch"
              id="useSsl"
              name="useSsl"
              checked={settings.useSsl || false}
              onChange={handleInputChange}
              label=""
              />
            </div>
            <Form.Group className="mb-3">
              <Form.Label>SSL Certificate Path</Form.Label>
              <InputGroup>
              <InputGroup.Text>
                <i className="bi bi-file-earmark-lock"></i>
              </InputGroup.Text>
              <Form.Control
                type="text"
                name="certificatePath"
                value={settings.certificatePath || ''}
                onChange={handleInputChange}
                disabled={!settings.useSsl}
                placeholder="Path to .pfx certificate file"
              />
              </InputGroup>
              <Form.Text className="text-muted">
              Path to your .pfx certificate file
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-3">
              <div className="d-flex justify-content-between align-items-center">
                <Form.Label>Upload Certificate</Form.Label>
                <input 
                  type="file" 
                  accept=".pfx,.p12"
                  className="d-none"
                  ref={fileInputRef}
                  onChange={handleFileSelect}
                  disabled={!settings.useSsl}
                />
                <Button 
                  variant="outline-primary" 
                  size="sm"
                  onClick={triggerFileInput}
                  disabled={!settings.useSsl || !uploadCertificate}
                >
                  <i className="bi bi-upload me-2"></i>
                  Upload Certificate
                </Button>
                <Button 
                  variant="outline-primary" 
                  size="sm"
                  onClick={handleRegenerateCertificate}
                  disabled={!settings.useSsl || isRegenerating}
                >
                  <i className="bi bi-patch-check-fill"></i>
                  {isRegenerating ? 'Regenerating...' : 'Regenerate self-signed certificate'}
                </Button>
              </div>
              <Form.Text className="text-muted">
                Upload a .pfx or .p12 certificate file
              </Form.Text>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Certificate Password</Form.Label>
              <InputGroup>
              <InputGroup.Text>
                <i className="bi bi-key"></i>
              </InputGroup.Text>
              <Form.Control
                type="password"
                name="certificatePassword"
                value={settings.certificatePassword || ''}
                onChange={handleInputChange}
                disabled={!settings.useSsl}
                placeholder="Enter certificate password"
              />
              </InputGroup>
              <Form.Text className="text-muted">
              Password for your certificate file (if required)
              </Form.Text>
            </Form.Group>
            </Col>
          
          <Col md={12} lg={6}>
            <Card className={`mb-3 ${settings.useSsl ? 'border-success' : 'border-secondary'}`}>
              <Card.Header className={`${settings.useSsl ? 'bg-success text-white' : 'bg-light'}`}>
                <i className={`bi ${settings.useSsl ? 'bi-shield-fill-check' : 'bi-shield-lock'} me-2`}></i>
                SSL Status
              </Card.Header>
              <Card.Body>
                {settings.useSsl ? (
                  <div>
                    <p className="mb-2">
                      <i className="bi bi-check-circle-fill text-success me-2"></i>
                      <strong>SSL is enabled</strong> for your server.
                    </p>
                    {settings.certificatePath ? (
                      <p className="mb-0 small">
                        Certificate path: {settings.certificatePath}
                      </p>
                    ) : (
                      <p className="text-warning">
                        <i className="bi bi-exclamation-triangle-fill me-2"></i>
                        No certificate path specified.
                      </p>
                    )}
                  </div>
                ) : (
                  <p>
                    <i className="bi bi-shield-slash me-2"></i>
                    SSL is currently disabled. Enable SSL using the toggle switch to secure connections to your server.
                  </p>
                )}
              </Card.Body>
            </Card>
            
            <div className="alert alert-warning">
              <i className="bi bi-info-circle-fill me-2"></i>
              For testing purposes, you can generate a self-signed certificate,
              but for production use, a certificate from a trusted CA is recommended.
            </div>
          </Col>
        </Row>
      </Card.Body>
    </Card>
  );
};

export default TlsSettingsComponent;