using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PriceTracker.Api.Infrastructure;
using System.Security.Claims;

namespace PriceTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext db, IPasswordHasher<User> passwordHasher, ITokenService tokenService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(x => x.Email == email))
            return Conflict(new { message = "Email already exists" });

        var user = new User { Email = email };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await MergeAnonymousProductsToUser(user.Id);

        var token = _tokenService.CreateToken(user);
        SetAuthCookie(token);

        return Ok(new { user = new { user.Id, user.Email } });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null) return Unauthorized(new { message = "Invalid credentials" });

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid credentials" });

        await MergeAnonymousProductsToUser(user.Id);

        var token = _tokenService.CreateToken(user);
        SetAuthCookie(token);

        return Ok(new { user = new { user.Id, user.Email } });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = User.FindFirstValue(ClaimTypes.Email)
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(JwtTokenService.AuthCookieName);
        return NoContent();
    }

    private void SetAuthCookie(string token)
    {
        Response.Cookies.Append(JwtTokenService.AuthCookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    private async Task MergeAnonymousProductsToUser(int userId)
    {
        if (!Request.Cookies.TryGetValue(VisitorIdentity.AnonymousCookieName, out var anonId) ||
            !Guid.TryParse(anonId, out _))
            return;

        var anonOwner = $"anon:{anonId}";
        var userOwner = $"user:{userId}";

        await _db.Products
            .Where(p => p.OwnerKey == anonOwner)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.OwnerKey, userOwner));
    }
}
