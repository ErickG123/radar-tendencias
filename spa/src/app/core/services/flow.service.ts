import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class FlowService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:8080/fluxos';

  getFluxos(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  salvarFluxo(fluxo: any): Observable<any> {
    return this.http.post<any>(this.apiUrl, fluxo);
  }
}
