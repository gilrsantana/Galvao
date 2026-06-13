import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ArticleService } from '../../../core/services/article.service';
import { ArticleResponse } from '../../../core/models/article.models';
import { DatePipe, DOCUMENT } from '@angular/common';
import { Title, Meta } from '@angular/platform-browser';

@Component({
  selector: 'app-article-detail',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './article-detail.component.html',
  styleUrl: './article-detail.component.css'
})
export class ArticleDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly articleService = inject(ArticleService);
  private readonly titleService = inject(Title);
  private readonly metaService = inject(Meta);
  private readonly document = inject(DOCUMENT);

  readonly article = signal<ArticleResponse | null>(null);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.articleService.getById(id).subscribe({
        next: (res) => {
          this.article.set(res);

          // Dynamic SEO metadata
          const truncatedDesc = res.content.length > 150 ? res.content.substring(0, 150) + '...' : res.content;
          this.titleService.setTitle(`${res.title} | Blog Galvão`);
          this.metaService.updateTag({ name: 'description', content: truncatedDesc });

          // Dynamic JSON-LD Schema
          this.removeExistingJsonLd();
          const script = this.document.createElement('script');
          script.type = 'application/ld+json';
          script.id = 'article-jsonld';
          script.text = JSON.stringify({
            '@context': 'https://schema.org',
            '@type': 'Article',
            'headline': res.title,
            'description': truncatedDesc,
            'author': {
              '@type': 'Person',
              'name': res.author
            },
            'datePublished': res.publishedAt || res.createdAt,
            'dateModified': res.updatedAt || res.createdAt
          });
          this.document.head.appendChild(script);
        },
        error: () => console.error('Failed to load article details')
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
    const script = this.document.getElementById('article-jsonld');
    if (script) {
      script.remove();
    }
  }
}
