import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrls: ['./login.component.scss']
})
export class RegisterComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  
  nome = signal('');
  email = signal('');
  senha = signal('');
  erro = signal('');
  sucesso = signal('');
  isCarregando = signal(false);

  fazerCadastro() {
    if (!this.nome() || !this.email() || !this.senha()) {
      this.erro.set('Preencha todos os campos.');
      return;
    }

    this.isCarregando.set(true);
    this.erro.set('');
    this.sucesso.set('');
    
    this.authService.registrar({ nome: this.nome(), email: this.email(), senha: this.senha() }).subscribe({
      next: () => {
        this.sucesso.set('Usuário cadastrado com sucesso!');
        this.isCarregando.set(false);
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1500);
      },
      error: () => {
        this.erro.set('Ocorreu um erro ao cadastrar o usuário. Pode já existir.');
        this.isCarregando.set(false);
      }
    });
  }
}
