using System.ComponentModel.DataAnnotations;

namespace couch_backend.ModelDTOs.Requests
{
    public class ResendConfirmEmailRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
