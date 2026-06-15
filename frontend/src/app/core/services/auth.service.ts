import { Injectable, signal, computed, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { TokenResponse, LoginRequest, RegisterRequest } from '../models/auth.models';
import { environment } from '../../../environments/environment';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly apiBase = `${environment.apiUrl}/api`;

  // Reactive State Signals
  readonly tokenResponse = signal<TokenResponse | null>(null);
  readonly isAuthenticated = computed(() => this.tokenResponse() !== null);
  readonly currentUser = computed(() => {
    const response = this.tokenResponse();
    if (!response) return null;
    return this.decodeJwt(response.accessToken);
  });
  readonly currentUserId = computed(() => this.currentUser()?.[ 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier' ] || null);

  readonly userRoles = computed<string[]>(() => {
    const user = this.currentUser();
    if (!user) return [];
    const roles = user['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    if (!roles) return [];
    return Array.isArray(roles) ? roles : [roles];
  });

  readonly isAdmin = computed(() => this.userRoles().includes('Admin'));

  constructor() {
    this.loadTokensFromStorage();
  }

  private loadTokensFromStorage() {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }
    const tokenStr = localStorage.getItem('galvao_tokens');
    if (tokenStr) {
      try {
        const tokens = JSON.parse(tokenStr) as TokenResponse;
        // Check if token is expired, if yes, we can refresh it or log out.
        // For simplicity, we just load them now and let interceptor refresh on 401.
        this.tokenResponse.set(tokens);
      } catch {
        this.logout();
      }
    }
  }

  async login(credentials: LoginRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<TokenResponse>(`${this.apiBase}/auth/login`, credentials)
    );
    this.saveTokens(response);
  }

  async register(request: RegisterRequest): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${this.apiBase}/auth/register`, request)
    );
  }

  async confirmEmail(userId: string, token: string): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${this.apiBase}/auth/confirm-email`, { userId, token })
    );
  }

  async resendConfirmationEmail(email: string): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${this.apiBase}/auth/resend-confirmation-email`, { email })
    );
  }

  async refreshTokens(): Promise<TokenResponse> {
    const currentTokens = this.tokenResponse();
    if (!currentTokens) throw new Error('No refresh token available');

    const response = await firstValueFrom(
      this.http.post<TokenResponse>(`${this.apiBase}/auth/refresh`, {
        accessToken: currentTokens.accessToken,
        refreshToken: currentTokens.refreshToken
      })
    );
    this.saveTokens(response);
    return response;
  }

  logout() {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('galvao_tokens');
    }
    this.tokenResponse.set(null);
  }

  private saveTokens(tokens: TokenResponse) {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('galvao_tokens', JSON.stringify(tokens));
    }
    this.tokenResponse.set(tokens);
  }

  private decodeJwt(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      const payload = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(payload);
    } catch {
      return null;
    }
  }
}
