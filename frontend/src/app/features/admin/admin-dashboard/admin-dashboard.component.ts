import { Component, OnInit, inject, signal } from '@angular/core';
import { ShowroomService } from '../../../core/services/showroom.service';
import { ArticleService } from '../../../core/services/article.service';
import { RoleService } from '../../../core/services/role.service';
import { AuthService } from '../../../core/services/auth.service';
import { ShowroomItemResponse } from '../../../core/models/showroom.models';
import { ArticleResponse } from '../../../core/models/article.models';
import { RoleResponse } from '../../../core/models/role.models';
import { FormsModule } from '@angular/forms';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { AdminButtonComponent } from '../components/admin-button.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [FormsModule, CurrencyPipe, DatePipe, AdminButtonComponent],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.css'
})
export class AdminDashboardComponent implements OnInit {
  private readonly showroomService = inject(ShowroomService);
  private readonly articleService = inject(ArticleService);
  private readonly roleService = inject(RoleService);
  private readonly authService = inject(AuthService);

  // Active Tab
  readonly activeTab = signal<'showroom' | 'articles' | 'roles'>('showroom');

  // Lists Signals
  readonly showroomItems = signal<ShowroomItemResponse[]>([]);
  readonly articles = signal<ArticleResponse[]>([]);
  readonly roles = signal<RoleResponse[]>([]);

  // Modals / Forms Open Status
  readonly showItemModal = signal<boolean>(false);
  readonly showPhotoModal = signal<boolean>(false);
  readonly showArticleModal = signal<boolean>(false);
  readonly showRoleModal = signal<boolean>(false);

  // Form State - Showroom Item
  readonly editingItemId = signal<string | null>(null);
  readonly itemTitle = signal<string>('');
  readonly itemDescription = signal<string>('');
  readonly itemPrice = signal<number>(0);
  readonly itemCategory = signal<string>('');

  // Form State - Photos
  readonly currentItemForPhotos = signal<ShowroomItemResponse | null>(null);
  readonly editingPhotoId = signal<string | null>(null);
  readonly photoUrl = signal<string>('');
  readonly photoCaption = signal<string>('');
  readonly photoIsPrimary = signal<boolean>(false);

  // Form State - Article
  readonly editingArticleId = signal<string | null>(null);
  readonly articleTitle = signal<string>('');
  readonly articleContent = signal<string>('');
  readonly articleAuthor = signal<string>('');

  // Form State - Role Creation & Assignment
  readonly newRoleName = signal<string>('');
  readonly newRoleDescription = signal<string>('');
  readonly assignUserId = signal<string>('');
  readonly assignRoleName = signal<string>('');

  ngOnInit() {
    this.loadShowroomItems();
    this.loadArticles();
    this.loadRoles();
  }

  // TAB HANDLING
  switchTab(tab: 'showroom' | 'articles' | 'roles') {
    this.activeTab.set(tab);
  }

  // --- SHOWROOM CRUD ---
  loadShowroomItems() {
    this.showroomService.getPaged(1, 100).subscribe({
      next: (res) => this.showroomItems.set(res.items),
      error: () => console.error('Failed to load admin showroom items')
    });
  }

  openNewItemModal() {
    this.editingItemId.set(null);
    this.itemTitle.set('');
    this.itemDescription.set('');
    this.itemPrice.set(0);
    this.itemCategory.set('');
    this.showItemModal.set(true);
  }

  openEditItemModal(item: ShowroomItemResponse) {
    this.editingItemId.set(item.id);
    this.itemTitle.set(item.title);
    this.itemDescription.set(item.description);
    this.itemPrice.set(Number(item.price));
    this.itemCategory.set(item.category);
    this.showItemModal.set(true);
  }

  closeItemModal() {
    this.showItemModal.set(false);
  }

  saveItem() {
    const id = this.editingItemId();
    const request = {
      title: this.itemTitle(),
      description: this.itemDescription(),
      price: this.itemPrice(),
      category: this.itemCategory()
    };

    if (id) {
      this.showroomService.update(id, request).subscribe({
        next: () => {
          this.loadShowroomItems();
          this.closeItemModal();
        },
        error: (err) => console.error(err)
      });
    } else {
      this.showroomService.create(request).subscribe({
        next: () => {
          this.loadShowroomItems();
          this.closeItemModal();
        },
        error: (err) => console.error(err)
      });
    }
  }

  // --- SHOWROOM PHOTOS ---
  openPhotosModal(item: ShowroomItemResponse) {
    this.currentItemForPhotos.set(item);
    this.cancelPhotoEdit();
    this.showPhotoModal.set(true);
  }

  closePhotosModal() {
    this.showPhotoModal.set(false);
    this.currentItemForPhotos.set(null);
  }

  selectPhotoForEdit(photo: any) {
    this.editingPhotoId.set(photo.id);
    this.photoUrl.set(photo.url);
    this.photoCaption.set(photo.caption || '');
    this.photoIsPrimary.set(photo.isPrimary);
  }

