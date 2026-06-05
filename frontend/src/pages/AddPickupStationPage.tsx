import { useNavigate } from "react-router-dom";
import StationForm from "../components/stations/StationForm";
import StationPageHeader from "../components/stations/StationPageHeader";
import { stationService } from "../services/stations/stationService";

export default function AddPickupStationPage() {
  const navigate = useNavigate();

  return (
    <div className="mx-auto max-w-4xl space-y-5 p-6">
      <StationPageHeader
        title="Dodaj paketomat"
        subtitle="Prevzemite predhodno registrirani fizični paketomat in določite njegovo lokacijo."
      />
      <StationForm
        submitLabel="Dodaj paketomat"
        onCancel={() => navigate("/paketniki")}
        onSubmit={async (values) => {
          await stationService.createStation(values);
          navigate("/paketniki");
        }}
      />
    </div>
  );
}
