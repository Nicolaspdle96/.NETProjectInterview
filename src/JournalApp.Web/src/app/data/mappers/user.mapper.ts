import { AuthResponseDto, UserDto } from '../dtos/auth.dto';
import { AuthSession, User } from '../../domain/models/user';

export function toUser(dto: UserDto): User {
  return {
    id: dto.id,
    username: dto.username,
    email: dto.email,
    createdAt: new Date(dto.createdAt),
  };
}

export function toAuthSession(dto: AuthResponseDto): AuthSession {
  return {
    token: dto.token,
    expiresAt: new Date(dto.expiresAt),
    user: toUser(dto.user),
  };
}
