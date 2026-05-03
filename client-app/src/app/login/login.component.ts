import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../shared/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly username = signal('');
  readonly password = signal('');
  readonly error = signal('');
  readonly loading = signal(false);

  ngOnInit(): void {
    // If already logged in, skip the login page entirely
    if (this.auth.isLoggedIn()) {
      this.router.navigate(['/products']);
      return;
    }
    // Pre-fill from query params and auto-submit (useful for demo links)
    const params = this.route.snapshot.queryParamMap;
    const u = params.get('username');
    const p = params.get('password');
    if (u && p) {
      this.username.set(u);
      this.password.set(p);
      this.login();
    }
  }

  login(): void {
    this.error.set('');
    this.loading.set(true);
    this.auth.login(this.username(), this.password()).subscribe({
      next: (res) => {
        this.auth.storeToken(res.token);
        this.router.navigate(['/products']);
      },
      error: () => {
        this.error.set('Invalid username or password.');
        this.loading.set(false);
      },
    });
  }
}
