// Program.cs
using GameJam.Repositories;
using GameJam.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using SBUGameJam.Data;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Kestrel for Docker and large files
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080); // Listen on port 8080 for Docker
    options.Limits.MaxRequestBodySize = 3L * 1024 * 1024 * 1024; // 3GB
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(30);
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(30);
    options.Limits.MinRequestBodyDataRate = null; // Disable rate limit for large uploads
    options.Limits.MinResponseDataRate = null;
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5, // ✅ افزایش retry برای Docker
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(300); // ✅ 5 دقیقه timeout برای عملیات سنگین
        }));

// Add repositories
builder.Services.AddScoped<ITeamRepository, TeamRepository>();

// Add services
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IArchiveService, ArchiveService>();

// Add controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ✅ Configure FormOptions for large files
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 3L * 1024 * 1024 * 1024; // 3GB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
    options.BufferBodyLengthLimit = 3L * 1024 * 1024 * 1024;
});

var app = builder.Build();

// ✅ Apply migrations with retry logic for Docker
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<ApplicationDbContext>();

    var retryCount = 0;
    var maxRetries = 10;
    var delay = TimeSpan.FromSeconds(5);

    while (retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting to migrate database (attempt {RetryCount}/{MaxRetries})...",
                retryCount + 1, maxRetries);
            db.Database.Migrate();
            logger.LogInformation("Database migration completed successfully.");
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            if (retryCount >= maxRetries)
            {
                logger.LogError(ex, "Failed to migrate database after {MaxRetries} attempts.", maxRetries);
                throw;
            }
            logger.LogWarning(ex, "Database migration failed. Retrying in {Delay} seconds...",
                delay.TotalSeconds);
            Thread.Sleep(delay);
        }
    }
}

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

// ✅ حذف HTTPS redirection برای Docker (Nginx این کار رو انجام میده)
// app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();

// Fallback to index.html for SPA
app.MapFallbackToFile("index.html");

app.Run();