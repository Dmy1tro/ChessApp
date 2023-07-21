export interface MoveEvent {
    move: string,
    piece: string,
    color: string,
    x: boolean,
    check: boolean,
    stalemate: boolean,
    mate: boolean,
    checkmate: boolean,
    fen: string,
    pgn: { pgn: string },
    freeMode: boolean
}