using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using System;
using Wizscore.Configuration;
using Wizscore.Hubs;
using Wizscore.Models;
using Wizscore.Persistence.Repositories;
using Wizscore.Result;

namespace Wizscore.Managers
{

    public interface IGameManager
    {
        Task<Result<Player>> AddPlayerToGameAsync(string gameKey, string username);
        Task<Game> CreateGameAsync(int numberOfPlayers, string username);
        Task<Game?> GetGameByKeyAsync(string gameKey);
        Task<bool> IsPlayerWithUsernameIsCreatorAsync(Game game, string username);
        Task<Result<Game>> RemovePlayerAsync(string gameKey, string username, string currentUsername);
        Task<Result<Game>> StartGameAsync(string gameKey, string username);
        Task<Result<string>> GetCurrentDealerUsernameAsync(string gameKey);
        Task<Result<string>> GetNextPlayerUsernameToBidAsync(string gameKey);
        Task<Result<Game>> MovePlayerUpAsync(string gameKey, string username, string currentUsername);
        Task<Result<Game>> MovePlayerDownAsync(string gameKey, string username, string currentUsername);
    }

    public class GameManager : IGameManager
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IRoundRepository _roundRepository;
        private readonly IOptionsMonitor<Settings> _settings;
        private readonly IHubContext<WaitingRoomHub, IWaitingRoomHub> _waitingRoomHubContext;
        private Random _random;


        public GameManager(IGameRepository gameRepository,
            IPlayerRepository playerRepository,
            IRoundRepository roundRepository,
            IOptionsMonitor<Settings> settings,
            IHubContext<WaitingRoomHub, IWaitingRoomHub> waitingRoomHubContext)
        {
            _gameRepository = gameRepository;
            _playerRepository = playerRepository;
            _roundRepository = roundRepository;
            _settings = settings;
            _waitingRoomHubContext = waitingRoomHubContext;
            _random = new Random();
        }

        public async Task<Game> CreateGameAsync(int numberOfPlayers, string username)
        {
            var gameKey = await GetNewGameKeyAsync();
            var game = await _gameRepository.CreateGameAsync(numberOfPlayers, gameKey);

            var player = await _playerRepository.CreatePlayerAsync(game.Id, username, 1);
            await _gameRepository.SetGamePlayerCreatorIdAsync(game.Id, player.Id);

            game.Players.Add(player);

            return game;
        }

        private async Task<string> GetNewGameKeyAsync()
        {
            var uniqueGameKeyNotFound = true;
            var randomKey = string.Empty;
            while (uniqueGameKeyNotFound)
            {
                randomKey = GenerateGameKey();
                uniqueGameKeyNotFound = await _gameRepository.CheckIfGameKeyExistsAsync(randomKey);
            }

            return randomKey;
        }

        public Task<Game?> GetGameByKeyAsync(string gameKey) 
            =>_gameRepository.GetGameByKeyAsync(gameKey);
        

        private string GenerateGameKey()
        {
            int length = _settings.CurrentValue?.GameKeyLength ?? 5;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public async Task<Result<Player>> AddPlayerToGameAsync(string gameKey, string username)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if(game == null)
            {
                return Result<Player>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            if(game.NumberOfPlayers == game.Players.Count)
            {
                return Result<Player>.Failure(Error.FromError("This game is full", "Game.Full"));
            }

            if(game.Players.Any(p => p.Username == username))
            {
                return Result<Player>.Failure(Error.FromError("This username is already taken", "Game.Players.UsernameTaken"));
            }

            var playerNumber = game.Players.Count() + 1;
            var player = await _playerRepository.CreatePlayerAsync(game.Id, username, playerNumber);

            //Notify user in waiting room a new player has been added
            await _waitingRoomHubContext.Clients.Group(gameKey).RefreshPlayerListAsync();

            return Result<Player>.Success(player);
        }

        public async Task<bool> IsPlayerWithUsernameIsCreatorAsync(Game game, string username)
        {
            var player = await _playerRepository.GetPlayerByGameIdAndUsernameAsync(game.Id, username);
            if(player == null)
            {
                return false;
            }

            return game.PlayerCreatorId == player.Id;
        }

        public async Task<Result<Game>> StartGameAsync(string gameKey, string username)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if(game == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var isCreator = await IsPlayerWithUsernameIsCreatorAsync(game, username);
            if(!isCreator)
            {
                return Result<Game>.Failure(Error.FromError("Only game creator can start game", "Game.OnlyCreatorCanStart"));
            }

            if(game.Players.Count != game.NumberOfPlayers)
            {
                await _gameRepository.SetGameNumbersOfPlayerAsync(game.Id, game.Players.Count);
                game.NumberOfPlayers = game.Players.Count;
            }

            await _gameRepository.SetGameHasStartAsync(game.Id);
            game.HasStarted = true;

            //Notify everyone that the game has started
            await _waitingRoomHubContext.Clients.Group(gameKey).GameStartedAsync();

            //Create the first round
            await _roundRepository.CreateRoundAsync(game.Id, SuitEnum.None, game.PlayerCreatorId, 1);

            return Result<Game>.Success(game);
        }

        public async Task<Result<Game>> RemovePlayerAsync(string gameKey, string username, string currentUsername)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if(game == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var player = await _playerRepository.GetPlayerByGameIdAndUsernameAsync(game.Id, username);
            if(player == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {username}", "Game.Players.NotExisting"));
            }

            var currentPlayer = await _playerRepository.GetPlayerByGameIdAndUsernameAsync(game.Id, currentUsername);
            if (currentPlayer == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {currentUsername}", "Game.Players.NotExisting"));
            }

            if(game.PlayerCreatorId != currentPlayer.Id)
            {
                return Result<Game>.Failure(Error.FromError("Only player who created the game can remove other players", "Game.Players.NotCreator"));
            }

            var updatedGame = await _gameRepository.RemovePlayerAsync(game.Id, player.Id);
            await _waitingRoomHubContext.Clients.Group(gameKey).RefreshPlayerListAsync();

            return Result<Game>.Success(updatedGame);
        }

        public async Task<Result<string>> GetCurrentDealerUsernameAsync(string gameKey)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<string>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).FirstOrDefault();
            if (latestRound == null)
            {
                return Result<string>.Failure(Error.FromError($"Game with Key: {gameKey} has not started yet", "Game.NotStarted"));
            }

