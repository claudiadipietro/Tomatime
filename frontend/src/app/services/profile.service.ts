import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface Profile {
  name: string;
  email: string;
  pomoBalance: number;
  currentTheme: string;
  notificationsEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);

  get() {
    return this.http.get<Profile>('/api/profile');
  }

  updateNotifications(enabled: boolean) {
    return this.http.put<void>('/api/profile/notifications', { enabled });
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.post<void>('/api/profile/password', { currentPassword, newPassword });
  }
}
