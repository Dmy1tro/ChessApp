import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { AuthService } from './auth.service';
import { Observable, Subject, from, of } from 'rxjs';
import { environment } from 'src/environments/environment';
import { Side } from '../app.component';

export enum ChessHubEvent {
  GameStarted = 'GameStarted',
  GameFinished = 'GameFinished',
  OnMove = 'OnMove'
}

export enum ChessHubRequest {
  SearchGame = 'SearchGame',
  CancelSearch = 'CancelSearch',
  Move = 'Move'
}

@Injectable({
  providedIn: 'root'
})
export class ChessService {
  private connection?: HubConnection;

  private _gameStarted = new Subject<{ gameId: string, side: Side }>();
  private _onMove = new Subject<{ gameId: string, move: string }>();
  private _gameFinished = new Subject<{ gameId: string, win: boolean }>();

  public gameStarted$ = this._gameStarted.asObservable();
  public onMove$ = this._onMove.asObservable();
  public gameFinished$ = this._gameFinished.asObservable();

  constructor(private readonly authService: AuthService) { }

  getConnectionStatus(): HubConnectionState | undefined {
    return this.connection?.state
  }

  connect(): Observable<any> {
    console.log('Connecting to PaintHub...');

    if (this.connection) {
      return of({});
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(`${environment.baseApiUrl}/api/chess`, { 
        accessTokenFactory: () => this.authService.getToken()
      })
      .build();

    this.connection.on(ChessHubEvent.GameStarted, (gameId: string, side: Side) => {
      this._gameStarted.next({ gameId, side });
    });

    this.connection.on(ChessHubEvent.OnMove, (gameId: string, move: string) => {
      this._onMove.next({ gameId, move });
    });

    this.connection.on(ChessHubEvent.GameFinished, (gameId: string, win: boolean) => {
      this._gameFinished.next({ gameId, win });
    });

    return from(this.connection.start());
  }

  searchGame(side: Side): void {
    this.connection?.send(ChessHubRequest.SearchGame, side);
  }

  cancelSearch(): void {
    this.connection?.send(ChessHubRequest.CancelSearch);
  }

  move(gameId: string, move: string) {
    this.connection?.send(ChessHubRequest.Move, gameId, move);
  }
}