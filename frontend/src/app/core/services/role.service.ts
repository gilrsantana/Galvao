import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RoleResponse, CreateRoleRequest, AssignRoleRequest } from '../models/role.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = `${environment.apiUrl}/api`;

  createRole(request: CreateRoleRequest): Observable<void> {
    return this.http.post<void>(`${this.apiBase}/roles`, request);
  }

  assignRole(request: AssignRoleRequest): Observable<void> {
    return this.http.post<void>(`${this.apiBase}/roles/assign`, request);
  }

  removeRole(request: AssignRoleRequest): Observable<void> {
    return this.http.post<void>(`${this.apiBase}/roles/remove`, request);
  }

  getUserRoles(userId: string): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiBase}/roles/user/${userId}`);
  }

  getAvailableRoles(): Observable<RoleResponse[]> {
    return this.http.get<RoleResponse[]>(`${this.apiBase}/roles`);
  }
}
