using TokenChat.Data;
using TokenChat.Models;
using TokenChat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TokenChat.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext context;
        private readonly PasswordHasher<AppUser> hasher;
        private readonly TokenService tokens;

        public AuthController(AppDbContext context, PasswordHasher<AppUser> hasher, TokenService tokens)
        {
            this.context = context;
            this.hasher = hasher;
            this.tokens = tokens;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(AccountInput input)
        {
            string username = input.Username.Trim();
            if (username.Length < 2)
            {
                return BadRequest(new { message = "Имя должно содержать минимум 2 символа." });
            }
            string normalized = username.ToUpperInvariant();
            if (await context.Users.AnyAsync(u => u.NormalizedUsername == normalized))
            {
                return BadRequest(new { message = "Это имя уже занято." });
            }
            AppUser user = new AppUser();
            user.Username = username;
            user.NormalizedUsername = normalized;
            user.PasswordHash = hasher.HashPassword(user, input.Password);
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return Ok(new { token = tokens.CreateToken(user) });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(AccountInput input)
        {
            string normalized = input.Username.Trim().ToUpperInvariant();
            AppUser user = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalized);
            if (user == null)
            {
                return Unauthorized(new { message = "Неверное имя или пароль." });
            }
            PasswordVerificationResult result = hasher.VerifyHashedPassword(user, user.PasswordHash, input.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Неверное имя или пароль." });
            }
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = hasher.HashPassword(user, input.Password);
                await context.SaveChangesAsync();
            }
            return Ok(new { token = tokens.CreateToken(user) });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new { username = User.Identity.Name });
        }
    }
}
