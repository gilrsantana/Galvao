import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShowroomService } from '../../core/services/showroom.service';
import { ArticleService } from '../../core/services/article.service';
import { ShowroomItemResponse } from '../../core/models/showroom.models';
import { ArticleResponse } from '../../core/models/article.models';
import { CurrencyPipe, DatePipe } from '@angular/common';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, CurrencyPipe, DatePipe],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  private readonly showroomService = inject(ShowroomService);
  private readonly articleService = inject(ArticleService);

  readonly featuredItems = signal<ShowroomItemResponse[]>([]);
  readonly latestArticles = signal<ArticleResponse[]>([]);
  readonly isLoading = signal<boolean>(true);

  ngOnInit() {
    let completedCount = 0;
    const checkCompletion = () => {
      completedCount++;
      if (completedCount >= 2) {
        this.isLoading.set(false);
      }
    };

    this.showroomService.getPaged(1, 3).subscribe({
      next: (res) => {
        this.featuredItems.set(res.items);
        checkCompletion();
      },
      error: () => {
        console.error('Failed to load featured showroom items');
        checkCompletion();
      }
    });

    this.articleService.getPaged(1, 3, true).subscribe({
      next: (res) => {
        this.latestArticles.set(res.items);
        checkCompletion();
      },
      error: () => {
        console.error('Failed to load latest decoration articles');
        checkCompletion();
      }
    });
  }

  getPrimaryPhotoUrl(item: ShowroomItemResponse): string {
    const primary = item.photos.find(p => p.isPrimary);
    if (primary) return primary.url;
    if (item.photos.length > 0) return item.photos[0].url;
    return 'assets/placeholder.jpg'; // default placeholder
  }
}
