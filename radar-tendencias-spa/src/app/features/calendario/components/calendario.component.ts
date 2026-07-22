import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-calendario',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './calendario.component.html',
  styleUrls: ['./calendario.component.scss']
})
export class CalendarioComponent implements OnInit {
  private http = inject(HttpClient);

  agenda = signal<any[]>([]);
  isCarregando = signal<boolean>(false);

  ngOnInit() {
    this.carregarAgenda();
  }

  carregarAgenda() {
    this.isCarregando.set(true);
    this.http.get<any[]>('http://localhost:8080/calendario/semana').subscribe({
      next: (res) => {
        this.agenda.set(res ?? []);
        this.isCarregando.set(false);
      },
      error: () => this.isCarregando.set(false)
    });
  }
}
