using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using CloudFiles.Database;
using CloudFiles.Database.Models;
using CloudFiles.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CloudFiles.Controllers;

[Authorize]
[ApiController]
[Route("files")]
public class FilesController : Controller
{
    [HttpPost("add")]
    [RequestSizeLimit(long.MaxValue)]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    public async Task<ActionResult> AddFile(List<IFormFile> files)
    {
        try
        {
            await using var context = new ApplicationContext();

            var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(name))
                return Unauthorized();

            var user = await context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Name == name);
            if (user == null)
                return Unauthorized();
            
            foreach (var formFile in files)
            {
                if (formFile.Length <= 0)
                    continue;

                var guid = Guid.NewGuid();
                var fileInfo = formFile.FileName.Split('.');
                var fileName = ChangeRussianWord(fileInfo[0]);
                var folderName = $"{user.Name}_{user.Id}";
                var filePath = $"D:\\CloudFilesFiles\\{folderName}\\{fileName}.{fileInfo[fileInfo.Length - 1]}";

                await using var stream = System.IO.File.Create(filePath);

                await formFile.CopyToAsync(stream);
                
                var file = new UserFile
                {
                    Name = fileName,
                    Guid = guid,
                    Extenstion = fileInfo[^1],
                    UserId = user.Id,
                    Bytes = formFile.Length
                };

                await context.Files.AddAsync(file);
            }

            await context.SaveChangesAsync();
            
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("getList")]
    public async Task<ActionResult<List<FileDto>>> GetFiles()
    {
        try
        {
            await using var context = new ApplicationContext();

            var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(name))
                return Unauthorized();

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Name == name);
            if (user == null)
                return Unauthorized();
            
            var files = await context.Files
                .AsNoTracking()
                .Where(f => f.UserId == user.Id)
                .Select(f => new FileDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Extension = f.Extenstion,
                    Size = f.Bytes
                })
                .ToListAsync();

            return files;
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("delete/{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            await using var context = new ApplicationContext();

            var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(name))
                return Unauthorized();

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Name == name);
            if (user == null)
                return Unauthorized();
            
            var userFile = await context.Files.FirstOrDefaultAsync(uf => uf.Id == id && uf.UserId == user.Id);
            if (userFile == null)
                return NotFound("Такого файла не существует");

            context.Files.Remove(userFile);

            await context.SaveChangesAsync();

            var folderName = $"{user.Name}_{user.Id}";
            var filePath = $"D:\\CloudFilesFiles\\{folderName}\\{userFile.Name}.{userFile.Extenstion}";
            System.IO.File.Delete(filePath);
            
            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("deleteAll")]
    public async Task<ActionResult> DeleteAll()
    {
        try
        {
            await using var context = new ApplicationContext();

            var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(name))
                return Unauthorized();

            var user = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Name == name);
            if (user == null)
                return Unauthorized();

            var files = context.Files.Where(uf => uf.UserId == user.Id);
            if (!files.Any())
                return Ok();

            var folderName = $"{user.Name}_{user.Id}";
            foreach (var userFile in files)
            {
                var filePath = $"D:\\CloudFilesFiles\\{folderName}\\{userFile.Name}.{userFile.Extenstion}";
                
                System.IO.File.Delete(filePath);
            }
            
            context.RemoveRange(files);

            await context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpGet("get/{id:int}")]
    public async Task<ActionResult> GetFile(int id)
    {
        await using var context = new ApplicationContext();

        var name = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(name))
            return Unauthorized();

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Name == name);
        if (user == null)
            return Unauthorized();

        var file = await context.Files.FirstOrDefaultAsync(uf => uf.Id == id && uf.UserId == user.Id);
        if (file == null)
            return NotFound();

        var folderName = $"{user.Name}_{user.Id}";
        var fileInfo = GetFile($"{file.Name}.{file.Extenstion}", folderName);

        new FileExtensionContentTypeProvider().Mappings.TryGetValue(fileInfo.Extension, out var contentType);

        var cType = contentType ?? "application/octet-stream";

        byte[] fileBytes = System.IO.File.ReadAllBytes($"D:\\CloudFilesFiles\\{folderName}\\{file.Name}.{file.Extenstion}");

        return File(fileBytes, cType, fileInfo.Name, true);
    }

    private FileInfo GetFile(string fileName, string folderName)
    {
        return new FileInfo($"D:\\CloudFilesFiles\\{folderName}\\{fileName}");
    }

    private string ChangeRussianWord(string word)
    {
        var map = new Dictionary<char, string>
        {
            { 'а', "a" },
            { 'б', "b" },
            { 'в', "v" },
            { 'г', "g" },
            { 'д', "d" },
            { 'е', "e" },
            { 'ё', "e" },
            { 'ж', "zh" },
            { 'з', "z" },
            { 'и', "i" },
            { 'й', "ie" },
            { 'к', "k" },
            { 'л', "l" },
            { 'м', "m" },
            { 'н', "n" },
            { 'о', "o" },
            { 'п', "p" },
            { 'р', "r" },
            { 'с', "s" },
            { 'т', "t" },
            { 'у', "y" },
            { 'ф', "f" },
            { 'х', "x" },
            { 'ц', "c" },
            { 'ч', "ch" },
            { 'ш', "sh" },
            { 'щ', "sha" },
            { 'ъ', "" },
            { 'ы', "" },
            { 'ь', "" },
            { 'э', "e" },
            { 'ю', "u" },
            { 'я', "ya" }
        };

        var newWord = word.ToLower();

        var stringBuilder = new StringBuilder();

        foreach (char ch in newWord)
        {
            try
            {
                if (!map.ContainsKey(ch))
                    stringBuilder.Append(ch);
                else
                    stringBuilder.Append(map[ch]);
            } 
            catch
            {
                stringBuilder.Append("");
            }
        }

        return stringBuilder.ToString();
    }
}