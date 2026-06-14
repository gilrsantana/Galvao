import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  MemberResponse,
  UpdateProfileRequest,
  ChangeEmailRequest,
  ChangePasswordRequest,
  UpdateMarketingPreferencesRequest
} from '../models/member.models';

@Injectable({
  providedIn: 'root'
})
export class MemberService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = `${environment.apiUrl}/api`;

  getById(id: string): Observable<MemberResponse> {
    return this.http.get<MemberResponse>(`${this.apiBase}/members/${id}`);
  }

  updateProfile(id: string, request: UpdateProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/members/${id}/profile`, request);
  }

  changeEmail(id: string, request: ChangeEmailRequest): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/members/${id}/email`, request);
  }

  changePassword(id: string, request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/members/${id}/password`, request);
  }

  updatePreferences(id: string, request: UpdateMarketingPreferencesRequest): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/members/${id}/preferences`, request);
  }

  purgeUser(id: string, password: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/members/${id}`, { body: { password } });
  }
}
