using System.ComponentModel.DataAnnotations;

namespace couch_backend.ModelDTOs.Requests
{
    public class ComingSoonDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
