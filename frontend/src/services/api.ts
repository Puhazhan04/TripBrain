import axios from 'axios';
import type { Trip } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5178/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const tripApi = {
  // Get all trips (summaries)
  getAllTrips: async (): Promise<Trip[]> => {
    const response = await apiClient.get<Trip[]>('/trips');
    return response.data;
  },

  // Get a single trip with its full dayplans and activities
  getTripById: async (id: string): Promise<Trip> => {
    const response = await apiClient.get<Trip>(`/trips/${id}`);
    return response.data;
  },

  // Generate and save a new trip
  createTrip: async (tripRequest: {
    destinationCity: string;
    budget: string;
    durationDays: number;
    startDate?: string;
    interests: string[];
  }): Promise<Trip> => {
    const response = await apiClient.post<Trip>('/trips', tripRequest);
    return response.data;
  },

  // Delete a trip
  deleteTrip: async (id: string): Promise<void> => {
    await apiClient.delete(`/trips/${id}`);
  },
};
