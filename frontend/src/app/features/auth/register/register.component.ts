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
  readonly acceptNews = signal<boolean>(false);
  readonly acceptPromo = signal<boolean>(false);
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
        lastName: this.lastName(),
        acceptNews: this.acceptNews(),
        acceptPromo: this.acceptPromo()
      });

      // Redirect to welcome page to inform the user to check their email for account confirmation
      this.router.navigate(['/welcome'], { queryParams: { email: this.email() } });
    } catch (err: any) {
      console.error(err);
      this.errorMessage.set(err?.error?.detail || 'Registration failed. Make sure details are valid.');
    } finally {
      this.isSubmitting.set(false);
    }
  }
}
