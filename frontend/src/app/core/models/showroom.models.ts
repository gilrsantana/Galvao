export interface ShowroomItemPhotoResponse {
  id: string;
  url: string;
  caption: string;
  isPrimary: boolean;
}

export interface ShowroomItemResponse {
  id: string;
  title: string;
  description: string;
  price: number;
  category: string;
  active: boolean;
  createdAt: string;
  updatedAt?: string;
  photos: ShowroomItemPhotoResponse[];
}

export interface CreateShowroomItemRequest {
  title: string;
  description: string;
  price: number;
  category: string;
}

export interface UpdateShowroomItemRequest {
  title: string;
  description: string;
  price: number;
  category: string;
}

export interface AddShowroomItemPhotoRequest {
  url: string;
  caption: string;
  isPrimary: boolean;
}
