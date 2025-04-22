import React, { useState } from 'react';
import { Button, Card, Modal, ProgressBar } from 'react-bootstrap';
import { AuthService } from '../../services/AuthService';
import './SystemSettings.css'; // We'll create this CSS file

interface SystemSettingsProps {
  setSuccess: (message: string | null) => void;
  setError: (message: string | null) => void;
}

const SystemSettingsComponent: React.FC<SystemSettingsProps> = ({ setSuccess, setError }) => {
  // State for confirmation modals
  const [showRestartConfirm, setShowRestartConfirm] = useState<boolean>(false);
  const [showResetConfirm, setShowResetConfirm] = useState<boolean>(false);
  
  // State for progress bar
  const [showProgress, setShowProgress] = useState<boolean>(false);
  const [progress, setProgress] = useState<number>(0);
  const [statusMessage, setStatusMessage] = useState<string>('Preparing server maintenance...');

  // Fun status messages for the progress bar
  const funStatusMessages = [
    'Reticulating splines...',
    'Calibrating quantum flux capacitor...',
    'Defragmenting memory crystals...',
    'Synchronizing timeline variants...',
    'Optimizing neural pathways...',
    'Realigning digital chakras...',
    'Reconfiguring holographic matrices...',
    'Compiling artificial wisdom...',
    'Buffering reality streams...',
    'Debugging butterfly effects...',
    'Restructuring digital ecosystem...',
    'Re-energizing server hamsters...',
    'Polishing binary code...',
    'Unfolding quantum possibilities...',
    'Brewing server coffee...'
  ];

  // Function to show random fun messages during progress
  const updateProgressWithFunMessages = () => {
    let progressValue = 0;
    const interval = setInterval(() => {
      if (progressValue >= 100) {
        clearInterval(interval);
        return;
      }
      
      // Increment progress
      progressValue += Math.floor(Math.random() * 8) + 3; // Random increment between 3-10
      if (progressValue > 100) progressValue = 100;
      setProgress(progressValue);
      
      // Update message randomly
      if (progressValue % 10 === 0 || (progressValue > 90 && progressValue % 5 === 0)) {
        const randomIndex = Math.floor(Math.random() * funStatusMessages.length);
        setStatusMessage(funStatusMessages[randomIndex]);
      }
      
      // When complete, finalize
      if (progressValue === 100) {
        setStatusMessage('Operation complete! Redirecting to login...');
        setTimeout(() => {
          AuthService.logout();
          window.location.href = '/login';
        }, 2000);
      }
    }, 400);
  };

  // Handler for restarting server
  const handleRestartServer = async () => {
    setShowRestartConfirm(false);
    setShowProgress(true);
    setProgress(0);
    setStatusMessage('Initiating server restart procedure...');
    
    // Start the progress animation
    updateProgressWithFunMessages();
    
    try {
      // Call the restart endpoint - we don't wait for the response as the server will restart
      await AuthService.restartServer();
      // The progress animation will handle the logout and redirect
    } catch (err) {
      console.error('Error restarting server:', err);
      setError('Failed to restart server');
      setShowProgress(false);
    }
  };

  // Handler for resetting to factory defaults
  const handleResetToDefaults = async () => {
    setShowResetConfirm(false);
    setShowProgress(true);
    setProgress(0);
    setStatusMessage('Initiating factory reset procedure...');
    
    // Start the progress animation
    updateProgressWithFunMessages();
    
    try {
      // Call the reset endpoint - we don't wait for the response as the server will restart
      await AuthService.resetToDefaults();
      // The progress animation will handle the logout and redirect
    } catch (err) {
      console.error('Error resetting to defaults:', err);
      setError('Failed to reset to factory defaults');
      setShowProgress(false);
    }
  };

  return (
    <>
      <Card className="mb-4">
        <Card.Header className="bg-light">
          <i className="bi bi-pc-display me-2"></i>System Maintenance
        </Card.Header>
        <Card.Body>
          <div className="mb-4 maintenance-section">
            <h5>Server Management</h5>
            <p>
              These options allow you to restart the server or reset it to factory defaults. 
              Please use these options with caution.
            </p>
            
            <div className="d-grid gap-3">
              <Button 
                variant="warning" 
                onClick={() => setShowRestartConfirm(true)}
                size="lg"
                className="d-flex align-items-center justify-content-center"
              >
                <i className="bi bi-arrow-repeat me-2 fs-5"></i>
                Restart Server
              </Button>
              
              <Button 
                variant="danger" 
                onClick={() => setShowResetConfirm(true)}
                size="lg"
                className="d-flex align-items-center justify-content-center"
              >
                <i className="bi bi-exclamation-triangle me-2 fs-5"></i>
                Reset to Factory Defaults
              </Button>
            </div>
          </div>

          <div className="alert alert-warning">
            <i className="bi bi-exclamation-triangle-fill me-2"></i>
            <strong>Warning:</strong> Both operations will log you out and make the application unavailable for a few moments.
            Reset to factory defaults will erase all custom settings and restore the application to its initial state.
          </div>
        </Card.Body>
      </Card>

      {/* Restart Server Confirmation Modal */}
      <Modal
        show={showRestartConfirm}
        onHide={() => setShowRestartConfirm(false)}
        backdrop="static"
        keyboard={false}
      >
        <Modal.Header closeButton>
          <Modal.Title>Confirm Server Restart</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <div className="d-flex align-items-center mb-3">
            <i className="bi bi-exclamation-triangle-fill text-warning fs-1 me-3"></i>
            <div>
              <p className="mb-0"><strong>Are you sure you want to restart the server?</strong></p>
              <p className="text-muted mb-0">This will log you out and the application will be unavailable for a few moments.</p>
            </div>
          </div>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowRestartConfirm(false)}>
            Cancel
          </Button>
          <Button variant="warning" onClick={handleRestartServer}>
            <i className="bi bi-arrow-repeat me-2"></i>
            Yes, Restart Server
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Reset to Defaults Confirmation Modal */}
      <Modal
        show={showResetConfirm}
        onHide={() => setShowResetConfirm(false)}
        backdrop="static"
        keyboard={false}
      >
        <Modal.Header closeButton>
          <Modal.Title>Confirm Factory Reset</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <div className="d-flex align-items-center mb-3">
            <i className="bi bi-exclamation-triangle-fill text-danger fs-1 me-3"></i>
            <div>
              <p className="mb-0"><strong>Are you sure you want to reset to factory defaults?</strong></p>
              <p className="text-muted mb-0">This will erase all your settings and restore the application to its initial state. This action cannot be undone.</p>
            </div>
          </div>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onClick={() => setShowResetConfirm(false)}>
            Cancel
          </Button>
          <Button variant="danger" onClick={handleResetToDefaults}>
            <i className="bi bi-exclamation-triangle me-2"></i>
            Yes, Reset to Factory Defaults
          </Button>
        </Modal.Footer>
      </Modal>

      {/* Progress Modal */}
      <Modal
        show={showProgress}
        backdrop="static"
        keyboard={false}
        centered
      >
        <Modal.Header>
          <Modal.Title>Server Maintenance</Modal.Title>
        </Modal.Header>
        <Modal.Body>
          <div className="text-center mb-3">
            <i className="bi bi-gear-wide-connected fs-1 text-primary mb-3 spinner"></i>
            <h5>{statusMessage}</h5>
            <ProgressBar 
              animated 
              now={progress} 
              label={`${progress}%`} 
              className="mt-3" 
              variant={progress < 50 ? "info" : progress < 90 ? "primary" : "success"}
            />
          </div>
          <p className="text-muted text-center mt-3 small">
            Please do not close this window. You will be redirected to the login page when the operation is complete.
          </p>
        </Modal.Body>
      </Modal>
    </>
  );
};

export default SystemSettingsComponent;