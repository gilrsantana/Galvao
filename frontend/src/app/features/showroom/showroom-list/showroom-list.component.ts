import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShowroomService } from '../../../core/services/showroom.service';
import { ShowroomItemResponse } from '../../../core/models/showroom.models';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-showroom-list',
  standalone: true,
  imports: [RouterLink, CurrencyPipe],
  templateUrl: './showroom-list.component.html',
  styleUrl: './showroom-list.component.css'
})
export class ShowroomListComponent implements OnInit {
  private readonly showroomService = inject(ShowroomService);

  readonly items = signal<ShowroomItemResponse[]>([]);
  readonly filteredItems = signal<ShowroomItemResponse[]>([]);
  readonly categories = signal<string[]>(['All']);
  readonly selectedCategory = signal<string>('All');
  readonly isLoading = signal<boolean>(true);

  // Pagination signals
  readonly currentPage = signal<number>(1);
  readonly pageSize = signal<number>(9);
  readonly totalCount = signal<number>(0);
  readonly totalPages = signal<number>(1);

  ngOnInit() {
    this.loadItems();
  }

  loadItems() {
    this.isLoading.set(true);
    this.showroomService.getPaged(this.currentPage(), this.pageSize()).subscribe({
      next: (res) => {
        this.items.set(res.items);
        this.totalCount.set(res.totalCount);
        this.totalPages.set(res.totalPages);
        
        // Extract unique categories dynamically
        const uniqueCategories = new Set(res.items.map(item => item.category));
        this.categories.set(['All', ...Array.from(uniqueCategories)]);

        this.applyFilter();
        this.isLoading.set(false);
      },
      error: () => {
        console.error('Failed to load showroom items');
        this.isLoading.set(false);
      }
    });
  }

  selectCategory(category: string) {
    this.selectedCategory.set(category);
    this.applyFilter();
  }

  private applyFilter() {
    const category = this.selectedCategory();
    if (category === 'All') {
      this.filteredItems.set(this.items());
    } else {
      this.filteredItems.set(this.items().filter(item => item.category === category));
    }
  }

  changePage(newPage: number) {
    if (newPage >= 1 && newPage <= this.totalPages()) {
      this.currentPage.set(newPage);
      this.loadItems();
    }
  }

  getPrimaryPhotoUrl(item: ShowroomItemResponse): string {
    const primary = item.photos.find(p => p.isPrimary);
    if (primary) return primary.url;
    if (item.photos.length > 0) return item.photos[0].url;
    return 'assets/placeholder.jpg';
  }
}
