import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', loadComponent: () => import('./features/dashboard/components/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'fluxos', loadComponent: () => import('./features/flow-editor/components/flow-editor.component').then(m => m.FlowEditorComponent) },
  { path: 'franquia/:id', loadComponent: () => import('./features/franquia-detalhes/components/franquia-detalhes.component').then(m => m.FranquiaDetalhesComponent) },
  { path: 'busca', loadComponent: () => import('./features/busca/components/busca.component').then(m => m.BuscaComponent) },
  { path: 'temporadas', loadComponent: () => import('./features/temporadas/components/temporadas.component').then(m => m.TemporadasComponent) },
  { path: 'watchlist', loadComponent: () => import('./features/watchlist/components/watchlist.component').then(m => m.WatchlistComponent) },
  { path: 'notificacoes', loadComponent: () => import('./features/notificacoes/components/notificacoes.component').then(m => m.NotificacoesComponent) }
];
