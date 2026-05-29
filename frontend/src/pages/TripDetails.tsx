import React, { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { tripApi } from '../services/api';
import type { Trip } from '../types';
import { MapComponent } from '../components/MapComponent';
import { 
  ArrowLeft, Calendar, Star, 
  MapPin, Clock, CloudRain, CloudSun, 
  Map, List, CloudSnow, Sun, Loader2
} from 'lucide-react';

export const TripDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [trip, setTrip] = useState<Trip | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  
  // Navigation & Map States
  const [selectedDayIndex, setSelectedDayIndex] = useState(0);
  const [hoveredActivityIndex, setHoveredActivityIndex] = useState<number | null>(null);
  const [mobileView, setMobileView] = useState<'list' | 'map'>('list'); // mobile layout tab switcher

  useEffect(() => {
    if (id) {
      fetchTripDetails(id);
    }
  }, [id]);

  const fetchTripDetails = async (tripId: string) => {
    try {
      setLoading(true);
      const data = await tripApi.getTripById(tripId);
      setTrip(data);
      setError(null);
    } catch (err) {
      console.error(err);
      setError('Die Reisedetails konnten nicht abgerufen werden. Bitte stelle sicher, dass der Backend-Server erreichbar ist.');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="py-32 flex flex-col items-center justify-center space-y-3">
        <Loader2 className="w-12 h-12 text-indigo-500 animate-spin" />
        <p className="text-slate-400 font-medium">Dein Reiseplan wird geöffnet...</p>
      </div>
    );
  }

  if (error || !trip) {
    return (
      <div className="max-w-md mx-auto py-16 text-center space-y-6">
        <div className="p-6 rounded-2xl border border-rose-500/15 bg-rose-500/5">
          <p className="text-rose-400 text-sm font-medium">{error || 'Reiseplan nicht gefunden.'}</p>
        </div>
        <Link
          to="/"
          className="inline-flex items-center space-x-1.5 px-4 py-2 bg-slate-800 hover:bg-slate-700 text-white rounded-xl text-sm font-semibold transition"
        >
          <ArrowLeft className="w-4 h-4" />
          <span>Zurück zu meinen Reisen</span>
        </Link>
      </div>
    );
  }

  const dayPlans = trip.dayPlans || [];
  const sortedDayPlans = [...dayPlans].sort((a, b) => a.dayNumber - b.dayNumber);
  const currentDayPlan = sortedDayPlans[selectedDayIndex] || null;
  const currentActivities = currentDayPlan?.activities || [];

  // Weather Icon Matcher
  const getWeatherIcon = (code: string) => {
    const size = 'w-5 h-5';
    switch (code.toLowerCase()) {
      case 'rain': return <CloudRain className={`${size} text-blue-400`} />;
      case 'snow': return <CloudSnow className={`${size} text-sky-300`} />;
      case 'cloud': return <CloudSun className={`${size} text-slate-400`} />;
      default: return <Sun className={`${size} text-amber-400`} />;
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category.toLowerCase()) {
      case 'nature': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'food': return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      case 'museums': return 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20';
      case 'shopping': return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
      case 'culture': return 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20';
      case 'sightseeing': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      default: return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
    }
  };

  const translateCategory = (category: string) => {
    switch (category.toLowerCase()) {
      case 'nature': return 'Natur';
      case 'food': return 'Gastronomie';
      case 'museums': return 'Museen';
      case 'shopping': return 'Einkaufen';
      case 'culture': return 'Kultur';
      case 'sightseeing': return 'Sehenswürdigkeiten';
      default: return category;
    }
  };

  const translateBudget = (budget: string) => {
    switch (budget.toLowerCase()) {
      case 'economy': return 'Günstig';
      case 'luxury': return 'Luxus';
      default: return 'Mittelklasse';
    }
  };

  const isPrecipitationForecasted = currentDayPlan?.weatherConditionCode === 'rain' || currentDayPlan?.weatherConditionCode === 'snow';

  return (
    <div className="space-y-8">
      {/* Header Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="space-y-1">
          <Link
            to="/"
            className="inline-flex items-center space-x-1.5 text-slate-400 hover:text-slate-200 text-xs font-semibold transition"
          >
            <ArrowLeft className="w-3.5 h-3.5" />
            <span>Reiseverlauf</span>
          </Link>
          <h1 className="text-3xl font-extrabold tracking-tight text-white pr-4">
            {trip.destinationCity}
          </h1>
        </div>

        {/* Trip Stats Badges */}
        <div className="flex flex-wrap items-center gap-2">
          <span className="inline-flex items-center space-x-1 px-3 py-1.5 rounded-xl text-xs font-semibold bg-slate-900 border border-slate-800 text-slate-300">
            <Calendar className="w-3.5 h-3.5 text-indigo-400" />
            <span>{trip.durationDays} Tage</span>
          </span>
            <span className="inline-flex items-center space-x-1 px-3 py-1.5 rounded-xl text-xs font-semibold bg-slate-900 border border-slate-800 text-slate-300">
              <span>{translateBudget(trip.budget)}</span>
            </span>
          <span className="inline-flex items-center space-x-1.5 px-3.5 py-1.5 rounded-xl text-xs font-bold bg-indigo-500/15 text-indigo-300 border border-indigo-500/20 shadow-md">
            <span>Geschätzte Kosten:</span>
            <span className="text-slate-100">{(trip.totalEstimatedCost * 0.92).toFixed(2)} CHF</span>
          </span>
        </div>
      </div>

      {/* Day Swapper / Tab switcher */}
      {dayPlans.length > 1 && (
        <div className="flex items-center space-x-2 overflow-x-auto pb-2 border-b border-slate-900 scrollbar-none">
          {sortedDayPlans.map((day, idx) => (
            <button
              key={day.id}
              onClick={() => {
                setSelectedDayIndex(idx);
                setMobileView('list'); // reset view choice
              }}
              className={`flex items-center space-x-3 px-5 py-3 rounded-2xl border transition-all cursor-pointer whitespace-nowrap ${
                selectedDayIndex === idx
                  ? 'bg-indigo-500/10 border-indigo-500 text-slate-100'
                  : 'bg-slate-900/40 border-slate-900 text-slate-400 hover:border-slate-850 hover:text-slate-200'
              }`}
            >
              <div className="text-left leading-none">
                <span className="text-xs font-medium uppercase opacity-55 block">Tag</span>
                <span className="text-lg font-bold">{day.dayNumber}</span>
              </div>
              <div className="h-6 w-px bg-slate-800" />
              <div className="flex items-center space-x-1.5">
                {getWeatherIcon(day.weatherConditionCode)}
                <span className="text-sm font-semibold">{day.weatherTemp}°C</span>
              </div>
            </button>
          ))}
        </div>
      )}

      {/* Mobile Layout Switcher Toggle */}
      <div className="flex md:hidden bg-slate-900/60 p-1.5 rounded-xl border border-slate-850">
        <button
          onClick={() => setMobileView('list')}
          className={`flex-1 flex items-center justify-center space-x-1.5 py-2.5 rounded-lg text-xs font-bold transition-all ${
            mobileView === 'list'
              ? 'bg-indigo-600 text-white shadow-md'
              : 'text-slate-400 hover:text-slate-200'
          }`}
        >
          <List className="w-4 h-4" />
          <span>Zeitleisten-Ansicht</span>
        </button>
        <button
          onClick={() => setMobileView('map')}
          className={`flex-1 flex items-center justify-center space-x-1.5 py-2.5 rounded-lg text-xs font-bold transition-all ${
            mobileView === 'map'
              ? 'bg-indigo-600 text-white shadow-md'
              : 'text-slate-400 hover:text-slate-200'
          }`}
        >
          <Map className="w-4 h-4" />
          <span>Karten-Ansicht</span>
        </button>
      </div>

      {/* Main Grid View */}
      {currentDayPlan && (
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
          
          {/* Left / Itinerary view pane */}
          <div 
            className={`lg:col-span-7 space-y-6 ${
              mobileView === 'map' ? 'hidden md:block' : 'block'
            }`}
          >
            {/* Weather warning alert widget */}
            <div className="p-4 rounded-2xl border border-slate-900 bg-slate-900/40 flex items-start space-x-3.5 glass-panel">
              <div className="p-2.5 rounded-xl bg-indigo-500/10 text-indigo-400">
                {getWeatherIcon(currentDayPlan.weatherConditionCode)}
              </div>
              <div className="space-y-1">
                <h4 className="text-slate-100 font-bold text-sm">
                  Wettervorhersage für Tag {currentDayPlan.dayNumber} — {currentDayPlan.weatherSummary}
                </h4>
                <p className="text-slate-400 text-xs leading-relaxed">
                  Die erwartete Durchschnittstemperatur liegt bei etwa {currentDayPlan.weatherTemp}°C. 
                  {isPrecipitationForecasted 
                    ? " 🌧️ Niederschlagswarnung aktiv. TripBrain hat Indoor-Alternativen wie Museen oder Galerien eingeplant, damit du trocken bleibst."
                    : " ☀️ Hervorragende Aussenbedingungen erwartet. Ideal für Parks, Aussichtspunkte und Wanderwege."}
                </p>
              </div>
            </div>

            {/* Activities Timeline list */}
            <div className="space-y-4 relative pl-6 before:absolute before:left-2 before:top-4 before:bottom-4 before:w-[2px] before:bg-slate-800">
              {currentActivities.map((activity, index) => (
                <div
                  key={activity.id}
                  onMouseEnter={() => setHoveredActivityIndex(index)}
                  onMouseLeave={() => setHoveredActivityIndex(null)}
                  className={`relative p-5 rounded-2xl border transition-all duration-300 group hover-card bg-slate-900/20 ${
                    hoveredActivityIndex === index 
                      ? 'border-indigo-500 shadow-xl shadow-indigo-500/5 bg-slate-900/40' 
                      : 'border-slate-900'
                  }`}
                >
                  {/* Pin Circle index indicator on the timeline wire */}
                  <div className={`absolute -left-[24px] top-6 w-3 h-3 rounded-full border-2 transition-all ${
                    hoveredActivityIndex === index 
                      ? 'bg-purple-500 border-white scale-125 shadow-lg' 
                      : 'bg-indigo-600 border-slate-950'
                  }`} />

                  {/* Body Content */}
                  <div className="space-y-3.5">
                    <div className="flex flex-wrap items-start justify-between gap-2">
                      <div className="space-y-1">
                        <span className="text-[10px] text-indigo-400 font-bold tracking-wider uppercase block">
                          {activity.recommendedTime}
                        </span>
                        <h3 className="text-base font-extrabold text-slate-100 group-hover:text-indigo-300 transition-colors">
                          {activity.name}
                        </h3>
                      </div>
                      <span className={`inline-flex px-2 py-0.5 rounded text-[10px] font-semibold border ${getCategoryColor(activity.category)}`}>
                        {translateCategory(activity.category)}
                      </span>
                    </div>

                    <p className="text-slate-400 text-xs leading-relaxed">
                      {activity.description}
                    </p>

                    <div className="flex flex-wrap items-center gap-y-2 gap-x-4 text-xs text-slate-500 border-t border-slate-900 pt-3">
                      <span className="flex items-center gap-1 font-medium">
                        <Clock className="w-3.5 h-3.5 text-slate-500" />
                        <span>{activity.durationHours} Std.</span>
                      </span>
                      <span className="flex items-center gap-1 font-medium text-slate-300">

                        <span>{activity.estimatedCost === 0 ? 'Kostenlos' : `${(activity.estimatedCost * 0.92).toFixed(2)} CHF`}</span>
                      </span>
                      <span className="flex items-center gap-1">
                        <Star className="w-3.5 h-3.5 text-amber-500 fill-amber-500/25" />
                        <span className="font-semibold text-slate-300">{activity.rating}</span>
                      </span>
                      {activity.address && (
                        <span className="flex items-center gap-1 truncate max-w-[200px] text-slate-500" title={activity.address}>
                          <MapPin className="w-3.5 h-3.5" />
                          <span className="truncate">{activity.address.split(',')[0]}</span>
                        </span>
                      )}
                      <span className="text-[10px] px-1.5 py-0.5 rounded bg-slate-950 text-slate-400 border border-slate-900">
                        {activity.isIndoor ? '🏛️ Innen' : '🌲 Aussen'}
                      </span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Right / Map view pane */}
          <div 
            className={`lg:col-span-5 h-[450px] lg:h-[600px] lg:sticky lg:top-24 ${
              mobileView === 'list' ? 'hidden md:block' : 'block'
            }`}
          >
            <div className="h-full">
              <MapComponent
                latitude={trip.latitude}
                longitude={trip.longitude}
                activities={currentActivities}
                selectedActivityIndex={hoveredActivityIndex}
              />
            </div>
          </div>

        </div>
      )}
    </div>
  );
};
