import { TestBed } from '@angular/core/testing';
import { AdminButtonComponent } from './admin-button.component';

describe('AdminButtonComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminButtonComponent]
    }).compileComponents();
  });

  it('should create the component', () => {
    const fixture = TestBed.createComponent(AdminButtonComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should apply appropriate classes based on variant', () => {
    const fixture = TestBed.createComponent(AdminButtonComponent);
    const component = fixture.componentInstance;
    
    component.variant = 'primary';
    fixture.detectChanges();
    expect(component.getButtonClass()).toContain('btn-primary');
    
    component.variant = 'secondary';
    fixture.detectChanges();
    expect(component.getButtonClass()).toContain('btn-secondary');
  });

  it('should apply appropriate size classes', () => {
    const fixture = TestBed.createComponent(AdminButtonComponent);
    const component = fixture.componentInstance;
    
    component.size = 'sm';
    fixture.detectChanges();
    expect(component.getButtonClass()).toContain('btn-sm');
    
    component.size = 'xs';
    fixture.detectChanges();
    expect(component.getButtonClass()).toContain('btn-sm');
  });

  it('should support fullWidth option', () => {
    const fixture = TestBed.createComponent(AdminButtonComponent);
    const component = fixture.componentInstance;
    
    component.fullWidth = true;
    fixture.detectChanges();
    expect(component.getButtonClass()).toContain('w-full');
  });

  it('should emit btnClick event on button click when not disabled', () => {
    const fixture = TestBed.createComponent(AdminButtonComponent);
    const component = fixture.componentInstance;
    let emitted = false;
    component.btnClick.subscribe(() => emitted = true);

    const buttonEl = fixture.nativeElement.querySelector('button');
    buttonEl.click();

    expect(emitted).toBe(true);
  });
});
