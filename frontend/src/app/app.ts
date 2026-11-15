import { Component, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { catchError, filter, map, of, switchMap } from 'rxjs';
import { LanguagePickerComponent } from './components/language-picker-component/language-picker-component';
import { AuthSessionService } from './services/auth-session.service';
import { RewardShopService } from './services/reward-shop.service';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  imports: [RouterOutlet, LanguagePickerComponent, TranslatePipe],
  selector: 'app-root',
  styleUrl: './app.css',
  template: `
    @if (!isDashboard()) {
      <header class="app-toolbar">
        <app-language-picker />
      </header>
    }
    @if (rewards.themeError()) {
      <p class="theme-error" role="alert">{{ 'rewards.themeLoadError' | translate }}</p>
    }
    <main>
      <router-outlet />
    </main>
  `,
})
export class App {
  private readonly auth = inject(AuthSessionService);
  protected readonly rewards = inject(RewardShopService);
  private readonly router = inject(Router);
  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );
  readonly isDashboard = computed(() =>
    ['home', 'sessions', 'rewards', 'profile'].includes(
      this.router.parseUrl(this.currentUrl()).root.children['primary']?.segments[0]?.path ?? '',
    ),
  );

  constructor() {
    toObservable(this.auth.currentUser).pipe(
      switchMap((user) => {
        this.rewards.reset();
        return user ? this.rewards.load().pipe(catchError((error: unknown) => {
          console.error('Could not load the current theme.', error);
          return of(null);
        })) : of(null);
      }),
      takeUntilDestroyed(),
    ).subscribe();
  }
}
