import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { MainLayoutComponent } from './core/layout/main-layout.component';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent) },
  { path: 'register', loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent) },
  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'animes', loadComponent: () => import('./features/animes/animes-list.component').then(m => m.AnimesListComponent) },
      { path: 'discovery', loadComponent: () => import('./features/discovery/discovery.component').then(m => m.DiscoveryComponent) },
      { path: 'comparador', loadComponent: () => import('./features/comparador/comparador.component').then(m => m.ComparadorComponent) },
      { path: 'alertas', loadComponent: () => import('./features/alertas/alertas.component').then(m => m.AlertasComponent) },
      { path: 'configuracoes', loadComponent: () => import('./features/configuracoes/configuracoes.component').then(m => m.ConfiguracoesComponent) },
      { path: 'telemetria', loadComponent: () => import('./features/telemetria/telemetria.component').then(m => m.TelemetriaComponent) },
      { path: 'franquia/:id', loadComponent: () => import('./features/franquia-detalhes/franquia-detalhes.component').then(m => m.FranquiaDetalhesComponent) },
      { path: 'fluxos', loadComponent: () => import('./features/flow-editor/flow-editor.component').then(m => m.FlowEditorComponent) },
      { path: 'busca', loadComponent: () => import('./features/busca/busca.component').then(m => m.BuscaComponent) },
      { path: 'temporadas', loadComponent: () => import('./features/temporadas/temporadas.component').then(m => m.TemporadasComponent) },
      { path: 'watchlist', loadComponent: () => import('./features/watchlist/watchlist.component').then(m => m.WatchlistComponent) },
      { path: 'calendario', loadComponent: () => import('./features/calendario/calendario.component').then(m => m.CalendarioComponent) },
      { path: 'notificacoes', loadComponent: () => import('./features/notificacoes/notificacoes.component').then(m => m.NotificacoesComponent) }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
