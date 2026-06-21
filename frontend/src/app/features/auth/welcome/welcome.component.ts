import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-welcome',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './welcome.component.html',
  styleUrl: './welcome.component.css'
})
export class WelcomeComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  readonly email = signal<string>('');
  readonly isSubmittingResend = signal<boolean>(false);
  readonly successMessage = signal<string>('');
  readonly errorMessage = signal<string>('');

  ngOnInit() {
    this.route.queryParams.subscribe({
      next: (params) => {
        this.email.set(params['email'] || '');
      }
    });
  }

  async onResend() {
    const emailToUse = this.email();
    if (!emailToUse) {
      this.errorMessage.set('Nenhum endereço de e-mail disponível para reenvio.');
      return;
    }

    this.successMessage.set('');
    this.errorMessage.set('');
    this.isSubmittingResend.set(true);

    try {
      await this.authService.resendConfirmationEmail(emailToUse);
      this.successMessage.set('E-mail de confirmação reenviado com sucesso! Verifique sua caixa de entrada.');
    } catch (err: any) {
      console.error(err);
      this.errorMessage.set(
        err?.error?.detail || 
        'Erro ao reenviar o e-mail de confirmação. Por favor, tente novamente.'
      );
    } finally {
      this.isSubmittingResend.set(false);
    }
  }
}
