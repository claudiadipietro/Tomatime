import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from './i18n/language.service';

@Component({
  imports: [RouterOutlet, TranslatePipe],
  selector: 'app-root',
  styleUrl: './app.css',
  template: `
    <header class="app-toolbar">
      <div class="language-picker">
        <label for="language-select">{{ 'language.label' | translate }}</label>
        <select
          id="language-select"
          [value]="languageService.currentLanguage()"
          (change)="onLanguageChange($event)"
        >
          <option value="en">English</option>
          <option value="es">Español</option>
        </select>
      </div>
    </header>
    <main>
      <router-outlet />
    </main>
  `,
})
export class App {
  constructor(readonly languageService: LanguageService) {}

  onLanguageChange(event: Event): void {
    const target = event.currentTarget;
    if (!(target instanceof HTMLSelectElement)) {
      return;
    }

    const language = target.value;
    if (language === 'en' || language === 'es') {
      void this.languageService.setLanguage(language);
    }
  }
}
