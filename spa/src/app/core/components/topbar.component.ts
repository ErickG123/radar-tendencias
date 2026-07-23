import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AlertasService } from '../services/alertas.service';
import { LayoutService } from '../layout/layout.service';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './topbar.component.html'
})
export class TopbarComponent implements OnInit {
  layoutService = inject(LayoutService);
  private alertasService = inject(AlertasService);
  private router = inject(Router);

  alertas = signal<any[]>([]);
  isNotificationsOpen = signal<boolean>(false);
  unreadCount = signal<number>(0);

  ngOnInit() {
    this.carregarAlertas();
  }

  carregarAlertas() {
    this.alertasService.getAlertas().subscribe({
      next: (dados) => {
        this.alertas.set(dados);
        this.unreadCount.set(dados.filter(a => !a.Lido).length);
      },
      error: () => {}
    });
  }

  toggleNotifications() {
    this.isNotificationsOpen.update(v => !v);
    if (this.isNotificationsOpen()) {
      this.carregarAlertas();
    }
  }

  marcarComoLido(alerta: any) {
    if (alerta.Lido) return;
    this.alertasService.marcarComoLido(alerta.AlertaID).subscribe({
      next: () => {
        this.carregarAlertas();
      }
    });
  }

  buscarFranquia(termo: string) {
    if (!termo || termo.trim().length === 0) return;
    this.router.navigate(['/busca'], { queryParams: { q: termo } });
    const inputElement = document.querySelector('.topbar input[type="text"]') as HTMLInputElement;
    if (inputElement) {
      inputElement.value = '';
    }
  }
}
