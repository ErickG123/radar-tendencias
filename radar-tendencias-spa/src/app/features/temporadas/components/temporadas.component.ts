import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-temporadas',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './temporadas.component.html',
  styleUrls: ['./temporadas.component.scss']
})
export class TemporadasComponent implements OnInit {
  private http = inject(HttpClient);

  anoSelecionado = signal<number>(2026);
  temporadaSelecionada = signal<string>('SPRING');
  
  anosDisponiveis = [2026, 2025, 2024, 2023, 2022];
  temporadasDisponiveis = [
    { label: 'Primavera (Spring)', value: 'SPRING' },
    { label: 'Verão (Summer)', value: 'SUMMER' },
    { label: 'Outono (Fall)', value: 'FALL' },
    { label: 'Inverno (Winter)', value: 'WINTER' }
  ];

  animes = signal<any[]>([]);
  isCarregando = signal<boolean>(false);

  blockbusters = computed(() => this.animes().filter(a => a.StatusPopularidade === 'Blockbuster' || a.StatusPopularidade === 'Popular'));
  joiasOcultas = computed(() => this.animes().filter(a => a.StatusPopularidade === 'Nicho / Underground'));

  ngOnInit() {
    this.carregarDadosTemporada();
  }

  carregarDadosTemporada() {
    this.isCarregando.set(true);
    this.http.get<any[]>(`http://localhost:8080/temporadas/analise?ano=${this.anoSelecionado()}&temporada=${this.temporadaSelecionada()}`).subscribe({
      next: (res) => {
        this.animes.set(res);
        this.isCarregando.set(false);
      },
      error: () => {
        this.isCarregando.set(false);
      }
    });
  }

  mudarFiltros() {
    this.carregarDadosTemporada();
  }
}
