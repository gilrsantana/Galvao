import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './confirm-email.component.html',
  styleUrl: './confirm-email.component.css'
})
export class ConfirmEmailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  readonly status = signal<'confirming' | 'success' | 'error' | 'resending' | 'resend-success'>('confirming');
  readonly errorMessage = signal<string>('');
  readonly successMessage = signal<string>('');
  readonly emailToResend = signal<string>('');
  readonly isSubmittingResend = signal<boolean>(false);
  readonly countdown = signal<number>(4);

  ngOnInit() {
    this.route.queryParams.subscribe({
      next: (params) => {
        const userId = params['userId'];
        const token = params['token'];

        if (!userId || !token) {
          this.status.set('error');
          this.errorMessage.set('O link de confirmação parece estar incompleto ou inválido.');
          return;
        }

        this.confirmEmail(userId, token);
      }
    });
  }

  private async confirmEmail(userId: string, token: string) {
    try {
      this.status.set('confirming');
      await this.authService.confirmEmail(userId, token);
      this.status.set('success');
      this.startCountdown();
    } catch (err: any) {
      console.error(err);
      this.status.set('error');
      this.errorMessage.set(
        err?.error?.detail || 
        'Não foi possível confirmar o e-mail. O token pode ter expirado ou ser inválido.'
      );
    }
  }

  private startCountdown() {
    const interval = setInterval(() => {
      this.countdown.update((val) => val - 1);
      if (this.countdown() <= 0) {
        clearInterval(interval);
        this.router.navigate(['/']);
      }
    }, 1000);
  }

  async onResend() {
    if (!this.emailToResend()) {
      return;
    }

    this.isSubmittingResend.set(true);
    try {
      await this.authService.resendConfirmationEmail(this.emailToResend());
      this.status.set('resend-success');
      this.successMessage.set('E-mail de confirmação reenviado com sucesso! Verifique sua caixa de entrada.');
    } catch (err: any) {
      console.error(err);
      this.errorMessage.set(
        err?.error?.detail || 
        'Erro ao reenviar o e-mail de confirmação. Verifique se o e-mail informado está correto.'
      );
    } finally {
      this.isSubmittingResend.set(false);
    }
  }

  resetError() {
    this.status.set('error');
    this.errorMessage.set('');
  }
}
