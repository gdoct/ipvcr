import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Button, Modal } from 'react-bootstrap';
import { Prism as SyntaxHighlighter } from 'react-syntax-highlighter';
import { tomorrow } from 'react-syntax-highlighter/dist/esm/styles/prism';
import { TaskDefinitionModel } from '../types/recordings';

interface TaskEditorProps {
  show: boolean;
  onHide: () => void;
  onSave: (id: string, content: string) => void;
  taskDefinition: TaskDefinitionModel | null;
}

const TaskEditor: React.FC<TaskEditorProps> = ({
  show,
  onHide,
  onSave,
  taskDefinition
}) => {
  const [content, setContent] = useState<string>('');
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const highlighterRef = useRef<HTMLDivElement>(null);
  
  // Initialize editor with task definition
  useEffect(() => {
    if (taskDefinition && show) {
      setContent(taskDefinition.content);
    }
  }, [taskDefinition, show]);

  // Handle content change
  const handleContentChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setContent(e.target.value);
  };

  // Handle save button click
  const handleSave = useCallback(() => {
    if (taskDefinition?.id) {
      onSave(taskDefinition.id, content);
    }
  }, [taskDefinition, content, onSave]);

  // Handle safe close
  const handleClose = useCallback(() => {
    // Clear content to prevent memory leaks
    setContent('');
    // Call the parent's onHide function
    onHide();
  }, [onHide]);
  
  // Handle tab key press in textarea
  const handleKeyDown = useCallback((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Tab') {
      e.preventDefault();
      
      const textarea = textareaRef.current;
      if (textarea) {
        const start = textarea.selectionStart;
        const end = textarea.selectionEnd;
        
        // Insert tab at cursor position
        const newContent = content.substring(0, start) + '    ' + content.substring(end);
        setContent(newContent);
        
        // Move cursor after the inserted tab
        setTimeout(() => {
          if (textarea) {
            textarea.selectionStart = textarea.selectionEnd = start + 4;
          }
        }, 0);
      }
    }
  }, [content]);

  // Synchronize scrolling between textarea and syntax highlighter
  const handleScroll = useCallback((e: React.UIEvent<HTMLTextAreaElement>) => {
    if (highlighterRef.current && textareaRef.current) {
      highlighterRef.current.scrollTop = textareaRef.current.scrollTop;
      highlighterRef.current.scrollLeft = textareaRef.current.scrollLeft;
    }
  }, []);

  // Clean up resources when component unmounts or modal closes
  useEffect(() => {
    // If modal is not shown, clean up event listeners
    const cleanup = () => {
      const textarea = textareaRef.current;
      if (textarea) {
        // Remove any event listeners that might be causing issues
        textarea.onscroll = null;
        textarea.onkeydown = null;
      }
    };

    if (!show) {
      cleanup();
    }

    // Also clean up when component unmounts
    return cleanup;
  }, [show]);

  return (
    <Modal 
      show={show} 
      onHide={handleClose} 
      size="lg" 
      backdrop="static"
      dialogClassName="task-editor-modal"
    >
      <Modal.Header closeButton className="bg-secondary text-white">
        <Modal.Title>
          <i className="bi bi-braces me-2"></i>
          Edit Task Definition: {taskDefinition?.name || ''}
        </Modal.Title>
      </Modal.Header>
      <Modal.Body className="d-flex flex-column" style={{ overflow: 'hidden' }}>
        <div className="mb-2 d-flex justify-content-between align-items-center">
          <label className="form-label fw-bold">Task Definition</label>
          <small className="text-muted">Edit the task definition below</small>
        </div>
        
        <div className="flex-grow-1 border rounded editor-container" 
          style={{ 
            position: 'relative', 
            minHeight: '300px', 
            width: '100%',
            overflow: 'hidden'
          }}
        >
          {/* Editable textarea with visible scrollbars */}
          <textarea
            ref={textareaRef}
            value={content}
            onChange={handleContentChange}
            onKeyDown={handleKeyDown}
            onScroll={handleScroll}
            spellCheck={false}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              width: '100%',
              height: '100%',
              color: 'transparent',
              caretColor: 'black',
              background: 'transparent',
              padding: '12px',
              fontSize: '0.9rem',
              fontFamily: 'monospace',
              resize: 'none',
              overflow: 'auto',
              whiteSpace: 'pre',
              overflowWrap: 'normal',
              border: 'none',
              outline: 'none',
              zIndex: 2
            }}
          />
          
          {/* Syntax highlighting display layer without scrollbars */}
          <div 
            ref={highlighterRef}
            style={{ 
              position: 'absolute', 
              top: 0, 
              left: 0, 
              right: 0, 
              bottom: 0, 
              pointerEvents: 'none',
              padding: '12px',
              overflow: 'hidden',
              zIndex: 1
            }}
          >
            <SyntaxHighlighter
              language="bash"
              style={tomorrow}
              wrapLines={true}
              wrapLongLines={false}
              customStyle={{
                margin: 0,
                padding: 0,
                background: 'transparent',
                fontSize: '0.9rem',
                fontFamily: 'monospace',
                overflow: 'visible' // Prevent scrollbars in the highlighter
              }}
            >
              {content || ' '}
            </SyntaxHighlighter>
          </div>
        </div>
      </Modal.Body>
      <Modal.Footer className="bg-light p-3">
        <Button variant="secondary" className="me-2" onClick={handleClose}>
          <i className="bi bi-x-circle me-1"></i>Cancel
        </Button>
        <Button 
          variant="primary" 
          onClick={handleSave}
        >
          <i className="bi bi-save me-1"></i>Save
        </Button>
      </Modal.Footer>
    </Modal>
  );
};

export default TaskEditor;