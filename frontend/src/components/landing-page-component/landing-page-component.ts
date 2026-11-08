import { Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  imports: [TranslatePipe],
  selector: 'app-landing-page-component',
  styleUrl: './landing-page-component.css',
  templateUrl: './landing-page-component.html',
})
export class LandingPageComponent {}
