import { Observable } from 'rxjs';
import { AuthResult, User } from '../models';

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone: string;
  password: string;
  currencyCode: string;
}

export interface LoginRequest {
  identifier: string;
  password: string;
}

export interface UpdateMeRequest {
  fullName?: string;
  phone?: string;
  currencyCode?: string;
}

export abstract class AuthRepository {
  abstract register(req: RegisterRequest, photo?: File): Observable<AuthResult>;
  abstract login(req: LoginRequest): Observable<AuthResult>;
  abstract refresh(refreshToken: string): Observable<AuthResult>;
  abstract logout(refreshToken: string): Observable<void>;
  abstract me(): Observable<User>;
  abstract updateMe(p: UpdateMeRequest): Observable<User>;
  abstract uploadPhoto(file: File): Observable<void>;
  abstract photoUrl(userId: string): string;
}
