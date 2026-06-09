import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.css'
})
export class RegisterComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly email = signal<string>('');
  readonly password = signal<string>('');
  readonly displayName = signal<string>('');
  readonly firstName = signal<string>('');
  readonly lastName = signal<string>('');
  readonly errorMessage = signal<string>('');
  readonly isSubmitting = signal<boolean>(false);

  async onSubmit() {
    this.errorMessage.set('');
    this.isSubmitting.set(true);

    try {
      // 1. Register Account and Member Profile
      await this.authService.register({
        email: this.email(),
        password: this.password(),
        displayName: this.displayName(),
        firstName: this.firstName(),
        lastName: this.lastName()
      });

      // 2. Perform Automatic Login for a seamless user experience
      await this.authService.login({
        email: this.email(),
        password: this.password()
      });

      this.router.navigate(['/']);
    } catch (err: any) {
      console.error(err);
      this.errorMessage.set(err?.error?.detail || 'Registration failed. Make sure details are valid.');
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
