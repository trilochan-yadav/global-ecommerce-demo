import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  // All routes use client rendering — auth requires browser localStorage
  { path: '**', renderMode: RenderMode.Client },
];
