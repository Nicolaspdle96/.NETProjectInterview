export interface User {
  readonly id: string;
  readonly username: string;
  readonly email: string;
  readonly createdAt: Date;
}

export interface AuthSession {
  readonly token: string;
  readonly expiresAt: Date;
  readonly user: User;
}

export interface LoginCredentials {
  readonly email: string;
  readonly password: string;
}

export interface Registration {
  readonly username: string;
  readonly email: string;
  readonly password: string;
}

/** Business rules shared with the backend. */
export const USER_RULES = {
  usernameMinLength: 3,
  usernameMaxLength: 30,
  passwordMinLength: 8,
  emailPattern: /^[^@\s]+@[^@\s]+\.[^@\s]+$/,
} as const;
