using System.Security.Claims;
using CloudFiles.Database;
using CloudFiles.Services;
using CloudFiles.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudFiles.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : Controller
{
    private TokenService TokenService { get; }

    public AuthController(TokenService tokenService)
    {
        TokenService = tokenService;
    }
    
    [HttpPost("signIn")]
    public async Task<ActionResult<object>> SignIn([FromBody] SignInDto signInDto)
    {
        await using var context = new ApplicationContext();

        var user = context.Users
            .Include(u => u.Role)
            .FirstOrDefault(u => u.Name == signInDto.Name);
        if (user == null)
            return BadRequest("Неверный логин или пароль");

        user.LastLoginAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var verifyPassword = PasswordEncryption.CheckPassword(user.Password, signInDto.Password);
        if (!verifyPassword) 
            return BadRequest("Неверный логин или пароль");

        var token = TokenService.CreateToken(user, user.Role);

        var cookieOptions = new CookieOptions
        {
            Expires = DateTime.Now.AddMinutes(30)
        };
        
        HttpContext.Response.Cookies.Append("TOKEN", token, cookieOptions);

        return new { username=user.Name, role=new { name=user.Role.Name, normalized=user.Role.NormalizedName } };
    }

    [Authorize]
    [HttpPost("signOut")]
    public new ActionResult SignOut()
    {
        HttpContext.Response.Cookies.Delete("TOKEN");
        
        return Ok();
    }

    [Authorize]
    [HttpGet("verify")]
    public async Task<ActionResult<object>> VerifySignIn()
    {
        await using var context = new ApplicationContext();
        
        var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(name))
            return NotFound();

        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Name == name);
        
        return new { username=user?.Name, role=new { name=user?.Role.Name, normalized=user?.Role.NormalizedName } };
    }
}