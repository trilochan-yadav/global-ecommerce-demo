import { Component, inject, OnInit, signal } from '@angular/core';
import { BffService } from '../shared/services/bff.service';

const SERVICES = [
  'BFF.API',
  'Product.API',
  'Order.API',
  'Payment.API',
  'Shipping.API',
  'Analytics.API',
];

const LEVEL_CLASSES: Record<string, string> = {
  INF: 'text-success',
  WRN: 'text-warning',
  ERR: 'text-danger',
  DBG: 'text-muted',
};

@Component({
  selector: 'app-logs',
  imports: [],
  templateUrl: './logs.component.html',
  styleUrl: './logs.component.css',
})
export class LogsComponent implements OnInit {
  private readonly bff = inject(BffService);

  readonly services = SERVICES;
  readonly selectedService = signal('BFF.API');
  readonly lines = signal(100);
  readonly logLines = signal<string[]>([]);
  readonly loading = signal(false);
  readonly error = signal('');

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error.set('');
    this.loading.set(true);
    this.bff.getLogs(this.selectedService(), this.lines()).subscribe({
      next: (data) => {
        this.logLines.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load logs.');
        this.loading.set(false);
      },
    });
  }

  lineClass(line: string): string {
    for (const [level, cls] of Object.entries(LEVEL_CLASSES)) {
      if (line.includes(`[${level}]`) || line.includes(` ${level} `)) return cls;
    }
    return 'text-light';
  }
}
