using System.Collections.Generic;

namespace couch_backend.ModelDTOs.Responses
{
    public class LoginResponseDTO
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public string ExpiryTime { get; set; }
        public List<string> Roles { get; set; }
        public string UserName { get; set; }
    }
}
