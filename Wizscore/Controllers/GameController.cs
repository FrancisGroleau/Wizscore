using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wizscore.Configuration;
using Wizscore.Managers;
using Wizscore.ViewModels;

namespace Wizscore.Controllers
{
    public class GameController : Controller
    {
        private readonly IGameManager _gameManager;
        private readonly IOptionsMonitor<Settings> _settings;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<GameController> _logger;


        public GameController(IGameManager gameManager,
            IOptionsMonitor<Settings> settings,
            IWebHostEnvironment webHostEnvironment,
            ILogger<GameController> logger)
        {
            _gameManager = gameManager;
            _settings = settings;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public IActionResult Create() => View(nameof(Create));


        [HttpPost]
        public async Task<IActionResult> SubmitCreate([FromForm] GameSubmitCreateViewModel request)
        {
            try
            {
                var gameResult = await _gameManager.CreateGameAsync(request.NumberOfPlayers, request.Username);
                if (!gameResult.IsSuccess)
                {
                    ModelState.AddModelError("Error", gameResult.Error.Message);
                    return View(nameof(Create));
                }

                ManageCookie(Constants.Cookies.GameKey, gameResult.Value.Key);
                ManageCookie(Constants.Cookies.UserName, gameResult.Value.Players.First().Username);

                return RedirectToAction(nameof(WaitingRoom));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return View(nameof(Create));
            }
        }

        public async Task<IActionResult> WaitingRoom()
        {
            //Make sure we only are here if we are in a game
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            if (string.IsNullOrEmpty(gameKey))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var game = await _gameManager.GetGameByKeyAsync(gameKey);
            if (game == null)
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            //When a player is being removed from a game we need to remove is current cookie and redirect him to the home screen
            if (!game.Players.Any(a => a.Username == username))
            {
                Response.Cookies.Delete(Constants.Cookies.GameKey);
                Response.Cookies.Delete(Constants.Cookies.UserName);
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var isGameCreator = await _gameManager.IsPlayerWithUsernameIsCreatorAsync(game, username);

            var vm = new WaitingRoomViewModel()
            {
                GameKey = gameKey,
                ShareUrl = _webHostEnvironment.IsDevelopment()
                    ? $"{nameof(JoinWithKey)}?GameKey={gameKey}"
                    : $"https://wizscore.io/Game/JoinWithKey?GameKey={gameKey}",
                NumberOfPlayer = game.NumberOfPlayers,
                IsGameCreator = isGameCreator,
                CurrentUserName = username,
                Players = game.Players
                .Select(s => new WaitingRoomPlayerViewModel()
                {
                    Username = s.Username,
                    PlayerNumber = s.PlayerNumber
                }).ToList()
            };


            return View(nameof(WaitingRoom), vm);
        }

        public IActionResult Join() => View(nameof(Join));

        public IActionResult JoinWithKey(string gameKey)
        {
            if (string.IsNullOrWhiteSpace(gameKey))
            {
                return RedirectToAction(nameof(Join));
            }
            ViewBag.gameKey = gameKey;
            return View(nameof(Join));
        }


        [HttpPost]
        public async Task<IActionResult> JoinSubmit([FromForm] JoinSubmitViewModel request)
        {
            var result = await _gameManager.AddPlayerToGameAsync(request.GameKey, request.Username);
            if (result.IsSuccess)
            {
                ManageCookie(Constants.Cookies.GameKey, request.GameKey);
                ManageCookie(Constants.Cookies.UserName, result.Value.Username);
                return RedirectToAction(nameof(WaitingRoom));
            }

            ModelState.AddModelError("Error", result.Error.Message);
            return View(nameof(Join));
        }

        public async Task<IActionResult> RemovePlayer(string username)
        {
            //if we are already in a game we can't join a new one
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            var currentUsername = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(gameKey) || string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.RemovePlayerAsync(gameKey, username, currentUsername);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
                return await WaitingRoom();
            }

            return RedirectToAction(nameof(WaitingRoom));
        }

        public async Task<IActionResult> MovePlayerUp(string username)
        {
            //if we are already in a game we can't join a new one
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            var currentUsername = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(gameKey) || string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.MovePlayerUpAsync(gameKey, username, currentUsername);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
                return await WaitingRoom();
            }

            return RedirectToAction(nameof(WaitingRoom));
        }

