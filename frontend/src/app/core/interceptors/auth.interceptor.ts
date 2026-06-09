import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { catchError, switchMap, throwError, from } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const tokenResponse = authService.tokenResponse();

  let authReq = req;
  if (tokenResponse) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${tokenResponse.accessToken}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        // Attempt to refresh tokens asynchronously using RxJS 'from' wrapper
        return from(authService.refreshTokens()).pipe(
          switchMap((newTokens) => {
            const retryReq = req.clone({
              setHeaders: {
                Authorization: `Bearer ${newTokens.accessToken}`
              }
            });
            return next(retryReq);
          }),
          catchError((refreshError) => {
            authService.logout();
            return throwError(() => refreshError);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
