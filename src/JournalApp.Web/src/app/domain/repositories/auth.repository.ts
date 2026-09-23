import { Observable } from 'rxjs';
import { AuthSession, LoginCredentials, Registration, User } from '../models/user';

/** Contract used as a DI token; bound to its HTTP implementation in app.config.ts. */
export abstract class AuthRepository {
  abstract login(credentials: LoginCredentials): Observable<AuthSession>;
  abstract register(registration: Registration): Observable<User>;
  abstract me(): Observable<User>;
}
