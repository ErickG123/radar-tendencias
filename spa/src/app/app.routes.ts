import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent) },
  { path: 'dashboard', canActivate: [authGuard], loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'discovery', canActivate: [authGuard], loadComponent: () => import('./features/discovery/discovery.component').then(m => m.DiscoveryComponent) },
  { path: 'comparador', canActivate: [authGuard], loadComponent: () => import('./features/comparador/comparador.component').then(m => m.ComparadorComponent) },
  { path: 'alertas', canActivate: [authGuard], loadComponent: () => import('./features/alertas/alertas.component').then(m => m.AlertasComponent) },
  { path: 'configuracoes', canActivate: [authGuard], loadComponent: () => import('./features/configuracoes/configuracoes.component').then(m => m.ConfiguracoesComponent) },
  { path: 'telemetria', canActivate: [authGuard], loadComponent: () => import('./features/telemetria/telemetria.component').then(m => m.TelemetriaComponent) },
  { path: 'franquia/:id', canActivate: [authGuard], loadComponent: () => import('./features/franquia-detalhes/franquia-detalhes.component').then(m => m.FranquiaDetalhesComponent) }
];
