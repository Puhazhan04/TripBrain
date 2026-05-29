import React, { useEffect, useRef } from 'react';
import L from 'leaflet';

// Load Leaflet CSS dynamically to ensure styling is applied without complex asset compilation issues
const ensureLeafletCss = () => {
  const cssId = 'leaflet-cdn-css';
  if (!document.getElementById(cssId)) {
    const link = document.createElement('link');
    link.id = cssId;
    link.rel = 'stylesheet';
    link.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
    link.integrity = 'sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY=';
    link.crossOrigin = '';
    document.head.appendChild(link);
  }
};

interface MapActivity {
  name: string;
  category: string;
  latitude: number;
  longitude: number;
  recommendedTime: string;
  rating: number;
}

interface MapComponentProps {
  latitude: number;
  longitude: number;
  activities: MapActivity[];
  selectedActivityIndex?: number | null;
}

export const MapComponent: React.FC<MapComponentProps> = ({
  latitude,
  longitude,
  activities,
  selectedActivityIndex,
}) => {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markersRef = useRef<L.Marker[]>([]);
  const polylineRef = useRef<L.Polyline | null>(null);

  useEffect(() => {
    ensureLeafletCss();
  }, []);

  // Initialize Map
  useEffect(() => {
    if (!mapContainerRef.current) return;

    // Destroy existing map if it exists
    if (mapRef.current) {
      mapRef.current.remove();
      mapRef.current = null;
    }

    // Create Map
    const map = L.map(mapContainerRef.current, {
      zoomControl: true,
      scrollWheelZoom: true,
    }).setView([latitude, longitude], 13);

    // Darker stylized tiles or standard OpenStreetMap
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    }).addTo(map);

    mapRef.current = map;

    return () => {
      if (mapRef.current) {
        mapRef.current.remove();
        mapRef.current = null;
      }
    };
  }, [latitude, longitude]);

  // Update Markers & Paths
  useEffect(() => {
    const map = mapRef.current;
    if (!map) return;

    // Clear old markers
    markersRef.current.forEach((marker) => marker.remove());
    markersRef.current = [];

    // Clear old polyline
    if (polylineRef.current) {
      polylineRef.current.remove();
      polylineRef.current = null;
    }

    if (!activities || activities.length === 0) return;

    const latlngs: L.LatLngExpression[] = [];

    activities.forEach((activity, index) => {
      const isSelected = selectedActivityIndex === index;
      latlngs.push([activity.latitude, activity.longitude]);

      // Create a gorgeous custom numbered HTML icon
      const customIcon = L.divIcon({
        html: `
          <div class="relative flex items-center justify-center">
            <span class="absolute inline-flex h-full w-full rounded-full bg-indigo-400 ${
              isSelected ? 'animate-ping opacity-75' : 'opacity-0'
            }"></span>
            <div class="w-8 h-8 rounded-full flex items-center justify-center border-2 shadow-lg font-bold text-sm transition-all duration-300 ${
              isSelected
                ? 'bg-purple-600 border-white text-white scale-110'
                : 'bg-indigo-600 border-slate-900 text-slate-100'
            }">
              ${index + 1}
            </div>
          </div>
        `,
        className: 'custom-numbered-marker',
        iconSize: [32, 32],
        iconAnchor: [16, 16],
      });

      const popupContent = `
        <div class="text-slate-900 font-sans p-1">
          <div class="font-bold text-base flex items-center justify-between gap-2 border-b border-slate-100 pb-1 mb-1">
            <span>#${index + 1} ${activity.name}</span>
            <span class="text-xs bg-indigo-100 text-indigo-800 px-1.5 py-0.5 rounded font-semibold">${activity.category}</span>
          </div>
          <p class="text-xs text-slate-500 mb-1">⏰ Geplant: ${activity.recommendedTime}</p>
          <div class="flex items-center text-xs text-amber-500">
            ★ <span class="text-slate-700 font-medium ml-1">${activity.rating} / 5.0</span>
          </div>
        </div>
      `;

      const marker = L.marker([activity.latitude, activity.longitude], { icon: customIcon })
        .bindPopup(popupContent)
        .addTo(map);

      // Open popup if selected
      if (isSelected) {
        marker.openPopup();
      }

      markersRef.current.push(marker);
    });

    // Draw route path between activities
    if (latlngs.length > 1) {
      const polyline = L.polyline(latlngs, {
        color: '#6366f1', // Indigo-500
        weight: 3,
        opacity: 0.7,
        dashArray: '6, 8', // Dashed line to look like a path trail
      }).addTo(map);

      polylineRef.current = polyline;
    }

    // Auto fit map boundary to contain all activities
    if (latlngs.length > 0) {
      const bounds = L.latLngBounds(latlngs);
      map.fitBounds(bounds, {
        padding: [60, 60],
        maxZoom: 15,
      });
    }
  }, [activities, selectedActivityIndex]);

  return (
    <div className="relative w-full h-full overflow-hidden border border-slate-800/80 rounded-2xl shadow-2xl">
      {activities.length === 0 && (
        <div className="absolute inset-0 flex items-center justify-center bg-slate-900/50 backdrop-blur-sm z-20">
          <p className="text-slate-400 font-medium text-sm">Für diesen Tag sind keine Orte auf der Karte eingetragen.</p>
        </div>
      )}
      <div ref={mapContainerRef} className="w-full h-full min-h-[450px]" />
    </div>
  );
};
