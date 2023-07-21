import { Component, OnInit, ViewChild, AfterViewInit, OnDestroy } from '@angular/core';
import { NgxChessBoardView } from 'ngx-chess-board';
import { MoveEvent } from './models/moveEvent';
import { ChessService } from './services/chess.service';
import { Subject, first, switchMap, takeUntil } from 'rxjs';
import { AuthService } from './services/auth.service';
import { HubConnectionState } from '@microsoft/signalr';

export enum Side {
  Undefined = 0,
  White = 1,
  Black = 2
}

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit, OnDestroy {
  @ViewChild('board') board!: NgxChessBoardView;

  playerSide: Side = Side.Undefined;
  lightDisabled = false;
  darkDisabled = false;

  isSearchingGame: boolean = false;
  isGameStarted: boolean = false;
  isGameFinished: boolean = false;
  gameId?: string;
  matchResult: string = '';

  Side = Side;

  private destroy$ = new Subject<void>();

  constructor(private readonly authService: AuthService,
              private readonly chessService: ChessService) {}

  get hubConnected(): boolean {
    return this.chessService.getConnectionStatus() === HubConnectionState.Connected;
  }

  ngOnInit(): void {
    this.authService.authorize()
      .pipe(
        switchMap(() => this.chessService.connect()),
        first()
      ).subscribe();

    this.chessService.gameStarted$
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ gameId, side }) => {
        this.gameId = gameId;
        this.playerSide = side;

        if (side === Side.Black) {
          this.board.reverse();
          this.lightDisabled = true;
          this.darkDisabled = false
        } else {
          this.lightDisabled = false;
          this.darkDisabled = true;
        }

        this.isSearchingGame = false;
        this.isGameStarted = true;
      });

    this.chessService.onMove$
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ gameId, move }) => {
        this.board.move(move);
      });

    this.chessService.gameFinished$
      .pipe(takeUntil(this.destroy$))
      .subscribe(({ gameId, win }) => {
        this.isGameFinished = true;

        if (win) {
          this.matchResult = `${Side[this.playerSide]} win!`;
        } else {
          const opponentSide = this.playerSide === Side.White ? Side.Black : Side.White;
          this.matchResult = `${Side[opponentSide]} win!`;
        }
      })
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onMoveChange($event: Partial<MoveEvent>): void {
    const side = $event.color === 'white' ? Side.White : Side.Black;

    if (this.playerSide === side) {
      this.chessService.move(this.gameId!, $event.move!);
    }

    if ($event.stalemate || $event.checkmate) {
      this.isGameFinished = true;

      if ($event.stalemate) {
        this.matchResult = 'Stalemate'
      }
      
      this.matchResult = `${Side[side]} win!`;
    }
  }

  startGame(): void {
    this.chessService.searchGame(this.playerSide);
    this.isSearchingGame = true;
  }

  cancelSearchingGame(): void {
    this.chessService.cancelSearch();
    this.isSearchingGame = false;
  }

  closeGame(): void {
    this.matchResult = '';
    this.gameId = '';
    this.isGameStarted = false;
    this.isSearchingGame = false;
    this.isGameFinished = false;
    this.board.reset();
  }
}
