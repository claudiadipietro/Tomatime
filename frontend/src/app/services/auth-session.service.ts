import { DOCUMENT } from '@angular/common';
import { computed, inject, Injectable, signal } from '@angular/core';
import { AuthResponse, AuthSession } from '../models/auth.models';
import { User } from '../models/user.model';

const SESSION_STORAGE_KEY = 'tomatime.auth';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly storage = inject(DOCUMENT).defaultView?.sessionStorage;
  private readonly session = signal<AuthSession | null>(this.readSession());

  readonly currentUser = computed(() => this.session()?.user ?? null);

  get accessToken(): string | null {
    const session = this.session();
    if (session && Date.parse(session.expiresAt) <= Date.now()) {
      this.clear();
      return null;
    }
    return session?.accessToken ?? null;
  }

  save(response: AuthResponse): void {
    const user: User = {
      id: response.userId,
      name: response.name,
      email: response.email,
    };
    const session: AuthSession = {
      user,
      accessToken: response.accessToken,
      expiresAt: response.expiresAt,
    };

    this.storage?.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
    this.session.set(session);
  }

  clear(): void {
    this.storage?.removeItem(SESSION_STORAGE_KEY);
    this.session.set(null);
  }

  private readSession(): AuthSession | null {
    const serializedSession = this.storage?.getItem(SESSION_STORAGE_KEY);
    if (!serializedSession) {
      return null;
    }

    try {
      const session = JSON.parse(serializedSession) as AuthSession;
      if (
        !session.accessToken ||
        !session.user ||
        !Number.isFinite(Date.parse(session.expiresAt)) ||
        Date.parse(session.expiresAt) <= Date.now()
      ) {
        this.storage?.removeItem(SESSION_STORAGE_KEY);
        return null;
      }

      return session;
    } catch {
      this.storage?.removeItem(SESSION_STORAGE_KEY);
      return null;
    }
  }
}
