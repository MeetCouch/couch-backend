using System.ComponentModel.DataAnnotations;

namespace couch_backend.ModelDTOs.Requests
{
    public class SocialLoginDTO
    {
        [Required]
        public string Issuer { get; set; }
        [Required]
        public string Uid { get; set; }
        [EmailAddress]
        [Required]
        public string Email { get; set; }
    }
}
