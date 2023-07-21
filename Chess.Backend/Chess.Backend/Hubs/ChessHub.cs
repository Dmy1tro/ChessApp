using System.ComponentModel.DataAnnotations;
using Chess.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chess.Backend.Hubs
{
    [Authorize]
    public class ChessHub : Hub<IChessHubEvents>, IChessHubRequests
    {
        private readonly UserProvider _userProvider;
        private readonly GameManager _gameManager;

        public ChessHub(UserProvider userProvider, GameManager gameManager)
        {
            _userProvider = userProvider;
            _gameManager = gameManager;
        }

        public async Task SearchGame(PlayerSide side)
        {
            var userId = _userProvider.GetUserId();
            var game = _gameManager.FindPendingGame(side);

            if (game != null)
            {
                _gameManager.AddPlayer(game.Id, new Player
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = userId,
                    Side = side
                });

                _gameManager.StartGame(game.Id);

                var (player1, player2) = _gameManager.GetPlayers(game.Id);

                await Clients.Client(player1.ConnectionId).GameStarted(game.Id, player1.Side);
                await Clients.Client(player2.ConnectionId).GameStarted(game.Id, player2.Side);
            }
            else
            {
                game = _gameManager.CreateGame();
                _gameManager.AddPlayer(game.Id, new Player
                {
                    ConnectionId = Context.ConnectionId,
                    UserId = userId,
                    Side = side
                });
            }
        }

        public async Task CancelSearch()
        {
            _gameManager.HandleUserLeave(Context.ConnectionId);
        }

        public async Task Surrender(Guid gameId)
        {
            _gameManager.HandleUserLeave(_userProvider.GetUserId());

            var (player1, player2) = _gameManager.GetPlayers(gameId);

            if (player1 != null)
            {
                await Clients.Client(player1.ConnectionId).GameFinished(gameId, true);
            }

            if (player2 != null)
            {
                await Clients.Clients(player2.ConnectionId).GameFinished(gameId, true);
            }
        }

        public async Task Move(Guid gameId, string move)
        {
            var userId = _userProvider.GetUserId();
            var opponentPlayer = _gameManager.GetOpponent(gameId, userId)!;

            await Clients.Client(opponentPlayer.ConnectionId).OnMove(gameId, move);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);

            var game = _gameManager.HandleUserLeave(Context.ConnectionId);

            if (game == null)
            {
                return;
            }

            var (player1, player2) = _gameManager.GetPlayers(game.Id);

            if (player1 != null)
            {
                await Clients.Client(player1.ConnectionId).GameFinished(game.Id, true);
            }

            if (player2 != null)
            {
                await Clients.Client(player2.ConnectionId).GameFinished(game.Id, true);
            }
        }
    }

    public interface IChessHubRequests
    {
        Task SearchGame(PlayerSide side);

        Task CancelSearch();

        Task Surrender(Guid gameId);

        Task Move(Guid gameId, string move);
    }

    public interface IChessHubEvents
    {
        Task GameStarted(Guid gameId, PlayerSide side);

        Task OnMove(Guid gameId, string move);

        Task GameFinished(Guid gameId, bool win);
    }
}
