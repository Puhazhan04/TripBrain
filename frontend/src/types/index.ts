export interface DayActivity {
  id: string;
  name: string;
  description: string;
  category: string;
  estimatedCost: number;
  durationHours: number;
  recommendedTime: string;
  latitude: number;
  longitude: number;
  isIndoor: boolean;
  address: string;
  rating: number;
}

export interface DayPlan {
  id: string;
  dayNumber: number;
  date: string;
  weatherSummary: string;
  weatherTemp: number;
  weatherConditionCode: string;
  weatherIcon: string;
  activities: DayActivity[];
}

export interface Trip {
  id: string;
  destinationCity: string;
  latitude: number;
  longitude: number;
  budget: string;
  durationDays: number;
  interests: string[];
  createdAt: string;
  totalEstimatedCost: number;
  dayPlans?: DayPlan[];
}
