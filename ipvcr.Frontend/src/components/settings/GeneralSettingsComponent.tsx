// filepath: /home/guido/dotnet/ipvcr/ipvcr.Frontend/src/components/settings/GeneralSettingsComponent.tsx
import React from 'react';
import { Card, Col, Form, InputGroup, Row } from 'react-bootstrap';
import { SchedulerSettings } from '../../types/recordings';

interface GeneralSettingsProps {
  settings: SchedulerSettings;
  handleInputChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

const GeneralSettingsComponent: React.FC<GeneralSettingsProps> = ({ settings, handleInputChange }) => {
  return (
    <Card className="mb-4">
      <Card.Header className="bg-light">
        <i className="bi bi-sliders me-2"></i>General Settings
      </Card.Header>
      <Card.Body>
        <Row className="mb-3">
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>Media Path</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-folder2"></i>
                </InputGroup.Text>
                <Form.Control
                  type="text"
                  name="mediaPath"
                  value={settings.mediaPath}
                  onChange={handleInputChange}
                />
              </InputGroup>
              <Form.Text className="text-muted">
                Directory where media files will be stored
              </Form.Text>
            </Form.Group>
          </Col>
          
          <Col md={12} lg={6}>
            <Form.Group className="mb-3">
              <Form.Label>Data Path</Form.Label>
              <InputGroup>
                <InputGroup.Text>
                  <i className="bi bi-folder2-open"></i>
                </InputGroup.Text>
                <Form.Control
                  type="text"
                  name="dataPath"
                  value={settings.dataPath}
                  onChange={handleInputChange}
                />
              </InputGroup>
              <Form.Text className="text-muted">
                Directory where configuration data will be stored
              </Form.Text>
            </Form.Group>
          </Col>
        </Row>

        <Form.Group className="mb-3">
          <Form.Check
            type="checkbox"
            id="removeTaskAfterExecution"
            name="removeTaskAfterExecution"
            label="Remove tasks after execution"
            checked={settings.removeTaskAfterExecution}
            onChange={handleInputChange}
          />
          <Form.Text className="text-muted">
            When enabled, tasks will be automatically removed after they are completed
          </Form.Text>
        </Form.Group>
      </Card.Body>
    </Card>
  );
};

export default GeneralSettingsComponent;