using System.ComponentModel.DataAnnotations;

namespace Wizscore.ViewModels
{
    public class JoinSubmitViewModel
    {
        [Required]
        [Display(Name = "Game key")]
        public string GameKey { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Username { get; set; }
    }
}
