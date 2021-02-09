using System.ComponentModel.DataAnnotations;

namespace couch_backend.ModelDTOs.Requests
{
    public class ForgotPasswordDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
