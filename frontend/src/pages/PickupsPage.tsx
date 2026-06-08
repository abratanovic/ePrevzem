import PickupsTable from "../components/dashboard/PickupsTable";
import { getAllPickups } from "../services/pickupsService";

export default function PickupsPage() {
  return (
    <div className="space-y-5 p-6">
      <PickupsTable
        loadPickups={getAllPickups}
        title="Seznam prevzemov"
        subtitle="Vsi ustvarjeni prevzemi"
        showAllLink={false}
      />
    </div>
  );
}
