export interface MemberResponse {
  id: string;
  email: string;
  displayName: string;
  firstName: string;
  lastName: string;
  acceptNews: boolean;
  acceptPromo: boolean;
}

export interface UpdateProfileRequest {
  displayName: string;
  firstName: string;
  lastName: string;
}

export interface ChangeEmailRequest {
  newEmail: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface UpdateMarketingPreferencesRequest {
  acceptNews: boolean;
  acceptPromo: boolean;
  consentToken: string;
  consentedAt: string;
}
