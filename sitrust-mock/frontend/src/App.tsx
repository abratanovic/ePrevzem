import { Route, Routes } from "react-router-dom";
import LandingPage from "./pages/LandingPage";
import SipassLoginPage from "./pages/SiPassLoginPage";
import EosebnaPage from "./pages/EosebnaPage";
import EPrevzemHomePage from "./pages/EPrevzemHomePage";

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/sipass/login" element={<SipassLoginPage />} />
      <Route path="/sipass/eosebna" element={<EosebnaPage />} />
      <Route path="/home" element={<EPrevzemHomePage />} />
    </Routes>
  );
}
