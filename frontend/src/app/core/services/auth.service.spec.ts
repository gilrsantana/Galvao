import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { HttpClient } from '@angular/common/http';

describe('AuthService', () => {
  let service: AuthService;
  let mockHttpClient: any;

  beforeEach(() => {
    mockHttpClient = {};
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: HttpClient, useValue: mockHttpClient }
      ]
    });
    service = TestBed.inject(AuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should correctly decode UTF-8 characters in JWT payload', () => {
    // header.eyJkaXNwbGF5TmFtZSI6Ikpvw6NvIEpvc8OpIEXDp2EgR29kw6oifQ.signature
    // where payload is {"displayName":"João José Eça Godê"}
    const token = 'header.eyJkaXNwbGF5TmFtZSI6Ikpvw6NvIEpvc8OpIEXDp2EgR29kw6oifQ.signature';
    service.tokenResponse.set({
      accessToken: token,
      refreshToken: 'refresh',
      expiration: '2026-06-16T10:00:00Z'
    });

    const user = service.currentUser();
    expect(user).toBeTruthy();
    expect(user.displayName).toBe('João José Eça Godê');
  });
});
