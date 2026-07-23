import { Component, inject, input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { switchMap, filter, catchError, of, Subject, startWith, merge, map } from 'rxjs';

@Component({
  selector: 'app-franquia-detalhes',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './franquia-detalhes.component.html',
  styleUrls: ['./franquia-detalhes.component.scss']
})
export class FranquiaDetalhesComponent {
  private http = inject(HttpClient);
  
  id = input.required<string>();
  private id$ = toObservable(this.id).pipe(filter(val => !!val));

  private detalhesData = toSignal(
    this.id$.pipe(
      switchMap(id => this.http.get<any>(`http://localhost:8080/franquias/${id}/detalhes`).pipe(
        catchError(() => of(null))
      ))
    ),
    { initialValue: null }
  );

  franquia = computed(() => this.detalhesData()?.Detalhes ?? null);
  historico = computed(() => this.detalhesData()?.Historico ?? []);
  resumoIA = computed(() => this.detalhesData()?.ResumoIA ?? '');

  isGerandoResumo = signal<boolean>(false);
  resumoGerado = signal<string>('');

  gerarResumoIA(id: string) {
    this.isGerandoResumo.set(true);
    this.http.post<any>(`http://localhost:8080/api/franquias/${id}/gerar-resumo`, {}).subscribe({
      next: (res) => {
        this.resumoGerado.set(res.Resumo);
        this.isGerandoResumo.set(false);
      },
      error: () => this.isGerandoResumo.set(false)
    });
  }
  personagens = toSignal(
    this.id$.pipe(
      switchMap(id => this.http.get<any[]>(`http://localhost:8080/franquias/${id}/personagens`).pipe(
        catchError(() => of([]))
      ))
    ),
    { initialValue: undefined }
  );
  personagensLoaded = computed(() => this.personagens() !== undefined);

  feedbacksComunidade = toSignal(
    this.id$.pipe(
      switchMap(id => this.http.get<any[]>(`http://localhost:8080/franquias/${id}/comunidade`).pipe(
        catchError(() => of([]))
      ))
    ),
    { initialValue: undefined }
  );
  comunidadeLoaded = computed(() => this.feedbacksComunidade() !== undefined);

  relacoesFranquia = toSignal(
    this.id$.pipe(
      switchMap(id => this.http.get<any[]>(`http://localhost:8080/franquias/${id}/relacoes`).pipe(
        catchError(() => of([]))
      ))
    ),
    { initialValue: [] }
  );

  private toggleFavoritoSubject = new Subject<void>();
  
  isFavorito = toSignal(
    merge(
      this.id$.pipe(
        switchMap(id => this.http.get<any>(`http://localhost:8080/favoritos/check/${id}`).pipe(
          catchError(() => of({ Favorito: false }))
        ))
      ),
      this.toggleFavoritoSubject.pipe(
        switchMap(() => this.http.post<any>(`http://localhost:8080/favoritos/toggle/${this.id()}`, {}).pipe(
          catchError(() => of({ Favorito: false }))
        ))
      )
    ).pipe(map(res => res.Favorito)),
    { initialValue: false }
  );

  toggleFavorito(id: string) {
    this.toggleFavoritoSubject.next();
  }

  comparativoRegional = toSignal(
    this.id$.pipe(
      switchMap(id => this.http.get<any>(`http://localhost:8080/franquias/${id}/comparativo-regional`).pipe(
        catchError(() => of(null))
      ))
    ),
    { initialValue: null }
  );

  streamingProviders = toSignal(
    this.id$.pipe(
      switchMap(id => this.http.get<any[]>(`http://localhost:8080/franquias/${id}/streaming`).pipe(
        catchError(() => of([]))
      ))
    ),
    { initialValue: undefined }
  );
  streamingLoaded = computed(() => this.streamingProviders() !== undefined);

  episodios = toSignal(
    this.id$.pipe(
      switchMap(id => this.http.get<any[]>(`http://localhost:8080/api/franquias/${id}/episodios`).pipe(
        catchError(() => of([]))
      ))
    ),
    { initialValue: [] }
  );

  palavrasChave = toSignal(
    this.id$.pipe(
      switchMap(id => this.http.get<any[]>(`http://localhost:8080/api/franquias/${id}/palavras-chave`).pipe(
        catchError(() => of([]))
      ))
    ),
    { initialValue: [] }
  );

  getTamanhoFonte(weight: number): string {
    const minSize = 0.8;
    const maxSize = 2.5;
    const size = minSize + (weight * 0.15);
    return `${Math.min(size, maxSize)}rem`;
  }
}
