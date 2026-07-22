import { Component, signal, effect, inject, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AlertasService } from './core/services/alertas.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule],
  templateUrl: './app.html',
  styleUrls: ['./app.scss']
})
export class App implements OnInit {
  private alertasService = inject(AlertasService);
  private router = inject(Router);
  private http = inject(HttpClient);
  
  isDarkMode = signal<boolean>(false);
  isSidebarCollapsed = signal<boolean>(false);
  alertas = signal<any[]>([]);
  isNotificationsOpen = signal<boolean>(false);
  unreadCount = signal<number>(0);

  constructor() {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
      this.isDarkMode.set(true);
    }
    
    effect(() => {
      const isDark = this.isDarkMode();
      if (isDark) {
        document.documentElement.setAttribute('data-theme', 'dark');
        localStorage.setItem('theme', 'dark');
      } else {
        document.documentElement.removeAttribute('data-theme');
        localStorage.setItem('theme', 'light');
      }
    });
  }

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

  toggleTheme() {
    this.isDarkMode.update(v => !v);
  }

  toggleSidebar() {
    this.isSidebarCollapsed.update(v => !v);
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
