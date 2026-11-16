using System.ComponentModel.DataAnnotations;

namespace TokenChat.Models
{
    public class AccountInput
    {
        [Required(ErrorMessage = "Введите имя пользователя.")]
        [StringLength(40, MinimumLength = 2, ErrorMessage = "Имя должно содержать от 2 до 40 символов.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Введите пароль.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
