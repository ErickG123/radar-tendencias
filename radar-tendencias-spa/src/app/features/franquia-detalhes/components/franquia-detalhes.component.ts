import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-franquia-detalhes',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './franquia-detalhes.component.html',
  styleUrls: ['./franquia-detalhes.component.scss']
})
export class FranquiaDetalhesComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  
  franquia = signal<any>(null);
  historico = signal<any[]>([]);
  personagens = signal<any[]>([]);
  feedbacksComunidade = signal<any[]>([]);
  relacoesFranquia = signal<any[]>([]);
  isFavorito = signal<boolean>(false);
  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.carregarDetalhes(id);
        this.carregarPersonagens(id);
        this.carregarReddit(id);
        this.carregarRelacoes(id);
        this.verificarFavorito(id);
      }
    });
  }

  carregarDetalhes(id: string) {
    this.http.get<any>(`http://localhost:8080/franquias/${id}/detalhes`).subscribe({
      next: (res) => {
        this.franquia.set(res.Detalhes);
        this.historico.set(res.Historico);
      }
    });
  }

  carregarPersonagens(id: string) {
    this.http.get<any[]>(`http://localhost:8080/franquias/${id}/personagens`).subscribe({
      next: (res) => {
        this.personagens.set(res);
      }
    });
  }

  carregarReddit(id: string) {
    this.http.get<any[]>(`http://localhost:8080/franquias/${id}/comunidade`).subscribe({
      next: (res) => {
        this.feedbacksComunidade.set(res);
      }
    });
  }

  carregarRelacoes(id: string) {
    this.http.get<any[]>(`http://localhost:8080/franquias/${id}/relacoes`).subscribe({
      next: (res) => {
        this.relacoesFranquia.set(res);
      }
    });
  }

  verificarFavorito(id: string) {
    this.http.get<any>(`http://localhost:8080/favoritos/check/${id}`).subscribe({
      next: (res) => this.isFavorito.set(res.Favorito)
    });
  }

  toggleFavorito(id: string) {
    this.http.post<any>(`http://localhost:8080/favoritos/toggle/${id}`, {}).subscribe({
      next: (res) => this.isFavorito.set(res.Favorito)
    });
  }
}
