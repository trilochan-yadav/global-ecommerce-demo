import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { BffService } from '../shared/services/bff.service';
import type { ClickEvent, Conversion } from '../shared/models';

@Component({
  selector: 'app-analytics',
  imports: [DatePipe],
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.css',
})
export class AnalyticsComponent implements OnInit {
  private readonly bff = inject(BffService);

  readonly clicks = signal<ClickEvent[]>([]);
  readonly conversions = signal<Conversion[]>([]);
  readonly loadingClicks = signal(true);
  readonly loadingConversions = signal(true);
  readonly error = signal('');

  ngOnInit(): void {
    this.bff.getAnalyticsClicks().subscribe({
      next: (d) => {
        this.clicks.set(d);
        this.loadingClicks.set(false);
      },
      error: () => {
        this.error.set('Failed to load analytics.');
        this.loadingClicks.set(false);
      },
    });
    this.bff.getAnalyticsConversions().subscribe({
      next: (d) => {
        this.conversions.set(d);
        this.loadingConversions.set(false);
      },
      error: () => {
        this.loadingConversions.set(false);
      },
    });
  }
}
