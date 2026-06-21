import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ArticleService } from '../../../core/services/article.service';
import { ArticleResponse } from '../../../core/models/article.models';
import { DatePipe, DOCUMENT } from '@angular/common';
import { Title, Meta, DomSanitizer, SafeHtml } from '@angular/platform-browser';

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
  private readonly sanitizer = inject(DomSanitizer);

  readonly article = signal<ArticleResponse | null>(null);
  readonly renderedContent = signal<SafeHtml>('');

  ngOnInit() {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (slug) {
      this.articleService.getBySlug(slug).subscribe({
        next: (res) => {
          this.article.set(res);
          const parsed = this.parseMarkdown(res.content);
          this.renderedContent.set(this.sanitizer.bypassSecurityTrustHtml(parsed));

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

  private parseMarkdown(markdown: string): string {
    if (!markdown) return '<em>Nenhum conteúdo para visualizar.</em>';

    let html = markdown;

    html = html.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

    html = html.replace(/!\[([^\]]*)\]\(([^)]+)\)/g, '<img src="$2" alt="$1" class="md-img" />');

    html = html.replace(/```(\w*)\n([\s\S]*?)\n```/g, (match, lang, code) => {
      return `<pre class="code-block language-${lang}"><code>${code.trim()}</code></pre>`;
    });

    html = html.replace(/`([^`]+)`/g, '<code class="inline-code">$1</code>');

    html = html.replace(/^# (.*?)$/gm, '<h1 class="md-h1">$1</h1>');
    html = html.replace(/^## (.*?)$/gm, '<h2 class="md-h2">$1</h2>');
    html = html.replace(/^### (.*?)$/gm, '<h3 class="md-h3">$1</h3>');

    html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');

    html = html.replace(/^-\s+(.*?)$/gm, '<li>$1</li>');
    html = html.replace(/(<li>.*<\/li>)/g, '<ul>$1</ul>');
    html = html.replace(/<\/ul>\s*<ul>/g, '');

    const blocks = html.split(/\n\n+/);
    html = blocks
      .map((block) => {
        const trimmed = block.trim();
        if (!trimmed) return '';
        if (
          trimmed.startsWith('<h') ||
          trimmed.startsWith('<pre') ||
          trimmed.startsWith('<ul') ||
          trimmed.startsWith('<li') ||
          trimmed.startsWith('<img') ||
          trimmed.startsWith('<p>')
        ) {
          return trimmed;
        }
        return `<p class="md-p">${trimmed.replace(/\n/g, '<br/>')}</p>`;
      })
      .join('\n');

    return html;
  }
}
