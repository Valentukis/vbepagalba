using System.ComponentModel.DataAnnotations;

namespace VbeSort.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Prisijungimo vardas privalomas")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Slaptažodis privalomas")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}

