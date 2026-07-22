import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AlertasService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:8080/alertas';

  getAlertas(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  marcarComoLido(id: number): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}/ler`, {});
  }
}
