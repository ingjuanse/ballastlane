import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'ft-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <main class="login-page min-vh-100 d-flex align-items-center py-4">
      <div class="container">
        <div class="row justify-content-center">
          <div class="col-12 col-sm-10 col-md-7 col-lg-5 col-xl-4">
            <section class="card border-0 shadow-sm">
              <div class="card-body p-4 p-md-5">
                <div class="text-center mb-4">
                  <div class="login-brand mx-auto mb-3">F</div>
                  <h1 class="h3 mb-2">Welcome to FlowTrack</h1>
                  <p class="text-secondary mb-0">Sign in to manage your work.</p>
                </div>

                @if (error) {
                  <div class="alert alert-danger" role="alert">{{ error }}</div>
                }

                <form [formGroup]="form" (ngSubmit)="submit()" novalidate>
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
                      autocomplete="current-password"
                      formControlName="password"
                      placeholder="Enter your password">
                    @if (passwordInvalid) {
                      <div class="invalid-feedback">Enter your password.</div>
                    }
                  </div>

                  <button class="btn btn-primary w-100" type="submit" [disabled]="form.invalid || loading">
                    @if (loading) {
                      <span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>
                      Signing in...
                    } @else {
                      Sign in
                    }
                  </button>
                </form>

                <div class="text-center mt-4">
                  <span class="text-secondary">New to FlowTrack?</span>
                  <a class="btn btn-outline-primary w-100 mt-2" routerLink="/register">Create an account</a>
                </div>
              </div>
            </section>
          </div>
        </div>
      </div>
    </main>
  `,
  styles: [`
    .login-page { background: linear-gradient(135deg, #eff6ff 0%, #f8fafc 55%, #eef2ff 100%); }
    .login-brand {
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
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', Validators.required]
  });

  loading = false;
  error = '';

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
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/dashboard';

    this.auth.login(this.form.getRawValue()).pipe(
      finalize(() => this.loading = false)
    ).subscribe({
      next: () => void this.router.navigateByUrl(returnUrl),
      error: () => this.error = 'Unable to sign in. Check your email and password and try again.'
    });
  }
}
