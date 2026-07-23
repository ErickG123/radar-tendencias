import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private authService = inject(AuthService);
  
  email = signal('');
  senha = signal('');
  erro = signal('');
  isCarregando = signal(false);

  fazerLogin() {
    this.isCarregando.set(true);
    this.erro.set('');
    
    this.authService.login({ email: this.email(), senha: this.senha() }).subscribe({
      error: () => {
        this.erro.set('Credenciais inválidas.');
        this.isCarregando.set(false);
      }
    });
  }
}
