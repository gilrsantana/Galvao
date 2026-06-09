import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ArticleService } from '../../../core/services/article.service';
import { ArticleResponse } from '../../../core/models/article.models';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-article-detail',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './article-detail.component.html',
  styleUrl: './article-detail.component.css'
})
export class ArticleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly articleService = inject(ArticleService);

  readonly article = signal<ArticleResponse | null>(null);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.articleService.getById(id).subscribe({
        next: (res) => this.article.set(res),
        error: () => console.error('Failed to load article details')
      });
    }
  }
}
