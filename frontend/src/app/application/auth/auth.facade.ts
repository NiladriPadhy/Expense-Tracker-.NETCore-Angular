import { Injectable, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { AuthResult } from '../../domain/models';
import { AuthRepository, LoginRequest, RegisterRequest } from '../../domain/ports/auth.repository';
import { JwtTokenStore } from '../../infrastructure/auth/jwt-token.store';

@Injectable({ providedIn: 'root' })
export class AuthFacade {
  private repo = inject(AuthRepository);
  private store = inject(JwtTokenStore);
  private router = inject(Router);

  readonly user = this.store.user;
  readonly isAuthenticated = this.store.isAuthenticated;
  readonly isAdmin = computed(() => this.store.user()?.role === 'Admin');

  register(req: RegisterRequest, photo?: File): Observable<AuthResult> {
    return this.repo.register(req, photo).pipe(tap((r) => this.store.setSession(r)));
  }

  login(req: LoginRequest): Observable<AuthResult> {
    return this.repo.login(req).pipe(tap((r) => this.store.setSession(r)));
  }

  logout(): void {
    const rt = this.store.refreshToken();
    if (rt) this.repo.logout(rt).subscribe({ error: () => undefined });
    this.store.clear();
    this.router.navigateByUrl('/login');
  }

  refreshMe(): Observable<void> {
    return new Observable<void>((sub) => {
      this.repo.me().subscribe({
        next: (u) => {
          this.store.setUser(u);
          sub.next();
          sub.complete();
        },
        error: (e) => sub.error(e),
      });
    });
  }
}
