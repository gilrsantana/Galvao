import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'showroom/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'articles/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'settings',
    renderMode: RenderMode.Server
  },
  {
    path: 'admin',
    renderMode: RenderMode.Server
  },
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
