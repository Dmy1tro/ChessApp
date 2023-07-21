using System.Collections.Concurrent;

namespace Chess.Backend.Services
{
    public class GameManager
    {
        private static readonly ConcurrentDictionary<Guid, GameModel> _games = new();

        public GameModel CreateGame()
        {
            var game = new GameModel();
            _games[game.Id] = game;

            return game;
        }

        public GameModel? FindPendingGame(PlayerSide playerSide)
        {
            var createdGames = _games.Values.Where(g => g.State == GameState.Created).ToList();

            if (playerSide == PlayerSide.Undefined)
            {
                return createdGames.FirstOrDefault();
            }

            if (playerSide == PlayerSide.White)
            {
                return createdGames.FirstOrDefault(g => g.Player1?.Side != PlayerSide.White && g.Player2?.Side != PlayerSide.White);
            }

            if (playerSide == PlayerSide.Black)
            {
                return createdGames.FirstOrDefault(g => g.Player1?.Side != PlayerSide.Black && g.Player2?.Side != PlayerSide.Black);
            }

            return null;
        }

        public void AddPlayer(Guid gameId, Player player)
        {
            var game = _games[gameId];

            if (game.Player1 != null && game.Player2 != null)
            {
                throw new Exception("Cannot add player.");
            }

            if (game.Player1 == null)
            {
                game.Player1 = player;
            }
            else
            {
                game.Player2 = player;
            }

            if (game.Player1 != null && game.Player2 != null)
            {
                game.State = GameState.ReadyToStart;
            }
        }

        public void StartGame(Guid gameId)
        {
            var game = _games[gameId];

            if (game.Player1 is null || game.Player2 is null)
            {
                throw new Exception("Cannot start game without two players.");
            }

            if (game.Player1.Side == PlayerSide.Undefined && game.Player2.Side == PlayerSide.Undefined)
            {
                game.Player1.Side = PlayerSide.White;
                game.Player2.Side = PlayerSide.Black;
            }

            if (game.Player1.Side == PlayerSide.Undefined)
            {
                game.Player1.Side = game.Player2.Side == PlayerSide.Black ? PlayerSide.White : PlayerSide.Black;
            }

            if (game.Player2.Side == PlayerSide.Undefined)
            {
                game.Player2.Side = game.Player1.Side == PlayerSide.Black ? PlayerSide.White : PlayerSide.Black;
            }

            game.State = GameState.Started;
        }

        public (Player, Player) GetPlayers(Guid gameId)
        {
            var game = _games[gameId];

            return (game.Player1, game.Player2);
        }

        public Player? GetOpponent(Guid gameId, string userId)
        {
            var game = _games[gameId];

            return game.Player1?.UserId != userId ? game.Player1 : game.Player2;
        }

        public void FinishGame(Guid gameId)
        {
            var game = _games[gameId];

            game.State = GameState.Finished;
        }

        public GameModel? FindGameByConnectionId(string connectionId)
        {
            var game = _games.Values.FirstOrDefault(g => g.Player1?.ConnectionId == connectionId || g.Player2?.ConnectionId == connectionId);

            return game;
        }

        public GameModel? HandleUserLeave(string connectionId)
        {
            var game = _games.Values.FirstOrDefault(g => g.Player1?.ConnectionId == connectionId || g.Player2?.ConnectionId == connectionId);

            if (game == null || game.State == GameState.Finished)
            {
                return null;
            }

            if (game.State == GameState.Created)
            {
                _games.TryRemove(game.Id, out _);
                return null;
            }

            if (game.State == GameState.Started)
            {
                game.State = GameState.Finished;
            }

            if (game.State == GameState.ReadyToStart)
            {
                if (game.Player1.ConnectionId == connectionId)
                {
                    game.Player1 = null;
                }
                else
                {
                    game.Player2 = null;
                }

                game.State = GameState.Created;
            }

            return game;
        }
    }

    public class GameModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public GameState State { get; set; } = GameState.Created;

        public Player? Player1 { get; set; }

        public Player? Player2 { get; set; }
    }

    public class Player
    {
        public string UserId { get; set; }

        public string ConnectionId { get; set; }

        public PlayerSide Side { get; set; }
    }

    public enum GameState
    {
        Created = 0,
        ReadyToStart = 1,
        Started = 2,
        Finished = 3
    }

    public enum PlayerSide
    {
        Undefined = 0,
        White = 1,
        Black = 2
    }
}
