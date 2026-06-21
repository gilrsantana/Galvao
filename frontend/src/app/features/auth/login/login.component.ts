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
  readonly showResendButton = signal<boolean>(false);
  readonly resendSuccessMessage = signal<string>('');
  readonly resendErrorMessage = signal<string>('');
  readonly isResending = signal<boolean>(false);

  async onSubmit() {
    this.errorMessage.set('');
    this.showResendButton.set(false);
    this.resendSuccessMessage.set('');
    this.resendErrorMessage.set('');
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
      const errorCode = err?.error?.errorCode || err?.error?.extensions?.errorCode;
      if (errorCode === 'Auth.EmailNotConfirmed' || errorCode === 'Auth.PhoneNotConfirmed' || errorCode === 'Auth.AccountNotConfirmed') {
        this.showResendButton.set(true);
      }
      this.errorMessage.set(err?.error?.detail || 'Invalid email or password.');
    } finally {
      this.isSubmitting.set(false);
    }
  }

  async onResendConfirmation() {
    const emailToUse = this.email();
    if (!emailToUse) {
      this.resendErrorMessage.set('Digite o seu e-mail para reenviar a confirmação.');
      return;
    }

    this.resendSuccessMessage.set('');
    this.resendErrorMessage.set('');
    this.isResending.set(true);

    try {
      await this.authService.resendConfirmationEmail(emailToUse);
      this.resendSuccessMessage.set('E-mail de confirmação reenviado com sucesso! Verifique sua caixa de entrada.');
      this.showResendButton.set(false);
    } catch (err: any) {
      console.error(err);
      this.resendErrorMessage.set(
        err?.error?.detail || 
        'Erro ao reenviar o e-mail de confirmação. Por favor, tente novamente.'
      );
    } finally {
      this.isResending.set(false);
    }
  }
}
