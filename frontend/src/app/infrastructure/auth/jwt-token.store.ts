import { Injectable, signal } from '@angular/core';
import { AuthResult, User } from '../../domain/models';

const ACCESS_KEY = 'et.access';
const REFRESH_KEY = 'et.refresh';
const EXPIRY_KEY = 'et.expiry';
const USER_KEY = 'et.user';

@Injectable({ providedIn: 'root' })
export class JwtTokenStore {
  readonly user = signal<User | null>(this.readUser());
  readonly isAuthenticated = signal<boolean>(!!this.accessToken());

  accessToken(): string | null {
    return sessionStorage.getItem(ACCESS_KEY);
  }

  refreshToken(): string | null {
    return sessionStorage.getItem(REFRESH_KEY);
  }

  expiresAtUtc(): Date | null {
    const v = sessionStorage.getItem(EXPIRY_KEY);
    return v ? new Date(v) : null;
  }

  isExpiringSoon(skewMs = 30_000): boolean {
    const exp = this.expiresAtUtc();
    if (!exp) return false;
    return exp.getTime() - Date.now() < skewMs;
  }

  setSession(r: AuthResult): void {
    sessionStorage.setItem(ACCESS_KEY, r.accessToken);
    sessionStorage.setItem(REFRESH_KEY, r.refreshToken);
    sessionStorage.setItem(EXPIRY_KEY, r.expiresAtUtc);
    sessionStorage.setItem(USER_KEY, JSON.stringify(r.user));
    this.user.set(r.user);
    this.isAuthenticated.set(true);
  }

  setUser(user: User): void {
    sessionStorage.setItem(USER_KEY, JSON.stringify(user));
    this.user.set(user);
  }

  clear(): void {
    sessionStorage.removeItem(ACCESS_KEY);
    sessionStorage.removeItem(REFRESH_KEY);
    sessionStorage.removeItem(EXPIRY_KEY);
    sessionStorage.removeItem(USER_KEY);
    this.user.set(null);
    this.isAuthenticated.set(false);
  }

  private readUser(): User | null {
    const raw = sessionStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as User;
    } catch {
      return null;
    }
  }
}
