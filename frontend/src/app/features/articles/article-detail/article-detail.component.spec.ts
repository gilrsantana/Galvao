import { TestBed } from '@angular/core/testing';
import { ArticleDetailComponent } from './article-detail.component';
import { ArticleService } from '../../../core/services/article.service';
import { of } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { Title, Meta } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { vi } from 'vitest';

describe('ArticleDetailComponent', () => {
  let mockArticleService: any;
  let mockActivatedRoute: any;
  let titleService: Title;
  let metaService: Meta;
  let document: Document;

  beforeEach(async () => {
    mockArticleService = {
      getById: vi.fn().mockReturnValue(of({
        id: 'article-123',
        title: 'Designing with Wood',
        content: 'Wood is a timeless material that brings warmth and character to any minimal design space. We will discuss best practices.',
        author: 'Jane Doe',
        isPublished: true,
        publishedAt: '2026-06-12T10:00:00Z',
        createdAt: '2026-06-12T09:00:00Z',
        updatedAt: '2026-06-12T10:00:00Z'
      }))
    };

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: vi.fn().mockReturnValue('article-123')
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [ArticleDetailComponent],
      providers: [
        { provide: ArticleService, useValue: mockArticleService },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    titleService = TestBed.inject(Title);
    metaService = TestBed.inject(Meta);
    document = TestBed.inject(DOCUMENT);
  });

  it('should create and dynamically set title, meta description, and JSON-LD schema', () => {
    const titleSpy = vi.spyOn(titleService, 'setTitle');
    const metaSpy = vi.spyOn(metaService, 'updateTag');

    const fixture = TestBed.createComponent(ArticleDetailComponent);
    fixture.detectChanges(); // triggers ngOnInit

    // Verify Title
    expect(titleSpy).toHaveBeenCalledWith('Designing with Wood | Blog Galvão');

    // Verify Meta Description
    expect(metaSpy).toHaveBeenCalledWith({
      name: 'description',
      content: 'Wood is a timeless material that brings warmth and character to any minimal design space. We will discuss best practices.'
    });

    // Verify JSON-LD in DOM
    const script = document.getElementById('article-jsonld') as HTMLScriptElement;
    expect(script).toBeTruthy();
    expect(script.type).toBe('application/ld+json');
    
    const schema = JSON.parse(script.text);
    expect(schema['@type']).toBe('Article');
    expect(schema['headline']).toBe('Designing with Wood');
    expect(schema['description']).toBe('Wood is a timeless material that brings warmth and character to any minimal design space. We will discuss best practices.');
    expect(schema['author']['name']).toBe('Jane Doe');
    expect(schema['datePublished']).toBe('2026-06-12T10:00:00Z');
    expect(schema['dateModified']).toBe('2026-06-12T10:00:00Z');
  });

  it('should cleanly remove JSON-LD script and reset meta description on destroy', () => {
    const metaSpy = vi.spyOn(metaService, 'updateTag');

    const fixture = TestBed.createComponent(ArticleDetailComponent);
    fixture.detectChanges(); // triggers ngOnInit

    // Verify script is added
    let script = document.getElementById('article-jsonld');
    expect(script).toBeTruthy();

    // Destroy component
    fixture.destroy();

    // Verify script is removed
    script = document.getElementById('article-jsonld');
    expect(script).toBeNull();

    // Verify description is reset to default
    expect(metaSpy).toHaveBeenLastCalledWith({
      name: 'description',
      content: 'Galvão - Design de interiores minimalista e mobiliário de alta qualidade. Explore nosso showroom e transforme seus espaços com sofisticação.'
    });
  });

  it('should truncate the meta description if the article content exceeds 150 characters', () => {
    const metaSpy = vi.spyOn(metaService, 'updateTag');
    const longContent = 'a'.repeat(200);

    mockArticleService.getById.mockReturnValue(of({
      id: 'article-123',
      title: 'Designing with Wood',
      content: longContent,
      author: 'Jane Doe',
      createdAt: '2026-06-12T09:00:00Z'
    }));

    const fixture = TestBed.createComponent(ArticleDetailComponent);
    fixture.detectChanges(); // triggers ngOnInit

    expect(metaSpy).toHaveBeenCalledWith({
      name: 'description',
      content: 'a'.repeat(150) + '...'
    });
  });
});
