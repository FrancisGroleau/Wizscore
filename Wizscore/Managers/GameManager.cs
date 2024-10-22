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
        Task<Result<Game>> StartGameAsync(string gameKey, string username);
    }

    public class GameManager : IGameManager
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IOptionsMonitor<Settings> _settings;
        private readonly IHubContext<WaitingRoomHub, IWaitingRoomHub> _waitingRoomHubContext;
        private Random _random;


        public GameManager(IGameRepository gameRepository, 
            IPlayerRepository playerRepository, 
            IOptionsMonitor<Settings> settings,
            IHubContext<WaitingRoomHub, IWaitingRoomHub> waitingRoomHubContext)
        {
            _gameRepository = gameRepository;
            _playerRepository = playerRepository;
            _settings = settings;
            _waitingRoomHubContext = waitingRoomHubContext;
            _random = new Random();
        }

        public async Task<Game> CreateGameAsync(int numberOfPlayers, string username)
        {
            var gameKey = await GetNewGameKeyAsync();
            var game = await _gameRepository.CreateGameAsync(numberOfPlayers, gameKey);

            var player = await _playerRepository.CreatePlayerAsync(game.Id, username);
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

            var player = await _playerRepository.CreatePlayerAsync(game.Id, username);

            //Notify user in waiting room a new player has been added
            await _waitingRoomHubContext.Clients.Group(gameKey).PlayerAddedAsync(username);

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
                return Result<Game>.Failure(Error.FromError($"Not Game found with Game Key: {gameKey}", "Game.NotExisting"));
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
            

            return Result<Game>.Success(game);
        }
    }
}
