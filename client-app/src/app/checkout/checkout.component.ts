import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { BffService } from '../shared/services/bff.service';
import { CryptoService } from '../shared/services/crypto.service';
import type { Product } from '../shared/models';

@Component({
  selector: 'app-checkout',
  imports: [DecimalPipe],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css',
})
export class CheckoutComponent implements OnInit {
  private readonly bff = inject(BffService);
  private readonly crypto = inject(CryptoService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly product = signal<Product | null>(null);
  readonly quantity = signal(1);
  readonly scenario = signal<'valid' | 'decline'>('valid');
  readonly loading = signal(true);
  readonly placing = signal(false);
  readonly loadError = signal('');
  readonly orderError = signal('');

  readonly quantityError = computed(() => {
    const p = this.product();
    if (!p) return '';
    const q = this.quantity();
    if (q < 1) return 'Quantity must be at least 1.';
    if (q > p.stockQuantity) return `Only ${p.stockQuantity} in stock.`;
    return '';
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('productId'));
    this.bff.getProduct(id).subscribe({
      next: (p) => {
        this.product.set(p);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('Product not found.');
        this.loading.set(false);
      },
    });
  }

  async placeOrder(): Promise<void> {
    const p = this.product();
    if (!p || this.quantityError()) return;
    this.orderError.set('');
    this.placing.set(true);

    const uuid = crypto.randomUUID();
    const rawToken = this.scenario() === 'valid' ? `tok_valid_${uuid}` : `tok_fail_${uuid}`;

    let encryptedToken: string;
    try {
      encryptedToken = await this.crypto.encrypt(rawToken);
    } catch {
      this.orderError.set('Encryption failed. Please refresh and try again.');
      this.placing.set(false);
      return;
    }

    this.bff
      .createOrder({
        productId: p.id,
        quantity: this.quantity(),
        unitPrice: p.price,
        paymentToken: encryptedToken,
      })
      .subscribe({
        next: () => this.router.navigate(['/order-placed']),
        error: (err) => {
          this.orderError.set(err.error?.message ?? 'Failed to place order.');
          this.placing.set(false);
        },
      });
  }
}
