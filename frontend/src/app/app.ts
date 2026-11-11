import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { LanguagePickerComponent } from './components/language-picker-component/language-picker-component';

@Component({
  imports: [RouterOutlet, LanguagePickerComponent],
  selector: 'app-root',
  styleUrl: './app.css',
  template: `
    @if (!isDashboard()) {
      <header class="app-toolbar">
        <app-language-picker />
      </header>
    }
    <main>
      <router-outlet />
    </main>
  `,
})
export class App {
  private readonly router = inject(Router);
  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );
  readonly isDashboard = computed(
    () =>
      this.router.parseUrl(this.currentUrl()).root.children['primary']?.segments[0]?.path ===
      'home',
  );
}
