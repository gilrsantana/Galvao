import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ShowroomService } from '../../../core/services/showroom.service';
import { ShowroomItemResponse, ShowroomItemPhotoResponse } from '../../../core/models/showroom.models';
import { CurrencyPipe, DOCUMENT } from '@angular/common';
import { Title, Meta } from '@angular/platform-browser';

@Component({
  selector: 'app-showroom-detail',
  standalone: true,
  imports: [RouterLink, CurrencyPipe],
  templateUrl: './showroom-detail.component.html',
  styleUrl: './showroom-detail.component.css'
})
export class ShowroomDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly showroomService = inject(ShowroomService);
  private readonly titleService = inject(Title);
  private readonly metaService = inject(Meta);
  private readonly document = inject(DOCUMENT);

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

          // Dynamic SEO metadata
          this.titleService.setTitle(`${res.title} | showroom Galvão`);
          this.metaService.updateTag({ name: 'description', content: res.description });

          // Dynamic JSON-LD Schema
          this.removeExistingJsonLd();
          const script = this.document.createElement('script');
          script.type = 'application/ld+json';
          script.id = 'showroom-jsonld';
          script.text = JSON.stringify({
            '@context': 'https://schema.org',
            '@type': 'Product',
            'name': res.title,
            'description': res.description,
            'category': res.category,
            'offers': {
              '@type': 'Offer',
              'price': res.price,
              'priceCurrency': 'BRL',
              'availability': 'https://schema.org/InStock'
            }
          });
          this.document.head.appendChild(script);
        },
        error: () => console.error('Failed to load item details')
      });
    }
  }

  ngOnDestroy() {
    this.removeExistingJsonLd();
    // Reset to default meta description
    this.metaService.updateTag({
      name: 'description',
      content: 'Galvão - Design de interiores minimalista e mobiliário de alta qualidade. Explore nosso showroom e transforme seus espaços com sofisticação.'
    });
  }

  private removeExistingJsonLd() {
    const script = this.document.getElementById('showroom-jsonld');
    if (script) {
      script.remove();
    }
  }

  selectPhoto(photo: ShowroomItemPhotoResponse) {
    this.selectedPhotoUrl.set(photo.url);
    this.selectedPhotoCaption.set(photo.caption);
  }
}
