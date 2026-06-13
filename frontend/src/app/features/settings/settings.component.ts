import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { MemberService } from '../../core/services/member.service';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly memberService = inject(MemberService);
  private readonly router = inject(Router);

  // States
  readonly displayName = signal<string>('');
  readonly firstName = signal<string>('');
  readonly lastName = signal<string>('');
  
  readonly newEmail = signal<string>('');

  readonly currentPassword = signal<string>('');
  readonly newPassword = signal<string>('');
  readonly confirmPassword = signal<string>('');

  readonly acceptNews = signal<boolean>(false);
  readonly acceptPromo = signal<boolean>(false);

  // Statuses
  readonly isLoading = signal<boolean>(true);
  readonly profileMessage = signal<string>('');
  readonly profileError = signal<string>('');
  readonly emailMessage = signal<string>('');
  readonly emailError = signal<string>('');
  readonly passwordMessage = signal<string>('');
  readonly passwordError = signal<string>('');
  readonly preferencesMessage = signal<string>('');
  readonly preferencesError = signal<string>('');

  readonly isSavingProfile = signal<boolean>(false);
  readonly isSavingEmail = signal<boolean>(false);
  readonly isSavingPassword = signal<boolean>(false);
  readonly isSavingPreferences = signal<boolean>(false);

  ngOnInit() {
    this.loadMemberData();
  }

  private async loadMemberData() {
    const userId = this.authService.currentUserId();
    if (!userId) {
      this.router.navigate(['/login']);
      return;
    }

    try {
      const member = await firstValueFrom(this.memberService.getById(userId));
      this.displayName.set(member.displayName);
      this.firstName.set(member.firstName);
      this.lastName.set(member.lastName);
      this.newEmail.set(member.email);
      this.acceptNews.set(member.acceptNews);
      this.acceptPromo.set(member.acceptPromo);
    } catch (err) {
      console.error(err);
      this.profileError.set('Erro ao carregar dados do perfil.');
    } finally {
      this.isLoading.set(false);
    }
  }

  async onUpdateProfile() {
    const userId = this.authService.currentUserId();
    if (!userId) return;

    if (!this.displayName().trim() || !this.firstName().trim() || !this.lastName().trim()) {
      this.profileError.set('Todos os campos do perfil são obrigatórios.');
      return;
    }

    this.profileMessage.set('');
    this.profileError.set('');
    this.isSavingProfile.set(true);

    try {
      await firstValueFrom(this.memberService.updateProfile(userId, {
        displayName: this.displayName(),
        firstName: this.firstName(),
        lastName: this.lastName()
      }));

      this.profileMessage.set('Perfil atualizado com sucesso!');
      
      // Refresh token to update navbar name
      await this.authService.refreshTokens();
    } catch (err: any) {
      console.error(err);
      this.profileError.set(err?.error?.detail || 'Erro ao atualizar perfil.');
    } finally {
      this.isSavingProfile.set(false);
    }
  }

  async onChangeEmail() {
    const userId = this.authService.currentUserId();
    if (!userId) return;

    if (!this.newEmail() || !this.newEmail().includes('@')) {
      this.emailError.set('Por favor, insira um e-mail válido.');
      return;
    }

    this.emailMessage.set('');
    this.emailError.set('');
    this.isSavingEmail.set(true);

    try {
      await firstValueFrom(this.memberService.changeEmail(userId, {
        newEmail: this.newEmail()
      }));

      this.emailMessage.set('E-mail alterado com sucesso!');

      // Refresh token to update navbar email
      await this.authService.refreshTokens();
    } catch (err: any) {
      console.error(err);
      this.emailError.set(err?.error?.detail || 'Erro ao alterar e-mail.');
    } finally {
      this.isSavingEmail.set(false);
    }
  }

  async onChangePassword() {
    const userId = this.authService.currentUserId();
    if (!userId) return;

    if (!this.currentPassword() || !this.newPassword()) {
      this.passwordError.set('Por favor, preencha todos os campos de senha.');
      return;
    }

    if (this.newPassword() !== this.confirmPassword()) {
      this.passwordError.set('As senhas não coincidem.');
      return;
    }

    if (this.newPassword().length < 6) {
      this.passwordError.set('A nova senha deve ter pelo menos 6 caracteres.');
      return;
    }

    this.passwordMessage.set('');
    this.passwordError.set('');
    this.isSavingPassword.set(true);

    try {
      await firstValueFrom(this.memberService.changePassword(userId, {
        currentPassword: this.currentPassword(),
        newPassword: this.newPassword()
      }));

      this.passwordMessage.set('Senha alterada com sucesso!');
      this.currentPassword.set('');
      this.newPassword.set('');
      this.confirmPassword.set('');
    } catch (err: any) {
      console.error(err);
      this.passwordError.set(err?.error?.detail || 'Erro ao alterar senha. Verifique sua senha atual.');
    } finally {
      this.isSavingPassword.set(false);
    }
  }

  async onUpdatePreferences() {
    const userId = this.authService.currentUserId();
    if (!userId) return;

    this.preferencesMessage.set('');
    this.preferencesError.set('');
    this.isSavingPreferences.set(true);

    try {
      const consentToken = typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'consent_token_' + Math.random().toString(36).substring(2, 15);
      const consentedAt = new Date().toISOString();

      await firstValueFrom(this.memberService.updatePreferences(userId, {
        acceptNews: this.acceptNews(),
        acceptPromo: this.acceptPromo(),
        consentToken,
        consentedAt
      }));

      this.preferencesMessage.set('Preferências de marketing atualizadas!');
    } catch (err: any) {
      console.error(err);
      this.preferencesError.set(err?.error?.detail || 'Erro ao atualizar preferências.');
    } finally {
      this.isSavingPreferences.set(false);
    }
  }

  // Purge User features
  readonly purgePassword = signal<string>('');
  readonly purgeStage = signal<number>(0);
  readonly purgeError = signal<string>('');
  readonly isPurging = signal<boolean>(false);

  onStartPurge() {
    this.purgeStage.set(1);
    this.purgeError.set('');
    this.purgePassword.set('');
  }

  onConfirmPurgeWarning() {
    this.purgeStage.set(2);
  }

  onCancelPurge() {
    this.purgeStage.set(0);
    this.purgePassword.set('');
    this.purgeError.set('');
  }

  async onPurgeUser() {
    const userId = this.authService.currentUserId();
    if (!userId) return;

    if (!this.purgePassword()) {
      this.purgeError.set('A senha é obrigatória.');
      return;
    }

    this.purgeError.set('');
    this.isPurging.set(true);

    try {
      await firstValueFrom(this.memberService.purgeUser(userId, this.purgePassword()));
      // Reset state and logout
      this.authService.logout();
      this.purgeStage.set(0);
      this.purgePassword.set('');
      
      // Redirect to home page
      this.router.navigate(['/']);
    } catch (err: any) {
      console.error(err);
      this.purgeError.set(err?.error?.detail || 'Erro ao excluir conta. Verifique sua senha.');
    } finally {
      this.isPurging.set(false);
    }
  }
}
