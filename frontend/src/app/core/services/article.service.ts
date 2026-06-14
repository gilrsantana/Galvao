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
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ArticleService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = `${environment.apiUrl}/api`;

  getPaged(page: number = 1, pageSize: number = 10, onlyPublished: boolean = true): Observable<PagedResponse<ArticleResponse>> {
    return this.http.get<PagedResponse<ArticleResponse>>(
      `${this.apiBase}/articles?page=${page}&pageSize=${pageSize}&onlyPublished=${onlyPublished}`
    );
  }

  getById(id: string): Observable<ArticleResponse> {
    return this.http.get<ArticleResponse>(`${this.apiBase}/articles/${id}`);
  }

  create(request: CreateArticleRequest): Observable<string> {
    return this.http.post<string>(`${this.apiBase}/articles`, request);
  }

  update(id: string, request: UpdateArticleRequest): Observable<void> {
    return this.http.put<void>(`${this.apiBase}/articles/${id}`, request);
  }

  publish(id: string, publish: boolean): Observable<void> {
    const request: PublishArticleRequest = { publish };
    return this.http.put<void>(`${this.apiBase}/articles/${id}/publish`, request);
  }
}
