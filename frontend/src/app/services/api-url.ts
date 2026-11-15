import { InjectionToken } from '@angular/core';
import { apiOrigin } from '../config/api-origin';

export const API_ORIGIN = new InjectionToken<string>('API origin', {
  providedIn: 'root',
  factory: () => apiOrigin,
});

export function apiUrl(path: string, origin: string): string {
  if (!path.startsWith('/api/')) {
    throw new Error('API paths must start with /api/.');
  }
  return `${origin}${path}`;
}
