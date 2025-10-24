import React from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { useStore } from "./stores";
import { LoginPage } from "./pages/LoginPage";
import { SignupPage } from "./pages/SignupPage";
import { DashboardPage } from "./pages/DashboardPage";

const ProtectedRoute: React.FC<{ children: React.ReactElement }> = ({ children }) => {
  const { authStore } = useStore();
  return authStore.isAuthenticated ? children : <Navigate to="/login" />;
};

export const App: React.FC = observer(() => (
  <BrowserRouter>
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/signup" element={<SignupPage />} />
      <Route
        path="/dashboard"
        element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        }
      />
      <Route path="/" element={<Navigate to="/dashboard" />} />
    </Routes>
  </BrowserRouter>
));





