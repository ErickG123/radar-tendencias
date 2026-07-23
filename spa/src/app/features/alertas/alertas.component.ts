import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { Subject, switchMap, startWith } from 'rxjs';

@Component({
  selector: 'app-alertas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './alertas.component.html',
  styleUrls: ['./alertas.component.scss']
})
export class AlertasComponent {
  private http = inject(HttpClient);
  
  private refreshTrigger = new Subject<void>();
  
  alertas = toSignal(
    this.refreshTrigger.pipe(
      startWith(null),
      switchMap(() => this.http.get<any[]>('http://localhost:8080/api/alertas'))
    ),
    { initialValue: [] }
  );

  novoAlerta = signal({ franquiaId: null, tipoMetrica: 'Hype', condicao: 'Maior', valorAlvo: 80 });

  salvarAlerta() {
    if (!this.novoAlerta().franquiaId) return;
    this.http.post('http://localhost:8080/api/alertas', this.novoAlerta()).subscribe({
      next: () => {
        this.refreshTrigger.next();
        this.novoAlerta.set({ franquiaId: null, tipoMetrica: 'Hype', condicao: 'Maior', valorAlvo: 80 });
      }
    });
  }

  excluirAlerta(id: number) {
    this.http.delete(`http://localhost:8080/api/alertas/${id}`).subscribe({
      next: () => this.refreshTrigger.next()
    });
  }
}
