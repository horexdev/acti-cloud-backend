using CloudFiles.Database;
using CloudFiles.Database.Models;
using CloudFiles.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CloudFiles.Controllers;

[ApiController]
[Route("users")]
public class UsersController : Controller
{
    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    public async Task<ActionResult> Create([FromBody] UserDto userDto)
    {
        await using var context = new ApplicationContext();

        var role = await context.Roles
            .FirstOrDefaultAsync(r => r.Name == userDto.RoleName);
        if (role == null)
            return BadRequest("Такой роли не существует");
        
        var user = new User
        {
            RoleId = role.Id,
            Name = userDto.Name,
            Password = PasswordEncryption.Encrypt(userDto.Password),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(user);

        await context.SaveChangesAsync();

        Directory.CreateDirectory($"D:\\CloudFilesFiles\\{user.Name}_{user.Id}");

        user.Role = role;

        return Ok();
    }
}