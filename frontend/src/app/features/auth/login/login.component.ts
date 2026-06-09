import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly email = signal<string>('');
  readonly password = signal<string>('');
  readonly errorMessage = signal<string>('');
  readonly isSubmitting = signal<boolean>(false);

  async onSubmit() {
    this.errorMessage.set('');
    this.isSubmitting.set(true);

    try {
      await this.authService.login({
        email: this.email(),
        password: this.password()
      });
      // Redirect to Admin dashboard or Home
      this.router.navigate([this.authService.isAdmin() ? '/admin' : '/']);
    } catch (err: any) {
      console.error(err);
      this.errorMessage.set(err?.error?.detail || 'Invalid email or password.');
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
