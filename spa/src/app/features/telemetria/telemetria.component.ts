import { Component, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { timer, switchMap, catchError, of } from 'rxjs';

export interface TelemetriaStatus {
  apiStatus: string;
  memoriaUsadaMb: number;
  databaseStatus: string;
  totalFranquiasMonitoradas: number;
  redisStatus: string;
  ultimaSincronizacaoWorker: string | null;
}

@Component({
  selector: 'app-telemetria',
  standalone: true,
  imports: [CommonModule],
  providers: [DatePipe],
  templateUrl: './telemetria.component.html',
  styleUrls: ['./telemetria.component.scss']
})
export class TelemetriaComponent {
  private http = inject(HttpClient);
  
  telemetria = toSignal(
    timer(0, 10000).pipe(
      switchMap(() => this.http.get<TelemetriaStatus>('http://localhost:8080/api/telemetria/status').pipe(
        catchError(() => of({
          apiStatus: 'Offline',
          databaseStatus: 'Offline',
          redisStatus: 'Offline',
          memoriaUsadaMb: 0,
          totalFranquiasMonitoradas: 0,
          ultimaSincronizacaoWorker: null
        } as TelemetriaStatus))
      ))
    ),
    { 
      initialValue: {
        apiStatus: 'Carregando...',
        databaseStatus: 'Carregando...',
        redisStatus: 'Carregando...',
        memoriaUsadaMb: 0,
        totalFranquiasMonitoradas: 0,
        ultimaSincronizacaoWorker: null
      } as TelemetriaStatus 
    }
  );
}
