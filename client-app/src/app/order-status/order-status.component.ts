import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Subscription } from 'rxjs';
import { BffService } from '../shared/services/bff.service';
import { SignalRService } from '../shared/services/signalr.service';
import type { Order } from '../shared/models';

const STATUS_CLASSES: Record<string, string> = {
  Pending: 'badge bg-secondary',
  PaymentFailed: 'badge bg-danger',
  PaymentProcessed: 'badge bg-info text-dark',
  Shipped: 'badge bg-success',
  Failed: 'badge bg-danger',
};

const TERMINAL_STATUSES = new Set(['Shipped', 'PaymentFailed', 'Failed']);

@Component({
  selector: 'app-order-status',
  imports: [DatePipe],
  templateUrl: './order-status.component.html',
  styleUrl: './order-status.component.css',
})
export class OrderStatusComponent implements OnInit, OnDestroy {
  private readonly bff = inject(BffService);
  private readonly signalr = inject(SignalRService);

  readonly orders = signal<Order[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');
  readonly watchId = signal(1);
  readonly liveOrderId = signal<number | null>(null);
  readonly liveStatus = signal('');

  private statusSub: Subscription | null = null;

  ngOnInit(): void {
    // Subscribe to the hot Observable to keep table in sync
    this.statusSub = this.signalr.orderStatus$.subscribe(({ orderId, status }) => {
      this.liveOrderId.set(orderId);
      this.liveStatus.set(status);
      this.orders.update((list) => list.map((o) => (o.id === orderId ? { ...o, status } : o)));
    });

    this.bff.getOrders().subscribe({
      next: (data) => {
        this.orders.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load orders.');
        this.loading.set(false);
      },
    });
  }

  async watch(): Promise<void> {
    const current = this.liveOrderId();
    if (current) await this.signalr.leaveOrderGroup(current);
    this.liveOrderId.set(this.watchId());
    this.liveStatus.set('');
    await this.signalr.joinOrderGroup(this.watchId());
  }

  async unwatch(): Promise<void> {
    const current = this.liveOrderId();
    if (current) {
      await this.signalr.leaveOrderGroup(current);
      this.liveOrderId.set(null);
      this.liveStatus.set('');
    }
  }

  badgeClass(status: string): string {
    return STATUS_CLASSES[status] ?? 'badge bg-secondary';
  }

  ngOnDestroy(): void {
    this.statusSub?.unsubscribe();
    // Group membership is managed by AppComponent — do NOT leave groups here
  }
}
