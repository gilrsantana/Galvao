import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { ShowroomListComponent } from './features/showroom/showroom-list/showroom-list.component';
import { ShowroomDetailComponent } from './features/showroom/showroom-detail/showroom-detail.component';
import { ArticleListComponent } from './features/articles/article-list/article-list.component';
import { ArticleDetailComponent } from './features/articles/article-detail/article-detail.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { ConfirmEmailComponent } from './features/auth/confirm-email/confirm-email.component';
import { AdminDashboardComponent } from './features/admin/admin-dashboard/admin-dashboard.component';
import { adminGuard } from './core/guards/admin.guard';
import { SettingsComponent } from './features/settings/settings.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent, title: 'Início | Galvão Design' },
  { path: 'settings', component: SettingsComponent, canActivate: [authGuard], title: 'Configurações | Galvão' },
  { path: 'showroom', component: ShowroomListComponent, title: 'Showroom de Móveis Minimalistas | Galvão' },
  { path: 'showroom/:id', component: ShowroomDetailComponent, title: 'Detalhes do Produto | Galvão' },
  { path: 'articles', component: ArticleListComponent, title: 'Artigos & Revista de Decoração | Galvão' },
  { path: 'articles/:id', component: ArticleDetailComponent, title: 'Detalhes do Artigo | Galvão' },
  { path: 'login', component: LoginComponent, title: 'Entrar | Galvão' },
  { path: 'register', component: RegisterComponent, title: 'Criar Conta | Galvão' },
  { path: 'confirm-email', component: ConfirmEmailComponent, title: 'Confirmar E-mail | Galvão' },
  { 
    path: 'admin', 
    component: AdminDashboardComponent, 
    canActivate: [adminGuard],
    title: 'Painel Administrativo | Galvão'
  },
  { path: '**', redirectTo: '' }
];
