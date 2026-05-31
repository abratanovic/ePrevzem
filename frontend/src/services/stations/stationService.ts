import { httpStationAdapter } from "./httpStationAdapter";
import { mockStationAdapter } from "./mockStationAdapter";

const useMockStations = import.meta.env.VITE_USE_MOCK_STATIONS !== "false";

export const stationService = useMockStations ? mockStationAdapter : httpStationAdapter;
