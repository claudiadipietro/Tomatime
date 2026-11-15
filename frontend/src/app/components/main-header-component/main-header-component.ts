import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthSessionService } from '../../services/auth-session.service';
import { LanguagePickerComponent } from '../language-picker-component/language-picker-component';
import { PushNotificationsService } from '../../services/push-notifications.service';

@Component({
  selector: 'app-main-header',
  imports: [RouterLink, TranslatePipe, LanguagePickerComponent],
  templateUrl: './main-header-component.html',
  styleUrl: './main-header-component.css',
})
export class MainHeaderComponent {
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly push = inject(PushNotificationsService);
  menuOpen = false;
  logoutError = false;

  async logout(): Promise<void> {
    this.logoutError = false;
    try {
      if (!await this.router.navigate(['/login'])) return;
      await this.push.unsubscribe();
      this.session.clear();
    } catch {
      this.logoutError = true;
      await this.router.navigate(['/profile'], { queryParams: { logoutError: '1' } });
    }
  }
}
