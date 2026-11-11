import { User } from './user.model';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest extends LoginRequest {
  name: string;
}

export interface AuthResponse {
  userId: number;
  name: string;
  email: string;
  accessToken: string;
  expiresAt: string;
}

export interface AuthSession {
  user: User;
  accessToken: string;
  expiresAt: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}
