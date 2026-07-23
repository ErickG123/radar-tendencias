import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

export interface WorkerLog {
  logID: number;
  dataExecucao: string;
  status: string;
  itensProcessados: number;
  mensagemErro?: string;
  detalhesJson?: string;
}

@Component({
  selector: 'app-configuracoes',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './configuracoes.component.html',
  styleUrls: ['./configuracoes.component.scss']
})
export class ConfiguracoesComponent implements OnInit {
  private http = inject(HttpClient);
  
  scraperHabilitado = signal<boolean>(false);
  intervaloBase = signal<number>(240);
  modoTurboHabilitado = signal<boolean>(false);
  intervaloPromocional = signal<number>(0);

  isSaving = signal<boolean>(false);
  saveMessage = signal<string>('');

  workerLogs = signal<WorkerLog[]>([]);
  isLoadingLogs = signal<boolean>(false);
  isTriggering = signal<boolean>(false);

  ngOnInit() {
    this.carregarConfiguracoes();
    this.carregarLogs();
  }

  carregarConfiguracoes() {
    this.http.get<any>('http://localhost:8080/api/workers/config').subscribe({
      next: (res) => {
        if (res) {
          this.scraperHabilitado.set(res.scraperHabilitado);
          this.intervaloBase.set(res.intervaloBaseMinutos);
          this.modoTurboHabilitado.set(res.modoTurboHabilitado);
          this.intervaloPromocional.set(res.intervaloPromocionalMinutos);
        }
      }
    });
  }

  carregarLogs() {
    this.isLoadingLogs.set(true);
    this.http.get<WorkerLog[]>('http://localhost:8080/api/workers/logs').subscribe({
      next: (res) => {
        this.workerLogs.set(res || []);
        this.isLoadingLogs.set(false);
      },
      error: () => {
        this.isLoadingLogs.set(false);
      }
    });
  }

  forcarExecucao() {
    this.isTriggering.set(true);
    this.http.post('http://localhost:8080/api/workers/trigger', {}).subscribe({
      next: () => {
        this.isTriggering.set(false);
        this.carregarLogs();
      },
      error: () => {
        this.isTriggering.set(false);
      }
    });
  }

  aplicarConfiguracoes() {
    this.isSaving.set(true);
    this.saveMessage.set('');
    
    const payload = {
      scraperHabilitado: this.scraperHabilitado(),
      intervaloBaseMinutos: this.intervaloBase(),
      modoTurboHabilitado: this.modoTurboHabilitado(),
      intervaloPromocionalMinutos: this.intervaloPromocional()
    };
    
    this.http.post('http://localhost:8080/api/workers/config', payload).subscribe({
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

  // Estado para visualização de detalhes
  selectedLogDetails = signal<any[] | null>(null);

  openDetails(log: WorkerLog) {
    if (log.detalhesJson) {
      try {
        const parsed = JSON.parse(log.detalhesJson);
        this.selectedLogDetails.set(parsed);
      } catch (e) {
        this.selectedLogDetails.set(null);
      }
    }
  }

  closeDetails() {
    this.selectedLogDetails.set(null);
  }
}
