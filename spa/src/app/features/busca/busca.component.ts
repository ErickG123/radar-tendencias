import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-busca',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './busca.component.html',
  styleUrls: ['./busca.component.scss']
})
export class BuscaComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  
  termoBusca = signal<string>('');
  resultados = signal<any[]>([]);
  isCarregando = signal<boolean>(false);

  resultadosMal = computed(() => this.resultados().filter(r => r.Fonte?.includes('MyAnimeList')));
  resultadosAnilist = computed(() => this.resultados().filter(r => r.Fonte === 'AniList'));
  resultadosTmdb = computed(() => this.resultados().filter(r => r.Fonte === 'TMDB'));

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      const q = params['q'];
      if (q) {
        this.termoBusca.set(q);
        this.realizarBusca(q);
      }
    });
  }

  realizarBusca(termo: string) {
    this.isCarregando.set(true);
    this.http.get<any[]>(`http://localhost:8080/pesquisa?q=${encodeURIComponent(termo)}`).subscribe({
      next: (res) => {
        this.resultados.set(res);
        this.isCarregando.set(false);
      },
      error: () => {
        this.isCarregando.set(false);
      }
    });
  }
}
