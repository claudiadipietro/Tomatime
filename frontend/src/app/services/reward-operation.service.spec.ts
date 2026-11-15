import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AuthSessionService } from './auth-session.service';
import { RewardOperationService } from './reward-operation.service';

describe('RewardOperationService', () => {
  let service: RewardOperationService;

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    TestBed.configureTestingModule({});
    TestBed.inject(AuthSessionService).save({
      userId: 7, name: 'Jane', email: 'jane@example.com', accessToken: 'token',
      expiresAt: new Date(Date.now() + 60_000).toISOString(),
    });
    service = TestBed.inject(RewardOperationService);
  });

  afterEach(() => {
    sessionStorage.clear();
    localStorage.clear();
  });

  it('reuses an uncertain purchase id until it is confirmed', () => {
    let first = '';
    service.run('extra-break', (id) => {
      first = id;
      return throwError(() => new HttpErrorResponse({ status: 0 }));
    }).subscribe({ error: () => {} });
    expect(service.hasPending('extra-break')).toBe(true);
    service.run('extra-break', (id) => {
      expect(id).toBe(first);
      return of(true);
    }).subscribe();
    expect(service.hasPending('extra-break')).toBe(false);
    service.run('extra-break', (id) => {
      expect(id).not.toBe(first);
      return of(true);
    }).subscribe();
  });

  it('clears a purchase after a definitive rejection', () => {
    service.run('pink', () => throwError(() => new HttpErrorResponse({ status: 409 })))
      .subscribe({ error: () => {} });
    expect(service.hasPending('pink')).toBe(false);
  });
});
