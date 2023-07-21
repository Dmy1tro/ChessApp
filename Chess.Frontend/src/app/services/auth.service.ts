import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly authUrl = `${environment.baseApiUrl}/api/authorize`;

  private token: string = '';

  constructor(private httpClient: HttpClient) { }

  authorize(): Observable<void> {
    return this.httpClient.post<{ idToken: string }>(`${this.authUrl}/authorize`, {})
      .pipe(
        map(tokenHolder => {
          this.token = tokenHolder.idToken;
        })
      );
  }

  getToken(): string {
    return this.token;
  }
}
