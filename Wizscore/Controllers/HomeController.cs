using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Wizscore.Managers;
using Wizscore.Models;
using Wizscore.ViewModels;

namespace Wizscore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IGameManager _gameManager;

        public HomeController(ILogger<HomeController> logger, IGameManager gameManager)
        {
            _logger = logger;
            _gameManager = gameManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new HomeViewModel();
            var gameKey = Request.Cookies[Constants.Cookies.GameKey];
            var currentUsername = Request.Cookies[Constants.Cookies.UserName];
            if (!string.IsNullOrEmpty(gameKey) && !string.IsNullOrEmpty(currentUsername))
            {
                try
                {
                    var game = await _gameManager.GetGameByKeyAsync(gameKey);
                    if (game != null && game.HasStarted)
                    {
                        vm.HasGameStarted = game.HasStarted;
                    }
                    if(game != null && game.HasStarted)
                    {
                        var isCurrentRoundFinishedResult = _gameManager.IsCurrentRoundFinished(game);
                        var isLastRoundResult = _gameManager.IsLastRound(game);
                        vm.IsCurrentRoundFinished = isCurrentRoundFinishedResult.IsSuccess && isCurrentRoundFinishedResult.Value;
                        vm.IsGameFinished = (isCurrentRoundFinishedResult.IsSuccess && isCurrentRoundFinishedResult.Value) &&
                            (isLastRoundResult.IsSuccess && isLastRoundResult.Value);
                    }
                    vm.CanRejoin = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error finding game with key: {gameKey}: {ex.Message}", ex);
                }
            }
        
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
               CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
               new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
           );

            return LocalRedirect(returnUrl);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
