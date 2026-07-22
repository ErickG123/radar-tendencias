import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Franquia } from '../models/franquia.model';
import { DashboardHype } from '../models/monitoramento.model';

@Injectable({ providedIn: 'root' })
export class FranquiasService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:8080';

  franquias = signal<Franquia[]>([]);
  dashboardData = signal<DashboardHype[]>([]);

  loadFranquias() {
    this.http.get<Franquia[]>(`${this.apiUrl}/franquias`).subscribe(data => {
      this.franquias.set(data);
    });
  }

  loadDashboardData() {
    this.http.get<DashboardHype[]>(`${this.apiUrl}/monitoramento/dashboard`).subscribe(data => {
      this.dashboardData.set(data);
    });
  }
}
