import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

interface FranquiaRanking {
  franquiaId: number;
  nome: string;
  categoria: string;
  hypeScore: number;
  sentimentoPositivo: number;
  bsrPosition: number;
}

@Component({
  selector: 'app-animes-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './animes-list.component.html',
  styleUrls: ['./animes-list.component.scss']
})
export class AnimesListComponent implements OnInit {
  private http = inject(HttpClient);

  franquias = signal<FranquiaRanking[]>([]);
  isLoading = signal<boolean>(true);
  searchTerm = signal<string>('');

  franquiasFiltradas = computed(() => {
    const term = this.searchTerm().toLowerCase();
    return this.franquias().filter(f => f.nome.toLowerCase().includes(term));
  });

  ngOnInit() {
    this.http.get<FranquiaRanking[]>('http://localhost:8080/api/franquias/ranking').subscribe({
      next: (data) => {
        this.franquias.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }
}
