import { TestBed } from '@angular/core/testing';
import { SettingsComponent } from './settings.component';
import { AuthService } from '../../core/services/auth.service';
import { MemberService } from '../../core/services/member.service';
import { of } from 'rxjs';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';

describe('SettingsComponent', () => {
  let mockAuthService: any;
  let mockMemberService: any;

  beforeEach(async () => {
    mockAuthService = {
      currentUserId: () => 'user-123',
      refreshTokens: vi.fn().mockImplementation(() => Promise.resolve({})),
      logout: vi.fn().mockImplementation(() => undefined)
    };

    mockMemberService = {
      getById: vi.fn().mockReturnValue(of({
        id: 'user-123',
        email: 'user@galvao.com',
        displayName: 'John Doe',
        firstName: 'John',
        lastName: 'Doe',
        acceptNews: true,
        acceptPromo: false
      })),
      updateProfile: vi.fn().mockReturnValue(of(undefined)),
      changeEmail: vi.fn().mockReturnValue(of(undefined)),
      changePassword: vi.fn().mockReturnValue(of(undefined)),
      updatePreferences: vi.fn().mockReturnValue(of(undefined)),
      purgeUser: vi.fn().mockReturnValue(of(undefined))
    };

    await TestBed.configureTestingModule({
      imports: [SettingsComponent],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
        { provide: MemberService, useValue: mockMemberService }
      ]
    }).compileComponents();
  });

  it('should create settings component', () => {
    const fixture = TestBed.createComponent(SettingsComponent);
    const component = fixture.componentInstance;
    expect(component).toBeTruthy();
  });

  it('should load member data on init', async () => {
    const fixture = TestBed.createComponent(SettingsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    expect(mockMemberService.getById).toHaveBeenCalledWith('user-123');
    expect(component.displayName()).toBe('John Doe');
    expect(component.firstName()).toBe('John');
    expect(component.lastName()).toBe('Doe');
    expect(component.newEmail()).toBe('user@galvao.com');
    expect(component.acceptNews()).toBe(true);
    expect(component.acceptPromo()).toBe(false);
  });

  it('should call updateProfile when profile form is submitted', async () => {
    const fixture = TestBed.createComponent(SettingsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    component.displayName.set('Updated Name');
    await component.onUpdateProfile();

    expect(mockMemberService.updateProfile).toHaveBeenCalledWith('user-123', {
      displayName: 'Updated Name',
      firstName: 'John',
      lastName: 'Doe'
    });
    expect(mockAuthService.refreshTokens).toHaveBeenCalled();
  });

  it('should manage purge flow states and call purgeUser on confirm', async () => {
    const fixture = TestBed.createComponent(SettingsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    expect(component.purgeStage()).toBe(0);

    component.onStartPurge();
    expect(component.purgeStage()).toBe(1);

    component.onConfirmPurgeWarning();
    expect(component.purgeStage()).toBe(2);

    component.purgePassword.set('secret-pass');
    await component.onPurgeUser();

    expect(mockMemberService.purgeUser).toHaveBeenCalledWith('user-123', 'secret-pass');
    expect(mockAuthService.logout).toHaveBeenCalled();
    expect(component.purgeStage()).toBe(0);
    expect(navigateSpy).toHaveBeenCalledWith(['/']);
  });

  it('should call updatePreferences with news, promo, consent token and timestamp when saving preferences', async () => {
    const fixture = TestBed.createComponent(SettingsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();

    component.acceptNews.set(true);
    component.acceptPromo.set(false);

    await component.onUpdatePreferences();

    expect(mockMemberService.updatePreferences).toHaveBeenCalledWith('user-123', expect.objectContaining({
      acceptNews: true,
      acceptPromo: false,
      consentToken: expect.any(String),
      consentedAt: expect.any(String)
    }));
  });
});
