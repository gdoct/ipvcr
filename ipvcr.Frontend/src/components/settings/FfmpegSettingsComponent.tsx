import React, { memo } from 'react';
import { Col, Form, Row } from 'react-bootstrap';
import { FfmpegSettings } from '../../types/recordings';

interface FfmpegSettingsProps {
  settings: FfmpegSettings;
  handleInputChange: (e: React.ChangeEvent<HTMLSelectElement>) => void;
}

// Define dropdown options for each field
const fileTypeOptions = ['default', 'mp4', 'mkv', 'avi', 'mov', 'flv', 'webm', 'ts'];
const videoCodecOptions = ['default', 'libx264', 'libx265', 'mpeg4', 'libvpx', 'libvpx-vp9'];
const audioCodecOptions = ['default', 'aac', 'mp3', 'ac3', 'flac', 'opus', 'vorbis'];
const videoBitrateOptions = ['default', '500k', '1000k', '2000k', '3000k', '5000k', '8000k', '10000k'];
const audioBitrateOptions = ['default', '64k', '96k', '128k', '192k', '256k', '320k'];
const resolutionOptions = ['default', '640x480', '800x600', '1280x720', '1920x1080', '2560x1440', '3840x2160'];
const frameRateOptions = ['default', '24', '25', '30', '50', '60'];
const aspectRatioOptions = ['default', '4:3', '16:9', '21:9', '1:1'];
const outputFormatOptions = ['default', 'mp4', 'mkv', 'avi', 'mov', 'flv', 'webm', 'ts'];

// Helper function to get display value (showing 'default' when value is empty)
const getDisplayValue = (value: string) => {
  return value === '' ? 'default' : value;
};

// Use React.memo to prevent unnecessary re-renders
const FfmpegSettingsComponent: React.FC<FfmpegSettingsProps> = ({ settings, handleInputChange }) => {
  // Render options for a select element
  const renderOptions = (options: string[]) => {
    return options.map(option => (
      <option key={option} value={option === 'default' ? '' : option}>
        {option}
      </option>
    ));
  };
  
  return (
    <div className="ffmpeg-settings">
      <Row className="g-2">
        {/* First row */}
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">File Type</Form.Label>
            <Form.Select 
              name="fileType"
              size="sm"
              value={getDisplayValue(settings.fileType)}
              onChange={handleInputChange}
            >
              {renderOptions(fileTypeOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
        
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">Output Format</Form.Label>
            <Form.Select
              name="outputFormat"
              size="sm"
              value={getDisplayValue(settings.outputFormat)}
              onChange={handleInputChange}
            >
              {renderOptions(outputFormatOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
        
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">Aspect Ratio</Form.Label>
            <Form.Select
              name="aspectRatio"
              size="sm"
              value={getDisplayValue(settings.aspectRatio)}
              onChange={handleInputChange}
            >
              {renderOptions(aspectRatioOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
        
        {/* Second row */}
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">Video Codec</Form.Label>
            <Form.Select
              name="codec"
              size="sm"
              value={getDisplayValue(settings.codec)}
              onChange={handleInputChange}
            >
              {renderOptions(videoCodecOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
        
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">Audio Codec</Form.Label>
            <Form.Select
              name="audioCodec"
              size="sm"
              value={getDisplayValue(settings.audioCodec)}
              onChange={handleInputChange}
            >
              {renderOptions(audioCodecOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
        
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">Resolution</Form.Label>
            <Form.Select
              name="resolution"
              size="sm"
              value={getDisplayValue(settings.resolution)}
              onChange={handleInputChange}
            >
              {renderOptions(resolutionOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
        
        {/* Third row */}
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">Video Bitrate</Form.Label>
            <Form.Select
              name="videoBitrate"
              size="sm"
              value={getDisplayValue(settings.videoBitrate)}
              onChange={handleInputChange}
            >
              {renderOptions(videoBitrateOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
        
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">Audio Bitrate</Form.Label>
            <Form.Select
              name="audioBitrate"
              size="sm"
              value={getDisplayValue(settings.audioBitrate)}
              onChange={handleInputChange}
            >
              {renderOptions(audioBitrateOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
        
        <Col md={4}>
          <Form.Group className="mb-2">
            <Form.Label className="small mb-0 fw-bold">Frame Rate</Form.Label>
            <Form.Select
              name="frameRate"
              size="sm"
              value={getDisplayValue(settings.frameRate)}
              onChange={handleInputChange}
            >
              {renderOptions(frameRateOptions)}
            </Form.Select>
          </Form.Group>
        </Col>
      </Row>
    </div>
  );
};

// Use memo to prevent unnecessary re-renders when props don't change
export default memo(FfmpegSettingsComponent, (prevProps, nextProps) => {
  // Deep compare settings to determine if we should re-render
  return JSON.stringify(prevProps.settings) === JSON.stringify(nextProps.settings);
});