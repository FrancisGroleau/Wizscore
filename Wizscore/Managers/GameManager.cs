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
        Task<Game?> GetGameByKeyAsync(string gameKey);

        Task<Result<Game>> CreateGameAsync(int numberOfPlayers, string username);


        Task<bool> IsPlayerWithUsernameIsCreatorAsync(Game game, string username);

        Task<Result<Player>> AddPlayerToGameAsync(string gameKey, string username);
        Task<Result<Game>> RemovePlayerAsync(string gameKey, string username, string currentUsername);
        Task<Result<Game>> MovePlayerUpAsync(string gameKey, string username, string currentUsername);
        Task<Result<Game>> MovePlayerDownAsync(string gameKey, string username, string currentUsername);

        Task<Result<Game>> StartGameAsync(string gameKey, string username);

        Result<string> GetCurrentDealerUsername(Game game);
        Result<string> GetNextPlayerUsernameToBid(Game game);
        Result<int> GetCurentRoundNumber(Game game);
        Task<Result<int>> GetCurentRoundNumberAsync(string gameKey);
        Task<Result<Game>> SubmitBidAsync(string gameKey, string username, int bidValue);
        Task<Result<Game>> SubmitBidResultAsync(string gameKey, string username, int actualValue);
        Result<List<string>> GetCurrentRoundBidMessages(Game game);
        Task<Result<Game>> ChangeCurrentSuitAsync(string gameKey, string username, SuitEnum suitValue);
        Result<SuitEnum> GetCurrentSuit(Game game);
        Result<bool> IsAllBidPlacedForCurrentRound(Game game);
        Task<Result<bool>> FinishRoundAsync(string gameKey, string username);
        Result<Score> CaclulateScores(Game game);
        Result<string> GetNextDealerUsername(Game game);
        Task<Result<Game>> StartNextRoundAsync(Game game, string username);
        Result<bool> IsCurrentRoundFinished(Game game);
        Result<bool> IsLastRound(Game game);
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
        private readonly IHubContext<ScoreHub, IScoreHub> _scoreHubContext;
        private Random _random;


        public GameManager(IGameRepository gameRepository,
            IPlayerRepository playerRepository,
            IRoundRepository roundRepository,
            IBidRepository bidRepository,
            IOptionsMonitor<Settings> settings,
            IHubContext<WaitingRoomHub, IWaitingRoomHub> waitingRoomHubContext,
            IHubContext<BidWaitingRoomHub, IBidWaitingRoomHub> bidWaitingRoomHubContext,
            IHubContext<ScoreHub, IScoreHub> scoreHubContext)
        {
            _gameRepository = gameRepository;
            _playerRepository = playerRepository;
            _roundRepository = roundRepository;
            _bidRepository = bidRepository;
            _settings = settings;
            _waitingRoomHubContext = waitingRoomHubContext;
            _bidWaitingRoomHubContext = bidWaitingRoomHubContext;
            _scoreHubContext = scoreHubContext;
            _random = new Random();
        }

        public async Task<Result<Game>> CreateGameAsync(int numberOfPlayers, string username)
        {
            var gameKey = await GetNewGameKeyAsync();

            var maxPlayers = _settings.CurrentValue?.MaxPlayers ?? 6;
            if(numberOfPlayers <= 1 || numberOfPlayers > maxPlayers)
            {
                return Result<Game>.Failure(Error.FromError($"Maximum number of players is: {maxPlayers}", "Game.TooManyPlayers"));
            }

            var game = await _gameRepository.CreateGameAsync(numberOfPlayers, gameKey);

            var player = await _playerRepository.CreatePlayerAsync(game.Id, username, 1);
            await _gameRepository.SetGamePlayerCreatorIdAsync(game.Id, player.Id);

            game.Players.Add(player);

            return Result<Game>.Success(game);
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
            if (game == null)
            {
                return Result<Player>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            if (game.NumberOfPlayers == game.Players.Count)
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
            await _waitingRoomHubContext.Clients.Group(game.Key).RefreshPlayerListAsync();

            return Result<Player>.Success(player);
        }

        public async Task<Result<Game>> RemovePlayerAsync(string gameKey, string username, string currentUsername)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var player = game.Players.FirstOrDefault(f => f.Username == username);
            if (player == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {game.Key} and username: {username}", "Game.Players.NotExisting"));
            }

            var currentPlayer = await _playerRepository.GetPlayerByGameIdAndUsernameAsync(game.Id, currentUsername);
            if (currentPlayer == null)
            {
                return Result<Game>.Failure(Error.FromError($"No Player found for Game Key: {game.Key} and username: {currentUsername}", "Game.Players.NotExisting"));
            }

            if (game.PlayerCreatorId != currentPlayer.Id)
            {
                return Result<Game>.Failure(Error.FromError("Only player who created the game can remove other players", "Game.Players.NotCreator"));
            }

            var updatedGame = await _gameRepository.RemovePlayerAsync(game.Id, player.Id);
            await _waitingRoomHubContext.Clients.Group(game.Key).RefreshPlayerListAsync();

            return Result<Game>.Success(updatedGame);
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
            if (game == null)
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

            //Create the first round
            var firstDealer = game.Players.First(f => f.PlayerNumber == 1);
            await _roundRepository.CreateRoundAsync(game.Id, SuitEnum.None, firstDealer.Id, 1);


            //Notify everyone that the game has started
            await _waitingRoomHubContext.Clients.Group(game.Key).GameStartedAsync();

            return Result<Game>.Success(game);
        }

        public async Task<Result<string>> GetCurrentDealerUsernameAsync(string gameKey)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<string>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            return GetCurrentDealerUsername(game);
        }

        public Result<string> GetCurrentDealerUsername(Game game)
        {
            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).FirstOrDefault();
            if (latestRound == null)
            {
                return Result<string>.Failure(Error.FromError($"Game with Key: {game.Key} has not started yet", "Game.NotStarted"));
            }

            var latestDealerId = latestRound.DealerId;
            var latestDealerPlayer = game.Players.FirstOrDefault(f => f.Id == latestDealerId);
            if (latestDealerPlayer == null)
            {
                return Result<string>.Failure(Error.FromError("No Player found as dealer for latest round", "Game.Players.NotFound"));
            }

            return Result<string>.Success(latestDealerPlayer.Username);
        }

        public Result<string> GetNextPlayerUsernameToBid(Game game)
        {
            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).FirstOrDefault();
            if (latestRound == null)
            {
                return Result<string>.Failure(Error.FromError($"Game with Key: {game.Key} has not started yet", "Game.NotStarted"));
            }

            var lastBid = latestRound.Bids.LastOrDefault();
            if (lastBid == null)
            {
                var dealerResult = GetCurrentDealerUsername(game);
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
            return Result<Game>.Success(updatedGame!);
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

            return Result<Game>.Success(updatedGame!);
        }

        public async Task<Result<int>> GetCurentRoundNumberAsync(string gameKey)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<int>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            return GetCurentRoundNumber(game);
        }

        public Result<int> GetCurentRoundNumber(Game game)
        {
            if (!game.HasStarted || game.Rounds.Count == 0)
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
         
            var playerToBidResult = GetNextPlayerUsernameToBid(game);
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

            if(bidValue > latestRound.RoundNumber)
            {
                return Result<Game>.Failure(Error.FromError("Cannot bid more than the current round", "Game.Rounds.BidTooMuch"));
            }

            var bid = await _bidRepository.CreateBidAsync(latestRound.Id, player.Id, bidValue);
            var updatedGame = await _gameRepository.GetGameByKeyAsync(gameKey);

            var isRoundFinished = updatedGame!.Rounds
                .OrderByDescending(o => o.RoundNumber).First().Bids.Count() == game.NumberOfPlayers;

            await _bidWaitingRoomHubContext.Clients.Group(gameKey).BidSubmittedAsync();
            
            return Result<Game>.Success(updatedGame);
        }

        public async Task<Result<Game>> SubmitBidResultAsync(string gameKey, string username, int actualValue)
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
            var playerBid = latestRound.Bids.FirstOrDefault(f => f.PlayerId == player.Id);
            if (playerBid == null)
            {
                return Result<Game>.Failure(Error.FromError($"Player did not bid for this round", "Game.Players.NotBid"));
            }

            await _bidRepository.UpdateBidActualValueAsync(playerBid.Id, actualValue);

            await _scoreHubContext.Clients.Group(gameKey).BidResultSubmittedAsync();

            var updatedGame = await _gameRepository.GetGameByKeyAsync(gameKey);

            return Result<Game>.Success(updatedGame!);
        }

        public Result<List<string>> GetCurrentRoundBidMessages(Game game)
        {
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
            return Result<Game>.Success(updatedGame!);
        }

        public Result<SuitEnum> GetCurrentSuit(Game game)
        {
            if (!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<SuitEnum>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();

            return Result<SuitEnum>.Success(latestRound.Suit);
        }

        public Result<bool> IsAllBidPlacedForCurrentRound(Game game)
        {
            if (!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<bool>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();

            var isRoundFinished = latestRound.Bids.Count == game.NumberOfPlayers;
            return Result<bool>.Success(isRoundFinished);
        }

        public async Task<Result<bool>> FinishRoundAsync(string gameKey, string username)
        {
            var game = await _gameRepository.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return Result<bool>.Failure(Error.FromError($"No Game found with Game Key: {gameKey}", "Game.NotExisting"));
            }

            var player = game.Players.FirstOrDefault(f => f.Username == username);
            if (player == null)
            {
                return Result<bool>.Failure(Error.FromError($"No Player found for Game Key: {gameKey} and username: {username}", "Game.Players.NotExisting"));
            }

            if (!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<bool>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();

            if(latestRound.Bids.Count != game.NumberOfPlayers)
            {
                return Result<bool>.Failure(Error.FromError("Current Round is not finished", "Game.Rounds.NotFinished"));
            }

            await _bidWaitingRoomHubContext.Clients.Group(gameKey).RoundFinishedAsync();

            return Result<bool>.Success(true);
        }

        public Result<Score> CaclulateScores(Game game)
        {
            var score = new Score();
            score.RoundsScores = game.Rounds.SelectMany(s => s.Bids.Select(b => new RoundScore()
            {
                Username = game.Players.First(f => f.Id == b.PlayerId).Username,
                ActualValue = b.ActualValue,
                BidValue = b.BidValue,
                RoundNumber = s.RoundNumber,
                Score = CalculateScore(b.BidValue, b.ActualValue)
            })).ToList();

            score.PlayerScores = score.RoundsScores.GroupBy(g => g.Username)
                .Select(s => new PlayerScore()
                {
                    Username = s.Key,
                    Score = s.Sum(ss => ss.Score)
                }).ToList();

            return Result<Score>.Success(score);
        }

        private int CalculateScore(int bid, int? actual)
        {
            if(actual == null)
            {
                return 0;
            }

            var score = 20;
            if(actual != bid)
            {
                var diff = Math.Abs(actual.Value - bid);
                score = 0;
                score -= (diff * 10);
            }
            else
            {
                score += (bid * 10);
            }

            return score;
        }

        public Result<string> GetNextDealerUsername(Game game)
        {
            if (!game.HasStarted || game.Rounds.Count == 0)
            {
                return Result<string>.Failure(Error.FromError($"Game has not started yet", "Game.NotStarted"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();
            var latestPlayer = game.Players.FirstOrDefault(f => f.Id == latestRound.DealerId);
            if (latestPlayer == null)
            {
                return Result<string>.Failure(Error.FromError($"Cannot found who was the latest dealer", "Game.Players.NotFound"));
            }

            if(latestPlayer.PlayerNumber == game.NumberOfPlayers)
            {
                var firstPlayerUsername = game.Players.First().Username;
                return Result<string>.Success(firstPlayerUsername);
            }

            var nextPlayer = game.Players.FirstOrDefault(f => f.PlayerNumber == latestPlayer.PlayerNumber + 1);
            if (nextPlayer == null)
            {
                return Result<string>.Failure(Error.FromError($"Cannot found the next dealer", "Game.Players.NotFound"));
            }

            return Result<string>.Success(nextPlayer.Username);
        }

        public async Task<Result<Game>> StartNextRoundAsync(Game game, string username)
        {
            var nextRoundDealerResult = GetNextDealerUsername(game);
            if(!nextRoundDealerResult.IsSuccess)
            {
                return Result<Game>.Failure(nextRoundDealerResult.Error);
            }

            if(nextRoundDealerResult.Value != username)
            {
                return Result<Game>.Failure(Error.FromError($"You are not the next dealer", "Game.Players.NotNextDealer"));
            }

            var isRoundFinished = IsCurrentRoundFinished(game);
            if (!isRoundFinished.IsSuccess)
            {
                return Result<Game>.Failure(isRoundFinished.Error);
            }
            if(isRoundFinished.IsSuccess && !isRoundFinished.Value)
            {
                return Result<Game>.Failure(Error.FromError("Not all players have submitted their actual value for the round", "Game.Rounds.NotFinished"));
            }

            var latestRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();
            var nextDealerPlayer = game.Players.First(f => f.Username == username);

            var nextRound = await _roundRepository.CreateRoundAsync(game.Id, SuitEnum.None, nextDealerPlayer.Id, latestRound.RoundNumber + 1);

            await _scoreHubContext.Clients.Group(game.Key).NextRoundStartedAsync();

            var updatedGame = await _gameRepository.GetGameByKeyAsync(game.Key);

            return Result<Game>.Success(updatedGame!);
        }

        public Result<bool> IsCurrentRoundFinished(Game game)
        {
            var lastRound = game.Rounds.OrderByDescending(o => o.RoundNumber).First();
            var isLastRoundFinished = lastRound.Bids.Any() && lastRound.Bids.All(a => a.ActualValue.HasValue);
            return Result<bool>.Success(isLastRoundFinished);
        }

        public Result<bool> IsLastRound(Game game)
        {
            if (!game.HasStarted)
            {
                return Result<bool>.Success(false);
            }

            var isLastRound = game.NumberOfPlayers == 6 && game.Rounds.Count == 10 ||
                game.NumberOfPlayers == 5 && game.Rounds.Count == 12 ||
                game.NumberOfPlayers == 4 && game.Rounds.Count == 15 ||
                game.NumberOfPlayers <= 3 && game.Rounds.Count == 20;

            return Result<bool>.Success(isLastRound);   
        }
    }
}
