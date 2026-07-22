import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-watchlist',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './watchlist.component.html',
  styleUrls: ['./watchlist.component.scss']
})
export class WatchlistComponent implements OnInit {
  private http = inject(HttpClient);
  
  favoritos = signal<any[]>([]);
  isCarregando = signal<boolean>(false);

  ngOnInit() {
    this.carregarFavoritos();
  }

  carregarFavoritos() {
    this.isCarregando.set(true);
    this.http.get<any[]>('http://localhost:8080/favoritos').subscribe({
      next: (res) => {
        this.favoritos.set(res);
        this.isCarregando.set(false);
      },
      error: () => this.isCarregando.set(false)
    });
  }

  removerFavorito(franquiaId: number, event: Event) {
    event.preventDefault();
    event.stopPropagation();
    this.http.post<any>(`http://localhost:8080/favoritos/toggle/${franquiaId}`, {}).subscribe({
      next: () => this.carregarFavoritos()
    });
  }
}
