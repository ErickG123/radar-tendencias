import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-notificacoes',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './notificacoes.component.html',
  styleUrls: ['./notificacoes.component.scss']
})
export class NotificacoesComponent implements OnInit {
  private http = inject(HttpClient);
  
  notificacoes = signal<any[]>([]);
  isCarregando = signal<boolean>(false);

  ngOnInit() {
    this.carregarNotificacoes();
  }

  carregarNotificacoes() {
    this.isCarregando.set(true);
    this.http.get<any[]>('http://localhost:8080/notificacoes').subscribe({
      next: (res) => {
        this.notificacoes.set(res);
        this.isCarregando.set(false);
      },
      error: () => this.isCarregando.set(false)
    });
  }

  marcarComoLida(id: number) {
    this.http.patch(`http://localhost:8080/notificacoes/${id}/ler`, {}).subscribe({
      next: () => {
        this.notificacoes.update(lista => lista.map(n => n.NotificacaoID === id ? { ...n, Lida: true } : n));
      }
    });
  }

  removerNotificacao(id: number, event: Event) {
    event.stopPropagation();
    this.http.delete(`http://localhost:8080/notificacoes/${id}`).subscribe({
      next: () => {
        this.notificacoes.update(lista => lista.filter(n => n.NotificacaoID !== id));
      }
    });
  }
}
