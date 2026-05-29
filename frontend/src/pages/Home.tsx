import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { tripApi } from '../services/api';
import type { Trip } from '../types';
import { Compass, Calendar, Trash2, ArrowRight, Sun, DollarSign, MapPin, Loader2, History as HistoryIcon } from 'lucide-react';

export const Home: React.FC = () => {
  const [trips, setTrips] = useState<Trip[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchTrips();
  }, []);

  const fetchTrips = async () => {
    try {
      setLoading(true);
      const data = await tripApi.getAllTrips();
      setTrips(data);
      setError(null);
    } catch (err: any) {
      setError('Deine gespeicherten Reisen konnten nicht geladen werden. Bitte stelle sicher, dass das Backend läuft.');
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const handleDelete = async (e: React.MouseEvent, id: string) => {
    e.preventDefault();
    if (!window.confirm('Bist du sicher, dass du diesen Reiseplan löschen möchtest?')) return;
    try {
      await tripApi.deleteTrip(id);
      setTrips(trips.filter((t) => t.id !== id));
    } catch (err) {
      alert('Fehler beim Löschen der Reise. Bitte versuche es erneut.');
      console.error(err);
    }
  };

  const getBudgetColor = (budget: string) => {
    switch (budget.toLowerCase()) {
      case 'economy':
        return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
      case 'luxury':
        return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
      default:
        return 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20';
    }
  };

  const getBudgetIcon = (budget: string) => {
    switch (budget.toLowerCase()) {
      case 'economy': return '💰';
      case 'luxury': return '💎';
      default: return '⚖️';
    }
  };

  const translateBudget = (budget: string) => {
    switch (budget.toLowerCase()) {
      case 'economy': return 'Günstig';
      case 'luxury': return 'Luxus';
      default: return 'Mittelklasse';
    }
  };

  return (
    <div className="space-y-12">
      {/* Hero Welcome Banner */}
      <section className="relative overflow-hidden rounded-3xl border border-slate-800/80 bg-slate-900/40 p-8 sm:p-12 lg:p-16 text-center">
        {/* Glow behind hero */}
        <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[500px] h-[250px] bg-indigo-500/10 blur-[120px] rounded-full pointer-events-none" />

        <div className="relative max-w-3xl mx-auto space-y-6">
          <span className="inline-flex items-center space-x-2 px-3.5 py-1.5 rounded-full text-xs font-semibold bg-indigo-500/10 text-indigo-300 border border-indigo-500/25">
            <Compass className="w-3.5 h-3.5 animate-spin-slow text-indigo-400" />
            <span>Triff deinen neuen Reisebegleiter</span>
          </span>

          <h1 className="text-4xl sm:text-5xl lg:text-6xl font-extrabold tracking-tight text-white leading-[1.1]">
            Entdecke die Welt mit{' '}
            <span className="bg-gradient-to-r from-indigo-400 via-purple-400 to-indigo-300 bg-clip-text text-transparent">
              Intelligenten Reiserouten
            </span>
          </h1>

          <p className="text-slate-400 text-base sm:text-lg leading-relaxed">
            TripBrain nutzt Live-Wetterberichte und personalisierte Budgets, um optimierte, tagesgenaue Reisepläne basierend auf deinen genauen Interessen zu erstellen.
          </p>

          <div className="pt-4 flex justify-center">
            <Link
              to="/planner"
              className="inline-flex items-center space-x-2 px-6 py-3.5 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 hover:from-indigo-600 hover:to-purple-700 text-white font-semibold text-base shadow-lg shadow-indigo-500/25 hover:shadow-indigo-500/40 hover:scale-[1.02] active:scale-[0.98] transition-all"
            >
              <span>Neue Reise planen</span>
              <ArrowRight className="w-5 h-5" />
            </Link>
          </div>
        </div>
      </section>

      {/* Feature Highlight Cards */}
      <section className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="p-6 rounded-2xl border border-slate-900 bg-slate-900/30 space-y-3">
          <div className="w-10 h-10 rounded-xl bg-indigo-500/10 flex items-center justify-center text-indigo-400">
            <Sun className="w-5 h-5" />
          </div>
          <h3 className="text-lg font-bold text-slate-100">Wetterabhängige Planung</h3>
          <p className="text-slate-400 text-sm leading-relaxed">
            Regentag erwartet? TripBrain tauscht automatisch malerische Parks und Wanderungen gegen Museen, gemütliche Bistros und Galerien.
          </p>
        </div>

        <div className="p-6 rounded-2xl border border-slate-900 bg-slate-900/30 space-y-3">
          <div className="w-10 h-10 rounded-xl bg-purple-500/10 flex items-center justify-center text-purple-400">
            <DollarSign className="w-5 h-5" />
          </div>
          <h3 className="text-lg font-bold text-slate-100">Budget-Filter</h3>
          <p className="text-slate-400 text-sm leading-relaxed">
            Wir passen Attraktionsfilter und Speiseoptionen nahtlos an Günstig, Mittelklasse oder Luxus an.
          </p>
        </div>

        <div className="p-6 rounded-2xl border border-slate-900 bg-slate-900/30 space-y-3">
          <div className="w-10 h-10 rounded-xl bg-blue-500/10 flex items-center justify-center text-blue-400">
            <MapPin className="w-5 h-5" />
          </div>
          <h3 className="text-lg font-bold text-slate-100">Geografisches Clustering</h3>
          <p className="text-slate-400 text-sm leading-relaxed">
            Aktivitäten werden logisch nach Viertel und Koordinaten gruppiert, um Umwege zu reduzieren und die Sightseeing-Zeit zu maximieren.
          </p>
        </div>
      </section>

      {/* Trip History List / Vault */}
      <section className="space-y-6">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold tracking-tight text-white flex items-center gap-2">
            <HistoryIcon className="w-5 h-5 text-indigo-400" />
            <span>Meine Reisen</span>
          </h2>
          <span className="text-xs text-slate-500 font-medium">
            Gespeicherte Pläne: {trips.length}
          </span>
        </div>

        {loading ? (
          <div className="py-20 flex flex-col items-center justify-center space-y-3">
            <Loader2 className="w-10 h-10 text-indigo-500 animate-spin" />
            <p className="text-slate-400 text-sm">Reisen werden geladen...</p>
          </div>
        ) : error ? (
          <div className="p-6 rounded-2xl border border-rose-500/15 bg-rose-500/5 text-center space-y-4">
            <p className="text-rose-400 text-sm font-medium">{error}</p>
            <button
              onClick={fetchTrips}
              className="px-4 py-2 bg-slate-800 hover:bg-slate-700 text-white rounded-lg text-xs font-semibold transition"
            >
              Verbindung erneut versuchen
            </button>
          </div>
        ) : trips.length === 0 ? (
          <div className="py-16 text-center border-2 border-dashed border-slate-800 rounded-3xl space-y-4">
            <p className="text-slate-400 text-sm">Noch keine Reisepläne gespeichert.</p>
            <Link
              to="/planner"
              className="inline-flex items-center space-x-1.5 text-indigo-400 hover:text-indigo-300 text-sm font-semibold transition"
            >
              <span>Erstelle deinen ersten Reiseplan</span>
              <ArrowRight className="w-4 h-4" />
            </Link>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {trips.map((trip) => (
              <Link
                key={trip.id}
                to={`/trip/${trip.id}`}
                className="group block relative rounded-2xl border border-slate-800/80 bg-slate-900/25 p-5 hover-card transition-all"
              >
                {/* Trash delete icon */}
                <button
                  onClick={(e) => handleDelete(e, trip.id)}
                  className="absolute top-4 right-4 p-2 rounded-lg bg-slate-800/60 hover:bg-rose-500/20 text-slate-400 hover:text-rose-400 border border-slate-700/50 hover:border-rose-500/20 transition-all opacity-0 group-hover:opacity-100 z-20"
                  title="Reiseplan löschen"
                >
                  <Trash2 className="w-4 h-4" />
                </button>

                <div className="space-y-4">
                  <div className="space-y-1">
                    <h3 className="text-lg font-bold text-slate-100 group-hover:text-indigo-400 transition-colors pr-8 truncate">
                      {trip.destinationCity}
                    </h3>
                    <div className="flex items-center space-x-2 text-xs text-slate-400">
                      <Calendar className="w-3.5 h-3.5 text-indigo-500" />
                      <span>
                        {trip.durationDays} {trip.durationDays === 1 ? 'Tag' : 'Tage'} Reiseroute
                      </span>
                    </div>
                  </div>

                  {/* Interests tags */}
                  <div className="flex flex-wrap gap-1.5">
                    {trip.interests.slice(0, 3).map((interest) => (
                      <span
                        key={interest}
                        className="text-[10px] bg-slate-800 text-slate-300 px-2 py-0.5 rounded-full font-medium"
                      >
                        {interest}
                      </span>
                    ))}
                    {trip.interests.length > 3 && (
                      <span className="text-[10px] text-slate-500 px-1 py-0.5">
                        +{trip.interests.length - 3} weitere
                      </span>
                    )}
                  </div>

                  {/* Footer details inside card */}
                  <div className="pt-4 border-t border-slate-800/60 flex items-center justify-between text-xs">
                    <span className="text-slate-500">
                      Geschätzte Kosten: <span className="font-semibold text-slate-300">{(trip.totalEstimatedCost * 0.92).toFixed(2)} CHF</span>
                    </span>
                    <span
                      className={`inline-flex items-center px-2 py-0.5 rounded-md text-[10px] font-semibold border ${getBudgetColor(
                        trip.budget
                      )}`}
                    >
                      {getBudgetIcon(trip.budget)} {translateBudget(trip.budget)}
                    </span>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        )}
      </section>
    </div>
  );
};
