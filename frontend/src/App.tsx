import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AuthProvider } from "./contexts/AuthContext";
import ProtectedRoute from "./components/ProtectedRoute";
import AppLayout from "./layouts/AppLayout";
import DashboardPage from "./pages/DashboardPage";
import ProfilePage from "./pages/ProfilePage";
import LoginPage from "./pages/LoginPage";
import AdminLoginPage from "./pages/AdminLoginPage";
import ChangePasswordPage from "./pages/ChangePasswordPage";
import LandingPage from "./pages/LandingPage";
import OrganizationAdminRoute from "./components/OrganizationAdminRoute";
import PickupStationsPage from "./pages/PickupStationsPage";
import AddPickupStationPage from "./pages/AddPickupStationPage";
import PickupStationDetailsPage from "./pages/PickupStationDetailsPage";
import EditPickupStationPage from "./pages/EditPickupStationPage";
import ProvisioningCodePage from "./pages/ProvisioningCodePage";
import OrganizacijaPage from "./pages/OrganizacijaPage";
import OrganizacijaClaniPage from "./pages/OrganizacijaClaniPage";

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<LandingPage />} />
          <Route path="/registracija" element={<ProvisioningCodePage />} />
          <Route path="/prijava" element={<LoginPage />} />
          <Route path="/prijava/skrbnik" element={<AdminLoginPage />} />
          <Route path="/sprememba-gesla" element={
            <ProtectedRoute>
              <ChangePasswordPage />
            </ProtectedRoute>
          } />
          <Route element={
            <ProtectedRoute>
              <AppLayout />
            </ProtectedRoute>
          }>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/profil" element={<ProfilePage />} />
            <Route path="/paketniki" element={<OrganizationAdminRoute><PickupStationsPage /></OrganizationAdminRoute>} />
            <Route path="/paketniki/dodaj" element={<OrganizationAdminRoute><AddPickupStationPage /></OrganizationAdminRoute>} />
            <Route path="/paketniki/:claimId" element={<OrganizationAdminRoute><PickupStationDetailsPage /></OrganizationAdminRoute>} />
            <Route path="/paketniki/:claimId/uredi" element={<OrganizationAdminRoute><EditPickupStationPage /></OrganizationAdminRoute>} />
            <Route path="/organizacija" element={<OrganizationAdminRoute><OrganizacijaPage /></OrganizationAdminRoute>} />
            <Route path="/organizacija/clani" element={<OrganizationAdminRoute><OrganizacijaClaniPage /></OrganizationAdminRoute>} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
