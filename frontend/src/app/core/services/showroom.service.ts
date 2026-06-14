import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResponse } from '../models/shared.models';
import { 
  ShowroomItemResponse, 
  CreateShowroomItemRequest, 
  UpdateShowroomItemRequest, 
  AddShowroomItemPhotoRequest 
} from '../models/showroom.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ShowroomService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = `${environment.apiUrl}/api`;

  getPaged(page: number = 1, pageSize: number = 10): Observable<PagedResponse<ShowroomItemResponse>> {
    return this.http.get<PagedResponse<ShowroomItemResponse>>(
      `${this.apiBase}/showroomitems?page=${page}&pageSize=${pageSize}`
    );
  }

  getById(id: string): Observable<ShowroomItemResponse> {
    return this.http.get<ShowroomItemResponse>(`${this.apiBase}/showroomitems/${id}`);
  }

  create(request: CreateShowroomItemRequest): Observable<string> {
    return this.http.post<string>(`${this.apiBase}/showroomitems`, request);
  }

  update(id: string, request: UpdateShowroomItemRequest): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/showroomitems/${id}`, request);
  }

  addPhoto(itemId: string, request: AddShowroomItemPhotoRequest): Observable<string> {
    return this.http.post<string>(`${this.apiBase}/showroomitems/${itemId}/photos`, request);
  }

  removePhoto(itemId: string, photoId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiBase}/showroomitems/${itemId}/photos/${photoId}`);
  }

  updatePhoto(itemId: string, photoId: string, request: AddShowroomItemPhotoRequest): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/showroomitems/${itemId}/photos/${photoId}`, request);
  }
}
