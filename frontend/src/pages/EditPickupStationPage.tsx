import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import StationForm from "../components/stations/StationForm";
import StationPageHeader from "../components/stations/StationPageHeader";
import { stationService } from "../services/stations/stationService";
import type { OrganizationPickupStation } from "../types/stations";

export default function EditPickupStationPage() {
  const { claimId = "" } = useParams();
  const navigate = useNavigate();
  const [station, setStation] = useState<OrganizationPickupStation | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    stationService.getStation(claimId)
      .then(setStation)
      .catch(() => setError("Paketomat ni bil najden."));
  }, [claimId]);

  if (error) return <p className="p-6 text-sm text-red-600">{error}</p>;
  if (!station) return <p className="p-6 text-sm text-slate-500">Nalagam paketomat ...</p>;

  return (
    <div className="mx-auto max-w-4xl space-y-5 p-6">
      <StationPageHeader
        title="Uredi paketomat"
        subtitle={`Spremenite lokacijo paketomata ${station.serialNumber}.`}
        backTo={`/paketniki/${station.claimId}`}
      />
      <StationForm
        initialValues={{ serialNumber: station.serialNumber, ...station.location }}
        lockSerialNumber
        submitLabel="Shrani spremembe"
        onCancel={() => navigate(`/paketniki/${station.claimId}`)}
        onSubmit={async (values) => {
          await stationService.updateStation(station.claimId, {
            latitude: values.latitude,
            longitude: values.longitude,
            address: values.address,
            houseNumber: values.houseNumber,
            zipCode: values.zipCode,
            city: values.city,
          });
          navigate(`/paketniki/${station.claimId}`);
        }}
      />
    </div>
  );
}
