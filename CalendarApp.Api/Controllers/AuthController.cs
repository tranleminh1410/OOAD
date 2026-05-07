using CalendarApp.Api.Infrastructure;
using CalendarApp.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace CalendarApp.Api.Controllers
{
    public class AuthDTO
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] AuthDTO dto)
        {
            if (_context.Users.Any(u => u.UserName == dto.UserName))
                return BadRequest(new { message = "Tên đăng nhập đã tồn tại!" });

            string hash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            string newUserId = "U" + DateTime.Now.Ticks.ToString().Substring(10);

            var newUser = new User(newUserId, dto.UserName, hash);
            _context.Users.Add(newUser);
            _context.SaveChanges();

            return Ok(new { message = "Đăng ký thành công!" });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] AuthDTO dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == dto.UserName);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu!" });

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { token = tokenString, userName = user.UserName, message = "Đăng nhập thành công!" });
        }
    }
}