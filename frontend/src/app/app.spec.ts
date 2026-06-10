import { TestBed } from '@angular/core/testing';
import { App } from './app';
import { AuthService } from './core/services/auth.service';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';

describe('App', () => {
  let mockAuthService: any;

  beforeEach(async () => {
    mockAuthService = {
      currentUser: signal<any>(null),
      isAuthenticated: signal<boolean>(false),
      isAdmin: signal<boolean>(false),
      logout: () => {}
    };

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should fallback to email when displayName is not present', () => {
    const fixture = TestBed.createComponent(App);
    mockAuthService.currentUser.set({ email: 'user@galvao.com' });
    fixture.detectChanges();
    
    expect(fixture.componentInstance.userDisplayName()).toBe('user@galvao.com');
  });

  it('should fallback to email when displayName is empty space', () => {
    const fixture = TestBed.createComponent(App);
    mockAuthService.currentUser.set({ email: 'user@galvao.com', displayName: '   ' });
    fixture.detectChanges();
    
    expect(fixture.componentInstance.userDisplayName()).toBe('user@galvao.com');
  });

  it('should use display name when displayName is less than or equal to 30 characters', () => {
    const fixture = TestBed.createComponent(App);
    mockAuthService.currentUser.set({ email: 'user@galvao.com', displayName: 'John Doe' });
    fixture.detectChanges();
    
    expect(fixture.componentInstance.userDisplayName()).toBe('John Doe');
  });

  it('should truncate and append three dots when displayName is greater than 30 characters', () => {
    const fixture = TestBed.createComponent(App);
    mockAuthService.currentUser.set({ 
      email: 'user@galvao.com', 
      displayName: 'This is an example of very big name dropped' 
    });
    fixture.detectChanges();
    
    expect(fixture.componentInstance.userDisplayName()).toBe('This is an example of very ...');
  });
});
