import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  imports: [RouterLink, TranslatePipe],
  selector: 'app-landing-page-component',
  styleUrl: './landing-page-component.css',
  templateUrl: './landing-page-component.html',
})
export class LandingPageComponent {}
