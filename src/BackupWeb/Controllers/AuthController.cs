using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using BackupDatabase;
using BackupDatabase.Models;

namespace BackupWeb.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize]
    public class AuthController : Controller
    {
        private readonly IUsersDBAccess usersDB;
        private readonly IConfiguration configuration;

        private readonly SymmetricSecurityKey key;
        private readonly SigningCredentials creds;


        public class UserLogin
        {
            public string Login { get; set; }
            public string Password { get; set; }
        }

        public class LoginError
        {
            public string Reason { get; set; }
        }

        public AuthController(IUsersDBAccess usersDB, IConfiguration configuration)
        {
            this.usersDB = usersDB;
            this.configuration = configuration;

            key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Tokens:Key"]));
            creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        }

        private dynamic CreateToken(DBUser user)
        {
            var claims = new[]
{
              new Claim(JwtRegisteredClaimNames.Sub, user.Login),
              new Claim("roles", user.Roles == null ? "" : string.Join(',', user.Roles))
            };

            var expires = DateTime.UtcNow.AddMinutes(30);
            var token = new JwtSecurityToken(
                configuration["Tokens:Issuer"],
                configuration["Tokens:Issuer"],
                claims,
                expires: expires,
                signingCredentials: creds);

            return new { Token = new JwtSecurityTokenHandler().WriteToken(token), Expires = expires };
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLogin loginuser)
        {
            if (string.IsNullOrWhiteSpace(loginuser.Password))
            {
                return BadRequest(new LoginError { Reason = "Password cannot be empty" });
            }

            var user = await usersDB.GetUser(loginuser.Login, loginuser.Password);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(CreateToken(user));
        }

        [HttpPost("create")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] UserLogin user)
        {
            await usersDB.AddUser(user.Login, user.Password );
            return NoContent();
        }

    }
}