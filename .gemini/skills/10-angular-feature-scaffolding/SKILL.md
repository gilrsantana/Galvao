---
name: angular-feature-scaffolding
description: Scaffold a complete frontend feature slice in Angular 22, including core models, HTTP services using firstValueFrom, components using Signals, lazy routing, and Tailwind v4 themes.
---

# Skill: Scaffolding a Frontend Feature Slice in Angular 22

This skill guides the assistant through creating a unified frontend feature slice.

---

## Steps

### 1. Create Feature Models
- Create `src/app/core/models/{feature}.models.ts`.
- Define type interfaces matching the backend API responses.
  ```typescript
  export interface CategoryResponse {
    id: string;
    name: string;
    description: string;
    createdAt: string;
  }

  export interface CreateCategoryRequest {
    name: string;
    description: string;
  }
  ```

---

### 2. Implement the HTTP Service
- Create `src/app/core/services/{feature}.service.ts`.
- Service must use the `providedIn: 'root'` decorator and property-inject `HttpClient`.
- Standardize on `firstValueFrom` to support async/await patterns in feature components:
  ```typescript
  import { Injectable, inject } from '@angular/core';
  import { HttpClient } from '@angular/common/http';
  import { Observable, firstValueFrom } from 'rxjs';
  import { CategoryResponse, CreateCategoryRequest } from '../models/category.models';
  import { environment } from '../../../environments/environment';

  @Injectable({
    providedIn: 'root'
  })
  export class CategoryService {
    private readonly http = inject(HttpClient);
    private readonly apiBase = `${environment.apiUrl}/api/categories`;

    async getAll(): Promise<CategoryResponse[]> {
      return await firstValueFrom(
        this.http.get<CategoryResponse[]>(this.apiBase)
      );
    }

    async create(request: CreateCategoryRequest): Promise<string> {
      return await firstValueFrom(
        this.http.post<string>(this.apiBase, request)
      );
    }
  }
  ```

---

### 3. Create Feature Components
- Navigate to `src/app/features/{feature}/`.
- Create list and detail components:
  - Folder `features/{feature}/{feature}-list/`
  - Folder `features/{feature}/{feature}-detail/`
- Every component must be standalone.
- Use Signals (`signal`, `computed`) for handling component state:
  ```typescript
  import { Component, OnInit, inject, signal } from '@angular/core';
  import { CategoryService } from '../../../core/services/category.service';
  import { CategoryResponse } from '../../../core/models/category.models';

  @Component({
    selector: 'app-category-list',
    standalone: true,
    imports: [],
    templateUrl: './category-list.component.html',
    styleUrl: './category-list.component.css'
  })
  export class CategoryListComponent implements OnInit {
    private readonly categoryService = inject(CategoryService);
    
    readonly categories = signal<CategoryResponse[]>([]);
    readonly isLoading = signal<boolean>(true);

    async ngOnInit() {
      await this.loadCategories();
    }

    async loadCategories() {
      this.isLoading.set(true);
      try {
        const data = await this.categoryService.getAll();
        this.categories.set(data);
      } catch (err) {
        console.error('Failed to load categories', err);
      } finally {
        this.isLoading.set(false);
      }
    }
  }
  ```

---

### 4. Apply Tailwind CSS v4 Styles
- In component CSS or HTML, compose classes referencing the custom variables.
- Example component CSS:
  ```css
  .container {
    background-color: var(--color-bg-primary);
    color: var(--color-text-primary);
    border: 1px solid var(--border-color);
    transition: var(--transition-smooth);
  }
  ```

---

### 5. Register Lazy Routing
- Open [app.routes.ts](file:///home/gilmar/Development/ai-driven-development/projects/galvao/frontend/src/app/app.routes.ts).
- Add the route utilizing dynamic imports:
  ```typescript
  {
    path: 'categories',
    loadComponent: () => import('./features/category/category-list/category-list.component').then(m => m.CategoryListComponent)
  }
  ```
