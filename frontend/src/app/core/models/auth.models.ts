export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiration: string;
}

export interface LoginRequest {
  email: string;
  password?: string; // Optional if handled via alternative flows, but let's make it standard
}

export interface RegisterRequest {
  email: string;
  password?: string;
  displayName: string;
  firstName: string;
  lastName: string;
}
