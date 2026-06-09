import { Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { ShowroomListComponent } from './features/showroom/showroom-list/showroom-list.component';
import { ShowroomDetailComponent } from './features/showroom/showroom-detail/showroom-detail.component';
import { ArticleListComponent } from './features/articles/article-list/article-list.component';
import { ArticleDetailComponent } from './features/articles/article-detail/article-detail.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { AdminDashboardComponent } from './features/admin/admin-dashboard/admin-dashboard.component';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'showroom', component: ShowroomListComponent },
  { path: 'showroom/:id', component: ShowroomDetailComponent },
  { path: 'articles', component: ArticleListComponent },
  { path: 'articles/:id', component: ArticleDetailComponent },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { 
    path: 'admin', 
    component: AdminDashboardComponent, 
    canActivate: [adminGuard] 
  },
  { path: '**', redirectTo: '' }
];
