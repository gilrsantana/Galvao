import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResponse } from '../models/shared.models';
import { 
  ArticleResponse, 
  CreateArticleRequest, 
  UpdateArticleRequest, 
  PublishArticleRequest 
} from '../models/article.models';
import { API_BASE } from '../constants/api.constants';

@Injectable({
  providedIn: 'root'
})
export class ArticleService {
  private readonly http = inject(HttpClient);

  getPaged(page: number = 1, pageSize: number = 10, onlyPublished: boolean = true): Observable<PagedResponse<ArticleResponse>> {
    return this.http.get<PagedResponse<ArticleResponse>>(
      `${API_BASE}/articles?page=${page}&pageSize=${pageSize}&onlyPublished=${onlyPublished}`
    );
  }

  getById(id: string): Observable<ArticleResponse> {
    return this.http.get<ArticleResponse>(`${API_BASE}/articles/${id}`);
  }

  create(request: CreateArticleRequest): Observable<string> {
    return this.http.post<string>(`${API_BASE}/articles`, request);
  }

  update(id: string, request: UpdateArticleRequest): Observable<void> {
    return this.http.put<void>(`${API_BASE}/articles/${id}`, request);
  }

  publish(id: string, publish: boolean): Observable<void> {
    const request: PublishArticleRequest = { publish };
    return this.http.put<void>(`${API_BASE}/articles/${id}/publish`, request);
  }
}