        public async Task<IActionResult> MovePlayerDown(string username)
        {
            //if we are already in a game we can't join a new one
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            var currentUsername = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(gameKey) || string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.MovePlayerDownAsync(gameKey, username, currentUsername);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
                return await WaitingRoom();
            }

            return RedirectToAction(nameof(WaitingRoom));
        }

        public async Task<IActionResult> Start(string gameKey)
        {
            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            await _gameManager.StartGameAsync(gameKey, username);

            return RedirectToAction(nameof(Bid));
        }

        public async Task<IActionResult> Bid()
        {
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            if (string.IsNullOrEmpty(gameKey))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var game = await _gameManager.GetGameByKeyAsync(gameKey);
            var dealerUserNameResult = await _gameManager.GetCurrentDealerUsernameAsync(gameKey);
            var isDealer = dealerUserNameResult.IsSuccess
                    ? dealerUserNameResult.Value == username
                    : false;

            ViewBag.IsDealer = isDealer;

            ViewBag.RoundNumber = game?.Rounds
                ?.OrderByDescending(x => x.RoundNumber)
                ?.FirstOrDefault()
                ?.RoundNumber ?? 1;

            var currentsuit = await _gameManager.GetCurrentSuitAsync(gameKey);

            ViewBag.Suit = currentsuit.IsSuccess
                ? currentsuit.Value
                : Models.SuitEnum.None;

            var isCurrentRoundFinishedResult = await _gameManager.IsCurrentRoundFinishedAsync(gameKey);
            if (isCurrentRoundFinishedResult.IsSuccess && isCurrentRoundFinishedResult.Value && isDealer)
            {
                return RedirectToAction(nameof(BidWaitingRoom));
            }

            var nextBiderUsernameResult = await _gameManager.GetNextPlayerUsernameToBidAsync(gameKey);
            if (nextBiderUsernameResult.IsSuccess && username != nextBiderUsernameResult.Value)
            {
                return RedirectToAction(nameof(BidWaitingRoom));
            }

            return View(nameof(Bid));
        }

        public async Task<IActionResult> BidWaitingRoom()
        {
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            if (string.IsNullOrEmpty(gameKey))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }


            var vm = new BidWaitingRoomViewModel();

            var roundNumberResult = await _gameManager.GetCurentRoundNumberAsync(gameKey);
            if (!roundNumberResult.IsSuccess)
            {
                ModelState.AddModelError("Error", roundNumberResult.Error.Message);
                return View(nameof(BidWaitingRoom), vm);
            }
            vm.RoundNumber = roundNumberResult.Value;

            var bidMessagesResult = await _gameManager.GetCurrentRoundBidMessagesAsync(gameKey);
            if (!bidMessagesResult.IsSuccess)
            {
                ModelState.AddModelError("Error", bidMessagesResult.Error.Message);
                return View(nameof(BidWaitingRoom), vm);
            }
            vm.BidMessages = bidMessagesResult.Value;


            var currentSuitResult = await _gameManager.GetCurrentSuitAsync(gameKey);
            if (!currentSuitResult.IsSuccess)
            {
                ModelState.AddModelError("Error", currentSuitResult.Error.Message);
                return View(nameof(BidWaitingRoom), vm);
            }
            vm.Suit = currentSuitResult.Value;

            var dealerPlayerUsernameResult = await _gameManager.GetCurrentDealerUsernameAsync(gameKey);
            if (!dealerPlayerUsernameResult.IsSuccess)
            {
                ModelState.AddModelError("Error", dealerPlayerUsernameResult.Error.Message);
                return View(nameof(BidWaitingRoom), vm);
            }
            vm.IsDealer = username == dealerPlayerUsernameResult.Value;

            var isCurrentRoundFinishedResult = await _gameManager.IsCurrentRoundFinishedAsync(gameKey);
            if (!isCurrentRoundFinishedResult.IsSuccess)
            {
                ModelState.AddModelError("Error", isCurrentRoundFinishedResult.Error.Message);
                return View(nameof(BidWaitingRoom), vm);
            }
            vm.IsRoundFinished = isCurrentRoundFinishedResult.Value;


