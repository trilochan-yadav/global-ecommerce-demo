import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { LoginResponse, DecodedToken } from '../models/auth';

const NAME_CLAIM = 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name';
const TOKEN_KEY = 'jwt_token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base = environment.bffBaseUrl;

  // ── State ────────────────────────────────────────────────────────────────
  private readonly _token = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  /** True when a non-expired JWT is stored. */
  readonly isLoggedIn = computed(() => {
    const token = this._token();
    if (!token) return false;
    const payload = this.decode(token);
    if (!payload?.exp) return true;
    return (payload.exp as number) * 1000 > Date.now();
  });

  /** 'Admin' | 'Customer' | '' */
  readonly role = computed(() => {
    const token = this._token();
    if (!token) return '';
    return (this.decode(token)?.role as string) ?? '';
  });

  /** Decoded display name from the JWT claims. */
  readonly username = computed(() => {
    const token = this._token();
    if (!token) return '';
    const payload = this.decode(token);
    return (payload?.[NAME_CLAIM] as string) ?? (payload?.['sub'] as string) ?? '';
  });

  // ── Commands ─────────────────────────────────────────────────────────────
  login(username: string, password: string) {
    return this.http.post<LoginResponse>(`${this.base}/api/auth/login`, { username, password });
  }

  storeToken(token: string): void {
    localStorage.setItem(TOKEN_KEY, token);
    this._token.set(token);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this._token.set(null);
    this.router.navigate(['/login']);
  }

  /** Raw token — used by the token interceptor and SignalR service. */
  getToken(): string | null {
    return this._token();
  }

  // ── Private ───────────────────────────────────────────────────────────────
  private decode(token: string): DecodedToken | null {
    try {
      return JSON.parse(atob(token.split('.')[1]));
    } catch {
      return null;
    }
  }
}
