import 'bootstrap-icons/font/bootstrap-icons.css';
import 'bootstrap/dist/css/bootstrap.min.css';
import React from 'react';
import { Link, Route, BrowserRouter as Router, Routes, useLocation } from 'react-router-dom';
import './App.css';
import RequireAuth from './components/RequireAuth';
import { RouterResetProvider } from './components/RouterReset';
import LoginPage from './pages/LoginPage';
import RecordingFormPage from './pages/RecordingFormPage';
import RecordingsPage from './pages/RecordingsPage';
import SettingsPage from './pages/SettingsPage';

// Navigation component
const Navigation: React.FC = () => {
  const location = useLocation();
  
  // Direct logout function that uses setTimeout to ensure the token removal happens
  // before any navigation is attempted
  const handleLogout = () => {
    // Remove token synchronously
    localStorage.removeItem('auth_token');
    
    // Use setTimeout to ensure the token removal completes
    // before attempting navigation
    setTimeout(() => {
      // Force a hard reload of the page to clear any React state
      window.location.href = '/login';
    }, 0);
    
    // Return false to prevent any default behavior
    return false;
  };
  
  return (
    <header className="bg-dark text-white py-2">
      <div className="container-fluid d-flex justify-content-between align-items-center">
        <Link 
          to="/" 
          className={`text-white me-3 ${location.pathname === '/' ? 'fw-bold' : ''}`}
          style={{ textDecoration: 'none' }}
        >
          <i className="bi bi-house-door me-1"></i>
          ipvcr
        </Link>
        <div className="d-flex">
          <Link 
            to="/settings" 
            className={`text-white ${location.pathname === '/settings' ? 'fw-bold' : ''}`}
            style={{ textDecoration: 'none' }}
          >
            <i className="bi bi-gear me-1"></i>
          </Link>
          &nbsp;
          <RequireAuth>
            <button
              onClick={handleLogout}
              className="btn btn-link text-white p-0 ms-3"
              style={{ textDecoration: 'none', border: 'none', background: 'none', cursor: 'pointer' }}
            >
              <i className="bi bi-box-arrow-right"></i>
            </button>
          </RequireAuth>
        </div>
      </div>
    </header>
  );
};

function App() {
  return (
    <RouterResetProvider>
      <Router>
        <AppContent />
      </Router>
    </RouterResetProvider>
  );
}

function AppContent() {
  const location = useLocation();

  return (
    <div className="App">
      {/* Render Navigation only if not on the login page */}
      {location.pathname !== '/login' && <Navigation />}
      <main>
        <Routes>
          <Route path="/" element={<RequireAuth><RecordingsPage /></RequireAuth>} />
          <Route path="/recordings" element={<RequireAuth><RecordingsPage /></RequireAuth>} />
          <Route path="/recordings/new" element={<RequireAuth><RecordingFormPage /></RequireAuth>} />
          <Route path="/recordings/:id" element={<RequireAuth><RecordingFormPage /></RequireAuth>} />
          <Route path="/settings" element={<RequireAuth><SettingsPage /></RequireAuth>} />
          <Route path="/login" element={<LoginPage />} />
        </Routes>
      </main>
      <footer className="bg-light text-center text-muted py-3 mt-5">
        <div className="container-fluid">
          <p className="mb-0">ipvcr &copy; {new Date().getFullYear()}</p>
        </div>
      </footer>
    </div>
  );
}

export default App;
