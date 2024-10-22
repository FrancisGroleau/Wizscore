using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Wizscore.ViewModels
{
    public class GameSubmitCreateViewModel
    {
        [Required]
        [Display(Name = "Number of players")]
        public int NumberOfPlayers { get; set; }

        [Required(AllowEmptyStrings = false)]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;
    }
}
