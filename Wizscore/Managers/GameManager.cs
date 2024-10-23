using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Options;
using Microsoft.Owin.Security.Provider;
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
        Task<Result<int>> GetCurentRoundNumberAsync(string gameKey);
        Task<Result<Game>> SubmitBidAsync(string gameKey, string username, int bidValue);
        Task<Result<List<string>>> GetCurrentRoundBidMessagesAsync(string gameKey);
        Task<Result<Game>> ChangeCurrentSuitAsync(string gameKey, string username, SuitEnum suitValue);
        Task<Result<SuitEnum>> GetCurrentSuitAsync(string gameKey);
    }

    public class GameManager : IGameManager
    {
        private readonly IGameRepository _gameRepository;
        private readonly IPlayerRepository _playerRepository;
        private readonly IRoundRepository _roundRepository;
        private readonly IBidRepository _bidRepository;
        private readonly IOptionsMonitor<Settings> _settings;
        private readonly IHubContext<WaitingRoomHub, IWaitingRoomHub> _waitingRoomHubContext;
        private readonly IHubContext<BidWaitingRoomHub, IBidWaitingRoomHub> _bidWaitingRoomHubContext;
        private Random _random;


        public GameManager(IGameRepository gameRepository,
            IPlayerRepository playerRepository,
            IRoundRepository roundRepository,
            IBidRepository bidRepository,
            IOptionsMonitor<Settings> settings,
            IHubContext<WaitingRoomHub, IWaitingRoomHub> waitingRoomHubContext,
            IHubContext<BidWaitingRoomHub, IBidWaitingRoomHub> bidWaitingRoomHubContext)
        {
            _gameRepository = gameRepository;
            _playerRepository = playerRepository;
            _roundRepository = roundRepository;
            _bidRepository = bidRepository;
            _settings = settings;
            _waitingRoomHubContext = waitingRoomHubContext;
            _bidWaitingRoomHubContext = bidWaitingRoomHubContext;
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

                //We also need to make sure there is not gap between players
                for (int i = 0; i < game.Players.Count; i++)
                {
                    var playerId = game.Players[i].Id;
                    await _playerRepository.UpdatePlayerNumberAsync(playerId, i + 1);
                }

                game.NumberOfPlayers = game.Players.Count;
            }

            await _gameRepository.SetGameHasStartAsync(game.Id);
            game.HasStarted = true;

            //Notify everyone that the game has started
            await _waitingRoomHubContext.Clients.Group(gameKey).GameStartedAsync();

            //Create the first round
            var firstDealer = game.Players.First(f => f.PlayerNumber == 1);
            await _roundRepository.CreateRoundAsync(game.Id, SuitEnum.None, firstDealer.Id, 1);

            return Result<Game>.Success(game);
        }

        public async Task<Result<Game>> RemovePlayerAsync(string gameKey, string username, string currentUsername)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if(game == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var player = game.Players.FirstOrDefault(f => f.Username == username);
            if (player == null)
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
                var dealerResult = await GetCurrentDealerUsernameAsync(gameKey);
                return dealerResult.Map(Result<string>.Success, Result<string>.Failure);
            }

            var lastBider = game.Players.FirstOrDefault(f => f.Id == lastBid.PlayerId);
            if (lastBider == null)
            {
                return Result<string>.Failure(Error.FromError("Could not found who was the last player to bid", "Game.Players.NotExisting"));
            }

            Player? nextPlayer;
            if (lastBider.PlayerNumber + 1 > game.NumberOfPlayers)
            {
                nextPlayer = game.Players.FirstOrDefault();
            }
            else
            {
                nextPlayer = game.Players.FirstOrDefault(f => f.PlayerNumber == lastBider.PlayerNumber + 1);
            }
            if(nextPlayer == null)
            {
                return Result<string>.Failure(Error.FromError("The last player of the round already bided", "Game.Round.Finished"));
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

            var player = game.Players.FirstOrDefault(f => f.Username == username);
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

            var playerAfter = game.Players.FirstOrDefault(f => f.PlayerNumber == player.PlayerNumber + 1);
            if (playerAfter != null)
            {
                await _playerRepository.UpdatePlayerNumberAsync(playerAfter.Id, playerAfter.PlayerNumber - 1);
            }
            await _playerRepository.UpdatePlayerNumberAsync(player.Id, player.PlayerNumber + 1);

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

            var player = game.Players.FirstOrDefault(f => f.Username == username);
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

            var playerBefore = game.Players.FirstOrDefault(f => f.PlayerNumber == player.PlayerNumber - 1);
            if (playerBefore != null)
            {
                await _playerRepository.UpdatePlayerNumberAsync(playerBefore.Id, playerBefore.PlayerNumber + 1);
            }
            await _playerRepository.UpdatePlayerNumberAsync(player.Id, player.PlayerNumber - 1);

            await _waitingRoomHubContext.Clients.Group(gameKey).RefreshPlayerListAsync();

            var updatedGame = await _gameRepository.GetGameByKeyAsync(gameKey);

            return Result<Game>.Success(updatedGame);
        }

        public async Task<Result<int>> GetCurentRoundNumberAsync(string gameKey)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<int>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            if(!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<int>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();

            return Result<int>.Success(latestRound.RoundNumber);
        }

        public async Task<Result<Game>> SubmitBidAsync(string gameKey, string username, int bidValue)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var player = game.Players.FirstOrDefault(f => f.Username == username);
            if (player == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {username}", "Game.Players.NotExisting"));
            }

            if (!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<Game>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var latestRound = game.Rounds.Last();
         
            var playerToBidResult = await GetNextPlayerUsernameToBidAsync(gameKey);
            if (!playerToBidResult.IsSuccess)
            {
                return Result<Game>.Failure(playerToBidResult.Error);
            }

            if(playerToBidResult.Value != player.Username)
            {
                return Result<Game>.Failure(Error.FromError($"Not this player turn to bid", "Game.Players.NotTurnToBid"));
            }

            if(latestRound.Bids.Any(a => a.PlayerId == player.Id))
            {
                return Result<Game>.Failure(Error.FromError($"Player already bidded for this round", "Game.Players.AlreadyBidded"));
            }

            var bid = await _bidRepository.CreateBidAsync(latestRound.Id, player.Id, bidValue);

            await _bidWaitingRoomHubContext.Clients.Group(gameKey).BidSubmittedAsync();

            var updatedGame = await _gameRepository.GetGameByKeyAsync(gameKey);

            return Result<Game>.Success(updatedGame);
        }

        public async Task<Result<List<string>>> GetCurrentRoundBidMessagesAsync(string gameKey)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<List<string>>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            if (!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<List<string>>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();

            var messages = latestRound.Bids?.Select(s =>
            {
                var player = game.Players.First(f => f.Id == s.PlayerId);
                return $"{player.Username} bidded: {s.BidValue}";
            })?.ToList() ?? new List<string>();

            return Result<List<string>>.Success(messages);
        }

        public async Task<Result<Game>> ChangeCurrentSuitAsync(string gameKey, string username, SuitEnum suitValue)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var player = game.Players.FirstOrDefault(f => f.Username == username);
            if (player == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {username}", "Game.Players.NotExisting"));
            }

            if (!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<Game>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var currentDealerResult = await GetCurrentDealerUsernameAsync(gameKey);

            if (!currentDealerResult.IsSuccess)
            {
                return Result<Game>.Failure(currentDealerResult.Error);
            }

            if(currentDealerResult.Value != username)
            {
                return Result<Game>.Failure(Error.FromError($"Only the dealer can change the current suit", "Game.Players.NotDealer"));
            }

            var latestRound = game.Rounds.Last();


            await _roundRepository.UpdateCurrentSuitAsync(latestRound.Id, suitValue);

            var updatedGame = await _gameRepository.GetGameByKeyAsync(gameKey);
            return Result<Game>.Success(updatedGame);
        }

        public async Task<Result<SuitEnum>> GetCurrentSuitAsync(string gameKey)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<SuitEnum>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            if (!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<SuitEnum>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();

            return Result<SuitEnum>.Success(latestRound.Suit);
        }
    }
}