  cancelPhotoEdit() {
    this.editingPhotoId.set(null);
    this.photoUrl.set('');
    this.photoCaption.set('');
    this.photoIsPrimary.set(false);
  }

  submitPhotoForm() {
    const item = this.currentItemForPhotos();
    if (!item) return;

    const photoId = this.editingPhotoId();
    const request = {
      url: this.photoUrl(),
      caption: this.photoCaption(),
      isPrimary: this.photoIsPrimary()
    };

    if (photoId) {
      this.showroomService.updatePhoto(item.id, photoId, request).subscribe({
        next: () => {
          this.showroomService.getById(item.id).subscribe((updated) => {
            this.currentItemForPhotos.set(updated);
            this.loadShowroomItems();
          });
          this.cancelPhotoEdit();
        },
        error: (err) => console.error(err)
      });
    } else {
      this.showroomService.addPhoto(item.id, request).subscribe({
        next: () => {
          this.showroomService.getById(item.id).subscribe((updated) => {
            this.currentItemForPhotos.set(updated);
            this.loadShowroomItems();
          });
          this.photoUrl.set('');
          this.photoCaption.set('');
          this.photoIsPrimary.set(false);
        },
        error: (err) => console.error(err)
      });
    }
  }

  removePhoto(photoId: string) {
    const item = this.currentItemForPhotos();
    if (!item) return;

    this.showroomService.removePhoto(item.id, photoId).subscribe({
      next: () => {
        this.showroomService.getById(item.id).subscribe((updated) => {
          this.currentItemForPhotos.set(updated);
          this.loadShowroomItems();
        });
      },
      error: (err) => console.error(err)
    });
  }

  // --- ARTICLES CRUD ---
  loadArticles() {
    this.articleService.getPaged(1, 100, false).subscribe({
      next: (res) => this.articles.set(res.items),
      error: () => console.error('Failed to load admin articles')
    });
  }

  openNewArticleModal() {
    this.editingArticleId.set(null);
    this.articleTitle.set('');
    this.articleContent.set('');
    this.articleAuthor.set('');
    this.showArticleModal.set(true);
  }

  openEditArticleModal(article: ArticleResponse) {
    this.editingArticleId.set(article.id);
    this.articleTitle.set(article.title);
    this.articleContent.set(article.content);
    this.articleAuthor.set(article.author);
    this.showArticleModal.set(true);
  }

  closeArticleModal() {
    this.showArticleModal.set(false);
  }

  saveArticle() {
    const id = this.editingArticleId();
    const request = {
      title: this.articleTitle(),
      content: this.articleContent(),
      author: this.articleAuthor()
    };

    if (id) {
      this.articleService.update(id, request).subscribe({
        next: () => {
          this.loadArticles();
          this.closeArticleModal();
        },
        error: (err) => console.error(err)
      });
    } else {
      this.articleService.create(request).subscribe({
        next: () => {
          this.loadArticles();
          this.closeArticleModal();
        },
        error: (err) => console.error(err)
      });
    }
  }

  togglePublish(article: ArticleResponse) {
    const nextPublishState = !article.isPublished;
    this.articleService.publish(article.id, nextPublishState).subscribe({
      next: () => this.loadArticles(),
      error: (err) => console.error(err)
    });
  }

  // --- ROLES CRUD ---
  loadRoles() {
    this.roleService.getAvailableRoles().subscribe({
      next: (res) => this.roles.set(res),
      error: () => console.error('Failed to load admin roles')
    });
  }

  openRoleModal() {
    this.newRoleName.set('');
    this.newRoleDescription.set('');
    this.showRoleModal.set(true);
  }

  closeRoleModal() {
    this.showRoleModal.set(false);
  }

  saveRole() {
    this.roleService.createRole({
      roleName: this.newRoleName(),
      description: this.newRoleDescription()
    }).subscribe({
      next: () => {
        this.loadRoles();
        this.closeRoleModal();
      },
      error: (err) => console.error(err)
    });
  }

  assignRole() {
    this.roleService.assignRole({
      userId: this.assignUserId(),
      roleName: this.assignRoleName()
    }).subscribe({
      next: () => {
        alert('Role assigned successfully!');
        this.assignUserId.set('');
        this.assignRoleName.set('');
      },
      error: (err) => alert('Failed to assign role. Make sure User ID exists.')
    });
  }

  removeRole() {
    this.roleService.removeRole({
      userId: this.assignUserId(),
      roleName: this.assignRoleName()
    }).subscribe({
      next: () => {
        alert('Role removed successfully!');
        this.assignUserId.set('');
        this.assignRoleName.set('');
      },
      error: (err) => alert('Failed to remove role.')
    });
  }

  openHangfire() {
    const tokens = this.authService.tokenResponse();
    if (tokens?.accessToken) {
      const url = `${environment.apiUrl}/api/admin/hangfire-redirect?token=${encodeURIComponent(tokens.accessToken)}`;
      window.open(url, '_blank');
    } else {
      alert('Authentication token is missing. Please log in again.');
    }
  }
}
