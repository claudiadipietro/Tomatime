import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  selector: 'app-session-duration',
  imports: [TranslatePipe],
  templateUrl: './session-duration-component.html',
  styleUrl: './session-duration-component.css',
})
export class SessionDurationComponent {
  readonly duration = input.required<number>();
  readonly disabled = input(false);
  readonly durationChange = output<number>();
  readonly blocks = computed(() => Array.from({ length: this.duration() / 25 }, (_, i) => i));
}
