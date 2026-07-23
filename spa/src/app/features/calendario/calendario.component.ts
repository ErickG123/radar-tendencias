import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

export interface Programacao {
  nome: string;
  categoria: string;
  diaSemana: number;
  horario: string;
  episodio: string;
}

export interface DiaProgramacao {
  nomeDia: string;
  itens: Programacao[];
}

@Component({
  selector: 'app-calendario',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './calendario.component.html',
  styleUrls: ['./calendario.component.scss']
})
export class CalendarioComponent implements OnInit {
  private http = inject(HttpClient);

  agendaAgrupada = signal<DiaProgramacao[]>([]);
  isCarregando = signal<boolean>(false);
  hasError = signal<boolean>(false);

  private diasSemanaMap = ['Domingo', 'Segunda-feira', 'Terça-feira', 'Quarta-feira', 'Quinta-feira', 'Sexta-feira', 'Sábado'];

  ngOnInit() {
    this.carregarAgenda();
  }

  carregarAgenda() {
    this.isCarregando.set(true);
    this.hasError.set(false);
    this.http.get<Programacao[]>('http://localhost:8080/api/calendario/semana').subscribe({
      next: (res) => {
        const dados = res || [];
        const map = new Map<number, Programacao[]>();
        
        dados.forEach(item => {
          if (!map.has(item.diaSemana)) {
            map.set(item.diaSemana, []);
          }
          map.get(item.diaSemana)!.push(item);
        });

        const agrupado: DiaProgramacao[] = Array.from(map.entries())
          .sort((a, b) => a[0] - b[0])
          .map(([dia, itens]) => ({
            nomeDia: this.diasSemanaMap[dia] || `Dia ${dia}`,
            itens
          }));

        this.agendaAgrupada.set(agrupado);
        this.isCarregando.set(false);
      },
      error: () => {
        this.hasError.set(true);
        this.isCarregando.set(false);
      }
    });
  }
}
