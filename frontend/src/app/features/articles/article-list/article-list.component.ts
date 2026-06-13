import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ArticleService } from '../../../core/services/article.service';
import { ArticleResponse } from '../../../core/models/article.models';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-article-list',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './article-list.component.html',
  styleUrl: './article-list.component.css'
})
export class ArticleListComponent implements OnInit {
  private readonly articleService = inject(ArticleService);

  readonly articles = signal<ArticleResponse[]>([]);
  readonly isLoading = signal<boolean>(true);

  // Pagination
  readonly currentPage = signal<number>(1);
  readonly pageSize = signal<number>(6);
  readonly totalCount = signal<number>(0);
  readonly totalPages = signal<number>(1);

  ngOnInit() {
    this.loadArticles();
  }

  loadArticles() {
    this.isLoading.set(true);
    // Public page fetches only published articles
    this.articleService.getPaged(this.currentPage(), this.pageSize(), true).subscribe({
      next: (res) => {
        this.articles.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
        this.isLoading.set(false);
      },
      error: () => {
        console.error('Failed to load articles');
        this.isLoading.set(false);
      }
    });
  }

  changePage(newPage: number) {
    if (newPage >= 1 && newPage <= this.totalPages()) {
      this.currentPage.set(newPage);
      this.loadArticles();
    }
  }
}
