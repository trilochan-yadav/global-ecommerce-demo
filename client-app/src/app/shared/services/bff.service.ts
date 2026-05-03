import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Product, Order, CreateOrderRequest, ClickEvent, Conversion } from '../models';

@Injectable({ providedIn: 'root' })
export class BffService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.bffBaseUrl;

  // /api/internal/* is API-key protected, not JWT
  private readonly internalHeaders = new HttpHeaders({
    'X-Api-Key': 'bff-internal-key-123',
  });

  private wrap<T>(obs: Observable<{ success: boolean; message: string; data: T }>): Observable<T> {
    return obs.pipe(map((r) => r.data));
  }

  // Products
  getProducts(): Observable<Product[]> {
    return this.wrap(
      this.http.get<{ success: boolean; message: string; data: Product[] }>(
        `${this.base}/api/products`,
      ),
    );
  }

  getProduct(id: number): Observable<Product> {
    return this.wrap(
      this.http.get<{ success: boolean; message: string; data: Product }>(
        `${this.base}/api/products/${id}`,
      ),
    );
  }

  // Orders
  createOrder(req: CreateOrderRequest): Observable<{ success: boolean; message: string }> {
    return this.http.post<{ success: boolean; message: string }>(`${this.base}/api/orders`, req);
  }

  getOrders(): Observable<Order[]> {
    return this.wrap(
      this.http.get<{ success: boolean; message: string; data: Order[] }>(
        `${this.base}/api/orders`,
      ),
    );
  }

  // Analytics (Admin only)
  getAnalyticsClicks(): Observable<ClickEvent[]> {
    return this.wrap(
      this.http.get<{ success: boolean; message: string; data: ClickEvent[] }>(
        `${this.base}/api/analytics/clicks`,
      ),
    );
  }

  getAnalyticsConversions(): Observable<Conversion[]> {
    return this.wrap(
      this.http.get<{ success: boolean; message: string; data: Conversion[] }>(
        `${this.base}/api/analytics/conversions`,
      ),
    );
  }

  // Internal logs (Admin only — API-key protected, not JWT)
  getLogs(service: string, lines: number): Observable<string[]> {
    return this.http.get<string[]>(
      `${this.base}/api/internal/logs?service=${encodeURIComponent(service)}&lines=${lines}`,
      { headers: this.internalHeaders },
    );
  }
}
