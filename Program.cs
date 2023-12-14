using System.Text;
using CloudFiles;
using CloudFiles.Database;
using CloudFiles.Database.Models;
using CloudFiles.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

Environment.SetEnvironmentVariable("JWT_ISSUER", builder.Configuration["Issuer"]);
Environment.SetEnvironmentVariable("JWT_AUDIENCE", builder.Configuration["Audience"]);
Environment.SetEnvironmentVariable("JWT_KEY", "misha_anime-misha-anime-nikita-banan");

builder.Services.AddCors(c => c.AddPolicy("cors", opt =>
{
    opt.AllowAnyHeader();
    opt.AllowCredentials();
    opt.AllowAnyMethod();
    opt.WithOrigins(builder.Configuration["Audience"]!);
}));

builder.Services.AddScoped(typeof(TokenService));
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.Cookie.Name = "TOKEN";
}).AddJwtBearer(options =>
{
    var key = Encoding.ASCII.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY")!);
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    };
        
    options.Configuration = new OpenIdConnectConfiguration();
    options.Authority = Environment.GetEnvironmentVariable("JWT_ISSUER");
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["TOKEN"];

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy =
        new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .Build();
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.WebHost.UseUrls("http://127.0.0.1:5666");

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("cors");

app.MapControllers();

app.Run();