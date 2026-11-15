import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthSessionService } from '../../services/auth-session.service';
import { LanguagePickerComponent } from '../language-picker-component/language-picker-component';

@Component({
  selector: 'app-main-header',
  imports: [RouterLink, TranslatePipe, LanguagePickerComponent],
  templateUrl: './main-header-component.html',
  styleUrl: './main-header-component.css',
})
export class MainHeaderComponent {
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  menuOpen = false;

  async logout(): Promise<void> {
    if (await this.router.navigate(['/login'])) this.session.clear();
  }
}
