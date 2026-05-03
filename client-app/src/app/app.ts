import { Component, effect, inject, OnDestroy } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterOutlet, RouterLinkActive } from '@angular/router';
import { Subscription, filter } from 'rxjs';
import { AuthService } from './shared/services/auth.service';
import { BffService } from './shared/services/bff.service';
import { SignalRService } from './shared/services/signalr.service';
import { ToastService } from './shared/services/toast.service';

const STATUS_LABELS: Record<
  string,
  { label: string; type: 'success' | 'info' | 'warning' | 'danger' }
> = {
  Pending: { label: 'Order received — processing payment...', type: 'info' },
  PaymentProcessed: { label: 'Payment confirmed — shipping your order!', type: 'success' },
  PaymentFailed: { label: 'Payment failed. Please try again.', type: 'danger' },
  Shipped: { label: 'Your order has been shipped!', type: 'success' },
  Failed: { label: 'Order processing failed.', type: 'danger' },
};

const TERMINAL_STATUSES = new Set(['Shipped', 'PaymentFailed', 'Failed']);

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnDestroy {
  readonly auth = inject(AuthService);
  readonly toast = inject(ToastService);
  private readonly signalr = inject(SignalRService);
  private readonly bff = inject(BffService);
  private readonly router = inject(Router);
  private toastSub: Subscription | null = null;
  private routerSub: Subscription | null = null;
  /** Groups joined at app level — persist across route changes. */
  private watchedOrderIds = new Set<number>();

  constructor() {
    // Show toasts for every status event, app-wide
    this.toastSub = this.signalr.orderStatus$.subscribe(({ orderId, status }) => {
      const info = STATUS_LABELS[status];
      const msg = info ? info.label : `Status: ${status}`;
      const type = info?.type ?? 'info';
      this.toast.show(`Order #${orderId}: ${msg}`, type);
      // Leave group once order reaches a terminal state — no more events expected
      if (TERMINAL_STATUSES.has(status) && this.watchedOrderIds.has(orderId)) {
        this.watchedOrderIds.delete(orderId);
        this.signalr.leaveOrderGroup(orderId).catch(() => {});
      }
    });

    // Connect + join active order groups as soon as the user logs in
    effect(() => {
      if (this.auth.isLoggedIn()) {
        this.signalr
          .connect()
          .then(() => this.joinActiveOrders())
          .catch(() => {});
      }
    });

    // Re-scan for new non-terminal orders on every navigation (catches orders placed mid-session)
    this.routerSub = this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe(() => {
        if (this.auth.isLoggedIn()) {
          this.joinActiveOrders();
        }
      });
  }

  /** Called to sync group memberships with current non-terminal orders. */
  private joinActiveOrders(): void {
    this.bff.getOrders().subscribe({
      next: (orders) => {
        for (const o of orders) {
          if (!TERMINAL_STATUSES.has(o.status ?? '') && o.id != null) {
            if (!this.watchedOrderIds.has(o.id)) {
              this.watchedOrderIds.add(o.id);
              this.signalr.joinOrderGroup(o.id).catch(() => {});
            }
          }
        }
      },
    });
  }

  ngOnDestroy(): void {
    this.toastSub?.unsubscribe();
    this.routerSub?.unsubscribe();
  }
}
