import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, distinctUntilChanged, tap } from 'rxjs';
import {
  AuthenticationResponse,
  LoginRequest,
  RegisterRequest,
  RegisterResponse
} from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenKey = 'flowtrack.accessToken';
  private readonly userIdKey = 'flowtrack.userId';
  private readonly apiUrl = '/api/auth';
  private readonly authenticatedSubject = new BehaviorSubject<boolean>(this.hasStoredToken());

  readonly isAuthenticated$ = this.authenticatedSubject.asObservable().pipe(distinctUntilChanged());

  login(request: LoginRequest): Observable<AuthenticationResponse> {
    return this.http.post<AuthenticationResponse>(`${this.apiUrl}/login`, request).pipe(
      tap(response => this.storeAuthentication(response))
    );
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.apiUrl}/register`, request);
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.userIdKey);
    this.authenticatedSubject.next(false);
  }

  isAuthenticated(): boolean {
    const token = this.getToken();
    if (!token || !this.isJwtValid(token)) {
      if (token) {
        this.logout();
      }

      return false;
    }

    return true;
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  getUserId(): string | null {
    return localStorage.getItem(this.userIdKey);
  }

  private storeAuthentication(response: AuthenticationResponse): void {
    localStorage.setItem(this.tokenKey, response.accessToken);
    localStorage.setItem(this.userIdKey, response.userId);
    this.authenticatedSubject.next(true);
  }

  private hasStoredToken(): boolean {
    return Boolean(localStorage.getItem(this.tokenKey));
  }

  private isJwtValid(token: string): boolean {
    try {
      const payload = token.split('.')[1];
      if (!payload) {
        return false;
      }

      const normalizedPayload = payload.replace(/-/g, '+').replace(/_/g, '/');
      const decodedPayload = atob(normalizedPayload.padEnd(Math.ceil(normalizedPayload.length / 4) * 4, '='));
      const claims = JSON.parse(decodedPayload) as { exp?: unknown };

      return typeof claims.exp === 'number' && claims.exp > Math.floor(Date.now() / 1000);
    } catch {
      return false;
    }
  }
}
