export interface UserDto {
  id: string;
  username: string;
  email: string;
  createdAt: string;
}

export interface AuthResponseDto {
  token: string;
  expiresAt: string;
  user: UserDto;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

export interface RegisterRequestDto {
  username: string;
  email: string;
  password: string;
}
