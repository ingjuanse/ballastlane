import { Component, OnInit, inject } from '@angular/core';
import { Location } from '@angular/common';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { TaskStatus } from '../../core/models/task.model';
import { TaskService } from '../../core/services/task.service';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

@Component({
  selector: 'ft-task-form',
  standalone: true,
  imports: [ReactiveFormsModule, PageHeaderComponent],
  template: `
    <ft-page-header
      [title]="isEditMode ? 'Edit task' : 'Create task'"
      [subtitle]="isEditMode ? 'Update the task details and status.' : 'Add a new task to your workflow.'">
    </ft-page-header>

    <div class="row justify-content-center">
      <div class="col-12 col-xl-8">
        <section class="card border-0 shadow-sm">
          <div class="card-body p-4 p-md-5">
            @if (error) {
              <div class="alert alert-danger" role="alert">{{ error }}</div>
            }

            @if (loadingTask) {
              <div class="text-center py-5" role="status">
                <div class="spinner-border text-primary" aria-hidden="true"></div>
                <span class="visually-hidden">Loading task...</span>
              </div>
            } @else {
              <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
                <div class="mb-3">
                  <label class="form-label" for="title">Title</label>
                  <input id="title" class="form-control" [class.is-invalid]="titleInvalid" formControlName="title" maxlength="200" placeholder="Enter a task title">
                  @if (titleInvalid) {
                    <div class="invalid-feedback">
                      @if (form.controls.title.hasError('required')) { Title is required. }
                      @else { Title cannot exceed 200 characters. }
                    </div>
                  }
                </div>

                <div class="mb-3">
                  <label class="form-label" for="description">Description</label>
                  <textarea id="description" class="form-control" [class.is-invalid]="descriptionInvalid" formControlName="description" maxlength="2000" rows="5" placeholder="Add details about this task"></textarea>
                  <div class="form-text text-end">{{ form.controls.description.value.length }}/2000</div>
                  @if (descriptionInvalid) { <div class="invalid-feedback">Description cannot exceed 2000 characters.</div> }
                </div>

                <div class="row g-3 mb-4">
                  <div class="col-12 col-md-6">
                    <label class="form-label" for="status">Status</label>
                    <select id="status" class="form-select" [class.is-invalid]="statusInvalid" formControlName="status">
                      @for (option of statusOptions; track option.value) { <option [ngValue]="option.value">{{ option.label }}</option> }
                    </select>
                    @if (statusInvalid) { <div class="invalid-feedback">Status is required.</div> }
                  </div>

                  <div class="col-12 col-md-6">
                    <label class="form-label" for="dueDate">Due date</label>
                    <input id="dueDate" class="form-control" [class.is-invalid]="dueDateInvalid" type="date" formControlName="dueDate">
                    @if (dueDateInvalid) {
                      <div class="invalid-feedback">
                        @if (form.controls.dueDate.hasError('required')) { Due date is required. }
                        @else { Due date cannot be in the past. }
                      </div>
                    }
                  </div>
                </div>

                <div class="d-flex flex-column flex-sm-row justify-content-end gap-2">
                  <button class="btn btn-outline-secondary" type="button" (click)="cancel()">Cancel</button>
                  <button class="btn btn-primary" type="submit" [disabled]="form.invalid || saving">
                    @if (saving) {
                      <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Saving...
                    } @else { {{ isEditMode ? 'Save changes' : 'Create task' }} }
                  </button>
                </div>
              </form>
            }
          </div>
        </section>
      </div>
    </div>
  `
})
export class TaskFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly taskService = inject(TaskService);

  readonly statusOptions: { value: TaskStatus; label: string }[] = [
    { value: 'Todo', label: 'To do' },
    { value: 'InProgress', label: 'In progress' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Cancelled', label: 'Cancelled' }
  ];

  readonly form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', Validators.maxLength(2000)],
    status: ['Todo' as TaskStatus, Validators.required],
    dueDate: ['', [Validators.required, TaskFormComponent.futureDateValidator]]
  });

  isEditMode = false;
  loadingTask = false;
  saving = false;
  error = '';
  private taskId?: string;

  get titleInvalid(): boolean { return this.invalid('title'); }
  get descriptionInvalid(): boolean { return this.invalid('description'); }
  get statusInvalid(): boolean { return this.invalid('status'); }
  get dueDateInvalid(): boolean { return this.invalid('dueDate'); }

  ngOnInit(): void {
    this.taskId = this.route.snapshot.paramMap.get('id') ?? undefined;
    this.isEditMode = Boolean(this.taskId);

    if (this.taskId) {
      this.loadTask(this.taskId);
    }
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.error = '';
    const value = this.form.getRawValue();
    const request = {
      title: value.title.trim(),
      description: value.description.trim(),
      dueDate: new Date(`${value.dueDate}T23:59:59`).toISOString()
    };

    const operation = this.isEditMode && this.taskId
      ? this.taskService.updateTask(this.taskId, { ...request, status: value.status })
      : this.taskService.createTask(request);

    operation.pipe(finalize(() => this.saving = false)).subscribe({
      next: () => void this.router.navigate(['/tasks']),
      error: () => this.error = `Unable to ${this.isEditMode ? 'update' : 'create'} the task. Please try again.`
    });
  }

  cancel(): void { this.location.back(); }

  private loadTask(id: string): void {
    this.loadingTask = true;
    this.taskService.getTask(id).pipe(finalize(() => this.loadingTask = false)).subscribe({
      next: task => this.form.patchValue({
        title: task.title,
        description: task.description,
        status: task.status,
        dueDate: task.dueDate.substring(0, 10)
      }),
      error: () => this.error = 'Unable to load the task.'
    });
  }

  private invalid(controlName: 'title' | 'description' | 'status' | 'dueDate'): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  private static readonly futureDateValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
    if (!control.value) return null;
    const selected = new Date(`${control.value}T23:59:59`);
    return selected >= new Date() ? null : { pastDate: true };
  };
}
