export interface ArticleResponse {
  id: string;
  title: string;
  content: string;
  author: string;
  isPublished: boolean;
  publishedAt?: string;
  active: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateArticleRequest {
  title: string;
  content: string;
  author: string;
}

export interface UpdateArticleRequest {
  title: string;
  content: string;
  author: string;
}

export interface PublishArticleRequest {
  publish: boolean;
}
