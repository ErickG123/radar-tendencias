import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-discovery',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './discovery.component.html',
  styleUrls: ['./discovery.component.scss']
})
export class DiscoveryComponent {
  private http = inject(HttpClient);
  
  minSentimento = signal<number>(80);
  maxHype = signal<number>(100);
  resultados = signal<any[]>([]);
  isCarregando = signal<boolean>(false);

  buscar() {
    this.isCarregando.set(true);
    this.http.get<any[]>(`http://localhost:8080/api/franquias/discovery?minSentimento=${this.minSentimento()}&maxHype=${this.maxHype()}`).subscribe({
      next: (res) => {
        this.resultados.set(res);
        this.isCarregando.set(false);
      },
      error: () => this.isCarregando.set(false)
    });
  }
}
