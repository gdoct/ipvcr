import React, { useEffect, useState } from 'react';
import { Alert, Button, Form, InputGroup, ListGroup, Modal } from 'react-bootstrap';
import { config } from '../config';
import { AuthService } from '../services/AuthService';

interface FolderBrowserModalProps {
  show: boolean;
  onHide: () => void;
  onSelect: (path: string) => void;
  basePath: string;
  initialPath: string;
}

interface FolderItem {
  name: string;
  isDirectory: boolean;
  fullPath: string;
}

const FolderBrowserModal: React.FC<FolderBrowserModalProps> = ({
  show,
  onHide,
  onSelect,
  basePath,
  initialPath,
}) => {
  const [currentPath, setCurrentPath] = useState(initialPath || basePath);
  const [folders, setFolders] = useState<FolderItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [newFolderName, setNewFolderName] = useState('');
  const [showNewFolderInput, setShowNewFolderInput] = useState(false);

  // Extract the relative path from the full path
  const getRelativePath = (fullPath: string): string => {
    // Remove the base path from the current path to get a relative path
    let relativePath = fullPath.startsWith(basePath)
      ? fullPath.substring(basePath.length)
      : fullPath;
    
    // If the relative path starts with a slash, remove it
    if (relativePath.startsWith('/')) {
      relativePath = relativePath.substring(1);
    }
    
    return relativePath || '';
  };

  const relativeCurrentPath = getRelativePath(currentPath);
  
  // Fetch folders when the currentPath changes
  useEffect(() => {
    if (!show) return;
    
    const fetchFolders = async () => {
      try {
        setLoading(true);
        setError(null);
        
        // Call the folder listing API
        const response = await fetch(`${config.apiBaseUrl}/folders/list?path=${encodeURIComponent(currentPath)}`, {
          headers: {
            'Authorization': 'Bearer ' + AuthService.getToken(),
            'Content-Type': 'application/json'
          }
        });
        
        if (!response.ok) {
          throw new Error(`HTTP error ${response.status}`);
        }
        
        const data = await response.json();
        setFolders(data);
      } catch (err) {
        console.error('Error fetching folders:', err);
        setError('Failed to fetch folders');
      } finally {
        setLoading(false);
      }
    };
    
    fetchFolders();
  }, [currentPath, show]);
  
  // Navigate to a folder
  const navigateToFolder = (folderPath: string) => {
    setCurrentPath(folderPath);
  };
  
  // Select current folder
  const handleSelect = () => {
    onSelect(currentPath);
    onHide();
  };
  
  // Handle new folder creation
  const handleCreateFolder = async () => {
    if (!newFolderName || newFolderName.trim() === '') {
      return;
    }
    
    try {
      setLoading(true);
      setError(null);
      
      // Call API to create a new folder
      const response = await fetch(`${config.apiBaseUrl}/folders/create`, {
        method: 'POST',
        headers: {
          'Authorization': 'Bearer ' + AuthService.getToken(),
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          parentPath: currentPath,
          folderName: newFolderName.trim()
        })
      });
      
      if (!response.ok) {
        const errorText = await response.text();
        throw new Error(errorText || `HTTP error ${response.status}`);
      }
      
      const newFolder = await response.json();
      
      // Add the new folder to the list
      setFolders([...folders, newFolder]);
      
      // Clear the input and hide it
      setNewFolderName('');
      setShowNewFolderInput(false);
      
      // Navigate to the new folder
      navigateToFolder(newFolder.fullPath);
      
    } catch (err) {
      console.error('Error creating folder:', err);
      setError('Failed to create folder: ' + (err instanceof Error ? err.message : 'Unknown error'));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Modal show={show} onHide={onHide} size="lg" centered className="folder-browser-modal">
      <Modal.Header closeButton className="bg-dark text-white">
        <Modal.Title className="d-flex align-items-center">
          <i className="bi bi-folder me-2"></i>
          Select Folder
        </Modal.Title>
      </Modal.Header>
      <Modal.Body className="p-0">
        {/* Current path breadcrumb */}
        <div className="bg-light p-3 border-bottom">
          <div className="d-flex align-items-center flex-wrap gap-1">
            <span className="fw-bold me-2">Location:</span>
            <span className="text-primary">
              {basePath}
              {relativeCurrentPath && (
                <>/<span className="fw-bold">{relativeCurrentPath}</span></>
              )}
            </span>
          </div>
        </div>
        
        {/* Error display */}
        {error && (
          <Alert variant="danger" className="m-3" onClose={() => setError(null)} dismissible>
            {error}
          </Alert>
        )}
        
        {/* Folder List */}
        <div style={{ height: '300px', overflowY: 'auto' }} className="p-0">
          {loading ? (
            <div className="text-center py-5">
              <div className="spinner-border text-primary" role="status">
                <span className="visually-hidden">Loading folders...</span>
              </div>
              <p className="mt-2">Loading folders...</p>
            </div>
          ) : folders.length === 0 ? (
            <div className="text-center py-5 text-muted">
              <i className="bi bi-folder fs-1"></i>
              <p className="mt-3">This folder is empty</p>
            </div>
          ) : (
            <ListGroup variant="flush">
              {folders.map((folder) => (
                <ListGroup.Item 
                  key={folder.fullPath}
                  action
                  className="d-flex align-items-center px-3 py-3 border-0 border-bottom"
                  onClick={() => navigateToFolder(folder.fullPath)}
                >
                  {folder.name === '..' ? (
                    <i className="bi bi-arrow-up-circle me-3 text-primary fs-5"></i>
                  ) : (
                    <i className="bi bi-folder-fill me-3 text-warning fs-5"></i>
                  )}
                  <span>{folder.name}</span>
                </ListGroup.Item>
              ))}
            </ListGroup>
          )}
        </div>
        
        {/* New folder input */}
        {showNewFolderInput && (
          <div className="p-3 border-top">
            <InputGroup>
              <InputGroup.Text>
                <i className="bi bi-folder-plus"></i>
              </InputGroup.Text>
              <Form.Control
                type="text"
                placeholder="New folder name"
                value={newFolderName}
                onChange={(e) => setNewFolderName(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && handleCreateFolder()}
                autoFocus
              />
              <Button variant="outline-secondary" onClick={() => setShowNewFolderInput(false)}>
                Cancel
              </Button>
              <Button variant="outline-primary" onClick={handleCreateFolder}>
                Create
              </Button>
            </InputGroup>
          </div>
        )}
      </Modal.Body>
      <Modal.Footer className="d-flex justify-content-between bg-light">
        <Button 
          variant="outline-secondary" 
          onClick={() => setShowNewFolderInput(true)} 
          disabled={showNewFolderInput}
        >
          <i className="bi bi-folder-plus me-1"></i>
          New Folder
        </Button>
        <div>
          <Button variant="secondary" className="me-2" onClick={onHide}>
            Cancel
          </Button>
          <Button variant="primary" onClick={handleSelect}>
            <i className="bi bi-arrow-return-left me-2"></i>
            Select This Folder
          </Button>
        </div>
      </Modal.Footer>
    </Modal>
  );
};

export default FolderBrowserModal;