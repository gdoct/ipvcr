import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { AuthService } from '../services/AuthService';

interface RequireAuthProps {
  children: React.ReactElement;
}

const RequireAuth: React.FC<RequireAuthProps> = ({ children }) => {
  const location = useLocation();
  
  // Check authentication status directly without memoization
  // so it's evaluated fresh each render
  const isAuthenticated = AuthService.isAuthenticated();

  // If not authenticated, redirect to login page
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // If authenticated, render the child component
  return children;
};

export default RequireAuth;