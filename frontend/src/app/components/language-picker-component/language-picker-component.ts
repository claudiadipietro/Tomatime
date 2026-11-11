import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../i18n/language.service';

@Component({
  selector: 'app-language-picker',
  imports: [TranslatePipe],
  styleUrl: './language-picker-component.css',
  template: `
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
  `,
})
export class LanguagePickerComponent {
  readonly languageService = inject(LanguageService);

  onLanguageChange(event: Event): void {
    const target = event.currentTarget;
    if (target instanceof HTMLSelectElement && (target.value === 'en' || target.value === 'es')) {
      void this.languageService.setLanguage(target.value);
    }
  }
}
