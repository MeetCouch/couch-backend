using System.ComponentModel.DataAnnotations;

namespace couch_backend.ModelDTOs.Requests
{
    public class RefreshTokenDTO
    {
        [Required]
        public string RefreshToken { get; set; }
        [Required]
        public string UserId { get; set; }
    }
}
