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
    return this.http.get<Profile>('/api/users/profile');
  }

  updateNotifications(enabled: boolean) {
    return this.http.put<void>('/api/users/notifications', { enabled });
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.put<void>('/api/users/password', { currentPassword, newPassword });
  }
}
