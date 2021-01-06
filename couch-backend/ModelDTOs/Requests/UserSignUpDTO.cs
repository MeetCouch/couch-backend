using System.ComponentModel.DataAnnotations;

namespace couch_backend.ModelDTOs.Requests
{
    public class UserSignUpDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
