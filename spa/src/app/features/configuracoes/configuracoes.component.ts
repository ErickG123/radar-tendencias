import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './configuracoes.component.html',
  styleUrls: ['./configuracoes.component.scss']
})
export class ConfiguracoesComponent implements OnInit {
  private http = inject(HttpClient);
  
  config = signal({
    amazonScraperAtivo: true,
    intervaloBaseMinutos: 240,
    modoPromocaoAtivo: false,
    intervaloPromocaoMinutos: 30
  });

  isSaving = signal<boolean>(false);
  saveMessage = signal<string>('');

  ngOnInit() {
    this.carregarConfiguracoes();
  }

  carregarConfiguracoes() {
    this.http.get<any>('http://localhost:8080/api/configuracoes/worker').subscribe({
      next: (res) => {
        if (res) this.config.set(res);
      }
    });
  }

  salvarConfiguracoes() {
    this.isSaving.set(true);
    this.saveMessage.set('');
    
    this.http.put('http://localhost:8080/api/configuracoes/worker', this.config()).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.saveMessage.set('Configurações aplicadas com sucesso. O Worker será atualizado no próximo ciclo.');
        setTimeout(() => this.saveMessage.set(''), 4000);
      },
      error: () => {
        this.isSaving.set(false);
        this.saveMessage.set('Erro ao salvar as configurações.');
      }
    });
  }
}
