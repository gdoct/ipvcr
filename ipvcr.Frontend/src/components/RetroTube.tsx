import React, { useEffect, useRef, useState } from 'react';
import './RetroTube.css'; // Import the CSS file for animations

interface RetroTubeProps {
  imageSrc: string;
  altText: string;
  maxHeight?: string;
  width?: string;
  imageWidth?: string;  // New prop for image width
  imageHeight?: string; // New prop for image height
}

/**
 * RetroTube component displays an image in a retro TV tube style.
 * Now with DVD-style bouncing logo animation and analog TV effects.
 */
const RetroTube: React.FC<RetroTubeProps> = ({ 
  imageSrc, 
  altText,
  maxHeight = '250px',
  width = '40%',
  imageWidth = '140px',
  imageHeight = '140px'
}) => {
  // State for image position and direction
  const [position, setPosition] = useState({ x: 20, y: 20 });
  const [direction, setDirection] = useState({ x: 1, y: 1 });
  const [color, setColor] = useState('hsl(120, 100%, 65%)'); // Start with green
  
  // Refs for container dimensions and animation
  const containerRef = useRef<HTMLDivElement>(null);
  const imageRef = useRef<HTMLImageElement>(null);
  const animationRef = useRef<number | undefined>(undefined);
  
  // Random color generator for bounces
  const getRandomColor = () => {
    const hue = Math.floor(Math.random() * 360);
    return `hsl(${hue}, 100%, 65%)`;
  };
  
  // Setup animation loop
  useEffect(() => {
    const bounceAnimation = () => {
      if (!containerRef.current || !imageRef.current) return;
      
      const containerRect = containerRef.current.getBoundingClientRect();
      const imageRect = imageRef.current.getBoundingClientRect();
      
      // Calculate new position
      let newX = position.x + direction.x * 2;
      let newY = position.y + direction.y * 2;
      let newXDir = direction.x;
      let newYDir = direction.y;
      let colorChange = false;
      
      // Check for horizontal bounds
      if (newX <= 0) {
        newX = 0;
        newXDir = 1;
        colorChange = true;
      } else if (newX + imageRect.width >= containerRect.width) {
        newX = containerRect.width - imageRect.width;
        newXDir = -1;
        colorChange = true;
      }
      
      // Check for vertical bounds
      if (newY <= 0) {
        newY = 0;
        newYDir = 1;
        colorChange = true;
      } else if (newY + imageRect.height >= containerRect.height) {
        // Changed: Now bounces when the bottom of image hits the bottom of container
        newY = containerRect.height - imageRect.height;
        newYDir = -1;
        colorChange = true;
      }
      
      // Update direction and position
      setDirection({ x: newXDir, y: newYDir });
      setPosition({ x: newX, y: newY });
      
      // Change color on bounce
      if (colorChange) {
        setColor(getRandomColor());
      }
      
      // Continue animation
      animationRef.current = requestAnimationFrame(bounceAnimation);
    };
    
    // Start animation
    animationRef.current = requestAnimationFrame(bounceAnimation);
    
    // Cleanup on unmount
    return () => {
      if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
      }
    };
  }, [position, direction]);
  
  return (
    <div 
      className="retro-tube-container" 
      ref={containerRef}
      style={{
        position: 'relative',
        background: '#111',
        borderRadius: '20px',
        padding: '15px',
        boxShadow: `0 0 15px rgba(0,255,0,0.3), inset 0 0 30px rgba(0,0,0,0.7)`,
        width: width,
        maxWidth: '500px',
        height: '300px', // Fixed height for bounce area
        margin: '0 auto',
        overflow: 'hidden'
      }}
    >
      <div 
        className="retro-tube-screen analog-effect" 
        style={{
          borderRadius: '15px',
          overflow: 'hidden',
          position: 'relative',
          background: '#f5f5f0', // Changed from #000 to off-white
          width: '100%',
          height: '100%'
        }}
      >
        {/* Random vertical glitch line */}
        <div className="vertical-glitch"></div>
        
        {/* Horizontal distortion band */}
        <div className="horizontal-distortion"></div>
        
        {/* Scan lines overlay */}
        <div className="scan-lines"></div>
        
        {/* The bouncing image */}
        <div
          style={{
            position: 'absolute',
            top: position.y,
            left: position.x,
            zIndex: 1,
            transition: 'filter 0.3s'
          }}
        >
          <img 
            ref={imageRef}
            src={imageSrc} 
            alt={altText} 
            className="tv-image"
            style={{ 
              maxHeight: imageHeight, // Using the configurable prop
              maxWidth: imageWidth,   // Using the configurable prop
              filter: `brightness(1.1) contrast(1.2) saturate(1.2) drop-shadow(0 0 5px ${color})`,
              transition: 'filter 0.5s ease'
            }}
          />
        </div>
        
        {/* Color overlay for analog color shifts */}
        <div className="color-shift"></div>
        
        {/* Slight glow effect */}
        <div className="screen-glow"></div>
        
        {/* Occasional frame jump effect */}
        <div className="frame-jump"></div>
      </div>
      
      {/* "TV Controls" */}
      <div style={{
        marginTop: '10px',
        display: 'flex',
        justifyContent: 'center',
        gap: '10px'
      }}>
        <div style={{ 
          width: '12px', 
          height: '12px', 
          borderRadius: '50%', 
          background: '#555',
          boxShadow: 'inset 0 0 3px #222'
        }}/>
        <div style={{ 
          width: '12px', 
          height: '12px', 
          borderRadius: '50%', 
          background: '#555',
          boxShadow: 'inset 0 0 3px #222'
        }}/>
      </div>
    </div>
  );
};

export default RetroTube;