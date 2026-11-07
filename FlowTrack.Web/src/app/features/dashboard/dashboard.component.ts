import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { TaskService } from '../../core/services/task.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'ft-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, RouterLink],
  template: `
    <ft-page-header title="Dashboard" subtitle="Keep your work moving forward.">
      <a class="btn btn-primary" routerLink="/tasks/new">New task</a>
    </ft-page-header>
    @if (error) {
      <div class="alert alert-danger" role="alert">{{ error }}</div>
    }
    <div class="row g-4">
      <div class="col-md-4">
        <div class="card border-0 shadow-sm">
          <div class="card-body">
            <span class="text-secondary">Open tasks</span>
            @if (loading) {
              <div class="display-6 fw-semibold" role="status">...</div>
            } @else if (openTaskCount !== null) {
              <div class="display-6 fw-semibold">{{ openTaskCount }}</div>
            } @else {
              <div class="display-6 fw-semibold">-</div>
            }
          </div>
        </div>
      </div>
    </div>
  `
})
export class DashboardComponent implements OnInit {
  private readonly taskService = inject(TaskService);

  openTaskCount: number | null = null;
  loading = false;
  error = '';

  ngOnInit(): void {
    this.loading = true;
    this.error = '';

    forkJoin([
      this.taskService.getTasks(1, 1, 'Todo'),
      this.taskService.getTasks(1, 1, 'InProgress')
    ]).pipe(
      finalize(() => this.loading = false)
    ).subscribe({
      next: ([todoTasks, inProgressTasks]) => {
        this.openTaskCount = todoTasks.totalCount + inProgressTasks.totalCount;
      },
      error: () => {
        this.error = 'Unable to load the open task count. Please try again.';
      }
    });
  }
}
