import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ShowroomService } from '../../../core/services/showroom.service';
import { ShowroomItemResponse, ShowroomItemPhotoResponse } from '../../../core/models/showroom.models';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-showroom-detail',
  standalone: true,
  imports: [RouterLink, CurrencyPipe],
  templateUrl: './showroom-detail.component.html',
  styleUrl: './showroom-detail.component.css'
})
export class ShowroomDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly showroomService = inject(ShowroomService);

  readonly item = signal<ShowroomItemResponse | null>(null);
  readonly selectedPhotoUrl = signal<string>('');
  readonly selectedPhotoCaption = signal<string>('');

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.showroomService.getById(id).subscribe({
        next: (res) => {
          this.item.set(res);
          const primary = res.photos.find(p => p.isPrimary) || res.photos[0];
          if (primary) {
            this.selectedPhotoUrl.set(primary.url);
            this.selectedPhotoCaption.set(primary.caption);
          }
        },
        error: () => console.error('Failed to load item details')
      });
    }
  }

  selectPhoto(photo: ShowroomItemPhotoResponse) {
    this.selectedPhotoUrl.set(photo.url);
    this.selectedPhotoCaption.set(photo.caption);
  }
}
