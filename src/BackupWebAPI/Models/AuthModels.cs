using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace BackupWebAPI.Models
{
    public class UserLogin
    {
        [JsonProperty("login")]
        [Required]
        public string Login { get; set; }

        [JsonProperty("password")]
        [Required]
        public string Password { get; set; }
    }

    public class LoginError
    {
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }


}
