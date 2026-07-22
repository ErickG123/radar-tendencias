import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-comparador',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './comparador.component.html',
  styleUrls: ['./comparador.component.scss']
})
export class ComparadorComponent {
  private http = inject(HttpClient);
  
  id1 = signal<string>('1');
  id2 = signal<string>('2');
  dados = signal<any[]>([]);

  comparar() {
    if(!this.id1() || !this.id2()) return;
    this.http.get<any[]>(`http://localhost:8080/api/franquias/comparar?ids=${this.id1()},${this.id2()}`).subscribe({
      next: (res) => this.dados.set(res)
    });
  }
}
