import { useEffect, useMemo } from "react";
import { divIcon, type LeafletMouseEvent, type Marker as LeafletMarker } from "leaflet";
import { MapContainer, Marker, TileLayer, useMap, useMapEvents } from "react-leaflet";

interface Coordinates {
  latitude: number;
  longitude: number;
}

interface StationMapPickerProps {
  coordinates: Coordinates | null;
  onChange: (coordinates: Coordinates) => void;
}

const SLOVENIA_CENTER: [number, number] = [46.1512, 14.9955];

function MapClickHandler({ onChange }: Pick<StationMapPickerProps, "onChange">) {
  useMapEvents({
    click(event: LeafletMouseEvent) {
      onChange({
        latitude: Number(event.latlng.lat.toFixed(6)),
        longitude: Number(event.latlng.lng.toFixed(6)),
      });
    },
  });
  return null;
}

function MapCenterUpdater({ coordinates }: Pick<StationMapPickerProps, "coordinates">) {
  const map = useMap();

  useEffect(() => {
    if (coordinates) {
      map.setView([coordinates.latitude, coordinates.longitude], Math.max(map.getZoom(), 17));
    }
  }, [coordinates, map]);

  return null;
}

export default function StationMapPicker({ coordinates, onChange }: StationMapPickerProps) {
  const markerIcon = useMemo(
    () => divIcon({
      className: "",
      html: '<div class="h-7 w-7 rounded-full border-4 border-white bg-accent shadow-lg ring-2 ring-accent/30"></div>',
      iconAnchor: [14, 14],
    }),
    [],
  );

  return (
    <div className="overflow-hidden rounded-2xl border border-slate-200 bg-slate-50">
      <MapContainer center={SLOVENIA_CENTER} zoom={8} className="h-80 w-full">
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />
        <MapClickHandler onChange={onChange} />
        <MapCenterUpdater coordinates={coordinates} />
        {coordinates && (
          <Marker
            draggable
            icon={markerIcon}
            position={[coordinates.latitude, coordinates.longitude]}
            eventHandlers={{
              dragend(event) {
                const marker = event.target as LeafletMarker;
                const position = marker.getLatLng();
                onChange({
                  latitude: Number(position.lat.toFixed(6)),
                  longitude: Number(position.lng.toFixed(6)),
                });
              },
            }}
          />
        )}
      </MapContainer>
    </div>
  );
}
