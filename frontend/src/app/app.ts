import { Component, inject, computed } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  readonly authService = inject(AuthService);

  readonly userDisplayName = computed(() => {
    const user = this.authService.currentUser();
    if (!user) return '';

    const name = user.displayName;
    if (!name || !name.trim()) {
      return user.email || '';
    }

    if (name.length > 30) {
      return name.substring(0, 27) + '...';
    }

    return name;
  });

  logout() {
    this.authService.logout();
  }
}
