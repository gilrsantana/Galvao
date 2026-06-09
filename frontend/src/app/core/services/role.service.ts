import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RoleResponse, CreateRoleRequest, AssignRoleRequest } from '../models/role.models';
import { API_BASE } from '../constants/api.constants';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  private readonly http = inject(HttpClient);

  createRole(request: CreateRoleRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE}/roles`, request);
  }

  assignRole(request: AssignRoleRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE}/roles/assign`, request);
  }

  removeRole(request: AssignRoleRequest): Observable<void> {
    return this.http.post<void>(`${API_BASE}/roles/remove`, request);
  }

  getUserRoles(userId: string): Observable<string[]> {
    return this.http.get<string[]>(`${API_BASE}/roles/user/${userId}`);
  }

  getAvailableRoles(): Observable<RoleResponse[]> {
    return this.http.get<RoleResponse[]>(`${API_BASE}/roles`);
  }
}
