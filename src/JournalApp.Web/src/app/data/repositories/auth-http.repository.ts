import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { API_BASE_URL } from '../../core/config/api.config';
import { AuthRepository } from '../../domain/repositories/auth.repository';
import { AuthSession, LoginCredentials, Registration, User } from '../../domain/models/user';
import { AuthResponseDto, LoginRequestDto, RegisterRequestDto, UserDto } from '../dtos/auth.dto';
import { mapHttpErrors } from '../mappers/error.mapper';
import { toAuthSession, toUser } from '../mappers/user.mapper';

@Injectable()
export class AuthHttpRepository extends AuthRepository {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_BASE_URL)}/auth`;

  login(credentials: LoginCredentials): Observable<AuthSession> {
    const body: LoginRequestDto = {
      email: credentials.email.trim(),
      password: credentials.password,
    };
    return this.http
      .post<AuthResponseDto>(`${this.baseUrl}/login`, body)
      .pipe(map(toAuthSession), mapHttpErrors());
  }

  register(registration: Registration): Observable<User> {
    const body: RegisterRequestDto = {
      username: registration.username.trim(),
      email: registration.email.trim(),
      password: registration.password,
    };
    return this.http
      .post<UserDto>(`${this.baseUrl}/register`, body)
      .pipe(map(toUser), mapHttpErrors());
  }

  me(): Observable<User> {
    return this.http.get<UserDto>(`${this.baseUrl}/me`).pipe(map(toUser), mapHttpErrors());
  }
}
