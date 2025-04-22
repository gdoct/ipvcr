import React, { useState } from 'react';
import { Button, Card, Col, Form, InputGroup, Row } from 'react-bootstrap';
import { AuthService } from '../../services/AuthService';
import { UserManagementSettings } from '../../types/recordings';

interface UserManagementSettingsProps {
  settings: UserManagementSettings;
  handleInputChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  setSuccess: (message: string | null) => void;
  setError: (message: string | null) => void;
}

const UserManagementSettingsComponent: React.FC<UserManagementSettingsProps> = ({ 
  settings, 
  handleInputChange, 
  setSuccess, 
  setError 
}) => {
  const [adminPassword, setAdminPassword] = useState<string>('');
  const [changingPassword, setChangingPassword] = useState<boolean>(false);

  const handlePasswordInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setAdminPassword(e.target.value);
  };

  // Handle password change
  const handlePasswordChange = async () => {
    if (!adminPassword) {
      setError('Please enter a new password');
      return;
    }
    
    setChangingPassword(true);
    try {
      const response = await fetch(`${process.env.REACT_APP_API_URL || '/api'}/login/changepassword`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${AuthService.getToken()}`
        },
        body: JSON.stringify({
          username: settings.adminUsername,
          password: adminPassword
        })
      });
      
      if (response.ok) {
        setSuccess('Password changed successfully');
        setAdminPassword(''); // Clear password field after successful change
      } else {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to update password');
      }
    } catch (err) {
      console.error('Error changing password:', err);
      setError('Failed to update password');
      setSuccess(null);
    } finally {
      setChangingPassword(false);
    }
  };

  return (
    <Card className="mb-4">
      <Card.Header className="bg-light">
        <i className="bi bi-people-fill me-2"></i>User Management
      </Card.Header>
      <Card.Body>
        <Row>
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>Admin Username</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-person"></i>
                </InputGroup.Text>
                <Form.Control
                  type="text"
                  name="adminUsername"
                  value={settings.adminUsername}
                  onChange={handleInputChange}
                />
              </InputGroup>
            </Form.Group>

            <Form.Group className="mb-3">
              <Form.Label>Admin Password</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-key"></i>
                </InputGroup.Text>
                <Form.Control
                  type="password"
                  name="adminPassword"
                  placeholder="Enter new password"
                  value={adminPassword}
                  onChange={handlePasswordInputChange}
                />
                <Button 
                  variant="outline-primary" 
                  onClick={handlePasswordChange}
                  disabled={changingPassword}
                >
                  {changingPassword ? (
                    <>
                      <span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
                      Changing...
                    </>
                  ) : (
                    <>Change</>
                  )}
                </Button>
              </InputGroup>
              <Form.Text className="text-muted">
                Password will not be saved with other settings
              </Form.Text>
            </Form.Group>
          </Col>

          <Col md={12} lg={6}>
            <Card className="mb-3">
              <Card.Header className="bg-light">
                <i className="bi bi-person-plus-fill me-2"></i>User Registration
              </Card.Header>
              <Card.Body>
                <Form.Group className="mb-3">
                  <Form.Check
                    type="checkbox"
                    id="allowUserRegistration"
                    name="allowUserRegistration"
                    label="Allow new user registration"
                    checked={settings.allowUserRegistration}
                    onChange={handleInputChange}
                  />
                </Form.Group>

                <Form.Group>
                  <Form.Label>Maximum users allowed</Form.Label>
                  <Form.Control
                    type="number"
                    name="maxUsersAllowed"
                    value={settings.maxUsersAllowed}
                    onChange={handleInputChange}
                    min={1}
                  />
                </Form.Group>
              </Card.Body>
            </Card>
          </Col>
        </Row>
      </Card.Body>
    </Card>
  );
};

export default UserManagementSettingsComponent;