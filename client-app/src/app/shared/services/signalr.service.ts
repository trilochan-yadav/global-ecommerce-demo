import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

export interface OrderStatusEvent {
  orderId: number;
  status: string;
}

@Injectable({ providedIn: 'root' })
export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private readonly base = environment.bffBaseUrl;

  /** Hot observable — emits every OrderStatusUpdated push from the server. */
  private readonly _orderStatus = new Subject<OrderStatusEvent>();
  readonly orderStatus$ = this._orderStatus.asObservable();

  constructor(private auth: AuthService) {}

  async connect(): Promise<void> {
    if (this.connection && this.connection.state !== signalR.HubConnectionState.Disconnected) {
      return; // already connected or connecting
    }
    const token = this.auth.getToken() ?? '';
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${this.base}/hubs/order-status`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    // Register the subject push BEFORE start() — persists through reconnects too
    this.connection.on('OrderStatusUpdated', (orderId: string, status: string) => {
      this._orderStatus.next({ orderId: parseInt(orderId, 10), status });
    });

    await this.connection.start();
  }

  async joinOrderGroup(orderId: number): Promise<void> {
    await this.connection?.invoke('JoinOrderGroup', orderId.toString());
  }

  async leaveOrderGroup(orderId: number): Promise<void> {
    await this.connection?.invoke('LeaveOrderGroup', orderId.toString());
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
  }
}
