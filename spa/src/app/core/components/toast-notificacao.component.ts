import { Component, effect, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SignalRService } from '../services/signalr.service';

@Component({
  selector: 'app-toast-notificacao',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-notificacao.component.html',
  styleUrls: ['./toast-notificacao.component.scss']
})
export class ToastNotificacaoComponent {
  private signalRService = inject(SignalRService);
  notificacao = this.signalRService.ultimaAtualizacao;

  fechar() {
    this.signalRService.ultimaAtualizacao.set(null);
  }
}
