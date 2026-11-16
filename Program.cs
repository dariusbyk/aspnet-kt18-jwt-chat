using System.Security.Cryptography;
using TokenChat.Data;
using TokenChat.Hubs;
using TokenChat.Models;
using TokenChat.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Ключ создаётся один раз и сохраняется вне wwwroot.
string keyPath = Path.Combine(builder.Environment.ContentRootPath, "jwt.key");
if (!File.Exists(keyPath))
{
    File.WriteAllBytes(keyPath, RandomNumberGenerator.GetBytes(32));
}
SymmetricSecurityKey signingKey = new SymmetricSecurityKey(File.ReadAllBytes(keyPath));
builder.Services.AddSingleton(signingKey);
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<PasswordHasher<AppUser>>();
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "TokenChat",
            ValidateAudience = true,
            ValidAudience = "TokenChatClient",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidAlgorithms = new string[] { SecurityAlgorithms.HmacSha256 },
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // Браузерный SignalR передаёт JWT в URL при WebSocket-подключении.
                string token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/chathub"))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await context.Database.EnsureCreatedAsync();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chathub", options =>
{
    options.CloseOnAuthenticationExpiration = true;
}).RequireAuthorization();
app.Run();
