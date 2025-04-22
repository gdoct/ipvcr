import React, { createContext, useContext, useState } from 'react';

// Create a context to manage router resets
interface RouterContextType {
  resetCount: number;
  resetRouter: () => void;
}

const RouterContext = createContext<RouterContextType>({
  resetCount: 0,
  resetRouter: () => {}
});

export const useRouterReset = () => useContext(RouterContext);

export const RouterResetProvider: React.FC<{children: React.ReactNode}> = ({ children }) => {
  const [resetCount, setResetCount] = useState<number>(0);
  
  // Function to force router to reset its internal state
  const resetRouter = () => {
    setResetCount(prev => prev + 1);
  };
  
  return (
    <RouterContext.Provider value={{ resetCount, resetRouter }}>
      {children}
    </RouterContext.Provider>
  );
};