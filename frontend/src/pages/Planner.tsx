import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { tripApi } from '../services/api';
import { Compass, Calendar, Sparkles, MapPin, Landmark, Utensils, Trees, Tent, ShoppingBag } from 'lucide-react';

export const Planner: React.FC = () => {
  const navigate = useNavigate();
  
  // Form State
  const [destination, setDestination] = useState('');
  const [startDate, setStartDate] = useState(() => {
    const today = new Date();
    return today.toISOString().split('T')[0];
  });
  const [budget, setBudget] = useState('Mid-range');
  const [duration, setDuration] = useState(3);
  const [interests, setInterests] = useState<string[]>([]);
  
  // Loading & Error States
  const [isGenerating, setIsGenerating] = useState(false);
  const [loadingStep, setLoadingStep] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const interestOrder: Record<string, number> = { nature: 1, food: 2, museums: 3, adventure: 4, shopping: 5 };
  const interestOptions = [
    { id: 'nature', label: 'Natur & Parks', icon: Trees, color: 'text-emerald-400 bg-emerald-500/10' },
    { id: 'food', label: 'Essen & Trinken', icon: Utensils, color: 'text-amber-400 bg-amber-500/10' },
    { id: 'museums', label: 'Museen & Kunst', icon: Landmark, color: 'text-indigo-400 bg-indigo-500/10' },
    { id: 'adventure', label: 'Abenteuer & Sehenswürdigkeiten', icon: Tent, color: 'text-rose-400 bg-rose-500/10' },
    { id: 'shopping', label: 'Einkaufen & Malls', icon: ShoppingBag, color: 'text-purple-400 bg-purple-500/10' },
  ];

  const loadingTexts = [
    'Koordinaten auf der Karte werden ermittelt...',
    'Lokale Wettervorhersagen von Open-Meteo werden abgerufen...',
    'Umgebung nach passenden Attraktionen für deinen Geschmack durchsucht...',
    'Regengüsse werden vermieden und Outdoor-Aktivitäten neu angeordnet...',
    'Kosten werden berechnet und dein interaktiver Reiseplan erstellt...',
    'Reisedetails werden in deinem Archiv gespeichert...'
  ];

  useEffect(() => {
    let interval: any;
    if (isGenerating) {
      interval = setInterval(() => {
        setLoadingStep((prev) => (prev < loadingTexts.length - 1 ? prev + 1 : prev));
      }, 2000);
    }
    return () => clearInterval(interval);
  }, [isGenerating]);

  const handleInterestToggle = (id: string) => {
    if (interests.includes(id)) {
      setInterests(interests.filter((i) => i !== id));
    } else {
      setInterests([...interests, id]);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!destination.trim()) {
      setError('Bitte gib eine Zielstadt ein.');
      return;
    }
    if (interests.length === 0) {
      setError('Bitte wähle mindestens ein Interesse, um die Aktivitäten anzupassen.');
      return;
    }

    try {
      setError(null);
      setIsGenerating(true);
      setLoadingStep(0);
      
      const trip = await tripApi.createTrip({
        destinationCity: destination.trim(),
        budget,
        durationDays: duration,
        startDate: startDate,
        interests: interests,
      });

      // Redirect to the newly generated trip page
      navigate(`/trip/${trip.id}`);
    } catch (err: any) {
      console.error(err);
      setError(err.response?.data?.message || 'Fehler beim Erstellen der Reise. Bitte überprüfe den Zielnamen und versuche es erneut.');
      setIsGenerating(false);
    }
  };

  return (
    <div className="max-w-3xl mx-auto">
      {isGenerating ? (
        /* Immersive Custom Generation Loader */
        <div className="py-24 px-6 rounded-3xl border border-slate-800/80 bg-slate-900/40 text-center space-y-8 relative overflow-hidden glass-panel">
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-80 h-80 bg-indigo-500/10 blur-[80px] rounded-full pointer-events-none" />
          
          <div className="relative space-y-6">
            <div className="flex justify-center">
              <div className="relative">
                <div className="w-20 h-20 rounded-full border-4 border-slate-800 border-t-indigo-500 animate-spin" />
                <Compass className="w-10 h-10 text-indigo-400 absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 animate-pulse" />
              </div>
            </div>
            
            <div className="space-y-2">
              <h2 className="text-2xl font-bold text-white flex items-center justify-center gap-2">
                <span>Deine Reiseroute wird erstellt</span>
                <Sparkles className="w-5 h-5 text-indigo-400 fill-indigo-400/20" />
              </h2>
              <div className="h-6 overflow-hidden">
                <p className="text-indigo-300 font-medium text-sm transition-all duration-500 animate-pulse">
                  {loadingTexts[loadingStep]}
                </p>
              </div>
            </div>
            
            <div className="max-w-xs mx-auto bg-slate-800/50 rounded-full h-1.5 overflow-hidden">
              <div 
                className="bg-indigo-500 h-1.5 rounded-full transition-all duration-1000 ease-out" 
                style={{ width: `${((loadingStep + 1) / loadingTexts.length) * 100}%` }}
              />
            </div>
            <p className="text-xs text-slate-500">Dies dauert normalerweise 5-10 Sekunden</p>
          </div>
        </div>
      ) : (
        /* Planner Form Interface */
        <div className="space-y-8">
          <div className="space-y-2 text-center sm:text-left">
            <h1 className="text-3xl font-extrabold tracking-tight text-white flex items-center justify-center sm:justify-start gap-2">
              <Compass className="w-8 h-8 text-indigo-500" />
              <span>Plane dein nächstes Abenteuer</span>
            </h1>
            <p className="text-slate-400 text-sm">
              Gib deine Details ein und lass TripBrain einen massgeschneiderten Reiseplan Tag für Tag erstellen.
            </p>
          </div>

          {error && (
            <div className="p-4 rounded-xl border border-rose-500/20 bg-rose-500/5 text-rose-400 text-sm font-medium">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Destination input */}
            <div className="p-6 rounded-2xl border border-slate-900 bg-slate-900/30 space-y-3">
              <label htmlFor="destination" className="block text-sm font-bold text-slate-200">
                Wohin geht die Reise?
              </label>
              <div className="relative">
                <MapPin className="absolute left-4 top-3.5 w-5 h-5 text-slate-500" />
                <input
                  type="text"
                  id="destination"
                  value={destination}
                  onChange={(e) => setDestination(e.target.value)}
                  placeholder="z.B. Paris, Tokio, San Francisco..."
                  className="w-full pl-12 pr-4 py-3.5 rounded-xl glass-input text-base placeholder-slate-600"
                />
              </div>
            </div>

            {/* Start Date Picker */}
            <div className="p-6 rounded-2xl border border-slate-900 bg-slate-900/30 space-y-3">
              <label htmlFor="startDate" className="block text-sm font-bold text-slate-200">
                Wann beginnt deine Reise?
              </label>
              <div className="relative">
                <Calendar className="absolute left-4 top-3.5 w-5 h-5 text-slate-500" />
                <input
                  type="date"
                  id="startDate"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  className="w-full pl-12 pr-4 py-3.5 rounded-xl glass-input text-base placeholder-slate-600"
                />
              </div>
            </div>

            {/* Budget Selector */}
            <div className="p-6 rounded-2xl border border-slate-900 bg-slate-900/30 space-y-4">
              <label className="block text-sm font-bold text-slate-200">
                Was ist dein Budget?
              </label>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {[
                  { id: 'Economy', title: 'Günstig 💰', desc: 'Priorisiert freien Eintritt, malerische Spaziergänge und günstiges Streetfood.' },
                  { id: 'Mid-range', title: 'Mittelklasse 💵', desc: 'Eine komfortable Mischung aus Standardattraktionen, Museen und lokalen Bistros.' },
                  { id: 'Luxury', title: 'Luxus 💎', desc: 'Fokussiert sich auf private Touren, erstklassige Galerien und gehobene Gastronomie.' }
                ].map((tier) => (
                  <button
                    key={tier.id}
                    type="button"
                    onClick={() => setBudget(tier.id)}
                    className={`p-4 rounded-xl text-left border-2 transition-all duration-200 ${
                      budget === tier.id
                        ? 'bg-indigo-500/10 border-indigo-500 text-slate-100 shadow-lg shadow-indigo-500/5'
                        : 'bg-slate-950/40 border-slate-900 text-slate-400 hover:border-slate-800 hover:text-slate-200'
                    }`}
                  >
                    <h4 className="font-bold text-base mb-1">{tier.title}</h4>
                    <p className="text-xs leading-relaxed opacity-80">{tier.desc}</p>
                  </button>
                ))}
              </div>
            </div>

            {/* Duration Slider */}
            <div className="p-6 rounded-2xl border border-slate-900 bg-slate-900/30 space-y-4">
              <div className="flex justify-between items-center">
                <label htmlFor="duration" className="text-sm font-bold text-slate-200">
                  Wie viele Tage?
                </label>
                <span className="text-indigo-400 font-bold bg-indigo-500/10 border border-indigo-500/20 px-3 py-1 rounded-lg text-sm flex items-center gap-1.5">
                  <Calendar className="w-4 h-4" />
                  {duration} {duration === 1 ? 'Tag' : 'Tage'}
                </span>
              </div>
              <input
                type="range"
                id="duration"
                min="1"
                max="30"
                value={duration}
                onChange={(e) => setDuration(parseInt(e.target.value))}
                className="w-full h-2 bg-slate-950 rounded-lg appearance-none cursor-pointer accent-indigo-500 border border-slate-800"
              />
              <div className="flex items-center mt-2 space-x-2">
                <label htmlFor="durationNumber" className="text-sm text-slate-400">Tage:</label>
                <input
                  id="durationNumber"
                  type="number"
                  min="1"
                  max="30"
                  value={duration}
                  onChange={(e) => setDuration(e.target.value ? parseInt(e.target.value) : 1)}
                  className="w-16 px-2 py-1 bg-slate-900 text-slate-100 border border-slate-800 rounded"
                />
              </div>
              <div className="flex justify-between text-xs text-slate-600 font-medium px-1">
                {Array.from({length: duration}, (_, i) => (
                  <span key={i}>{i + 1}</span>
                ))}
              </div>
            </div>

            {/* Interests Toggles */}
            <div className="p-6 rounded-2xl border border-slate-900 bg-slate-900/30 space-y-4">
              <label className="block text-sm font-bold text-slate-200">
                Was sind deine Hauptinteressen?
              </label>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
                {interestOptions.filter(opt => !(interests.includes('adventure') && opt.id !== 'adventure')).map((opt, idx) => {
                  const selected = interests.includes(opt.id);
                  return (
                    <button
                      key={opt.id}
                      type="button"
                      onClick={() => handleInterestToggle(opt.id)}
                      className={`flex items-center space-x-3 p-3.5 rounded-xl border-2 transition-all text-left ${
                        selected
                          ? 'border-indigo-500 bg-indigo-500/10 text-slate-100'
                          : 'border-slate-900 bg-slate-950/40 text-slate-400 hover:border-slate-800 hover:text-slate-200'
                      }`}
                    >
                      <div className="p-2 rounded-lg bg-gray-800 text-white font-bold">
                        {interestOrder[opt.id]}
                      </div>
                      <span className="text-sm font-semibold">{opt.label}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Generate Button */}
            <div className="flex justify-end pt-2">
              <button
                type="submit"
                className="w-full sm:w-auto px-8 py-4 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 hover:from-indigo-600 hover:to-purple-700 text-white font-bold text-base shadow-lg shadow-indigo-500/25 hover:scale-[1.01] active:scale-[0.99] transition-all cursor-pointer flex items-center justify-center space-x-2"
              >
                <span>Reiseplan erstellen</span>
                <Sparkles className="w-5 h-5" />
              </button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
};
