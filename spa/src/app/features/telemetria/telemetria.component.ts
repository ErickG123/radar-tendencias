import { Component, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { timer, switchMap, catchError, of } from 'rxjs';

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
      switchMap(() => this.http.get<any>('http://localhost:8080/api/telemetria/status').pipe(
        catchError(() => of(null))
      ))
    ),
    { initialValue: undefined }
  );
}