            var latestDealerId = latestRound.DealerId;
            var latestDealerPlayer = await _playerRepository.GetPlayerByIdAsync(latestDealerId);
            if (latestDealerPlayer == null)
            {
                return Result<string>.Failure(Error.FromError("No Player found as dealer for latest round", "Game.Players.NotFound"));
            }

            return Result<string>.Success(latestDealerPlayer.Username);
        }

        public async Task<Result<string>> GetNextPlayerUsernameToBidAsync(string gameKey)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<string>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).FirstOrDefault();
            if (latestRound == null)
            {
                return Result<string>.Failure(Error.FromError($"Game with Key: {gameKey} has not started yet", "Game.NotStarted"));
            }

            var lastBid = latestRound.Bids.LastOrDefault();
            if (lastBid == null)
            {
                return Result<string>.Failure(Error.FromError($"Round {latestRound.RoundNumber} has no bid yet", "Game.Rounds.NoBids"));
            }

            var nextPlayerId = game.Players.Last().Id;
            for (int i = 0; i < game.Players.Count; i++)
            {
                if(lastBid.PlayerId == game.Players[i].Id &&
                    i + 1 < game.Players.Count)
                {
                    nextPlayerId = game.Players[i + 1].Id;
                }
            }

            var nextPlayer = await _playerRepository.GetPlayerByIdAsync(nextPlayerId);
            if (nextPlayer == null)
            {
                return Result<string>.Failure(Error.FromError("Could not found next player to play", "Game.Players.NotExisting"));
            }

            return Result<string>.Success(nextPlayer.Username);
        }

        public async Task<Result<Game>> MovePlayerUpAsync(string gameKey, string username, string currentUsername)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var player = await _playerRepository.GetPlayerByGameIdAndUsernameAsync(game.Id, username);
            if (player == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {username}", "Game.Players.NotExisting"));
            }

            var currentPlayer = await _playerRepository.GetPlayerByGameIdAndUsernameAsync(game.Id, currentUsername);
            if (currentPlayer == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {currentUsername}", "Game.Players.NotExisting"));
            }

            if (game.PlayerCreatorId != currentPlayer.Id)
            {
                return Result<Game>.Failure(Error.FromError("Only player who created the game can remove other players", "Game.Players.NotCreator"));
            }


            if (player.PlayerNumber == game.NumberOfPlayers)
            {
                return Result<Game>.Failure(Error.FromError($"Cannot move up player past number of players in game: {game.NumberOfPlayers}", "Game.Players.CannotMoveUp"));
            }

            var playerAfter = game.Players[player.PlayerNumber + 1];
            await _playerRepository.UpdatePlayerNumberAsync(playerAfter.Id, playerAfter.PlayerNumber - 1);
            await _playerRepository.UpdatePlayerNumberAsync(player.Id, playerAfter.PlayerNumber + 1);

            await _waitingRoomHubContext.Clients.Group(gameKey).RefreshPlayerListAsync();

            var updatedGame = await _gameRepository.GetGameByKeyAsync(gameKey);
            return Result<Game>.Success(updatedGame);
        }

        public async Task<Result<Game>> MovePlayerDownAsync(string gameKey, string username, string currentUsername)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var player = await _playerRepository.GetPlayerByGameIdAndUsernameAsync(game.Id, username);
            if (player == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {username}", "Game.Players.NotExisting"));
            }

            var currentPlayer = await _playerRepository.GetPlayerByGameIdAndUsernameAsync(game.Id, currentUsername);
            if (currentPlayer == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {currentUsername}", "Game.Players.NotExisting"));
            }

            if (game.PlayerCreatorId != currentPlayer.Id)
            {
                return Result<Game>.Failure(Error.FromError("Only player who created the game can remove other players", "Game.Players.NotCreator"));
            }

            if(player.PlayerNumber == 1)
            {
                return Result<Game>.Failure(Error.FromError($"Cannot move down first player", "Game.Players.CannotMoveDown"));
            }

            var playerBefore = game.Players[player.PlayerNumber - 1];
            await _playerRepository.UpdatePlayerNumberAsync(playerBefore.Id, playerBefore.PlayerNumber + 1);
            await _playerRepository.UpdatePlayerNumberAsync(player.Id, player.PlayerNumber - 1);

            await _waitingRoomHubContext.Clients.Group(gameKey).RefreshPlayerListAsync();

            var updatedGame = await _gameRepository.GetGameByKeyAsync(gameKey);

            return Result<Game>.Success(updatedGame);
        }
    }
}