            return View(nameof(BidWaitingRoom), vm);
        }

        [HttpPost]
        public async Task<IActionResult> BidSubmit([FromForm] BidSubmitViewModel request)
        {
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            if (string.IsNullOrEmpty(gameKey))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.SubmitBidAsync(gameKey, username, request.Bid);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
                return await Bid();
            }


            return RedirectToAction(nameof(BidWaitingRoom));
        }

        public async Task<IActionResult> FinishRound()
        {
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            if (string.IsNullOrEmpty(gameKey))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.FinishRoundAsync(gameKey, username);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
                return await BidWaitingRoom();
            }

            return RedirectToAction(nameof(BidResult));
        }


        public async Task<IActionResult> BidResult()
        {
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            if (string.IsNullOrEmpty(gameKey))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var roundNumberResult = await _gameManager.GetCurentRoundNumberAsync(gameKey);
            if (!roundNumberResult.IsSuccess)
            {
                ModelState.AddModelError("Error", roundNumberResult.Error.Message);
                return View(nameof(BidResult));
            }
            ViewBag.RoundNumber = roundNumberResult.Value;

            return View(nameof(BidResult));
        }

        [HttpPost]
        public async Task<IActionResult> BidResultSubmit(BidResultSubmitViewModel request)
        {
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            if (string.IsNullOrEmpty(gameKey))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var submitBidResult = await _gameManager.SubmitBidResultAsync(gameKey, username, request.ActualValue);
            if (!submitBidResult.IsSuccess)
            {
                ModelState.AddModelError("Error", submitBidResult.Error.Message);
                return await BidResult();
            }

            return RedirectToAction(nameof(Score));
        }


        public async Task<IActionResult> Score()
        {
            var vm = new ScoreViewModel();

            try
            {
                //Make sure we only can see score of our current game
                var gameKey = Request.Cookies[Constants.Cookies.GameKey];
                if (string.IsNullOrEmpty(gameKey))
                {
                    return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
                }

                var username = Request.Cookies[Constants.Cookies.UserName];
                if (string.IsNullOrEmpty(username))
                {
                    return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
                }

                var game = await _gameManager.GetGameByKeyAsync(gameKey);
                if (game == null)
                {
                    return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
                }

                var scoreResult = await _gameManager.CaclulateScoresAsync(gameKey);
                if (!scoreResult.IsSuccess)
                {
                    ModelState.AddModelError("Error", scoreResult.Error.Message);
                    return View(nameof(Score), vm);
                }

                var nextDealerUserName = await _gameManager.GetNextDealerUsernameAsync(gameKey);
                if (!nextDealerUserName.IsSuccess)
                {
                    ModelState.AddModelError("Error", scoreResult.Error.Message);
                    return View(nameof(Score), vm);
                }

                vm = new ScoreViewModel()
                {
                    IsNextDealer = nextDealerUserName.Value == username,
                    PlayerScores = scoreResult.Value.PlayerScores.Select(s => new ScorePlayerViewModel
                    {
                        Username = s.Username,
                        Score = s.Score
                    }).ToList(),
                    RoundsScores = scoreResult.Value.RoundsScores.Select(s => new ScoreRoundViewModel
                    {
                        Username = s.Username,
                        BidValue = s.BidValue,
                        ActualValue = s.ActualValue,
                        RoundNumber = s.RoundNumber,
                        Score = s.Score
                    }).ToList(),
                };

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
            }

            return View(nameof(Score), vm);
        }

        public async Task<IActionResult> StartNextRound()
        {
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            if (string.IsNullOrEmpty(gameKey))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var username = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.StartNextRoundAsync(gameKey, username);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
                return await Score();
            }

            return RedirectToAction(nameof(Bid));
        }


        private void ManageCookie(string name, string value)
        {
            CookieOptions options = new CookieOptions()
            {
                Domain = GetDomain(), // Set the domain for the cookie
                Path = "/", // Cookie is available within the entire application
                Expires = DateTime.Now.AddDays(1), // Set cookie expiration to 7 days from now
                Secure = _webHostEnvironment.IsDevelopment() ? false : true, // Ensure the cookie is only sent over HTTPS (set to false for local development)
                HttpOnly = true, // Prevent client-side scripts from accessing the cookie
                IsEssential = true // Indicates the cookie is essential for the application to function
            };
            Response.Cookies.Append(name, value, options);

        }

        private string GetDomain()
        {
            if (_webHostEnvironment.IsDevelopment())
            {
                return "localhost";
            }

            return _settings.CurrentValue?.Domain ?? "Wizscore.io";
        }

    }
}
