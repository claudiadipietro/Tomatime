import { DOCUMENT } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { firstValueFrom } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class PushNotificationsService {
  private readonly http = inject(HttpClient);
  private readonly swPush = inject(SwPush, { optional: true });
  private readonly document = inject(DOCUMENT);

  get supported(): boolean {
    const window = this.document.defaultView;
    return !!this.swPush?.isEnabled && !!window && 'Notification' in window && 'PushManager' in window;
  }

  async current(): Promise<PushSubscription | null> {
    const navigator = this.document.defaultView?.navigator;
    if (!navigator || !('serviceWorker' in navigator)) return null;
    const registration = await navigator.serviceWorker.getRegistration();
    return registration?.pushManager ? registration.pushManager.getSubscription() : null;
  }

  async registered(): Promise<boolean> {
    const subscription = await this.current();
    if (!subscription) return false;
    const result = await firstValueFrom(this.http.post<{ registered: boolean }>('/api/push/status', { endpoint: subscription.endpoint }));
    if (!result.registered || this.document.defaultView!.Notification.permission !== 'granted') return false;
    await firstValueFrom(this.http.get<{ publicKey: string }>('/api/push/key'));
    return true;
  }

  async subscribe(): Promise<void> {
    if (!this.supported) throw new Error('Push notifications are not supported on this device.');
    const permission = await this.document.defaultView!.Notification.requestPermission();
    if (permission !== 'granted') throw new DOMException('Notification permission was not granted.', 'NotAllowedError');
    const { publicKey } = await firstValueFrom(this.http.get<{ publicKey: string }>('/api/push/key'));
    let subscription = await this.current();
    if (subscription) {
      const expected = Uint8Array.from(atob(publicKey.replace(/-/g, '+').replace(/_/g, '/')
        .padEnd(Math.ceil(publicKey.length / 4) * 4, '=')), (c) => c.charCodeAt(0));
      const existing = subscription.options.applicationServerKey;
      if (!existing || expected.length !== existing.byteLength ||
          expected.some((byte, index) => byte !== new Uint8Array(existing)[index])) {
        await this.unsubscribe();
        subscription = null;
      }
    }
    subscription ??= await this.swPush!.requestSubscription({ serverPublicKey: publicKey });
    const json = subscription.toJSON();
    if (!json.endpoint || !json.keys?.['p256dh'] || !json.keys?.['auth']) throw new Error('Invalid push subscription.');
    await firstValueFrom(this.http.put<void>('/api/push/subscription', {
      endpoint: json.endpoint, p256dh: json.keys['p256dh'], auth: json.keys['auth'],
      language: this.document.documentElement.lang === 'es' ? 'es' : 'en',
    }));
  }

  async unsubscribe(): Promise<void> {
    const subscription = await this.current();
    if (!subscription) return;
    await firstValueFrom(this.http.post<void>('/api/push/unsubscribe', { endpoint: subscription.endpoint }));
    if (!await subscription.unsubscribe()) throw new Error('Failed to unsubscribe this device.');
  }
}
