import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  currentUser = signal<any>(this.getUserFromStorage());
  isAuthenticated = signal<boolean>(!!this.getToken());

  login(credenciais: any) {
    return this.http.post<any>('http://localhost:8080/api/auth/login', credenciais).pipe(
      tap(res => {
        localStorage.setItem('jwt_token', res.Token);
        localStorage.setItem('user_data', JSON.stringify(res.Usuario));
        this.currentUser.set(res.Usuario);
        this.isAuthenticated.set(true);
        this.router.navigate(['/dashboard']);
      })
    );
  }

  logout() {
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('user_data');
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem('jwt_token');
  }

  private getUserFromStorage() {
    const data = localStorage.getItem('user_data');
    return data ? JSON.parse(data) : null;
  }
}
