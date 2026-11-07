import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, of, takeUntil } from 'rxjs';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { Task, TaskStatus } from '../../core/models/task.model';
import { TaskService } from '../../core/services/task.service';

@Component({
  selector: 'ft-task-list',
  standalone: true,
  imports: [DatePipe, FormsModule, PageHeaderComponent, RouterLink],
  template: `
    <ft-page-header title="Tasks" subtitle="Plan, prioritize, and complete your work.">
      <a class="btn btn-primary" routerLink="/tasks/new">New task</a>
    </ft-page-header>

    <section class="card border-0 shadow-sm">
      <div class="card-body border-bottom">
        <div class="row g-3 align-items-center">
          <div class="col-12 col-md-8">
            <label class="visually-hidden" for="task-search">Search tasks</label>
            <div class="input-group">
              <span class="input-group-text" aria-hidden="true">⌕</span>
              <input id="task-search" class="form-control" type="search" placeholder="Search by title or description..." [(ngModel)]="search" (ngModelChange)="searchChanged.next($event)">
            </div>
          </div>
          <div class="col-12 col-md-4">
            <label class="visually-hidden" for="task-status">Filter by status</label>
            <select id="task-status" class="form-select" [(ngModel)]="status" (ngModelChange)="filterChanged()">
              <option [ngValue]="undefined">All statuses</option>
              @for (option of statusOptions; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
            </select>
          </div>
        </div>
      </div>

      @if (error) { <div class="alert alert-danger m-3 mb-0" role="alert">{{ error }}</div> }

      <div class="position-relative">
        @if (loading) {
          <div class="loading-overlay d-flex align-items-center justify-content-center" role="status" aria-live="polite">
            <div class="spinner-border text-primary" aria-hidden="true"></div><span class="visually-hidden">Loading tasks...</span>
          </div>
        }

        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light"><tr><th>Task</th><th>Status</th><th>Due date</th><th>Created</th><th>Actions</th></tr></thead>
            <tbody>
              @for (task of tasks; track task.id) {
                <tr>
                  <td><div class="fw-semibold">{{ task.title }}</div><small class="text-secondary">{{ task.description || 'No description' }}</small></td>
                  <td><span class="badge" [class]="statusClass(task.status)">{{ statusLabel(task.status) }}</span></td>
                  <td class="text-nowrap">{{ task.dueDate | date:'mediumDate' }}</td>
                  <td class="text-nowrap text-secondary">{{ task.createdUtc | date:'mediumDate' }}</td>
                  <td class="text-nowrap">
                    <a class="btn btn-sm btn-outline-secondary me-2" [routerLink]="['/tasks', task.id, 'edit']">Edit</a>
                    <button class="btn btn-sm btn-outline-danger" type="button" (click)="deleteTask(task)" [disabled]="deletingTaskId === task.id">
                      {{ deletingTaskId === task.id ? 'Deleting...' : 'Delete' }}
                    </button>
                  </td>
                </tr>
              }
              @empty { <tr><td colspan="5" class="text-center text-secondary py-5">{{ loading ? 'Loading tasks...' : 'No tasks found.' }}</td></tr> }
            </tbody>
          </table>
        </div>
      </div>

      <div class="card-footer bg-white d-flex flex-column flex-sm-row gap-3 justify-content-between align-items-center">
        <small class="text-secondary">Showing {{ firstItem }}–{{ lastItem }} of {{ totalCount }} tasks</small>
        <nav aria-label="Task pagination"><ul class="pagination pagination-sm mb-0">
          <li class="page-item" [class.disabled]="pageNumber === 1"><button class="page-link" (click)="goToPage(pageNumber - 1)" [disabled]="pageNumber === 1">Previous</button></li>
          @for (page of pages; track page) { <li class="page-item" [class.active]="page === pageNumber"><button class="page-link" (click)="goToPage(page)">{{ page }}</button></li> }
          <li class="page-item" [class.disabled]="pageNumber === totalPages"><button class="page-link" (click)="goToPage(pageNumber + 1)" [disabled]="pageNumber === totalPages">Next</button></li>
        </ul></nav>
      </div>
    </section>
  `
  ,styles: [`
    .loading-overlay { position: absolute; inset: 0; min-height: 12rem; background: rgba(255, 255, 255, .7); z-index: 1; }
  `]
})
export class TaskListComponent implements OnInit, OnDestroy {
  private readonly taskService = inject(TaskService);
  private readonly destroy$ = new Subject<void>();
  readonly searchChanged = new Subject<string>();
  readonly pageSize = 10;
  readonly statusOptions: { value: TaskStatus; label: string }[] = [
    { value: 'Todo', label: 'To do' },
    { value: 'InProgress', label: 'In progress' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];
  tasks: Task[] = [];
  search = '';
  status?: TaskStatus;
  pageNumber = 1;
  totalCount = 0;
  loading = false;
  deletingTaskId: string | null = null;
  error = '';

  ngOnInit(): void {
    this.searchChanged.pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$)).subscribe(() => {
      this.pageNumber = 1;
      this.loadTasks();
    });
    this.loadTasks();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get totalPages(): number { return Math.max(1, Math.ceil(this.totalCount / this.pageSize)); }
  get pages(): number[] { return Array.from({ length: this.totalPages }, (_, index) => index + 1); }
  get firstItem(): number { return this.totalCount === 0 ? 0 : (this.pageNumber - 1) * this.pageSize + 1; }
  get lastItem(): number { return Math.min(this.pageNumber * this.pageSize, this.totalCount); }

  filterChanged(): void {
    this.pageNumber = 1;
    this.loadTasks();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.pageNumber) return;
    this.pageNumber = page;
    this.loadTasks();
  }

  deleteTask(task: Task): void {
    if (this.deletingTaskId || !window.confirm(`Delete "${task.title}"? This cannot be undone.`)) {
      return;
    }

    this.deletingTaskId = task.id;
    this.error = '';
    this.taskService.deleteTask(task.id).pipe(
      finalize(() => this.deletingTaskId = null)
    ).subscribe({
      next: () => this.loadTasks(),
      error: () => this.error = 'Unable to delete the task. Please try again.'
    });
  }

  statusLabel(status: TaskStatus): string {
    return this.statusOptions.find(option => option.value === status)?.label ?? status;
  }

  statusClass(status: TaskStatus): string {
    return ({ Todo: 'text-bg-secondary', InProgress: 'text-bg-primary', Completed: 'text-bg-success', Cancelled: 'text-bg-danger' } as Record<TaskStatus, string>)[status];
  }

  private loadTasks(): void {
    this.loading = true;
    this.error = '';
    this.taskService.getTasks(this.pageNumber, this.pageSize, this.status, this.search).pipe(
      finalize(() => this.loading = false),
      catchError(() => { this.error = 'Unable to load tasks. Please try again.'; return of({ items: [], pageNumber: this.pageNumber, pageSize: this.pageSize, totalCount: 0 }); })
    ).subscribe(result => { this.tasks = result.items; this.totalCount = result.totalCount; });
  }
}
