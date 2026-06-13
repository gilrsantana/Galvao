import { TestBed } from '@angular/core/testing';
import { ShowroomDetailComponent } from './showroom-detail.component';
import { ShowroomService } from '../../../core/services/showroom.service';
import { of } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { Title, Meta } from '@angular/platform-browser';
import { DOCUMENT } from '@angular/common';
import { vi } from 'vitest';

describe('ShowroomDetailComponent', () => {
  let mockShowroomService: any;
  let mockActivatedRoute: any;
  let titleService: Title;
  let metaService: Meta;
  let document: Document;

  beforeEach(async () => {
    mockShowroomService = {
      getById: vi.fn().mockReturnValue(of({
        id: 'item-123',
        title: 'Minimalist Chair',
        description: 'A beautiful minimalist chair.',
        price: 350.00,
        category: 'Chairs',
        photos: [
          { id: 'p1', url: 'chair.jpg', caption: 'Front view', isPrimary: true }
        ]
      }))
    };

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: vi.fn().mockReturnValue('item-123')
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [ShowroomDetailComponent],
      providers: [
        { provide: ShowroomService, useValue: mockShowroomService },
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

    const fixture = TestBed.createComponent(ShowroomDetailComponent);
    fixture.detectChanges(); // triggers ngOnInit

    // Verify Title
    expect(titleSpy).toHaveBeenCalledWith('Minimalist Chair | showroom Galvão');

    // Verify Meta Description
    expect(metaSpy).toHaveBeenCalledWith({
      name: 'description',
      content: 'A beautiful minimalist chair.'
    });

    // Verify JSON-LD in DOM
    const script = document.getElementById('showroom-jsonld') as HTMLScriptElement;
    expect(script).toBeTruthy();
    expect(script.type).toBe('application/ld+json');
    
    const schema = JSON.parse(script.text);
    expect(schema['@type']).toBe('Product');
    expect(schema['name']).toBe('Minimalist Chair');
    expect(schema['description']).toBe('A beautiful minimalist chair.');
    expect(schema['offers']['price']).toBe(350.00);
    expect(schema['offers']['priceCurrency']).toBe('BRL');
  });

  it('should clean up JSON-LD script and reset meta description on destroy', () => {
    const metaSpy = vi.spyOn(metaService, 'updateTag');

    const fixture = TestBed.createComponent(ShowroomDetailComponent);
    fixture.detectChanges(); // triggers ngOnInit

    // Verify script is added
    let script = document.getElementById('showroom-jsonld');
    expect(script).toBeTruthy();

    // Destroy component
    fixture.destroy();

    // Verify script is removed
    script = document.getElementById('showroom-jsonld');
    expect(script).toBeNull();

    // Verify description is reset to default
    expect(metaSpy).toHaveBeenLastCalledWith({
      name: 'description',
      content: 'Galvão - Design de interiores minimalista e mobiliário de alta qualidade. Explore nosso showroom e transforme seus espaços com sofisticação.'
    });
  });
});
