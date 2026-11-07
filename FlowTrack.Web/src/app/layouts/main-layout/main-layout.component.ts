import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'ft-main-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="d-flex min-vh-100">
      <aside class="bg-dark text-white p-3" style="width: 250px">
        <a routerLink="/dashboard" class="navbar-brand text-white fw-bold d-block mb-4">FlowTrack</a>
        <nav class="nav flex-column gap-2">
          <a routerLink="/dashboard" routerLinkActive="active" class="nav-link text-white">Dashboard</a>
          <a routerLink="/tasks" routerLinkActive="active" class="nav-link text-white">Tasks</a>
        </nav>
        <button class="btn btn-outline-light btn-sm mt-auto position-absolute bottom-0 mb-4" (click)="logout()">Sign out</button>
      </aside>
      <main class="flex-grow-1 p-4"><router-outlet /></main>
    </div>
  `
})
export class MainLayoutComponent {
  private readonly auth = inject(AuthService);
  logout(): void { this.auth.logout(); window.location.href = '/login'; }
}
