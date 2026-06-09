import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TokenResponse, LoginRequest, RegisterRequest } from '../models/auth.models';
import { API_BASE } from '../constants/api.constants';
import { firstValueFrom } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);

  // Reactive State Signals
  readonly tokenResponse = signal<TokenResponse | null>(null);
  readonly isAuthenticated = computed(() => this.tokenResponse() !== null);
  readonly currentUser = computed(() => {
    const response = this.tokenResponse();
    if (!response) return null;
    return this.decodeJwt(response.accessToken);
  });

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
      this.http.post<TokenResponse>(`${API_BASE}/auth/login`, credentials)
    );
    this.saveTokens(response);
  }

  async register(request: RegisterRequest): Promise<void> {
    await firstValueFrom(
      this.http.post<void>(`${API_BASE}/auth/register`, request)
    );
  }

  async refreshTokens(): Promise<TokenResponse> {
    const currentTokens = this.tokenResponse();
    if (!currentTokens) throw new Error('No refresh token available');

    const response = await firstValueFrom(
      this.http.post<TokenResponse>(`${API_BASE}/auth/refresh`, {
        accessToken: currentTokens.accessToken,
        refreshToken: currentTokens.refreshToken
      })
    );
    this.saveTokens(response);
    return response;
  }

  logout() {
    localStorage.removeItem('galvao_tokens');
    this.tokenResponse.set(null);
  }

  private saveTokens(tokens: TokenResponse) {
    localStorage.setItem('galvao_tokens', JSON.stringify(tokens));
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
