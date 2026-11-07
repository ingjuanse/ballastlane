import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'ft-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="register-page min-vh-100 d-flex align-items-center py-4">
      <div class="container">
        <div class="row justify-content-center">
          <div class="col-12 col-sm-10 col-md-7 col-lg-5 col-xl-4">
            <section class="card border-0 shadow-sm">
              <div class="card-body p-4 p-md-5">
                <div class="text-center mb-4">
                  <div class="register-brand mx-auto mb-3">F</div>
                  <h1 class="h3 mb-2">Create your account</h1>
                  <p class="text-secondary mb-0">Get started with FlowTrack.</p>
                </div>

                @if (registered) {
                  <div class="alert alert-success" role="status">Your account has been created. You can now sign in.</div>
                  <a class="btn btn-primary w-100" routerLink="/login">Go to sign in</a>
                } @else {
                  @if (error) {
                    <div class="alert alert-danger" role="alert">{{ error }}</div>
                  }

                  <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
                    <div class="mb-3">
                      <label class="form-label" for="userName">Username</label>
                      <input
                        id="userName"
                        class="form-control"
                        [class.is-invalid]="userNameInvalid"
                        type="text"
                        autocomplete="username"
                        formControlName="userName"
                        placeholder="Choose a username">
                      @if (userNameInvalid) {
                        <div class="invalid-feedback">Username must be between 3 and 100 characters.</div>
                      }
                    </div>

                    <div class="mb-3">
                      <label class="form-label" for="email">Email address</label>
                      <input
                        id="email"
                        class="form-control"
                        [class.is-invalid]="emailInvalid"
                        type="email"
                        autocomplete="email"
                        formControlName="email"
                        placeholder="you@example.com">
                      @if (emailInvalid) {
                        <div class="invalid-feedback">Enter a valid email address with no more than 254 characters.</div>
                      }
                    </div>

                    <div class="mb-4">
                      <label class="form-label" for="password">Password</label>
                      <input
                        id="password"
                        class="form-control"
                        [class.is-invalid]="passwordInvalid"
                        type="password"
                        autocomplete="new-password"
                        formControlName="password"
                        placeholder="Create a password">
                      @if (passwordInvalid) {
                        <div class="invalid-feedback">Use at least 8 characters with uppercase, lowercase, and a number.</div>
                      }
                    </div>

                    <button class="btn btn-primary w-100" type="submit" [disabled]="form.invalid || loading">
                      @if (loading) {
                        <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
                        Creating account...
                      } @else {
                        Create account
                      }
                    </button>
                  </form>

                  <div class="text-center mt-4">
                    <span class="text-secondary">Already have an account?</span>
                    <a class="btn btn-outline-primary w-100 mt-2" routerLink="/login">Sign in</a>
                  </div>
                }
              </div>
            </section>
          </div>
        </div>
      </div>
    </main>
  `,
  styles: [`
    .register-page { background: linear-gradient(135deg, #eff6ff 0%, #f8fafc 55%, #eef2ff 100%); }
    .register-brand {
      width: 3rem;
      height: 3rem;
      display: grid;
      place-items: center;
      border-radius: .75rem;
      background: var(--ft-primary);
      color: #fff;
      font-size: 1.5rem;
      font-weight: 700;
    }
  `]
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);

  readonly form = this.fb.nonNullable.group({
    userName: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$/)
    ]]
  });

  loading = false;
  registered = false;
  error = '';

  get userNameInvalid(): boolean {
    const control = this.form.controls.userName;
    return control.invalid && (control.dirty || control.touched);
  }

  get emailInvalid(): boolean {
    const control = this.form.controls.email;
    return control.invalid && (control.dirty || control.touched);
  }

  get passwordInvalid(): boolean {
    const control = this.form.controls.password;
    return control.invalid && (control.dirty || control.touched);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.error = '';
    this.auth.register(this.form.getRawValue()).pipe(
      finalize(() => this.loading = false)
    ).subscribe({
      next: () => this.registered = true,
      error: () => this.error = 'Unable to create your account. The email or username may already be registered.'
    });
  }
}
