import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection | undefined;
  
  public ultimaAtualizacao = signal<{franquiaId: number, nomeFranquia: string, tipoAtualizacao: string} | null>(null);

  public iniciarConexao() {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('http://localhost:8080/hubs/radar')
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().catch((err: any) => console.error(err));

    this.hubConnection.on('OnWorkerSyncComplete', (data: any) => {
      this.ultimaAtualizacao.set(data);
      setTimeout(() => this.ultimaAtualizacao.set(null), 5000);
    });
  }
}
