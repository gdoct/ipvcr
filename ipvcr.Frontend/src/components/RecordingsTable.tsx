import React, { useEffect, useState } from 'react';
import { Badge, Button, ButtonGroup, Table } from 'react-bootstrap';
import { getChannelLogo } from '../services/api';
import { obfuscateChannelUri, ScheduledRecording } from '../types/recordings';

interface RecordingsTableProps {
  recordings: ScheduledRecording[];
  onEdit: (id: string) => void;
  onEditTask: (id: string) => void;
  onDelete: (id: string) => void;
  onAdd: () => void;
  showAddButton: boolean;
}

const RecordingsTable: React.FC<RecordingsTableProps> = ({ 
  recordings, 
  onEdit, 
  onEditTask, 
  onDelete,
  onAdd,
  showAddButton
}) => {
  // State to store channel logos
  const [channelLogos, setChannelLogos] = useState<Record<string, string>>({});
  
  // Fetch channel logos when recordings change
  useEffect(() => {
    const fetchLogos = async () => {
      const logoPromises = recordings.map(async (recording) => {
        // Only fetch if we don't already have this logo
        if (!channelLogos[recording.channelUri]) {
          const logo = await getChannelLogo(recording.channelUri, recording.channelName);
          return { uri: recording.channelUri, logo };
        }
        return null;
      });
      
      const results = await Promise.all(logoPromises);
      
      // Update state with new logos
      const newLogos = { ...channelLogos };
      results.forEach(result => {
        if (result) {
          newLogos[result.uri] = result.logo;
        }
      });
      
      setChannelLogos(newLogos);
    };
    
    fetchLogos();
  }, [recordings, channelLogos]);

  if (!recordings || recordings.length === 0) {
    return (
      <div className="card shadow-sm">
        <div className="card-body text-center py-5">
          <i className="bi bi-calendar-x text-muted" style={{ fontSize: '3rem' }}></i>
          <h4 className="mt-3">No recordings scheduled</h4>
          <p className="text-muted">Start by adding a new recording.</p>
          {showAddButton && (
            <Button variant="primary" onClick={onAdd}>
              <i className="bi bi-plus-lg me-1"></i> Add New Recording
            </Button>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="card shadow-sm mb-4">
      <div className="card-header bg-primary text-white d-flex justify-content-between align-items-center">
        <h5 className="mb-0">
          <i className="bi bi-calendar-event me-2"></i>Upcoming Recordings
        </h5>
        {showAddButton && (
          <Button variant="light" onClick={onAdd}>
            <i className="bi bi-plus-lg me-1"></i> Add New Recording
          </Button>
        )}
      </div>
      <div className="card-body p-0">
        <div className="table-responsive">
          <Table hover striped className="mb-0">
            <thead className="table-light">
              <tr>
                <th style={{ width: '60px' }}></th>
                <th>Time</th>
                <th>Channel</th>
                <th>Program</th>
                <th className="text-center" style={{ width: '100px' }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {recordings.map(item => (
                <tr key={item.id}>
                  <td className="align-middle text-center">
                    {channelLogos[item.channelUri] ? (
                      <img 
                        src={channelLogos[item.channelUri]} 
                        className="img-fluid rounded" 
                        style={{ maxHeight: '40px', maxWidth: '40px' }} 
                        alt="Channel logo" 
                      />
                    ) : (
                      <div 
                        className="bg-light rounded d-flex align-items-center justify-content-center" 
                        style={{ height: '40px', width: '40px' }}
                      >
                        <i className="bi bi-tv text-muted"></i>
                      </div>
                    )}
                  </td>
                  <td className="align-middle">
                    <div className="d-flex flex-column">
                      <span className="fw-bold">
                        <i className="bi bi-play-fill text-success"></i> {new Date(item.startTime).toLocaleString()}
                      </span>
                      <span className="text-muted small">
                        <i className="bi bi-stop-fill text-danger"></i> {new Date(item.endTime).toLocaleString()}
                      </span>
                    </div>
                  </td>
                  <td className="align-middle" title={item.channelName}>
                    <div className="fw-bold">{item.channelName}</div>
                    <Badge bg="light" text="dark" className="small">{obfuscateChannelUri(item.channelUri)}</Badge>
                  </td>
                  <td className="align-middle" title={item.filename}>
                    <div>{item.name}</div>
                    <small className="text-muted text-truncate d-inline-block" style={{ maxWidth: '250px' }}>
                      <i className="bi bi-file-earmark-play"></i> {item.filename.split('/').pop()}
                    </small>
                  </td>
                  <td className="align-middle text-center">
                    <ButtonGroup size="sm">
                      <Button variant="outline-primary" title="Edit" onClick={() => onEdit(item.id)}>
                        <i className="bi bi-pencil"></i>
                      </Button>
                      <Button variant="outline-secondary" title="Edit Code" onClick={() => onEditTask(item.id)}>
                        <i className="bi bi-braces"></i>
                      </Button>
                      <Button 
                        variant="outline-danger" 
                        title="Delete" 
                        onClick={() => onDelete(item.id)}
                      >
                        <i className="bi bi-trash"></i>
                      </Button>
                    </ButtonGroup>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </div>
      </div>
    </div>
  );
};

export default RecordingsTable;