using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Wizscore.Configuration;
using Wizscore.Managers;
using Wizscore.Models;
using Wizscore.Persistence.Entity;
using Wizscore.Persistence.Repositories;
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

        public IActionResult Create() => View();


        [HttpPost]
        public async Task<IActionResult> SubmitCreate([FromForm] GameSubmitCreateViewModel request)
        {
            try
            {
                var game = await _gameManager.CreateGameAsync(request.NumberOfPlayers, request.Username);

                ManageCookie(Constants.Cookies.GameKey, game.Key);
                ManageCookie(Constants.Cookies.UserName, game.Players.First().Username);

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
            if(!game.Players.Any(a => a.Username == username))
            {
                Response.Cookies.Delete(Constants.Cookies.GameKey);
                Response.Cookies.Delete(Constants.Cookies.UserName);
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var isGameCreator = await _gameManager.IsPlayerWithUsernameIsCreatorAsync(game, username);

            var vm = new WaitingRoomViewModel()
            {
                GameKey = gameKey,
                NumberOfPlayer = game.NumberOfPlayers,
                IsGameCreator = isGameCreator,
                CurrentUserName = username,
                Players = game.Players
                .OrderBy(o => o.PlayerNumber)
                .Select(s => new WaitingRoomPlayerViewModel() { Username = s.Username }).ToList()
            };


            return View(vm);
        }


        public async Task<IActionResult> Join()
        {
            return View();
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
            if (string.IsNullOrEmpty(gameKey) || string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.RemovePlayerAsync(gameKey, username, currentUsername);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
            }

            return RedirectToAction(nameof(WaitingRoom));
        }

        public async Task<IActionResult> MovePlayerUp(string username)
        {
            //if we are already in a game we can't join a new one
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            var currentUsername = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(gameKey) || string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.MovePlayerUpAsync(gameKey, username, currentUsername);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
            }

            return RedirectToAction(nameof(WaitingRoom));
        }

        public async Task<IActionResult> MovePlayerDown(string username)
        {
            //if we are already in a game we can't join a new one
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            var currentUsername = Request.Cookies[Constants.Cookies.UserName];
            if (string.IsNullOrEmpty(gameKey) || string.IsNullOrEmpty(username))
            {
                return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
            }

            var result = await _gameManager.MovePlayerDownAsync(gameKey, username, currentUsername);
            if (!result.IsSuccess)
            {
                ModelState.AddModelError("Error", result.Error.Message);
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

            ViewBag.IsDealer = dealerUserNameResult.IsSuccess
                    ? dealerUserNameResult.Value == username
                    : false;

            ViewBag.RoundNumber = game?.Rounds
                ?.OrderByDescending(x => x.RoundNumber)
                ?.FirstOrDefault()
                ?.RoundNumber ?? 1;

            var currentUserToBidUsername = await _gameManager.GetNextPlayerUsernameToBidAsync(gameKey);

            return View();
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

            return View(nameof(Score));
        }

        

        public async Task<IActionResult> Score()
        {
            try
            {
                //Make sure we only can see score of our current game
                var gameKey = Request.Cookies[Constants.Cookies.GameKey];
                if (string.IsNullOrEmpty(gameKey))
                {
                    return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
                }

                var game = await _gameManager.GetGameByKeyAsync(gameKey);
                if (game == null)
                {
                    return RedirectToAction(nameof(HomeController.Index), nameof(HomeController).Replace("Controller", string.Empty));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
            }

            return View();
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
